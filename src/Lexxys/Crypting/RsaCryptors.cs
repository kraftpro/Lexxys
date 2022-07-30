// Lexxys Infrastructural library.
// file: RsaCryptors.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Lexxys.Crypting.Cryptors
{
	public class RsaCryptoBase: IEncryptorAlgorythm, IDecryptorAlgorythm, IDisposable
	{
		private readonly RSACryptoServiceProvider _h;
		readonly int _bsize;

		public RsaCryptoBase(object key, bool encryptor)
		{
			if (key == null)
				throw EX.ArgumentNull(nameof(key));
			_h = new RSACryptoServiceProvider();
#if !NETCOREAPP
			if (key is string s)
				_h.FromXmlString(s);
			else
#endif
			if (key is RSAParameters parameters)
				_h.ImportParameters(parameters);
			else
				throw EX.ArgumentOutOfRange(nameof(key), key);
			_bsize = encryptor ? _h.KeySize / 8 - 11 : _h.KeySize / 8;
		}
		public virtual void EncryptStream(System.IO.Stream bits, System.IO.Stream text)
		{
			throw EX.NotSupported("EncryptStream");
		}
		public virtual void DecryptStream(System.IO.Stream text, System.IO.Stream bits)
		{
			throw EX.NotSupported("DecryptStream");
		}

		public virtual byte[] Encrypt(byte[] text, int offset, int length)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (offset == 0 && length == text.Length)
				return _h.Encrypt(text, false);
			byte[] txt = new byte[length];
			Array.Copy(text, 0, txt, 0, length);
			return _h.Encrypt(txt, false);
		}
		public virtual byte[] Decrypt(byte[] bits, int offset, int length)
		{
			if (bits is null)
				throw new ArgumentNullException(nameof(bits));
			if (offset == 0 && length == bits.Length)
				return _h.Decrypt(bits, false);
			byte[] bts = new byte[length];
			Array.Copy(bits, 0, bts, 0, length);
			return _h.Decrypt(bts, false);
		}

		public virtual int BlockSize
		{
			get { return _bsize; }
		}

		public virtual bool SupportsBlock
		{
			get { return true; }
		}

		public virtual bool SupportsStream
		{
			get { return false; }
		}

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
				if (_h != null)
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
}
