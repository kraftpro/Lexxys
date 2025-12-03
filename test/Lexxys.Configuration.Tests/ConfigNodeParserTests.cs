using System.Text.RegularExpressions;

namespace Lexxys.Configuration.Tests;

public class ConfigNodeParserTests
{

	[Test]
	[MethodDataSource(nameof(GetTestCases))]
	public async Task Parse_ConfigString_CreatesExpectedNodeStructure(int caseId, string source, string expected)
	{
		var nodes = ConfigNodeParser.ParseCollection(source);
		var actual = nodes.Dump(new DumpWriterOptions { FieldSeparator = ',' });
		await Assert.That(Trim(actual)).IsEqualTo(Trim(expected));
	}

	private static string Trim(string value)
		=> Regex.Replace(value, @"\s+", "");

	public static IEnumerable<(int, string, string)> GetTestCases()
	{
		yield return (1,
			"""
			value1					# 1
			value2,					# 2
			( item3.1, item3.2 item3.3 ), # 3
			value` 4 (				# 4
				key4.1: value4.1,
				value4.2 ( key4.2.1: value4.2.1 key4.2.2 = value4.2.2 )
				key4.3 : value4.3, ( key4.3.1 = value4.3.1 key4.3.2 = value4.3.2 )
			)
			key4.1_ref: !${{key4.1}}!
			""",
			"""
			(
			  "value1",
			  "value2",
			  [ "item3.1", "item3.2", "item3.3" ],
			  value 4 = (
			    key4.1 = "value4.1",
				value4.2= { key4.2.1 = "value4.2.1", key4.2.2 = "value4.2.2" },
				key4.3 = "value4.3",
			    { key4.3.1 = "value4.3.1", key4.3.2 = "value4.3.2" }
			  ),
			  key4.1_ref = "!value4.1!"
			)
			"""
			);
		yield return (2,
			"""
			key1 = (value1,,value3)
			key2: (value1::value2)
			""",
			"""
			{
				key1= ["value1",null,"value3"],
				key2= ["value1::value2"]
			}
			"""
			);
	}
}
