using System;

namespace Lup.Telematics.Models {
	public class ParseResult<T> {
		public T Result { get; set; }
		public Int32 BytesUsed { get; set; }
		
		public ParseResult(T result, Int32 bytesUsed) {
			Result = result;
			BytesUsed = bytesUsed;
		}
	}
}