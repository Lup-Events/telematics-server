using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lup.Telematics.Extensions;
using Lup.Telematics.Models;

namespace Lup.Telematics {
	public class TelematicsServer {
		/// <summary>
		/// If the receiver has been disposed and needs to be re-instantiated before use.
		/// </summary>
		public Boolean IsDisposed { get; private set; }

		/// <summary>
		/// If the receiver is currently running and available to receive beacons.
		/// </summary>
		public Boolean IsRunning { get; private set; }

		/// <summary>
		/// The local endpoint used for listening. This is where you can set the listening port and/or IP address. By default this is all IPs on port 8965 (IPv6 and IPv6).
		/// </summary>
		public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 8965);

		/// <summary>
		/// The number of pending connections that will be queued waiting to be processed.
		/// <summary>
		public Int32 ConnectionBacklogLimit { get; set; } = 10;

		/// <summary>
		/// Raised when an error occurs when communicating with a tracker. This generally not fatal as the tracker should try again shortly.
		/// </summary>
		public event OnErrorEventHandler OnError;

		public event OnDeviceConnectedHandler OnDeviceConnected;

		public event OnErrorEventHandler OnTrackingReceived;


		private const Int32 BufferSize = 10 + UInt16.MaxValue; // Largest possible message appears to be 10b header + 65535b payload

		/// <summary>
		/// The size of the communications header which is received before any payload
		/// </summary>
		private const Int32 HeaderSize = 5;

		public delegate void OnDeviceConnectedHandler(Object sender, OnDeviceConnectedArgs e);

		public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);

		private readonly Socket Listener;

		private readonly Object Sync = new Object();

		public TelematicsServer() {
			// Create listener socket
			Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		/// <summary>
		/// Start listening for beacons.
		/// </summary>
		public void Start() {
			// Check that we are NOT already running in a thread-safe manner
			lock (Sync) {
				if (IsDisposed) {
					throw new ObjectDisposedException("Receiver has been disposed.");
				}

				if (IsRunning) {
					throw new InvalidOperationException("Receiver already running.");
				}

				IsRunning = true;
			}

			// Bind socket and listen for connections
			Listener.Bind(LocalEndPoint);
			Listener.Listen(ConnectionBacklogLimit);

			// Seed accepting
			AcceptStart();
		}

		private void AcceptStart() {
			try {
				Listener.BeginAccept(new AsyncCallback(AcceptEnd), null);
			} catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
			{
				return; // Abort accept - not needed since there's nothing after this code block - but there might be in a future refactor and it's consist with other areas!
			}
		}

		private void AcceptEnd(IAsyncResult result) {
			Socket connection;
			try {
				// Complete accept
				connection = Listener.EndAccept(result);
			} catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
			{
				return; // Abort accept
			}

			// Create state
			var state = new ConnectionState() {
				ReceiveBuffer = new Byte[BufferSize],
				Connection = connection,
				Device = new Device() {
					RemoteAddressString = ((IPEndPoint) connection.RemoteEndPoint).Address.ToString() // This is not available on the socket once it's in a erred state, so capture it now
				}
			};

			// Prepare for a new message
			ResetState(state);
			
			// Start read cycle
			ReceiveStart(state);

			// Start next accept cycle
			AcceptStart();
		}

		private void ReceiveStart(ConnectionState state) {
			// Begin receive
			try {
				state.Connection.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBuffer.Length, SocketFlags.None, ReceiveEnd, state);
			} catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
			{
				return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
			} catch (SocketException ex) // Occurs when there is a connection error
			{
				// Report error
				RaiseDeviceError(state, ex.Message, false);
				return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
			}
		}

		private void ReceiveEnd(IAsyncResult ar) {
			// Retrieve state
			var state = (ConnectionState) ar.AsyncState;

			// Complete read
			Int32 chunkLength;
			try {
				chunkLength = state.Connection.EndReceive(ar);
			} catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
			{
				return; // Abort read
			} catch (SocketException ex) // Occurs when there is a connection error
			{
				// Report error
				RaiseDeviceError(state, ex.Message, false);
				return; // Abort receive
			}

			if (state.ReceiveBufferExpected == state.ReceiveBufferUsed) {
				switch (state.ReceiveMessageType) {
					case null:
						// Check sync bytes
						if (state.ReceiveBuffer[0] != 0x02 || state.ReceiveBuffer[1] != 0x55) {
							RaiseDeviceError(state, $"Incorrect sync bytes encountered ({state.ReceiveBuffer[0]},{state.ReceiveBuffer[1]}).", true);
							return;
						}

						// Decode message type
						state.ReceiveMessageType = (MessageTypesRx) state.ReceiveBuffer[2];

						// Decode message length and increase the number of bytes expected
						var messageLength = BitConverter.ToUInt16(state.ReceiveBuffer, 3);
						state.ReceiveBufferExpected += messageLength;

						break;
					case MessageTypesRx.Hello:
						state.Device.Serial = BitConverter.ToUInt32(state.ReceiveBuffer, 5);
						state.Device.ModemIMEI = state.ReceiveBuffer.Skip(9).Take(16).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
						state.Device.SimSerial = state.ReceiveBuffer.Skip(25).Take(21).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
						state.Device.ProductId = state.ReceiveBuffer[46];
						state.Device.HardwareRevision = state.ReceiveBuffer[47];
						state.Device.FirmwareMajor = state.ReceiveBuffer[48];
						state.Device.FirmwareMinor = state.ReceiveBuffer[49];
						state.Device.IsPinEnabled = (state.ReceiveBuffer[50] & 0b00000001) > 0; // TODO: Check this is what "B0" means
						state.Device.IsHelloReceived = true;

						state.Connection.Send(new Byte[] {0x02, 0x55}); // Sync bytes
						state.Connection.Send((Byte) MessageTypesTx.HelloResponse); // Message type
						state.Connection.Send((UInt16) 8); // Message length
						state.Connection.Send(TelematicsTime.Encode(DateTime.UtcNow)); // Time
						state.Connection.Send((UInt32) 0); // Flags TODO: how to tell device to redirect to OEM?

						ResetState(state);
						break;
					default:
						RaiseDeviceError(state, $"Unsupported message type received ({state.ReceiveBuffer[0]},{state.ReceiveBuffer[1]}). Message discarded.", false);
						return;
				}
			}
			// TODO: Handle received payload //////////

			// Receive more
			ReceiveStart(state);
		}

		/// <summary>
		/// Prepare for the next message
		/// </summary>
		/// <param name="state"></param>
		private void ResetState(ConnectionState state) {
			state.ReceiveBufferExpected = HeaderSize;
			state.ReceiveBufferUsed = 0;
		}
		
		private void RaiseDeviceError(ConnectionState state, String message, Boolean disconnect) {
			// Report error
			OnError?.Invoke(this, new OnErrorEventArgs() {
				Message = message,
				Device = state.Device
			});

			// Close connection
			if (disconnect) {
				state.Connection.Close();
			}
		}

		protected virtual void Dispose(Boolean disposing) {
			lock (Sync) {
				if (IsDisposed) {
					return;
				}

				IsDisposed = true;
			}

			if (disposing) {
				// Dispose managed state (managed objects)
				Listener?.Dispose();
				IsRunning = false;
			}
		}

		/// <summary>
		/// Stop listening for beacons if started, and then destroy all managed resources
		/// </summary>
		public void Dispose() {
			Dispose(true);
		}
	}
}