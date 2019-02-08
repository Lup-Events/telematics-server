using System;

namespace Lup.Telematics.Models {
	public class OnErrorEventArgs {
		/// <summary>
		/// The remote address (IP and port).
		/// </summary>
		public String RemoteAddressString { get; set; }
		
		/// <summary>
		/// Information we know about the remote device.
		/// </summary>
		public Device Device { get; set; }
		
		/// <summary>
		/// The error message.
		/// </summary>
		public String Message { get; set; }
	}
}