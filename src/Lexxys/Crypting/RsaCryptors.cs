// Lexxys Infrastructural library.
// file: RsaCryptors.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Security.Cryptography;

namespace Lexxys.Crypting.Cryptors;

public class RsaCryptoBase: IEncryptorAlgorithm, IDecryptorAlgorithm, IDisposable
{
	private readonly RSACryptoServiceProvider _h;
	readonly int _blockSize;
	readonly int _keySize;

	public RsaCryptoBase(object key, bool encryptor)
	{
		_h = new RSACryptoServiceProvider();
		if (key is string s)
			_h.FromXmlString(s);
		else if (key is RSAParameters parameters)
			_h.ImportParameters(parameters);
		else if (key is byte[] blob)
			_h.ImportCspBlob(blob);
		else if (key != null)
			throw new ArgumentOutOfRangeException(nameof(key), key, null);
		_keySize = _h.KeySize / 8;
		_blockSize = _keySize - 11;
		BlockSize = encryptor ? _blockSize: _keySize;
	}

	public virtual void EncryptStream(System.IO.Stream bits, System.IO.Stream text) => throw new NotSupportedException(SR.OperationNotSupported("EncryptStream"));

	public virtual void DecryptStream(System.IO.Stream text, System.IO.Stream bits) => throw new NotSupportedException(SR.OperationNotSupported("DecryptStream"));

	public virtual byte[] Encrypt(byte[] text, int offset, int length)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if ((uint)offset >= text.Length)
			throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
		if (length < 0 || offset + length > text.Length)
			throw new ArgumentOutOfRangeException(nameof(length), length, null);
		if (offset == 0 && length == text.Length && length <= BlockSize)
			return _h.Encrypt(text, false);

		var result = new byte[_keySize * ((length + _blockSize - 1) / _blockSize)];
		var index = 0;
		byte[] bts = new byte[_blockSize];
		while (length >= _blockSize)
		{
			Array.Copy(text, offset, bts, 0, _blockSize);
			length -= _blockSize;
			offset += _blockSize;
			var t = _h.Encrypt(bts, false);
			Array.Copy(t, 0, result, index, _keySize);
			index += _keySize;
		}
		if (length > 0)
		{
			var tmp2 = new byte[length];
			Array.Copy(text, offset, bts, 0, length);
			var t = _h.Encrypt(tmp2, false);
			Array.Copy(t, 0, result, index, _keySize);
		}
		return result;
	}

	public virtual byte[] Decrypt(byte[] bits, int offset, int length)
	{
		if (bits is null)
			throw new ArgumentNullException(nameof(bits));
		if (length == 0)
			return Array.Empty<byte>();
		if (length % _keySize != 0)
			throw new ArgumentOutOfRangeException(nameof(length), length, null);
		if (offset == 0 && length == bits.Length && length == _keySize)
			return _h.Decrypt(bits, false);
		if (length == _keySize)
		{
			var tmp = new byte[length];
			Array.Copy(bits, offset, tmp, 0, length);
			return _h.Decrypt(tmp, false);
		}

		var bts = new byte[_keySize];
		var buffer = new byte[(length / _keySize) * _blockSize];
		var index = 0;
		do
		{
			Array.Copy(bits, offset, bts, 0, _keySize);
			byte[] dec = _h.Decrypt(bts, false);
			Array.Copy(dec, 0, buffer, index, dec.Length);
			offset += _keySize;
			length -= _keySize;
			index += dec.Length;
		} while (length > 0);

		if (index == buffer.Length)
			return buffer;
		var result = new byte[index];
		Array.Copy(buffer, 0, result, 0, index);
		return result;
	}

	public virtual int BlockSize { get; }

	public virtual bool SupportsBlock => true;

	public virtual bool SupportsStream => false;

	#region IDisposable Members
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		// Check to see if Dispose has already been called.
		if (disposing && !_disposed)
		{
			_disposed = true;
			_h.Dispose();
		}
	}
	private bool _disposed;
	#endregion
}

public class RsaEncryptor: RsaCryptoBase
{
	public RsaEncryptor(object key)
		: base(key, true)
	{
	}
}
public class RsaDecryptor: RsaCryptoBase
{
	public RsaDecryptor(object key)
		: base(key, false)
	{
	}
}
