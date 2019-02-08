using System;
using Lup.Telematics.Models;

namespace Lup.Telematics.Parsers {
	public static class ReadingParser {
		public const Int32 MessageLength = 21;
		
		public static Reading Parse(Byte[] input, Int32 offset) {
			var output = new Reading();
			output.GpsTime = TelematicsTime.Decode(BitConverter.ToUInt32(input, offset));
			output.PositionLatitude = BitConverter.ToInt32(input, offset + 4) / 1e7;
			output.PositionLongitude = BitConverter.ToInt32(input, offset + 8) / 1e7;
			output.PositionAltitude = BitConverter.ToInt16(input, offset + 12) ;
			output.Speed = BitConverter.ToUInt16(input, offset + 14) ;
			output.SpeedAccuracy = input[16] * 10 / 100;
			output.Heading = input[17] * 2;
			output.PDOP = input[18] / 10;
			output.PositionAccuracy = input[19];
			output.IsValidFix = (input[20] & 0b00000001) > 0; // TODO: check this is what is meant by "b0"
			output.Is3DFix = (input[20] & 0b00000010) > 0; // TODO: check this is what is meant by "b1"

			return output;
		}
	}
}