using System.Text;

namespace Lexxys.Tests.Dump;

[TestClass]
public class XmlDumpWriterTests
{
	static string StripSpaces(string s) => string.Concat(s.Where(c => !char.IsWhiteSpace(c)));

	[TestMethod]
    public void SimpleObjectProducesXml()
    {
        var sb = new StringBuilder();
        var writer = Lexxys.XmlDumpWriter.Create(sb);
        writer.Write("person", new { Name = "Alice", Age = 30, Tags = new[] { "dev", "lead" } });
        var xml = sb.ToString();

		Assert.AreEqual(StripSpaces("<person Name=\"Alice\" Age=\"30\"><Tags><item>dev</item><item>lead</item></Tags></person>"), StripSpaces(xml));
    }

    [TestMethod]
    public void PrimitivesAndEscaping()
    {
        var sb = new StringBuilder();
        var writer = Lexxys.XmlDumpWriter.Create(sb);
        writer.Write("int", 42);
        writer.Write("text", "a & < > \" '");
        var xml = sb.ToString();

        Assert.AreEqual(StripSpaces("<int>42</int><text>a &amp; &lt; &gt; &quot; &apos;</text>"), StripSpaces(xml));
    }

    [TestMethod]
    public void ArrayProducesElements()
    {
        var sb = new StringBuilder();
        var writer = Lexxys.XmlDumpWriter.Create(sb);
        writer.Write("tags", new[] { "dev", "lead" });
        var xml = sb.ToString();

        Assert.AreEqual(StripSpaces("<tags><item>dev</item><item>lead</item></tags>"), StripSpaces(xml));
    }

    [TestMethod]
    public void NullProducesEmptyElement()
    {
        var sb = new StringBuilder();
        var writer = Lexxys.XmlDumpWriter.Create(sb);
        writer.Write("empty", (object)null);
        var xml = sb.ToString();

        Assert.AreEqual(StripSpaces("<empty/>"), StripSpaces(xml));
    }

    [TestMethod]
    public void BinaryProducesHexContent()
    {
        var sb = new StringBuilder();
        var writer = Lexxys.XmlDumpWriter.Create(sb);
        writer.Write("bin", new byte[] { 0xAB, 0xCD });
        var xml = sb.ToString();

        Assert.AreEqual(StripSpaces("<bin>0xABCD</bin>"), StripSpaces(xml));
    }
}
