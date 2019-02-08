using System;

namespace Lup.Telematics {
	public static class FletcherChecksum {
		public static Byte[] Compute(Byte[] value) {
			if (null == value) {
				throw new ArgumentNullException(nameof(value));
			}

			var sum1 = 0;
			var sum2 = 0;

			foreach (var b in value) {
				sum1 = (sum1 + b) % 256;
				sum2 = (sum2 + sum1) % 256;
			}


			var check1 = (Byte) (256 - (sum1 + sum2) % 256);
			var check2 = (Byte) (256 - (sum1 + check1) % 256);

			return new Byte[] {check1, check2};
		}
	}
}