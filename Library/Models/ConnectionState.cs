using System;
using System.Net.Sockets;

namespace Lup.Telematics.Models {
	public class ConnectionState {
		public Socket Connection { get; set; }

		public Byte[] BinaryBuffer { get; set; }

		public String RemoteAddressString { get; set; }
	}
}