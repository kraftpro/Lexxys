using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	public static class Strings2
	{
		public static void Go()
		{
			string a = "Starting\r\nПривет Медвед!\r\nPrivet Medved!\r\nEnding\r\n\r\n";
			var aa = new StringBuilder();
			for (int i = 0; i < 4096; i++)
			{
				aa.Append(a);
			}
			string a2 = aa.ToString();

			using (var s = new StringStream(a, Encoding.UTF8))
			using (var f = File.OpenWrite(@"c:\tmp\a18.txt"))
			{
				s.CopyTo(f);
			}
			using (var s = new StringStream(a, Encoding.ASCII))
			using (var f = File.OpenWrite(@"c:\tmp\a1a.txt"))
			{
				s.CopyTo(f);
			}

			using (var s = new StringStream(a2, Encoding.UTF8))
			using (var f = File.OpenWrite(@"c:\tmp\a28.txt"))
			{
				s.CopyTo(f);
			}
			using (var s = new StringStream(a2, Encoding.ASCII))
			using (var f = File.OpenWrite(@"c:\tmp\a2a.txt"))
			{
				s.CopyTo(f);
			}
		}
	}

	class StringStream: Stream
	{
		private Encoding _encoding;
		private string _value;
		private int _index;
		private int _charBufferSize;
		private byte[] _byteBuffer;
		private int _byteBufferSize;
		private int _byteIndex;
		private int _byteCount;

		public StringStream(string value, Encoding encoding = default, int bufferSize = default)
		{
			_encoding = encoding ?? Encoding.UTF8;
			_value = value ?? "";
			_charBufferSize = bufferSize > 128 ? bufferSize : bufferSize == default ? 1024 : 128;
			_byteBufferSize = _encoding.GetMaxByteCount(_charBufferSize);
			_byteBuffer = new byte[_byteBufferSize];
		}

		public override unsafe int Read(byte[] buffer, int offset, int count)
		{
			int left = count;
			if (_byteCount > 0)
			{
				Array.Copy(_byteBuffer, _byteIndex, buffer, offset, Math.Min(count, _byteCount));
				if (_byteCount >= count)
				{
					_byteIndex += count;
					_byteCount -= count;
					return count;
				}
				_byteIndex = 0;
				offset += _byteCount;
				left -= _byteCount;
			}

			int charLeft = _value.Length - _index;
			if (charLeft == 0)
				return 0;

			fixed (byte* buf = _byteBuffer)
			fixed (char* str = _value)
			{
				char* s = str + _index;
				for (; ; )
				{
					int charCount = Math.Min(charLeft, _charBufferSize);
					_byteCount = _encoding.GetBytes(s, charCount, buf, _byteBufferSize);
					Debug.Assert(_byteCount > 0);
					s += charCount;
					_index += charCount;
					charLeft -= charCount;
					int n = Math.Min(left, _byteCount);
					Array.Copy(_byteBuffer, 0, buffer, offset, n);
					offset += n;
					_byteIndex = n;
					_byteCount -= n;
					left -= n;
					if (left == 0)
						return count;
					if (charLeft == 0)
						return count - left;
				}
			}
		}

		//public override unsafe int Read(byte[] buffer, int offset, int count)
		//{
		//	int left = count;
		//	if (_byteCount > 0)
		//	{
		//		Array.Copy(_byteBuffer, _byteIndex, buffer, offset, Math.Min(count, _byteCount));
		//		if (_byteCount >= count)
		//		{
		//			_byteIndex += count;
		//			_byteCount -= count;
		//			return count;
		//		}
		//		_byteIndex = 0;
		//		offset += _byteCount;
		//		left -= _byteCount;
		//	}

		//	fixed (byte* buf = _byteBuffer)
		//	fixed (char* str = _value)
		//	{
		//		for (; ; )
		//		{
		//			if (_value.Length == _index)
		//				return count - left;

		//			byte* b = buf;
		//			char* s = str + _index;
		//			int space = _byteBufferSize;
		//			int chars = _value.Length - _index;
		//			int charCount = 0;
		//			do
		//			{
		//				var x = CopyBytes(s, chars, b, space);
		//				b += x.ByteCount;
		//				space -= x.ByteCount;
		//				s += x.CharCount;
		//				chars -= x.CharCount;
		//				charCount += x.CharCount;
		//			} while (chars > 0 && space > 1024);

		//			_index += charCount;
		//			_byteCount = _byteBufferSize - space;
		//			Debug.Assert(_byteCount > 0);
		//			int n = Math.Min(left, _byteCount);
		//			Array.Copy(_byteBuffer, 0, buffer, offset, n);
		//			offset += n;
		//			_byteIndex = n;
		//			_byteCount -= n;
		//			left -= n;
		//			if (left == 0 || chars == 0)
		//				return offset;
		//		}
		//	}
		//}

		//private unsafe (int CharCount, int ByteCount) CopyBytes(char* s, int count, byte* b, int size)
		//{
		//	int xx = size * _charBufferSize / _byteBufferSize;
		//	if (xx < count)
		//		count = xx;
		//	int n = _encoding.GetBytes(s, count, b, size);
		//	return (count, n);
		//}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => _value.Length;

		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

		public override void SetLength(long value) => throw new NotImplementedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

		public override void Flush()
		{
		}
	}
}
