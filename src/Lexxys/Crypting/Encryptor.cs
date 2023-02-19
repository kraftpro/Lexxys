// Lexxys Infrastructural library.
// file: Encryptor.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Lexxys.Crypting
{

	/// <summary>Wrapper for generic encryption algorithm.</summary>
	public class Encryptor
	{
		private readonly IEncryptorAlgorythm _algorithm;

		public Encryptor(IEncryptorAlgorythm algorithm)
		{
			if (algorithm == null)
				throw new ArgumentNullException(nameof(algorithm));
			if (!algorithm.SupportsStream && !algorithm.SupportsBlock)
				throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
			_algorithm = algorithm;
		}

		public void Encrypt(Stream bits, Stream text)
		{
			if (bits == null)
				throw new ArgumentNullException(nameof(bits));
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (_algorithm.SupportsStream)
			{
				_algorithm.EncryptStream(bits, text);
				return;
			}
			int bsize = _algorithm.BlockSize == 0 ? 4 * 1024 : _algorithm.BlockSize;
			byte[] buffer = new byte[bsize];
			byte[] ciph = new byte[bsize];	// Filled by zero by default
			int n;

			while ((n = text.Read(buffer, 0, bsize)) == bsize)
			{
				System.Diagnostics.Debug.Assert(ciph.Length >= bsize, "ciph.Length >= bsize");
				for (int i = 0; i < bsize; ++i)
					buffer[i] ^= ciph[i];
				ciph = _algorithm.Encrypt(buffer, 0, n);
				bits.Write(ciph, 0, ciph.Length);
			}

			if (n == 0)
				return;

			System.Diagnostics.Debug.Assert(ciph.Length >= n, "ciph.Length >= n");
			for (int i = 0; i < n; ++i)
				buffer[i] ^= ciph[i];
			ciph = _algorithm.Encrypt(buffer, 0, n);
			bits.Write(ciph, 0, ciph.Length);
			System.Diagnostics.Debug.Assert(0 == text.Read(buffer, 0, bsize), "0 == text.Read(buffer, 0, bsize)");
		}

		public byte[] Encrypt(Stream text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			
			using var bits = new MemoryStream();
			_algorithm.EncryptStream(bits, text);
			return bits.GetBuffer();
		}
		
		public byte[] Encrypt(byte[] text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			return Encrypt(text, 0, text.Length);
		}

		public byte[] EncryptString(string text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			return Encrypt(Encoding.Unicode.GetBytes(text));
		}

		public byte[] EncryptString(string text, Encoding encoding)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (encoding is null)
				throw new ArgumentNullException(nameof(encoding));

			return Encrypt(encoding.GetBytes(text));
		}

		public byte[] Encrypt(byte[] text, int offset, int length)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (offset < 0 || offset >= text.Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
			if (length > text.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(length), length, null);
			if (_algorithm.SupportsBlock && (_algorithm.BlockSize >= length || _algorithm.BlockSize == 0))
				return _algorithm.Encrypt(text, offset, length);

			using var bits = new MemoryStream();
			using var stext = new MemoryStream(text, offset, length, false);
			Encrypt(bits, stext);
			return bits.ToArray();
		}
	}
}
