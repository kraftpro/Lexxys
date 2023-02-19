// Lexxys Infrastructural library.
// file: IniToXmlConverterTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using Lexxys.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Lexxys.Tests.Xml
{

	/// <summary>
	///This is a test class for IniToXmlConverterTest and is intended
	///to contain all IniToXmlConverterTest Unit Tests
	///</summary>
	[TestClass()]
	public class JsonToXmlConverterTest
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
		public void ConvertJsonToXmlTest()
		{
			(string Actual, string Result, bool Force)[] tables = new (string Actual, string Result, bool Force)[]
			{
				(
					Actual: "{\"type\":\"weekly\",\"week\":2,\"reminder\":{\"value\":\"P2D\",\"businessDays\":true,\"shift\":\"backward\"}}",
					Result: "<Schedule><type>weekly</type><week>2</week><reminder><value>P2D</value><businessDays>true</businessDays><shift>backward</shift></reminder></Schedule>",
					Force: false
				),
				(
					Actual: "{\"@type\":\"weekly\",\"@week\":2,\"reminder\":{\"@value\":\"P2D\",\"@businessDays\":true,\"@shift\":\"backward\"}}",
					Result: "<Schedule type=\"weekly\" week=\"2\"><reminder value=\"P2D\" businessDays=\"true\" shift=\"backward\"/></Schedule>",
					Force: false
				),
				(
					Actual: "{\"type\":\"weekly\",\"week\":2,\"reminder\":{\"@value\":\"P2D\",\"businessDays\":true,\"shift\":\"backward\"}}",
					Result: "<Schedule type=\"weekly\" week=\"2\"><reminder value=\"P2D\" businessDays=\"true\" shift=\"backward\"/></Schedule>",
					Force: true
				),
			};

			for (int i = 0; i < tables.Length; ++i)
			{
				var (source, target, force) = tables[i];

				var actual = JsonToXmlConverter.Convert(source, "Schedule", forceAttributes: force);
				Assert.IsNotNull(actual);
				XmlLiteNode expected = XmlLiteNode.FromXml(target, true);
				Assert.AreEqual(expected, actual, source);
			}
		}
	}
}
