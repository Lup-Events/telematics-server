using System;

namespace Lup.Telematics.Models {
	public class Device {
		/// <summary>
		/// The remote address (IP and port).
		/// </summary>
		public String RemoteAddressString { get; set; }

		public UInt32 Serial { get; set; }
		public String ModemIMEI { get; set; } // 16 bytes
		public String SimSerial { get; set; } // 21 bytes
		public Byte ProductId { get; set; }
		public Byte HardwareRevision { get; set; }
		public Byte FirmwareMajor { get; set; }
		public Byte FirmwareMinor { get; set; }
		
		public Boolean IsPinEnabled { get; set; }
		
		public Boolean IsHelloReceived { get; set; }
		
		[Flags]
		public enum Flag:UInt32 {
			PinEnabled = 0b00000000_00000000_00000000_00000001 // TODO: confirm this is what is meant by "B0"
		}
	}
}