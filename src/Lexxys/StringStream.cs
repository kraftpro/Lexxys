// Lexxys Infrastructural library.
// file: StringStream.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public class StringStream: Stream
	{
		private readonly string _value;
		private readonly Encoding _encoding;
		private readonly int _charBufferSize;
		private readonly int _byteBufferSize;
		private readonly byte[] _byteBuffer;
		private int _index;
		private int _byteIndex;
		private int _byteCount;

		public StringStream(string value, Encoding encoding = default, int bufferSize = default)
		{
			_encoding = encoding ?? Encoding.UTF8;
			_value = value ?? "";
			_charBufferSize = bufferSize > 128 ? bufferSize : bufferSize == default ? 4096 : 128;
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


