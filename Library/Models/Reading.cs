using System;
using System.Transactions;

namespace Lup.Telematics.Models {
	public class Reading {
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
	}
}