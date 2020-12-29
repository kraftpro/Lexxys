// Lexxys Infrastructural library.
// file: Cryp.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Lexxys.Crypting;

namespace Lexxys.Test.Con
{
	static class Cryp
	{
		static byte[] _key8bit;
		static byte[] _key24bit;
		static string _keyRsaXml;
		static RSAParameters _keyRsa;

		static Cryp()
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
			_keyRsaXml = rsa.ToXmlString(true);
			_keyRsa = rsa.ExportParameters(true);
			_key8bit = new byte[] { 188, 36, 52, 39, 168, 206, 154, 143 };
			_key24bit = new byte[] { 188, 36, 52, 39, 168, 206, 154, 143,
				88, 136, 152, 139, 68, 106, 54, 43,
				88, 236, 252, 239, 168, 6, 254, 243 };
		}

		public static void Test()
		{
			TestBlock("des", _key8bit, 1024);
			TestStream("rsa", _keyRsaXml, 1024);

			Hasher h = Crypto.Hasher("md5p");
			byte[] hash = h.Hash(System.Text.Encoding.Unicode.GetBytes("fucker12"));
			string sh = ToHexString(hash);
			Debug.Assert("5CDA470335F80A194685FDA4C0A3153A" == sh);
		}

		public static string Test1(string input)
		{
			Hasher h = Crypto.Hasher("md5");
			byte[] hash = h.Hash(System.Text.Encoding.Unicode.GetBytes(input));
			return ToHexString(hash);
		}

		public static string Test2(string input)
		{
			// step 1, calculate MD5 hash from input
			MD5 md5 = System.Security.Cryptography.MD5.Create();
			byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);
 
			// step 2, convert byte array to hex string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}
			return sb.ToString();
		}

		public static void TestBlock(string alg, object key, int dataSize)
		{
			string s1 = RandomData(dataSize);
			string s2 = RandomData(dataSize);

			Encryptor en = Crypto.Encryptor(alg, key);
			byte[] cyp1 = en.EncryptString(s1);
			byte[] cyp2 = en.EncryptString(s2);

			Decryptor de = Crypto.Decryptor(alg, key);

			string t1 = de.DecryptString(cyp1);
			Debug.Assert(s1 == t1);

			string t2 = de.DecryptString(cyp2);
			Debug.Assert(s2 == t2);
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
			Debug.Assert(s1 == t1);

			string t2 = UnicodeEncoding.Unicode.GetString(r2.ToArray());
			Debug.Assert(s2 == t2);
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
