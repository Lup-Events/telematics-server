using System;

namespace Lup.Telematics.Models {
	public class OnErrorEventArgs {
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