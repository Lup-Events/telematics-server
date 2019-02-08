using System;
using System.Net.Sockets;

namespace Lup.Telematics.Models {
	public class ConnectionState {
		/// <summary>
		/// The remote address (IP and port).
		/// </summary>
		public String RemoteAddressString { get; set; }
		
		/// <summary>
		/// The underlying socket connection.
		/// </summary>
		public Socket Connection { get; set; }

		/// <summary>
		/// Information we know about the remote device.
		/// </summary>
		public Device Device { get; set; }
		
		/// <summary>
		/// Buffer used to receive messages from the device.
		/// </summary>
		public Byte[] ReceiveBuffer { get; set; }
		
		/// <summary>
		/// The number of bytes received in the header.
		/// </summary>
		public Int32 ReceiveBufferUsed { get; set; }
		
		/// <summary>
		/// The number of bytes expected before the next processing run on the buffer.
		/// </summary>
		public Int32 ReceiveBufferExpected { get; set; }
		
		public MessageTypesRx? ReceiveMessageType { get; set; }
		
	}
}