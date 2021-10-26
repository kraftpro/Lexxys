// Lexxys Infrastructural library.
// file: LocalFileSchemeParserTest.cs
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
	///This is a test class for LocalFileSchemeParserTest and is intended
	///to contain all LocalFileSchemeParserTest Unit Tests
	///</summary>
	//[TestClass()]
	public class LocalFileSchemeParserTest
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

		/// <summary>
		///A test for Parse
		///</summary>
		[TestMethod()]
		public void ParseTest()
		{
			//FileScheme target = new FileScheme(); // TODO: Initialize to an appropriate value
			//string value = string.Empty; // TODO: Initialize to an appropriate value
			//Vote<string> score = Vote.Yes("file"); // TODO: Initialize to an appropriate value
			//CurlBuilder expected = null; // TODO: Initialize to an appropriate value
			//CurlBuilder actual;
			//actual = target.Parse(value, score);
			//Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		private void ParseTest(string value, string host, string path, string localpath, bool absolute)
		{
			FileScheme parser = new FileScheme();
			CurlBuilder locator = parser.Parse(value, FileScheme.Scheme);
			if (host == null && path == null && localpath == null && absolute)
			{
				Assert.IsNull(locator);
				return;
			}
			Assert.IsNotNull(locator);
		}
	}
}
