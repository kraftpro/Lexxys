// Lexxys Infrastructural library.
// file: CharStreamTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Tokenizer
{
	using Lexxys.Tokenizer;
	using Lexxys.Testing;

	/// <summary>
	///This is a test class for CharStreamTest and is intended
	///to contain all CharStreamTest Unit Tests
	///</summary>
	[TestClass()]
	public class CharStreamTest
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


		private static CharStream GetStream(int index = -1)
		{
			if (index < 0)
				index = __r.Next(__streamBuffer.Length);
			return new CharStream(__streamBuffer[index % __streamBuffer.Length]);
		}
		private static string[] __streamBuffer = new string[]
		{
			"ABCD\nEFGH\nIJKL\nMNOP\nQRS \nabcd\nefgh\nijkl\nmnop\nqrs \n"
		};
		private static Random __r = new Random();



		/// <summary>
		///A test for CharStream Constructor
		///</summary>
		[TestMethod()]
		public void CharStreamConstructorTest()
		{
			string buffer = string.Empty;
			int tabSize = 4;
			bool appendNewLine = true;
			CultureInfo culture = null;
			CharStream target = new CharStream(buffer, appendNewLine, tabSize, culture);
		}

		/// <summary>
		///A test for CharStream Constructor
		///</summary>
		[TestMethod()]
		public void CharStreamCopyConstructorTest()
		{
			CharStream charStream = new CharStream("12345", false, 0);
			CharStream target = new CharStream(charStream);
			Assert.AreEqual(charStream.ToString(), target.ToString());
		}

		/// <summary>
		///A test for Clone
		///</summary>
		[TestMethod()]
		public void CloneTest()
		{
			CharStream cs = GetStream();
			cs.Forward(15);
			CharStream actual = cs.Clone();
			Assert.AreEqual(cs.ToString(), actual.ToString());
		}

		/// <summary>
		///A test for Forward
		///</summary>
		[TestMethod()]
		public void ForwardTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharStream cs = GetStream(i);
				int length = cs.Length;
				for (int expected = 0; expected < length; )
				{
					int offset = __r.Next(length - expected + 5);
					expected += offset;
					cs.Forward(offset);
					Assert.AreEqual(expected > length ? length: expected, cs.Position);
				}
			}
		}

		/// <summary>
		///A test for IndexOf
		///</summary>
		[TestMethod()]
		public void IndexOfStringWithoutOffsetTest()
		{
			for (int i = 0; i < 10; ++i)
			{
				CharStream cs = GetStream(i);
				string text = cs.Substring(0, cs.Length);
				for (int j = 0; j < text.Length; ++j)
				{
					int index = __r.Next(text.Length);
					int length = __r.Next(text.Length - index);
					int position = __r.Next(index);
					string value = text.Substring(index, length);
					int expected = text.IndexOf(value, position) - position;
					cs.Rewind();
					cs.Forward(position);
					int actual = cs.IndexOf(value);
					Assert.AreEqual(expected, actual);
				}
			}
		}

		/// <summary>
		///A test for IndexOf
		///</summary>
		[TestMethod()]
		public void IndexOfCharacterWithoutOffsetTest()
		{
			for (int i = 0; i < 10; ++i)
			{
				CharStream cs = GetStream(i);
				string text = cs.Substring(0, cs.Length);
				for (int j = 0; j < text.Length; ++j)
				{
					int index = __r.Next(text.Length);
					int position = __r.Next(index);
					char value = text[index];
					int expected = text.IndexOf(value, position) - position;
					cs.Rewind();
					cs.Forward(position);
					int actual = cs.IndexOf(value);
					Assert.AreEqual(expected, actual);
				}
			}
		}

		/// <summary>
		///A test for IndexOf
		///</summary>
		[TestMethod()]
		public void IndexOfCharacterTest()
		{
			for (int i = 0; i < 10; ++i)
			{
				CharStream cs = GetStream(i);
				string text = cs.Substring(0, cs.Length);
				for (int j = 0; j < text.Length; ++j)
				{
					int index = __r.Next(text.Length);
					int position = __r.Next(index);
					int offset = __r.Next(index - position);
					char value = text[index];
					int expected = text.IndexOf(value, position + offset) - position;
					cs.Rewind();
					cs.Forward(position);
					int actual = cs.IndexOf(value, offset);
					Assert.AreEqual(expected, actual);
				}
			}
		}

		/// <summary>
		///A test for IndexOf
		///</summary>
		[TestMethod()]
		public void IndexOfRegexTest()
		{
			//CharStream charStream = null; // TODO: Initialize to an appropriate value
			//CharStream target = new CharStream(charStream); // TODO: Initialize to an appropriate value
			//Regex regex = null; // TODO: Initialize to an appropriate value
			//int offset = 0; // TODO: Initialize to an appropriate value
			//int expected = 0; // TODO: Initialize to an appropriate value
			//int actual;
			//actual = target.IndexOf(regex, offset);
			//Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for IndexOf
		///</summary>
		[TestMethod()]
		public void IndexOfStringTest()
		{
			for (int i = 0; i < 10; ++i)
			{
				CharStream cs = GetStream(i);
				string text = cs.Substring(0, cs.Length);
				for (int j = 0; j < text.Length; ++j)
				{
					int index = __r.Next(text.Length);
					int position = __r.Next(index);
					int offset = __r.Next(index - position);
					int length = __r.Next(text.Length - index);
					string value = text.Substring(index, length);
					int expected = text.IndexOf(value, position + offset) - position;
					cs.Rewind();
					cs.Forward(position);
					int actual = cs.IndexOf(value, offset);
					Assert.AreEqual(expected, actual);
				}
			}
		}

		private static char[] GetRandomCharArray(int length = -1)
		{
			if (length < 0)
				length = __r.Next(64);
			char[] result = new char[length];
			for (int i = 0; i < result.Length; ++i)
			{
				result[i] = (char)(byte)__r.Next(1, 127);
			}
			return result;
		}

		/// <summary>
		///A test for IndexOfAny
		///</summary>
		[TestMethod()]
		public void IndexOfAnyTest()
		{
			for (int i = 0; i < 10; ++i)
			{
				CharStream cs = GetStream(i);
				string text = cs.Substring(0, cs.Length);
				for (int j = 0; j < text.Length; ++j)
				{
					int index = __r.Next(text.Length);
					int position = __r.Next(index);
					int offset = __r.Next(index - position);
					char[] value = GetRandomCharArray();
					int expected = text.IndexOfAny(value, position + offset) - position;
					cs.Rewind();
					cs.Forward(position);
					int actual = cs.IndexOfAny(value, offset);
					Assert.AreEqual(expected, actual);
				}
			}
		}

		///// <summary>
		/////A test for Match
		/////</summary>
		//[TestMethod()]
		//public void MatchTest()
		//{
		//	//CharStream charStream = null; // TODO: Initialize to an appropriate value
		//	//CharStream target = new CharStream(charStream); // TODO: Initialize to an appropriate value
		//	//Regex regex = null; // TODO: Initialize to an appropriate value
		//	//int offset = 0; // TODO: Initialize to an appropriate value
		//	//Match expected = null; // TODO: Initialize to an appropriate value
		//	//Match actual;
		//	//actual = target.Match(regex, offset);
		//	//Assert.AreEqual(expected, actual);
		//	Assert.Inconclusive("Verify the correctness of this test method.");
		//}

		/// <summary>
		///A test for Rewind
		///</summary>
		[TestMethod()]
		public void RewindTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharStream cs = GetStream();
				cs.Forward(__r.Next(cs.Length));
				cs.Rewind();
				Assert.AreEqual(CharPosition.Start, cs.GetCharPosition());
			}
		}

		/// <summary>
		///A test for Move
		///</summary>
		[TestMethod()]
		public void MoveTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharStream cs = GetStream();
				cs.Forward(__r.Next(cs.Length));
				CharPosition expected = cs.GetCharPosition();
				cs.Rewind();
				cs.Move(expected.Position);
				Assert.AreEqual(expected, cs.GetCharPosition());
			}
		}

		/// <summary>
		///A test for Substring
		///</summary>
		[TestMethod()]
		public void SubstringTest()
		{
			//CharStream charStream = null; // TODO: Initialize to an appropriate value
			//CharStream target = new CharStream(charStream); // TODO: Initialize to an appropriate value
			//int start = 0; // TODO: Initialize to an appropriate value
			//int length = 0; // TODO: Initialize to an appropriate value
			//string expected = string.Empty; // TODO: Initialize to an appropriate value
			//string actual;
			//actual = target.Substring(start, length);
			//Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}


		/// <summary>
		///A test for Item
		///</summary>
		[TestMethod()]
		public void ItemTest()
		{
			CharStream c = GetStream();
			int length = c.Length;
			for (int i = 0; i < 100; ++i)
			{
				c.Rewind();
				var k = Rand.Int(length + 5) - 2;
				var a = c[k];
				var l = Rand.Int(length / 2);
				c.Forward(l);
				var b = c[k - l];
				Assert.AreEqual(a, b);
			}
		}
	}
}
