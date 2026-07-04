using System.Text;

namespace Lexxys.Tests;

[TestClass]
public class DumpWriterTests
{
	[TestMethod]
	public void DumpWriter_WritesNamedMembersToTheSuppliedTextWriter()
	{
		var text = new BufferingTextWriter();
		var writer = new DumpWriter(text, new DumpWriterOptions(DumpWriterOptions.Unlimited) { IncludeObjectType = false });

		writer.Begin().Write("Id", 7).Write("Name", "alpha").End();

		Assert.AreEqual("{Id=7;Name=\"alpha\"}", text.ToString());
	}

	[TestMethod]
	public void JsonDumpWriter_QuotesPropertyNames()
	{
		var writer = new JsonDumpStringWriter(DumpWriterOptions.Unlimited);

		writer.Begin().Write("full-name", 42).End();

		Assert.AreEqual("{\"full-name\":42}", writer.ToString());
	}

	[TestMethod]
	public void XmlDumpWriter_WritesPrimitiveMembersAsAttributes()
	{
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
		});

		writer.Begin().Write("Age", 7).Write("Name", "Bob").End();

		Assert.AreEqual("<object Age=\"7\" Name=\"Bob\"/>", writer.ToString());
	}

	[TestMethod]
	public void XmlDumpWriter_RespectsMaxDepthWhenWritingNestedObjects()
	{
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
			MaxDepth = 1,
		});

		writer.Write(null, new NestedNode
		{
			Child = new NestedNode
			{
				Child = new NestedNode(),
			},
		});

		Assert.AreEqual("<object><Child/></object>", writer.ToString());
	}

	[TestMethod]
	public void XmlDumpWriter_UsesNestedReferencePathsForRepeatedObjects()
	{
		var shared = new LeafNode();
		var writer = new XmlDumpStringWriter(new XmlDumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			UseAttributesForPrimitives = true,
		});

		writer.Write(null, new RootNode
		{
			Parent = new ParentNode { Child = shared },
			Alias = shared,
		});

		Assert.AreEqual("<object><Parent><Child/></Parent><Alias>$ref$:Parent.Child</Alias></object>", writer.ToString());
	}

	private sealed class BufferingTextWriter: ITextWriter
	{
		private readonly StringBuilder _buffer = new();

		public void Text(ReadOnlySpan<char> value) => _buffer.Append(value);

		public override string ToString() => _buffer.ToString();
	}

	private sealed class NestedNode
	{
		public NestedNode? Child { get; init; }
	}

	private sealed class RootNode
	{
		public ParentNode? Parent { get; init; }

		public LeafNode? Alias { get; init; }
	}

	private sealed class ParentNode
	{
		public LeafNode? Child { get; init; }
	}

	private sealed class LeafNode
	{
	}
}