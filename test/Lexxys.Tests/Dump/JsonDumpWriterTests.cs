namespace Lexxys.Tests.Dump;

#nullable enable

[TestClass]
public class JsonDumpWriterTests
{
	[TestMethod]
	public void JsonWriter_WritesObjectWithQuotedNames()
	{
		var writer = new JsonDumpStringWriter(DumpWriterOptions.Unlimited);

		writer.Begin().Write("full-name", "Ada").Write("age", 37).End();

		Assert.AreEqual("{\"full-name\":\"Ada\",\"age\":37}", writer.ToString());
	}

	[TestMethod]
	public void JsonWriter_WritesArraysAndNullValues()
	{
		var writer = new JsonDumpStringWriter(DumpWriterOptions.Unlimited);

		writer.Begin()
			.Write("items", new object?[] { 1, null, "x" })
			.Write("missing", (object?)null)
			.End();

		Assert.AreEqual("{\"items\":[1,null,\"x\"],\"missing\":null}", writer.ToString());
	}

	[TestMethod]
	public void JsonWriter_EscapesStringValues()
	{
		var writer = new JsonDumpStringWriter(DumpWriterOptions.Unlimited);

		writer.Begin().Write("text", "a \"quote\" and \n line").End();

		Assert.AreEqual("{\"text\":\"a \\\"quote\\\" and \\n line\"}", writer.ToString());
	}

	[TestMethod]
	public void JsonWriter_WritesBinaryAsBase64String()
	{
		var writer = new JsonDumpStringWriter(DumpWriterOptions.Unlimited);

		writer.Begin().Write("bin", new byte[] { 1, 2, 3, 4 }).End();

		Assert.AreEqual("{\"bin\":\"AQIDBA==\"}", writer.ToString());
	}
}
