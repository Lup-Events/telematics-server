using System;
using System.IO;

namespace Lup.Telematics.Extensions {
	public static class StreamExtensions {
		public static Byte ReadUInt8(this Stream target) {
			var b = target.ReadByte();
			if (b < 0) {
				throw new EndOfStreamException();
			}
			return (Byte)b;
		}
		public static UInt16 ReadUInt16(this Stream target) {
			return BitConverter.ToUInt16(target.Read(2),0);
		}
		
		public static UInt32 ReadUInt32(this Stream target) {
			return BitConverter.ToUInt32(target.Read(4),0);
		}
		
		public static Byte[] Read(this Stream target, Int32 count) {
			var buffer = new Byte[count];
			target.Read(buffer, 0, count);
			return buffer;
		}
	}
}