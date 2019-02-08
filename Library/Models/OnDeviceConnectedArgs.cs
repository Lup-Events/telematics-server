using System;

namespace Lup.Telematics.Models {
	public class OnDeviceConnectedArgs {
		/// <summary>
		/// The remote address (IP and port).
		/// </summary>
		public String RemoteAddressString { get; set; }
		
		/// <summary>
		/// The claimed details of the device providing this data.
		/// </summary>
		public Device Device { get; set; }
	}
}