// Lexxys Infrastructural library.
// file: DesCryptors.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using System.IO;

#nullable enable

namespace Lexxys.Crypting.Cryptors
{
	public class DesCryptoBase: IEncryptorAlgorythm, IDecryptorAlgorythm, IDisposable
	{
		private readonly SymmetricAlgorithm _h;
		private static readonly byte[] _staticIV = { 78, 134, 125, 4, 223, 3, 139, 73 };

		protected DesCryptoBase(object key, bool tripleDes)
		{
			if (key == null)
				throw EX.ArgumentNull(nameof(key));
			if (key is not byte[] bk)
			{
				if (key is not string sk || sk.Length == 0)
					throw new ArgumentTypeException(nameof(key), key.GetType(), typeof(string));
				using var h = new Sha1Hasher();
				byte[] bytes = Encoding.Unicode.GetBytes(sk);
				bytes = h.Hash(bytes, 0, bytes.Length);
				bk = new byte[tripleDes ? 16 : 8];
				Array.Copy(bytes, bk, bk.Length);
			}
			int len = bk.Length;
			if (len == 8)
				_h = DES.Create();
			else if (len == 16 || len == 24)
				_h = TripleDES.Create();
			else
				throw new ArgumentOutOfRangeException(nameof(key), key, null);
			_h.IV = _staticIV;
			_h.Key = bk;
		}

		public virtual int BlockSize => 0;

		public virtual bool SupportsBlock => true;

		public virtual bool SupportsStream => true;

		public virtual object MirrorKey => _h.Key;

		public virtual byte[] Encrypt(byte[] text, int offset, int length)
		{
			ICryptoTransform cr = _h.CreateEncryptor();
			return cr.TransformFinalBlock(text, offset, length);
		}

		public virtual void EncryptStream(Stream bits, Stream text)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			var s = new CryptoStream(bits, _h.CreateEncryptor(), CryptoStreamMode.Write);
			byte[] buffer = new byte[8 * 1024];
			int n;
			while ((n = text.Read(buffer, 0, 8 * 1024)) > 0)
				s.Write(buffer, 0, n);
			s.Close();
		}

		public virtual byte[] Decrypt(byte[] bits, int offset, int length)
		{
			ICryptoTransform cr = _h.CreateDecryptor();
			return cr.TransformFinalBlock(bits, offset, length);
		}

		public virtual void DecryptStream(Stream text, Stream bits)
		{
			if (bits is null)
				throw new ArgumentNullException(nameof(bits));

			var s = new CryptoStream(text, _h.CreateDecryptor(), CryptoStreamMode.Write);
			byte[] buffer = new byte[8 * 1024];
			int n;
			while ((n = bits.Read(buffer, 0, 8 * 1024)) > 0)
				s.Write(buffer, 0, n);
			s.Close();
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

	public class DesCryptor: DesCryptoBase
	{
		public DesCryptor(object key)
			: base(key, false)
		{
		}
	}
	public class TripleDesCryptor: DesCryptoBase
	{
		public TripleDesCryptor(object key)
			: base(key, true)
		{
		}
	}

	public class DesBlockEncryptor: DesCryptoBase
	{
		public DesBlockEncryptor(object key)
			: base(key, false)
		{
		}

		public override bool SupportsStream => false;

		public override int BlockSize => 10;
	}

	public class DesBlockDecryptor: DesCryptoBase
	{
		public DesBlockDecryptor(object key)
			: base(key, false)
		{
		}

		public override bool SupportsStream => false;

		public override int BlockSize => 16;
	}
}
