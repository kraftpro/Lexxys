// Lexxys Infrastructural library.
// file: ResourceLocatorTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using Lexxys.RL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Lexxys.Tests.RL
{
	
	
	/// <summary>
	///This is a test class for ResourceLocatorTest and is intended
	///to contain all ResourceLocatorTest Unit Tests
	///</summary>
	//[TestClass()]
	public class ResourceLocatorTest
	{


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get => testContextInstance;
			set => testContextInstance = value;
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
		///A test for Parse
		///</summary>
		[TestMethod()]
		public void ParseTest()
		{
			string value = string.Empty; // TODO: Initialize to an appropriate value
			Curl expected = null; // TODO: Initialize to an appropriate value
			Curl actual;
			actual = Curl.Create(value);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}


	}
}
