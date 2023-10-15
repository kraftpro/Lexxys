// Lexxys Infrastructural library.
// file: XmlLiteNodeTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Xml;
using System.Xml.XPath;

using Lexxys.Xml;

namespace Lexxys.Tests.Xml
{
	/// <summary>
	///This is a test class for XmlLiteNodeTest and is intended
	///to contain all XmlLiteNodeTest Unit Tests
	///</summary>
	[TestClass()]
	public class XmlLiteNodeTest
	{
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

		#region Data Tables
		private static readonly string[] Xml =
		[
			"""
			<root>
				Root Value1
				<node1 a='a1' b='b1'>
					Node1 Value
				</node1>
				Root Value2
				<node2 c='c2' d='d2' e='' />
				Root Value3
			</root>
			"""
		];
		#endregion

		/// <summary>
		///A test for XmlLiteNode Constructor
		///</summary>
		[TestMethod()]
		public void XmlLiteNodeXmlReaderConstructorTest()
		{
			var rdr = XmlReader.Create(new StringReader(Xml[0]));
			var target = XmlTools.FromXml(rdr, ignoreCase: false);
			Assert.AreEqual("root", target.Name);
			Assert.AreEqual("\n\tRoot Value1\n\tRoot Value2\n\tRoot Value3", target.Value);
			Assert.AreEqual(0, target.Attributes.Count);
			Assert.AreEqual(2, target.Elements.Count);
			Assert.AreEqual(2, target.Elements[0].Attributes.Count);

			Assert.AreEqual("node1", target.Elements[0].Name);
			Assert.AreEqual("\n\t\tNode1 Value", target.Elements[0].Value);
			Assert.AreEqual("a1", target.Elements[0]["a"]);
			Assert.AreEqual("b1", target.Elements[0]["b"]);
			Assert.AreEqual(0, target.Elements[0].Elements.Count);

			Assert.AreEqual("node2", target.Elements[1].Name);
			Assert.AreEqual("", target.Elements[1].Value);
			Assert.AreEqual(3, target.Elements[1].Attributes.Count);
			Assert.AreEqual("c2", target.Elements[1]["c"]);
			Assert.AreEqual("d2", target.Elements[1]["d"]);
			Assert.AreEqual("", target.Elements[1]["e"]);
			Assert.AreEqual(0, target.Elements[1].Elements.Count);

			Assert.IsNull(target.Elements[0]["A"]);
			Assert.IsNull(target.Elements[0]["c"]);
			Assert.IsNull(target.Elements[1]["C"]);
		}

		/// <summary>
		///A test for XmlLiteNode Constructor
		///</summary>
		[TestMethod()]
		public void XmlLiteNodeXPathNavigatorConstructorTest()
		{
			bool ignoreCase = false;
			do
			{
				for (int i = 0; i < Xml.Length; ++i)
				{
					IXmlReadOnlyNode expected;
					IXmlReadOnlyNode actual;
					using (var rdr = XmlReader.Create(new StringReader(Xml[i])))
						expected = XmlTools.FromXml(rdr, ignoreCase);

					using (var rdr = XmlReader.Create(new StringReader(Xml[i])))
						actual = XmlLiteNode.FromXml(new XPathDocument(rdr).CreateNavigator(), ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

					Assert.AreEqual(expected.ToString(), actual.ToString());
				}
				ignoreCase = !ignoreCase;
			} while (ignoreCase);
		}

		/// <summary>
		///A test for IsCaseSensitive
		///</summary>
		[TestMethod()]
		public void IsCaseSensitiveTest()
		{
			var c1 = XmlTools.FromXml(XmlReader.Create(new StringReader(Xml[0])), true);
			var c2 = XmlTools.FromXml(XmlReader.Create(new StringReader(Xml[0])), false);
			Assert.AreEqual(c1, c2);
			Assert.AreEqual(StringComparer.OrdinalIgnoreCase, c1.Comparer);
			Assert.AreEqual(StringComparer.Ordinal, c2.Comparer);

			Assert.AreEqual("a1", c1.Elements[0]["a"]);
			Assert.AreEqual("b1", c1.Elements[0]["b"]);
			Assert.AreEqual("a1", c1.Elements[0]["A"]);
			Assert.AreEqual("b1", c1.Elements[0]["B"]);

			Assert.AreEqual("a1", c2.Elements[0]["a"]);
			Assert.AreEqual("b1", c2.Elements[0]["b"]);
			Assert.IsNull(c2.Elements[0]["A"]);
			Assert.IsNull(c2.Elements[0]["B"]);

			Assert.IsFalse(c1.Element("node1").IsEmpty);
			Assert.IsFalse(c1.Element("node2").IsEmpty);
			Assert.IsFalse(c1.Element("Node1").IsEmpty);
			Assert.IsFalse(c1.Element("NODE2").IsEmpty);

			Assert.IsFalse(c2.Element("node1").IsEmpty);
			Assert.IsFalse(c2.Element("node2").IsEmpty);
			Assert.IsTrue(c2.Element("Node1").IsEmpty);
			Assert.IsTrue(c2.Element("NODE2").IsEmpty);
		}
	}
}
