using System;
using System.Transactions;

namespace Lup.Telematics.Models {
	public class Tracking {
		public DateTime RTCTime { get; set; }
		
		public DateTime GpsTime { get; set; }
		
		/// <summary>
		/// The devices' latitudinal position, in degrees.
		/// </summary>
		public Double PositionLatitude { get; set; }
		
		/// <summary>
		/// The devices' longitudinal position, in degrees.
		/// </summary>
		public Double PositionLongitude { get; set; }
		
		/// <summary>
		/// The devices elevation, in meters.
		/// </summary>
		public Double PositionAltitude { get; set; }
		
		/// <summary>
		/// 2D ground speed, in m/s
		/// </summary>
		public Double Speed { get; set; }
		
		/// <summary>
		/// The estimated accuracy of GroundSpeed, in m/s.
		/// </summary>
		/// <remarks>
		/// This is an estimate only.
		/// </remarks>
		public Double SpeedAccuracy { get; set; }
		
		/// <summary>
		/// 2D heading, in degrees.
		/// </summary>
		/// <remarks>
		/// Accuracy is to 2 degrees.
		/// </remarks>
		public Double Heading { get; set; }
		
		/// <summary>
		/// Position Dilution Of Precision describes error caused by the relative position of the GPS satellites.
		/// </summary>
		/// <remarks>
		/// Basically, the more signals a GPS receiver can “see” (spread apart versus close together), the more precise it can be.
		/// </remarks>
		public Double PDOP { get; set; }
		
		/// <summary>
		/// The estimated accuracy of the position, in meters.
		/// </summary>
		/// <remarks>
		/// This is an estimate only.
		/// </remarks>
		public Double  PositionAccuracy { get; set; }
		
		/// <summary>
		/// Is the GPS fix valid? Data all other data is largely meaningless if not.
		/// </summary>
		public Boolean IsValidFix { get; set; }
		
		/// <summary>
		/// Is the GPS fix valid in 3D? The altitude is largely meaningless if not.
		/// </summary>
		public Boolean Is3DFix { get; set; }
		
		public static Tracking Parse(Byte[] input, Int32 offset) {
			var output = new Tracking();
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