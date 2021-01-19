// Lexxys Infrastructural library.
// file: Hasher.cs
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

	/// <summary>Wrapper for generic hash algorithm.</summary>
	public class Hasher
	{
		private readonly IHasherAlgorythm _algorithm;

		public Hasher(IHasherAlgorythm algorithm)
		{
			if (algorithm == null)
				throw EX.ArgumentNull(nameof(algorithm));
			if (!algorithm.SupportsStream || !algorithm.SupportsBlock)
				throw EX.ArgumentOutOfRange(nameof(algorithm), algorithm);
			_algorithm = algorithm;
		}

		public int HashSize
		{
			get { return _algorithm.HashSize; }
		}

		public byte[] Hash(Stream text)
		{
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			return _algorithm.HashStream(text);
		}
		public byte[] Hash(byte[] text)
		{
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			return _algorithm.Hash(text, 0, text.Length);
		}
		public byte[] Hash(String text)
		{
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			byte[] bytes = Encoding.Unicode.GetBytes(text);
			return _algorithm.Hash(bytes, 0, bytes.Length);
		}
		public byte[] Hash(byte[] text, int offset, int length)
		{
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			if (offset < 0 || offset >= text.Length)
				throw EX.ArgumentOutOfRange(nameof(offset), offset);
			if (length > text.Length - offset)
				throw EX.ArgumentOutOfRange(nameof(length), length);
			return _algorithm.Hash(text, offset, length);
		}

		public bool Equal(byte[] hash, Stream text)
		{
			if (hash == null)
				throw EX.ArgumentNull(nameof(hash));
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			byte[] hash2 = _algorithm.HashStream(text);
			if (hash.Length != hash2.Length)
				return false;
			for (int i = 0; i < hash.Length; ++i)
				if (hash[i] != hash2[i])
					return false;
			return true;
		}
		public bool Equal(byte[] hash, byte[] text)
		{
			if (hash == null)
				throw EX.ArgumentNull(nameof(hash));
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			byte[] hash2 = _algorithm.Hash(text, 0, text.Length);
			if (hash.Length != hash2.Length)
				return false;
			for (int i = 0; i < hash.Length; ++i)
				if (hash[i] != hash2[i])
					return false;
			return true;
		}
		public bool Equal(byte[] hash, string text)
		{
			if (hash == null)
				throw EX.ArgumentNull(nameof(hash));
			if (text == null)
				throw EX.ArgumentNull(nameof(text));
			byte[] bytes = Encoding.Unicode.GetBytes(text);
			byte[] hash2 = _algorithm.Hash(bytes, 0, bytes.Length);
			if (hash.Length != hash2.Length)
				return false;
			for (int i = 0; i < hash.Length; ++i)
				if (hash[i] != hash2[i])
					return false;
			return true;
		}
	}
}
