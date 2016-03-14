using System;
using System.Text;

namespace arookas {
	static class hash {
		public static uint calculate(string data) {
			if (data == null) {
				throw new ArgumentNullException("data");
			}
			return calculate(Encoding.ASCII.GetBytes(data));
		}
		public static uint calculate(byte[] data) {
			if (data == null) {
				throw new ArgumentNullException("data");
			}
			// this code is so shitty
			var hash = 0u;
			for (var i = 0; i < data.Length; ++i) {
				hash <<= 8;
				hash += data[i];
				var r6 = unchecked((uint)((4993ul * hash) >> 32));
				var r0 = unchecked((byte)((((hash - r6) / 2) + r6) >> 24));
				hash -= r0 * 33554393u;
			}
			return hash;
		}
	}
}
