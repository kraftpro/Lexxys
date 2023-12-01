// Lexxys Infrastructural library.
// file: StdHashers.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Security.Cryptography;

#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms

namespace Lexxys.Crypting.Cryptors;

public sealed class MD5Hasher: IHasherAlgorithm, IDisposable
{
	private readonly MD5 _h;

	public MD5Hasher() => _h = MD5.Create();
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => 128 / 8;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

public sealed class Sha1Hasher: IHasherAlgorithm, IDisposable
{
	private readonly SHA1 _h;

	public Sha1Hasher() => _h = SHA1.Create();
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => 128 / 8;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

public sealed class Sha2Hasher: IHasherAlgorithm, IDisposable
{
	private readonly SHA256 _h;

	public Sha2Hasher() => _h = SHA256.Create();
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => 256 / 8;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

public sealed class Sha3Hasher: IHasherAlgorithm, IDisposable
{
	private readonly SHA384 _h;

	public Sha3Hasher() => _h = SHA384.Create();
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => 384 / 8;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

public sealed class Sha5Hasher: IHasherAlgorithm, IDisposable
{
	private readonly SHA512 _h;

	public Sha5Hasher() => _h = SHA512.Create();
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => _h.HashSize / 8;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

public sealed class HmaHasher: IHasherAlgorithm, IDisposable
{
	private readonly KeyedHashAlgorithm _h;

	public HmaHasher() => _h = new HMACSHA1();
	public HmaHasher(object key) => _h = key is byte[] bk ? new HMACSHA1(bk): throw (key is null ? new ArgumentNullException(nameof(key)): new ArgumentTypeException(nameof(key), key.GetType()));
	public bool SupportsStream => true;
	public bool SupportsBlock => true;
	public int HashSize => 20;
	public byte[] Hash(byte[] text, int offset, int length) => _h.ComputeHash(text, offset, length);
	public byte[] HashStream(System.IO.Stream text) => _h.ComputeHash(text);

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
}

