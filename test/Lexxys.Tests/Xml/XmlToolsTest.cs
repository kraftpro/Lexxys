// Lexxys Infrastructural library.
// file: XmlToolsTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Xml.Serialization;

using Lexxys.Xml;

namespace Lexxys.Tests.Xml
{
	[TestClass]
	public class XmlToolsTest
	{
		[TestMethod]
		public void TrySerializerTest()
		{
			string xml = """
				<root>
					<Id>123</Id>
					<Name>Name of item</Name>
				</root>
				""";
			var x = XmlTools.FromXml(xml);
			var success = XmlTools.TryGetValue<SerializeByAttrib>(x, out var result);
			Assert.IsTrue(success);
			Assert.AreEqual(123, result.Id);
			Assert.AreEqual("Name of item", result.Name);
		}

		[TestMethod]
		[DataRow("""<x><name>Name value</name><value>Value value</value></x>""")]
		[DataRow("""<x name="Name value" value="Value value"/>""")]
		[DataRow("""<x name="Name value"><value>Value value</value></x>""")]
		public void TryReflection1(string xml)
		{
			var x = XmlTools.FromXml(xml);
			var success = XmlTools.TryGetValue<NameValueClassProp>(x, out var result);
			Assert.IsTrue(success);
			Assert.AreEqual("Name value", result.Name);
			Assert.AreEqual("Value value", result.Value);
		}

		public class NameValueClassProp
		{
			// ReSharper disable UnusedAutoPropertyAccessor.Global
			public string Name { get; set; }
			public string Value { get; set; }
		}

		[TestMethod]
		[DataRow("""<x><name>Name value</name><value>Value value</value></x>""")]
		[DataRow("""<x name="Name value" value="Value value" />""")]
		[DataRow("""<x name="Name value" />""")]
		public void TryReflection2(string xml)
		{
			var x = XmlTools.FromXml(xml);
			var success = XmlTools.TryGetValue<NameValueClassConstruct>(x, out var result);
			Assert.IsTrue(success);
			Assert.AreEqual("Name value", result.Name);
			string expected = xml.Contains("<value") || xml.Contains("value=") ? "Value value": "default";
			Assert.AreEqual(expected, result.Value);
		}

		public record NameValueClassConstruct(string Name, string Value);

		[TestMethod]
		[DataRow("""<x><name>Name value</name><value>Value value</value></x>""")]
		[DataRow("""<x name="Name value" value="Value value" />""")]
		public void TryReflection3(string xml)
		{
			var x = XmlTools.FromXml(xml);
			var result = XmlTools.GetValue(x, new { Name = "", Value = "" });
			Assert.IsNotNull(result);
			Assert.AreEqual("Name value", result.Name);
			string expected = "Value value";
			Assert.AreEqual(expected, result.Value);
		}

		[TestMethod]
		[DataRow("""<x><name>Name value</name><value>One</value><value>Two</value><value>Three</value></x>""")]
		[DataRow("""<x name="Name value"><value>One</value><value>Two</value><value>Three</value></x>""")]
		[DataRow("""<x name="Name value"><value><item>One</item><item>Two</item><item>Three</item></value></x>""")]
		[DataRow("""<x name="Name value" value="One,Two,Three"/>""")]
		[DataRow("""<x name="Name value" value="One,Two,Three"><value>Four</value></x>""")]
		[DataRow("""<x name="Name value"><values><value>One</value><value>Two</value><value>Three</value></values></x>""")]
		public void TryReflection4_Arrays(string xml)
		{
			var x = XmlTools.FromXml(xml);
			var result = XmlTools.GetValue(x, new { Name = "", Value = Array.Empty<string>() });
			Assert.IsNotNull(result);
			Assert.AreEqual("Name value", result.Name);
			string expected = "One,Two,Three";
			Assert.AreEqual(expected, string.Join(",", result.Value));
		}

		[TestMethod]
		[DataRow("""<x><name>Name value</name><value>One</value><value>Two</value><value>Three</value></x>""")]
		[DataRow("""<x name="Name value"><value>One</value><value>Two</value><value>Three</value></x>""")]
		[DataRow("""<x name="Name value" value="One,Two,Three"/>""")]
		[DataRow("""<x name="Name value" value="One,Two"><value>Three</value></x>""")]
		[DataRow("""<x name="Name value" value="One"><values><value>Two</value><value>Three</value></values></x>""")]
		[DataRow("""<x name="Name value"><value><item>One</item><item>Two</item><item>Three</item></value></x>""")]
		public void TryReflection4_Lists(string xml)
		{
			var x = XmlTools.FromXml(xml);
			var result = XmlTools.GetValue(x, new { Name = "", Value = new List<string>() });
			Assert.IsNotNull(result);
			Assert.AreEqual("Name value", result.Name);
			string expected = "One,Two,Three";
			Assert.AreEqual(expected, string.Join(",", result.Value));
		}

	}

	[XmlRoot("root")]
	public class SerializeByAttrib
	{
		public string Name { get; set; }
		public int Id { get; set; }
	}
}
