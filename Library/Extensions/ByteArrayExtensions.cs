using System;

namespace Lup.Telematics.Extensions {
	public static class ByteArrayExtensions {
		public static String ToHexString(this Byte[] target) {
			return BitConverter.ToString(target).Replace("-", "");
		}
	}
}