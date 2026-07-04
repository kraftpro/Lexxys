using Lexxys.Tokenizer;

namespace Lexxys.Tests.Tokenizer;

[TestClass]
public class StringTokenRuleTests
{
	[TestMethod]
	public void TestConstructor_Default()
	{
		var rule = new StringTokenRule();
		Assert.AreEqual('\\', rule.EscapeChar);
		Assert.AreEqual(LexicalTokenType.STRING, rule.TokenType);
	}

	[TestMethod]
	public void TestConstructor_WithParameters()
	{
		var rule = new StringTokenRule(LexicalTokenType.IDENTIFIER, '#', s => s.ToUpper(), ("${", "}"));
		Assert.AreEqual('#', rule.EscapeChar);
		Assert.AreEqual(LexicalTokenType.IDENTIFIER, rule.TokenType);
	}

	[TestMethod]
	public void TestBeginningChars()
	{
		var rule = new StringTokenRule();
		Assert.AreEqual("\"'", rule.BeginningChars);
	}

	[TestMethod]
	public void TestTestBeginning()
	{
		var rule = new StringTokenRule();
		Assert.IsTrue(rule.TestBeginning('"'));
		Assert.IsTrue(rule.TestBeginning('\''));
		Assert.IsFalse(rule.TestBeginning('a'));
	}

	[TestMethod]
	public void TestTryParse()
	{
		var rule = new StringTokenRule();
		var stream = new CharStream("\"test\"");
		var token = rule.TryParse(ref stream);
		Assert.AreEqual(LexicalTokenType.STRING, token.TokenType);
		Assert.AreEqual("test", token.GetString(stream));
	}

	[TestMethod]
	[DataRow("", "")]
	[DataRow("test", "test")]
	public void TestParseString(string text, string expected)
	{
		var stream = new CharStream("\"" + text + "\"");
		var token = StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '\\');
		Assert.AreEqual(LexicalTokenType.STRING, token.TokenType);
		Assert.AreEqual(expected, token.GetValue(stream));
	}

	[TestMethod]
	[DataRow("`r`n`a`b`v`f`x91`cA`0", "\r\n\a\b\v\f\x91\x01\0")]
	[DataRow("`xA01x`xf1A2`uAbcD", "\x00A01x\xF1A2\uABCD")]
	public void TestParseEscape(string text, string expected)
	{
		var stream = new CharStream("\"" + text + "\"");
		var token = StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '`');
		Assert.AreEqual(expected, token.GetValue(stream));
	}

	[TestMethod]
	[DataRow(".${{Upper}}.${{Lower}}.", ".UPPER.Lower.")]
	[DataRow("--`$${{xx`}}--}}--", "--$(xx}}--)--")]
	[DataRow("--``${{xx``}}--", "--`[Xx]--")]
	public void TestTemplate(string text, string expected)
	{
		Func<string, string> macro = s => s switch
		{
			"Upper" => "UPPER",
			"Lower" => "Lower",
			"xx`" => "[Xx]",
			_ => "(" + s + ")"
		};
		var stream = new CharStream("\"" + text + "\"");
		var token = StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '`', macro, ("${{", "}}"));
		Assert.AreEqual(expected, token.GetValue(stream));
	}

	[TestMethod]
	[DataRow("\"simple string\"", "simple string")]
	[DataRow("'another string'", "another string")]
	[DataRow("\"string with \\\"escaped quotes\\\"\"", "string with \"escaped quotes\"")]
	[DataRow("\"string with \\n new line\"", "string with \n new line")]
	public void TestParseStringWithEscapes(string input, string expected)
	{
		var stream = new CharStream(input);
		var token = StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '\\');
		Assert.AreEqual(expected, token.GetValue(stream));
	}

	[TestMethod]
	[DataRow("\"string with ${macro}\"", "string with MACRO")]
	[DataRow("\"another ${macro} example\"", "another MACRO example")]
	public void TestParseStringWithMacro(string input, string expected)
	{
		Func<string, string> macro = s => s == "macro" ? "MACRO": s;
		var stream = new CharStream(input);
		var token = StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '\\', macro, ("${", "}"));
		Assert.AreEqual(expected, token.GetValue(stream));
	}

	[TestMethod]
	[DataRow("\"unterminated string")]
	[DataRow("\"string with invalid \\c1 escape\"")]
	[DataRow("\"string with invalid \\u12 escape\"")]
	public void TestParseStringWithErrors(string input)
	{
		Assert.ThrowsExactly<SyntaxException>(() => ParseString(input));
	}

	private static void ParseString(string input)
	{
		var stream = new CharStream(input);
		StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, '\\');
	}
}
