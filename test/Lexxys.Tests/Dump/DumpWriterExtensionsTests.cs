namespace Lexxys.Tests.Dump;

[TestClass]
public class DumpWriterExtensionsTests
{
	[TestMethod]
	public void DumpExtension_WrapsRegularDumpObjects()
	{
		var result = new RegularDump().Dump(new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});

		Assert.AreEqual("{Value=42}", result);
	}

	[TestMethod]
	public void DumpExtension_DoesNotWrapDumpValueObjects()
	{
		var result = new ScalarDump().Dump(new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});

		Assert.AreEqual("\"forty-two\"", result);
	}

	[TestMethod]
	public void FieldExtension_UsesCallerArgumentExpressionAsName()
	{
		var writer = new DumpStringWriter(new DumpWriterOptions(DumpWriterOptions.Unlimited)
		{
			IncludeObjectType = false,
		});
		int count = 3;

		writer.Begin().Field(count).End();

		Assert.AreEqual("{count=3}", writer.ToString());
	}

	private sealed class RegularDump: IDump
	{
		public void DumpContent(IDumpWriter writer) => writer.Write("Value", 42);
	}

	private sealed class ScalarDump: IDumpValue
	{
		public void DumpContent(IDumpWriter writer) => writer.Write("forty-two");
	}
}
