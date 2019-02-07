using System;
using System.Net;
using System.Net.Sockets;
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
		/// Raised when an error occurs when communicating with a GPS. This generally not fatal as the GPS should try again shortly.
		/// </summary>
		public event OnErrorEventHandler OnError;


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
				// TODO: buffer
				Connection = connection,
				RemoteAddressString = ((IPEndPoint) connection.RemoteEndPoint).Address.ToString() // This is not available on the socket once it's in a erred state, so capture it now
			};

			// Start read cycle
			ReceiveStart(state);

			// Start next accept cycle
			AcceptStart();
		}

		private void ReceiveStart(ConnectionState state) {
			// Begin receive
			try {
				state.Connection.BeginReceive(state.BinaryBuffer, 0, state.BinaryBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveEnd), state);
			} catch (ObjectDisposedException) // Occurs during in-flight disposal - we need to catch it for a graceful shutdown
			{
				return; // Abort receive - not needed since there's nothing after this code block - but there might be in a future refactor!
			} catch (SocketException ex) // Occurs when there is a connection error
			{
				// Report error
				OnError?.Invoke(this, new OnErrorEventArgs() {
					Message = ex.Message,
					RemoteAddressString = state.RemoteAddressString
				});
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
				OnError?.Invoke(this, new OnErrorEventArgs() {
					Message = ex.Message,
					RemoteAddressString = state.RemoteAddressString
				});
				return; // Abort receive
			}

			// TODO: Handle received payload //////////

			// Receive more
			ReceiveStart(state);
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