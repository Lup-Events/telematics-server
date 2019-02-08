using System;
using System.Collections.Generic;

namespace Lup.Telematics.Models {
	public class Record {
		public UInt32 Sequence { get; set; }
		public DateTime RTCTime { get; set; }
		public Reasons Reason { get; set; }
		
		public List<Tracking> TrackingFields { get; set; }
		
		public static Record Parse(Byte[] input, ref Int32 position) {
			var output = new Record();

			var recordLength = BitConverter.ToUInt16(input, position);
			output.Sequence = BitConverter.ToUInt32(input, position + 2);
			output.RTCTime = TelematicsTime.Decode(BitConverter.ToUInt32(input, position + 6));
			output.Reason = (Reasons) input[position + 10];
			position += 11;

			var recordUsed = 0;
			while (recordUsed < recordLength) {
				var fieldId = (Fields) input[position];
				var fieldLength = (UInt16) input[position + 1];
				position += 2;
				if (fieldLength == Byte.MaxValue) {
					fieldLength = BitConverter.ToUInt16(input,position);
					position += 2;
				}

				var position2 = position;
				position += fieldLength;
				
				switch (fieldId) {
					case Fields.Tracking:
						output.TrackingFields.Add(Tracking.Parse(input, position2));
						break;
					default:
						// Unknown message ignored
						break;
				}
			}

			return output;
		}
		
		public enum Reasons : Byte {
			Reserved = 0,
			StartOfTrip = 1,
			EndOfTrip = 2,
			ElapsedTime = 3,
			SpeedChange = 4,
			HeadingChange = 5,
			DistanceTravelled = 6,
			MaximumSpeed = 7,
			Stationary = 8,
			DigitalInputChanged = 9,
			DigitalOutputChanged = 10,
			Heartbeat = 11,
			HarshBrake = 12,
			HarshAcceleration = 13,
			HarshCornering = 14,
			ExternalPowerChange = 15,
			SystemPowerMonitoring = 16,
			DriverIdTagRead = 17,
			OverSpeed = 18,
			FuelSensorRecord = 19,
			TowingAlert = 20,
			Debug = 21,
			Sdi12SensorData = 22,
			Accident = 23,
			AccidentData = 24,
			SensorValueElapsedTime = 25,
			SensorValueChange = 26,
			SensorAlarm = 27,
			RainGaugeTipped = 28,
			TamperAlert = 29,
			BlobNotification = 30,
			TimeAndAttendance = 31,
			TripRestart = 32,
			TagGained = 33,
			TagUpdate = 34,
			TagLost = 35,
			RecoveryModeOn = 36,
			RecoveryModeOff = 37,
			ImmobiliserOn = 38,
			ImmobiliserOff = 39,
			GarminFmiStopResponse = 40,
			LoneWorkerAlarm = 41,
			DeviceCounters = 42,
			ConnectedDeviceData = 43,
			EnteredGeoFence = 44,
			ExitedGeoFence = 45,
			HighGEvent = 46,
		}
	}
}