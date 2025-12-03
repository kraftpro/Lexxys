using System.Xml.Linq;

#nullable enable

namespace Lexxys.Tests.Dump;

[TestClass]
public class XmlDumpWriterBehaviorTests
{
	[TestMethod]
	public void XmlWriter_WritesPrimitivePropertiesAsAttributes()
	{
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
		});

		writer.Write("person", new Person { Name = "Alice", Age = 30 });

		var person = XElement.Parse(writer.ToString());
		Assert.AreEqual("person", person.Name.LocalName);
		Assert.AreEqual("Alice", person.Attribute("Name")?.Value);
		Assert.AreEqual("30", person.Attribute("Age")?.Value);
	}

	[TestMethod]
	public void XmlWriter_WritesArraysAsItemElements()
	{
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
		});

		writer.Write("tags", new[] { "dev", "lead" });

		Assert.AreEqual("<tags><item>dev</item><item>lead</item></tags>", writer.ToString());
	}

	[TestMethod]
	public void XmlWriter_EncodesElementNamesAndEscapesValues()
	{
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = false,
		});

		writer.Begin(name: "bad name").Write("text value", "a & < > \" '").End();

		var xml = writer.ToString();
		var root = XElement.Parse(xml);

		Assert.AreEqual("bad_x0020_name", root.Name.LocalName);
		Assert.AreEqual("a & < > \" '", root.Element("text_x0020_value")?.Value);
	}

	[TestMethod]
	public void XmlWriter_WritesReferencesForRepeatedObjects()
	{
		var shared = new Leaf();
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
		});

		writer.Write(null, new ReferenceRoot
		{
			Parent = new Parent { Child = shared },
			Alias = shared,
		});

		Assert.AreEqual("<Object><Parent><Child/></Parent><Alias>$ref$:Parent.Child</Alias></Object>", writer.ToString());
	}

	private sealed class Person
	{
		public string? Name { get; init; }

		public int Age { get; init; }
	}

	private sealed class ReferenceRoot
	{
		public Parent? Parent { get; init; }

		public Leaf? Alias { get; init; }
	}

	private sealed class Parent
	{
		public Leaf? Child { get; init; }
	}

	private sealed class Leaf
	{
	}
}
