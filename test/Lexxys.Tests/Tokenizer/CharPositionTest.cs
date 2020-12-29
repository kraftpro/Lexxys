// Lexxys Infrastructural library.
// file: CharPositionTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using Lexxys.Tokenizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace Lexxys.Tests.Tokenizer
{
	
	
	/// <summary>
	///This is a test class for CharPositionTest and is intended
	///to contain all CharPositionTest Unit Tests
	///</summary>
	[TestClass()]
	public class CharPositionTest
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


		CharPosition NextCharPosition()
		{
			return new CharPosition(__r.Next(6553600), __r.Next(65536), __r.Next(120));
		}
		static Random __r = new Random();

		/// <summary>
		///A test for CharPosition Constructor
		///</summary>
		[TestMethod()]
		public void CharPositionConstructorTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				int position = __r.Next(6553600);
				int line = __r.Next(65536);
				int column = __r.Next(120);
				CharPosition target = new CharPosition(position, line, column);
				Assert.AreEqual(position, target.Position);
				Assert.AreEqual(line, target.Line);
				Assert.AreEqual(column, target.Column);
			}
		}

		/// <summary>
		///A test for CharPosition Constructor
		///</summary>
		[TestMethod()]
		public void CharPositionCopyConstructorTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharPosition value = NextCharPosition();
				CharPosition target = new CharPosition(value);
				Assert.AreEqual(value.Position, target.Position);
				Assert.AreEqual(value.Line, target.Line);
				Assert.AreEqual(value.Column, target.Column);
			}
		}

		/// <summary>
		///A test for Equals
		///</summary>
		[TestMethod()]
		public void EqualsTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharPosition value = NextCharPosition();
				Assert.IsFalse(value.Equals(null));
				Assert.IsFalse(value.Equals(new CharPosition()));
				CharPosition target = NextCharPosition();
				Assert.IsFalse(value.Equals(target));
				target = new CharPosition(value);
				Assert.IsTrue(value.Equals(target));
			}
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharPosition value = NextCharPosition();
				string target = value.ToString();
				Assert.IsTrue(target.Contains((value.Line + 1).ToString()));
				Assert.IsTrue(target.Contains((value.Column + 1).ToString()));
			}
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[TestMethod()]
		public void ToStringCultureTest1()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharPosition value = NextCharPosition();
				string target = value.ToString(CultureInfo.CurrentCulture);
				Assert.IsTrue(target.Contains((value.Line + 1).ToString()));
				Assert.IsTrue(target.Contains((value.Column + 1).ToString()));
			}
		}

		/// <summary>
		///A test for op_Equality
		///</summary>
		[TestMethod()]
		public void op_EqualityInequalityTest()
		{
			for (int i = 0; i < 100; ++i)
			{
				CharPosition left = NextCharPosition();
				CharPosition right = new CharPosition(left);
				Assert.IsTrue(left == right);
				right = new CharPosition(left.Position, left.Line + 1, left.Column);
				Assert.IsTrue(left != right);
				right = new CharPosition(left.Position + 1, left.Line, left.Column);
				Assert.IsTrue(left != right);
				right = new CharPosition(right.Position - 1, right.Line, right.Column);
				Assert.IsTrue(left == right);
			}
		}

		/// <summary>
		///A test for Column
		///</summary>
		[TestMethod()]
		public void ColumnLinePositionTest()
		{
			CharPosition value = new CharPosition();
			Assert.AreEqual(0, value.Column);
			value = new CharPosition(30, 20, 10);
			Assert.AreEqual(30, value.Position);
			Assert.AreEqual(20, value.Line);
			Assert.AreEqual(10, value.Column);
		}
	}
}
