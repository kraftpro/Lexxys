using System.Text.RegularExpressions;

namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	private static async Task AssertDump(ConfigNodeCollection nodes, string expected)
	{
		var actual = nodes.Dump(new DumpWriterOptions { FieldSeparator = ',' });
		await Assert.That(Trim(actual)).IsEqualTo(Trim(expected));
	}

	private static string Trim(string value)
		=> Regex.Replace(value, @"\s+", "");
}
