// Lexxys Infrastructural library.
// file: Decryptor.cs
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

	/// <summary>Wrapper for generic decryption algorithm.</summary>
	public class Decryptor
	{
		private readonly IDecryptorAlgorythm _da;

		public Decryptor(IDecryptorAlgorythm algorithm)
		{
			if (algorithm == null)
				throw EX.ArgumentNull("algorithm");
			if (!algorithm.SupportsStream && !algorithm.SupportsBlock)
				throw EX.ArgumentOutOfRange("algorithm", algorithm);
			_da = algorithm;
		}

		public int BlockSize
		{
			get { return _da.BlockSize; }
		}

		public void Decrypt(Stream text, Stream bits)
		{
			if (text == null)
				throw EX.ArgumentNull("text");
			if (bits == null)
				throw EX.ArgumentNull("bits");
			if (_da.SupportsStream)
			{
				_da.DecryptStream(text, bits);
				return;
			}
			int bsize = _da.BlockSize == 0 ? 8 * 1024 : _da.BlockSize;
			byte[] buffer = new byte[bsize];
			byte[] ciph = new byte[bsize];	// Filled by zero by default
			byte[] txt;
			int n;
			while ((n = bits.Read(buffer, 0, bsize)) == bsize)
			{
				txt = _da.Decrypt(buffer, 0, bsize);
				n = txt.Length;
#if DEBUG
				System.Diagnostics.Debug.Assert(n <= bsize, "txt.Length <= bsize");
#endif
				for (int i = 0; i < n; ++i)
					txt[i] ^= ciph[i];
				text.Write(txt, 0, n);
				Array.Copy(buffer, 0, ciph, 0, bsize);
			}

			if (n > 0)
			{
				txt = _da.Decrypt(buffer, 0, n);
				n = txt.Length;
#if DEBUG
				System.Diagnostics.Debug.Assert(n <= bsize, "txt.Length <= bsize");
#endif
				for (int i = 0; i < n; ++i)
					txt[i] ^= ciph[i];
				text.Write(txt, 0, n);
#if DEBUG
				n = bits.Read(buffer, 0, bsize);
				System.Diagnostics.Debug.Assert(n == 0, "n == 0");
#endif
			}
		}
		public byte[] Decrypt(Stream bits)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			using var text = new MemoryStream();
			_da.DecryptStream(text, bits);
			return text.GetBuffer();
		}
		public byte[] Decrypt(byte[] bits)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			return Decrypt(bits, 0, bits.Length);
		}
		public string DecryptString(byte[] bits)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			return Encoding.Unicode.GetString(Decrypt(bits));
		}
		public string DecryptString(byte[] bits, Encoding encoding)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			return encoding.GetString(Decrypt(bits));
		}
		public byte[] Decrypt(byte[] bits, int offset, int length)
		{
			if (bits == null)
				throw EX.ArgumentNull("bits");
			if (offset < 0 || offset >= bits.Length)
				throw EX.ArgumentOutOfRange("offset", offset);
			if (length > bits.Length - offset)
				throw EX.ArgumentOutOfRange("length", length);

			if (_da.SupportsBlock && (_da.BlockSize >= length || _da.BlockSize == 0))
				return _da.Decrypt(bits, offset, length);
			using var text = new MemoryStream();
			using var sbits = new MemoryStream(bits, offset, length, false);
			Decrypt(text, sbits);
			return text.ToArray();
		}
	}
}


