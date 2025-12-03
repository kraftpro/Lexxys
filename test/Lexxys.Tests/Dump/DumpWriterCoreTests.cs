using System.Text;

#nullable enable

namespace Lexxys.Tests.Dump;

[TestClass]
public class DumpWriterCoreTests
{
	[TestMethod]
	public void PlainWriter_WritesNestedObjectsAndArrays()
	{
		var writer = new DumpStringWriter(new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});

		writer.Write(null, new SampleObject
		{
			Name = "alpha",
			Numbers = [1, 2, 3],
		});

		var text = writer.ToString();
		Assert.IsTrue(text.StartsWith('{'));
		Assert.IsTrue(text.EndsWith('}'));
		Assert.Contains("Name=\"alpha\"", text);
		Assert.Contains("Numbers=[1,2,3]", text);
	}

	[TestMethod]
	public void PlainWriter_RespectsStringAndArrayLimits()
	{
		var options = new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
			StringMaxLength = 4,
			ArrayMaxLength = 2,
			MaxDepth = 16,
			MaxLength = 1024,
			BinaryMaxLength = 1024,
		};
		var writer = new DumpStringWriter(options);

		writer.Begin()
			.Write("Text", "abcdef")
			.Write("Items", new[] { 1, 2, 3 })
			.End();

		Assert.AreEqual($"{{Text=\"abcd{options.EllipsisString}\";Items=[1,2,{options.EllipsisString}]}}", writer.ToString());
	}

	[TestMethod]
	public void PlainWriter_WritesReferenceForRepeatedClassInstance()
	{
		var shared = new Leaf();
		var writer = new DumpStringWriter(new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});

		writer.Write(null, new ReferenceRoot
		{
			First = shared,
			Second = shared,
		});

		Assert.AreEqual("{First={};Second=($ref=#First)}", writer.ToString());
	}

	[TestMethod]
	public void DumpWriter_ForwardsToCustomTextWriter()
	{
		var text = new BufferingTextWriter();
		var writer = new DumpWriter(text, new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});

		writer.Begin().Write("Id", 7).Write("Name", "alpha").End();

		Assert.AreEqual("{Id=7;Name=\"alpha\"}", text.ToString());
	}

	private sealed class SampleObject
	{
		public string? Name { get; init; }

		public int[]? Numbers { get; init; }
	}

	private sealed class ReferenceRoot
	{
		public Leaf? First { get; init; }

		public Leaf? Second { get; init; }
	}

	private sealed class Leaf
	{
	}

	private sealed class BufferingTextWriter: ITextWriter
	{
		private readonly StringBuilder _buffer = new();

		public void Text(ReadOnlySpan<char> value) => _buffer.Append(value);

		public override string ToString() => _buffer.ToString();
	}
}
