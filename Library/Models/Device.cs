using System;
using System.Linq;
using Lup.Telematics.Extensions;

namespace Lup.Telematics.Models {
	public class Device {

		public UInt32 Serial { get; set; }
		public String ModemIMEI { get; set; } // 16 bytes
		public String SimSerial { get; set; } // 21 bytes
		public Byte ProductId { get; set; }
		public Byte HardwareRevision { get; set; }
		public Byte FirmwareMajor { get; set; }
		public Byte FirmwareMinor { get; set; }
		
		public Boolean IsPinEnabled { get; set; }
		
		[Flags]
		public enum Flag:UInt32 {
			PinEnabled = 0b00000000_00000000_00000000_00000001 // TODO: confirm this is what is meant by "B0"
		}
		
		public static Device Parse(Byte[] input, Int32 offset) {
			var output = new Device();
			output.Serial = BitConverter.ToUInt32(input, offset );
			output.ModemIMEI = input.Skip(offset + 4).Take(16).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
			output.SimSerial = input.Skip(offset + 20).Take(21).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
			output.ProductId = input[offset + 41];
			output.HardwareRevision = input[offset + 42];
			output.FirmwareMajor = input[offset + 43];
			output.FirmwareMinor = input[offset + 44];
			output.IsPinEnabled = (input[offset + 45] & 0b00000001) > 0; // TODO: Check this is what "B0" means

			offset += 54;
			
			return output;
		}
	}
}