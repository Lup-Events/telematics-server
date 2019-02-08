using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lup.Telematics.Extensions;
using Lup.Telematics.Models;
using Lup.Telematics.Parsers;

namespace Lup.Telematics {
	/// <summary>
	/// Server for receiving data from Telematics trackers.
	/// </summary>
	/// <remarks>
	/// The following is not supported at this time:
	///  - Async messaging
	///  - I/O
	/// </remarks>
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

		/// <summary>
		/// Raised when a device connects.
		/// </summary>
		public event OnDeviceConnectedHandler OnDeviceConnected;

		/// <summary>
		/// Raised when a device sends tracking records.
		/// </summary>
		public event OnRecordsReceivedHandler OnRecordsReceived;


		public delegate void OnDeviceConnectedHandler(Object sender, OnDeviceConnectedArgs e);

		public delegate void OnRecordsReceivedHandler(Object sender, OnRecordsReceivedArgs e);

		public delegate void OnErrorEventHandler(Object sender, OnErrorEventArgs e);


		/// <summary>
		/// The largest possible message size, and thus the size of the buffer for each connection.
		/// </summary>
		private const Int32 BufferSize = 10 + UInt16.MaxValue; // Largest possible message appears to be 10b header + 65535b payload

		/// <summary>
		/// The size of the communications header which is received before any payload
		/// </summary>
		private const Int32 HeaderSize = 5;

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

		/* For future Async implementation
		/// <summary>
		/// Reset the device when it next checks in.
		/// </summary>
		public void DeviceReset(UInt32 serial) {
			throw new NotImplementedException(); // TODO
		}
		
		/// <summary>
		/// Set the device operation mode when it next checks in.
		/// </summary>
		public void DeviceOperationMode(UInt32 serial) {
			throw new NotImplementedException(); // TODO
		}
		
		/// <summary>
		/// Instruct the device to connect to the OEM server for further instructions when it next checks in.
		/// </summary>
		public void DeviceConnectToOEM(UInt32 serial) {
			throw new NotImplementedException(); // TODO
		}
		*/

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
				RemoteAddressString = ((IPEndPoint) connection.RemoteEndPoint).Address.ToString() // This is not available on the socket once it's in a erred state, so capture it now
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

			// If the expected number of bytes hasn't arrived yet, wait for more bytes
			if (state.ReceiveBufferExpected != state.ReceiveBufferUsed) {
				// Receive more bytes
				ReceiveStart(state);
				return;
			}

			// If header hasn't been processed yet, process it
			if (null == state.ReceiveMessageType) {
				// Yes, this could have been in the switch below, but it made for some funky reading
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

				// Receive more bytes
				ReceiveStart(state);
				return;
			}

			switch (state.ReceiveMessageType.Value) {
				case MessageTypesRx.Hello: // Device is providing basic information about it
					var p = 5;
					state.Device = DeviceParser.Parse(state.ReceiveBuffer, ref p);

					state.Connection.Send(new Byte[] {0x02, 0x55}); // Sync bytes
					state.Connection.Send((Byte) MessageTypesTx.HelloResponse); // Message type
					state.Connection.Send((UInt16) 8); // Message length
					state.Connection.Send(TelematicsTime.Encode(DateTime.UtcNow)); // Time
					state.Connection.Send((UInt32) 0); // Flags TODO: how to tell device to redirect to OEM?

					OnDeviceConnected?.Invoke(this, new OnDeviceConnectedArgs() {
						RemoteAddressString = state.RemoteAddressString,
						Device = state.Device
					});

					break;
				
				case MessageTypesRx.Records: // Device is providing records
					// It seems that MESSAGE contains multiple RECORDS
					//   which contain multiple FIELDS
					//     which contains multiple things which I'm going to call ATTRIBUTES
					// Attributes are the actual data.
					// 
					// In summary: MESSAGE => RECORDS => FIELDS => ATTRIBUTES

					var position = 5;
					var records = new List<Record>();

					while (position < state.ReceiveBufferExpected) {
						records.Add(RecordParser.Parse(state.ReceiveBuffer, ref position));
					}

					OnRecordsReceived?.Invoke(this, new OnRecordsReceivedArgs() {
						RemoteAddressString = state.RemoteAddressString,
						Device = state.Device,
						Records = records
					});
					break;
				
				case MessageTypesRx.CommitRequest: // Device asking us to confirm that we've successfully received and stored records
					// Send confirmation // TODO: When would we not confirm a commit?
					state.Connection.Send(new Byte[] {0x02, 0x55}); // Sync bytes
					state.Connection.Send((Byte)MessageTypesTx.CommitResponse); // Message type
					state.Connection.Send((UInt16)1); // Message length
					state.Connection.Send((Byte) 1); // Success
					break;
				
				case MessageTypesRx.VersionData: // Device providing version information (no idea what this is for!!)
					RaiseDeviceError(state, $"Version information received and discarded.", false); // TODO: do something with the information
					break;
				
				case MessageTypesRx.TimeRequest: // Device asking for the current time
					state.Connection.Send(new Byte[] {0x02, 0x55}); // Sync bytes
					state.Connection.Send((Byte)MessageTypesTx.TimeResponse); // Message type
					state.Connection.Send((UInt16)4); // Message length
					state.Connection.Send(TelematicsTime.Encode(DateTime.UtcNow)); // Current time
					break;
				
				case MessageTypesRx.AsyncMessageResponse: // Device is giving responses to async messages, which is not currently implemented, so should not be recieved
					RaiseDeviceError(state, $"Message async responses received and discarded.", false); // TODO: do something with the information
					break;
				
				default:
					RaiseDeviceError(state, $"Unsupported message type received ({state.ReceiveMessageType.Value}). Message discarded.", false);
					break;
			}

			// Receive more
			ResetState(state);
			ReceiveStart(state);
		}

		/// <summary>
		/// Prepare for the next message.
		/// </summary>
		private void ResetState(ConnectionState state) {
			state.ReceiveBufferExpected = HeaderSize;
			state.ReceiveBufferUsed = 0;
			state.ReceiveMessageType = null;
		}

		private void RaiseDeviceError(ConnectionState state, String message, Boolean disconnect) {
			// Report error
			OnError?.Invoke(this, new OnErrorEventArgs() {
				Message = message,
				RemoteAddressString = state.RemoteAddressString,
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