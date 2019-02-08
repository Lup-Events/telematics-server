using System;
using System.Linq;
using Lup.Telematics.Extensions;
using Lup.Telematics.Models;

namespace Lup.Telematics.Parsers {
	public static class DeviceParser {
		public static Device Parse(Byte[] input, ref Int32 position) {
			var output = new Device();
			output.Serial = BitConverter.ToUInt32(input, position );
			output.ModemIMEI = input.Skip(position + 4).Take(16).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
			output.SimSerial = input.Skip(position + 20).Take(21).ToArray().ToHexString(); // TODO: Decode as a huge number instead of hex
			output.ProductId = input[position + 41];
			output.HardwareRevision = input[position + 42];
			output.FirmwareMajor = input[position + 43];
			output.FirmwareMinor = input[position + 44];
			output.IsPinEnabled = (input[position + 45] & 0b00000001) > 0; // TODO: Check this is what "B0" means

			position += 54;
			
			return output;
		}
	}
}