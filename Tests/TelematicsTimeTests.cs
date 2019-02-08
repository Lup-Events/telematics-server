using System;
using Xunit;

namespace Lup.Telematics {
	public class TelematicsTimeTests {
		[Fact]
		public void EncodeZero() {
			Assert.Equal((UInt32)0, TelematicsTime.Encode(new DateTime(2013, 01, 01, 0, 0, 0, DateTimeKind.Utc)));
		}

		[Fact]
		public void EncodeOne() {
			Assert.Equal((UInt32)1, TelematicsTime.Encode(new DateTime(2013, 01, 01, 0, 0, 1, DateTimeKind.Utc)));
		}


		[Fact]
		public void DecodeZero() {
			Assert.Equal(new DateTime(2013, 01, 01, 0, 0, 0, DateTimeKind.Utc), TelematicsTime.Decode(0));
		}


		[Fact]
		public void DecodeOne() {
			Assert.Equal(new DateTime(2013, 01, 01, 0, 0, 1, DateTimeKind.Utc), TelematicsTime.Decode(1));
		}
	}
}