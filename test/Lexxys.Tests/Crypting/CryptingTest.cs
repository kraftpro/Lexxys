// Lexxys Infrastructural library.
// file: CryptingTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Lexxys.Crypting;
using Lexxys.Configuration;

namespace Lexxys.Tests.Crypting
{

	[TestClass]
	public class CryptingTest
	{
		byte[] _key8bit;
		byte[] _key24bit;
		string _keyRsaXml;
		RSAParameters _keyRsa;

		public CryptingTest()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			_keyRsaXml = rsa.ToXmlString(true);
			_keyRsa = rsa.ExportParameters(true);
			_key8bit = new byte[] { 188, 36, 52, 39, 168, 206, 154, 143 };
			_key24bit = new byte[] { 188, 36, 52, 39, 168, 206, 154, 143,
				88, 136, 152, 139, 68, 106, 54, 43,
				88, 236, 252, 239, 168, 6, 254, 243 };
		}

		[TestMethod]
		public void DESBlock()
		{
			Tools.TestBlock("des", _key8bit, 1024);
		}

		[TestMethod]
		public void DESStream()
		{
			Tools.TestStream("des", _key8bit, 1024);
		}

		[TestMethod]
		public void TripleDESBlock()
		{
			Tools.TestBlock("des3", _key24bit, 1024);
		}

		[TestMethod]
		public void TripleDESStream()
		{
			Tools.TestStream("des3", _key24bit, 1024);
		}

		[TestMethod]
		public void RSABlock()
		{
			Tools.TestBlock("rsa", _keyRsa, 1024);
		}

		[TestMethod]
		public void RSAStream()
		{
			Tools.TestStream("rsa", _keyRsaXml, 1024);
		}

		[TestMethod]
		public void MD5Hash()
		{
			Hasher h = Crypto.Hasher("md5p");
			byte[] hash = h.Hash(System.Text.UnicodeEncoding.Unicode.GetBytes("fucker12"));
			string sh = Tools.ToHexString(hash);
			Assert.AreEqual<string>("5CDA470335F80A194685FDA4C0A3153A", sh);
		}

		[TestMethod]
		public void ShaHash()
		{
			Hasher h = Crypto.Hasher("SHA");
			byte[] hash = h.Hash(System.Text.UnicodeEncoding.Unicode.GetBytes("fucker12"));
			string sh = Tools.ToHexString(hash);
			Assert.AreEqual<string>("A3695D1111A63A5765546D581CE8A4648B1DB3C0", sh);
		}


		private class Tools
		{
			public static void TestBlock(string alg, object key, int dataSize)
			{
				string s1 = RandomData(dataSize);
				string s2 = RandomData(dataSize);

				Encryptor en = Crypto.Encryptor(alg, key);
				byte[] cyp1 = en.EncryptString(s1);
				byte[] cyp2 = en.EncryptString(s2);

				Decryptor de = Crypto.Decryptor(alg, key);

				string t1 = de.DecryptString(cyp1);
				Assert.AreEqual(s1, t1);

				string t2 = de.DecryptString(cyp2);
				Assert.AreEqual(s2, t2);
			}

			public static void TestStream(string alg, object key, int dataSize)
			{
				string s1 = RandomData(dataSize);
				string s2 = RandomData(dataSize);
				MemoryStream c1 = new MemoryStream(UnicodeEncoding.Unicode.GetBytes(s1), false);
				MemoryStream c2 = new MemoryStream(UnicodeEncoding.Unicode.GetBytes(s2), false);
				MemoryStream d1 = new MemoryStream(40960);
				MemoryStream d2 = new MemoryStream(40960);

				Encryptor en = Crypto.Encryptor(alg, key);
				en.Encrypt(d1, c1);
				en.Encrypt(d2, c2);

				Decryptor de = Crypto.Decryptor(alg, key);
				MemoryStream r1 = new MemoryStream(40960);
				MemoryStream r2 = new MemoryStream(40960);
				d1 = new MemoryStream(d1.ToArray());
				d2 = new MemoryStream(d2.ToArray());
				de.Decrypt(r1, d1);
				de.Decrypt(r2, d2);

				string t1 = UnicodeEncoding.Unicode.GetString(r1.ToArray());
				Assert.AreEqual(s1, t1);

				string t2 = UnicodeEncoding.Unicode.GetString(r2.ToArray());
				Assert.AreEqual<string>(s2, t2);
			}

			private static string RandomData(int dataSize)
			{
				string abc = "abcdefghijklmnopqrstuvwxyz";
				abc = abc + abc.ToUpper() + "0123456789";
				Random r = new Random();
				StringBuilder sb = new StringBuilder(dataSize);
				for (int i = 0; i < dataSize; ++i)
					sb.Append(abc[r.Next(abc.Length - 1)]);
				return sb.ToString();
			}

			public static string ToHexString(byte[] bits)
			{
				string s = "";
				foreach (byte b in bits)
				{
					s += b.ToString("X2");
				}
				return s;
			}
		}
	}
}
