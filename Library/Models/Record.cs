using System;
using System.Collections.Generic;

namespace Lup.Telematics.Models {
	public class Record {
		public UInt32 Sequence { get; set; }
		public DateTime RTCTime { get; set; }
		public Reasons Reason { get; set; }
		
		public List<Tracking> Tracking { get; set; }
	}
}