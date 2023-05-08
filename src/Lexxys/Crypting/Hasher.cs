// Lexxys Infrastructural library.
// file: Hasher.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys.Crypting;

/// <summary>Wrapper for generic hash algorithm.</summary>
public class Hasher
{
	private readonly IHasherAlgorithm _algorithm;

	public Hasher(IHasherAlgorithm algorithm)
	{
		if (algorithm == null)
			throw new ArgumentNullException(nameof(algorithm));
		if (!algorithm.SupportsStream || !algorithm.SupportsBlock)
			throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
		_algorithm = algorithm;
	}

	public int HashSize
	{
		get { return _algorithm.HashSize; }
	}

	public byte[] Hash(Stream text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		return _algorithm.HashStream(text);
	}
	public byte[] Hash(byte[] text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		return _algorithm.Hash(text, 0, text.Length);
	}
	public byte[] Hash(String text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		byte[] bytes = Encoding.Unicode.GetBytes(text);
		return _algorithm.Hash(bytes, 0, bytes.Length);
	}
	public byte[] Hash(byte[] text, int offset, int length)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		if ((uint)offset >= text.Length)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
		if ((uint)length > text.Length - offset)
			throw new ArgumentOutOfRangeException(nameof(length), length, null);
		return _algorithm.Hash(text, offset, length);
	}

	public bool Verify(byte[] hash, Stream text)
	{
		if (hash == null)
			throw new ArgumentNullException(nameof(hash));
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		byte[] hash2 = _algorithm.HashStream(text);
		if (hash.Length != hash2.Length)
			return false;
		for (int i = 0; i < hash.Length; ++i)
			if (hash[i] != hash2[i])
				return false;
		return true;
	}
	
	public bool Verify(byte[] hash, byte[] text)
	{
		if (hash == null)
			throw new ArgumentNullException(nameof(hash));
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		byte[] hash2 = _algorithm.Hash(text, 0, text.Length);
		if (hash.Length != hash2.Length)
			return false;
		for (int i = 0; i < hash.Length; ++i)
			if (hash[i] != hash2[i])
				return false;
		return true;
	}
	
	public bool Verify(byte[] hash, string text)
	{
		if (hash == null)
			throw new ArgumentNullException(nameof(hash));
		if (text == null)
			throw new ArgumentNullException(nameof(text));
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
