using System;
using System.IO;
using System.Net.Sockets;

namespace Lup.Telematics.Extensions {
	public static class StreamExtensions {
		public static void Send(this Socket target, Byte value) {
			target.Send(new Byte[]{value});
		}
		public static void Send(this Socket target, UInt16 value) {
			target.Send(BitConverter.GetBytes(value));
		}
		public static void Send(this Socket target, UInt32 value) {
			target.Send(BitConverter.GetBytes(value));
		}
	}
}