using System;

namespace Lup.Telematics {
	public static class TelematicsTime {
		public static readonly DateTime Epoch = new DateTime(2013, 01, 01, 0,0,0,DateTimeKind.Utc);
		
		public static UInt32 Encode(DateTime value) {
			// Convert to ticks
			var valueTicks = value.Ticks;
			if (valueTicks < Epoch.Ticks) {
				throw new ArgumentOutOfRangeException( nameof(value), "DateTime must be after 1/1/13 (UTC).");
			}

			return (UInt32)((valueTicks - Epoch.Ticks) / TimeSpan.TicksPerSecond);
		}

		public static DateTime Decode(UInt32 value) {
			// Convert it from seconds to ticks
			var v = value * TimeSpan.TicksPerSecond;

			// Add it to the epoch
			return new DateTime(Epoch.Ticks + v );

		}
	}
}