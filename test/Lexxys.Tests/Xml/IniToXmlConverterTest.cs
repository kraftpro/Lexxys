// Lexxys Infrastructural library.
// file: IniToXmlConverterTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using Lexxys.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Lexxys.Tests.Xml
{

	/// <summary>
	///This is a test class for IniToXmlConverterTest and is intended
	///to contain all IniToXmlConverterTest Unit Tests
	///</summary>
	[TestClass()]
	public class IniToXmlConverterTest
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
		///A test for Convert
		///</summary>
		[TestMethod()]
		public void ConvertIniToXmlTest()
		{
			string[] tables = new string[]
			{
@"[s_A] ; comment
a=1
b = 2
; comment",
@"<s_A a='1' b='2' />",

			};

			for (int i = 1; i < tables.Length; i += 2)
			{
				string xml = IniToXmlConverter.Convert(tables[i - 1]);
				Assert.IsNotNull(xml);
				XmlLiteNode actual = XmlLiteNode.FromXml(xml, true);
				XmlLiteNode expected = XmlLiteNode.FromXml(tables[i], true);
				Assert.AreEqual(expected, actual, tables[i-1]);
			}
		}
	}
}
