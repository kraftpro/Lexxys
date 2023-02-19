// Lexxys Infrastructural library.
// file: SixBitsCoderTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;

namespace Lexxys.Tests.Tools
{


	/// <summary>
	///This is a test class for SixBitsCoderTest and is intended
	///to contain all SixBitsCoderTest Unit Tests
	///</summary>
	[TestClass()]
	public class SixBitsCoderTest
	{
		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		/// <summary>
		///A test for BitsToChar / CharToBits
		///</summary>
		[TestMethod()]
		public void CharToBitsToCharTest()
		{
			for (int expected = 0; expected < 64; ++expected)
			{
				char c = SixBitsCoder.BitsToChar(expected);
				int actual = SixBitsCoder.CharToBits(c);
				Assert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		///A test for Encode / Decode
		///</summary>
		[TestMethod()]
		public void EncodeDecodeTest()
		{
			Random r = new Random();
			for (int i = 0; i < 200; ++i)
			{
				int n = r.Next(128);
				byte[] expected = new byte[n];
				r.NextBytes(expected);
				string value = SixBitsCoder.Encode(expected);
				byte[] actual = SixBitsCoder.Decode(value);
				CollectionAssert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		///A test for DecodedLength
		///</summary>
		[TestMethod()]
		public void DecodedLengthTest()
		{
			Random r = new Random();
			for (int expected = 0; expected < 512; ++expected)
			{
				byte[] bits = new byte[expected];
				r.NextBytes(bits);
				int actual = SixBitsCoder.DecodedLength(SixBitsCoder.Encode(bits).Length);
				Assert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		///A test for EncodedLength
		///</summary>
		[TestMethod()]
		public void EncodedLengthTest()
		{
			Random r = new Random();
			for (int n = 0; n < 512; ++n)
			{
				byte[] bits = new byte[n];
				r.NextBytes(bits);
				int expected = SixBitsCoder.Encode(bits).Length;
				int actual = SixBitsCoder.EncodedLength(n);
				Assert.AreEqual(expected, actual);
			}
		}

		/// <summary>
		///A test for GenerateSessionId
		///</summary>
		[TestMethod()]
		public void SessionIdTest()
		{
			Random r = new Random();
			HashSet<string> history = new HashSet<string>();
			for (int n = 0; n < 512; ++n)
			{
				string session = SixBitsCoder.GenerateSessionId();
				Assert.AreEqual(24, session.Length);
				Assert.IsFalse(history.Contains(session));
				history.Add(session);
				Assert.IsTrue(SixBitsCoder.IsWellFormedSessionId(session));
				char[] cs = session.ToCharArray();
				Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(session + "x"));
				Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(session.Substring(1)));
				int i = r.Next(0, 23);
				int j = r.Next(0, 23);
				char x = cs[i];
				char y = cs[j];
				if (x != y)
				{
					cs[j] = x;
					cs[i] = y;
					Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(cs.ToString()));
					cs[i] = x;
					cs[j] = y;
				}
				if (Char.IsUpper(x))
				{
					cs[i] = Char.ToLowerInvariant(x);
					Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(cs.ToString()));
					cs[i] = x;
				}
				if (Char.IsLower(y))
				{
					cs[j] = Char.ToUpperInvariant(y);
					Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(cs.ToString()));
					cs[j] = y;
				}
				char c = (char)r.Next(0, 255);
				if (c != x)
				{
					cs[i] = c;
					Assert.IsFalse(SixBitsCoder.IsWellFormedSessionId(cs.ToString()));
					cs[i] = x;
				}
			}
		}
	}
}
