using System;
using System.Collections.Generic;
using Lup.Telematics.Models;

namespace Lup.Telematics.Parsers {
	public static class RecordParser {
		public static Record Parse(Byte[] input, ref Int32 position) {
			var records = new Record();

			var recordLength = BitConverter.ToUInt16(input, position);
			records.Sequence = BitConverter.ToUInt32(input, position + 2);
			records.RTCTime = TelematicsTime.Decode(BitConverter.ToUInt32(input, position + 6));
			records.Reason = (Reasons) input[position + 10];

			var recordUsed = 0;
			while (recordUsed < recordLength) {
				var fieldId = (FieldIds) stream.ReadUInt8();
				var fieldLength = (UInt16) stream.ReadUInt8();
				if (fieldLength == Byte.MaxValue) {
					fieldLength = stream.ReadUInt16();
				}

				switch (fieldId) {
					case FieldIds.GpsData:
						ReadingParser.Parse(state.ReceiveBuffer)
						break;
					default:
						// Unknown message ignored
						RaiseDeviceError(state, $"Unknown field {fieldId} received. Field ignored", false);
				}
			}

			return new ParseResult<Record>(records);
		}
	}
}