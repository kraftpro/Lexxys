using Lexxys.Tokenizer;

namespace Lexxys.Tests.Tokenizer;

[TestClass]
public class TokenScannerTests
{
	private const int Equal = 1;
	private const int DoubleEqual = 2;

	[TestMethod]
	public void Next_UsesLongestSequenceBeforeShorterPrefix()
	{
		var scanner = new TokenScanner(new WhiteSpaceTokenRule(), new SequenceTokenRule((Equal, "="), (DoubleEqual, "==")));
		var stream = new CharStream("== =");

		var first = scanner.Next(ref stream);
		var second = scanner.Next(ref stream);

		Assert.IsTrue(first.Is(LexicalTokenType.SEQUENCE, DoubleEqual));
		Assert.IsTrue(second.Is(LexicalTokenType.SEQUENCE, Equal));
		Assert.AreEqual("==", first.GetString(stream));
		Assert.AreEqual("=", second.GetString(stream));
	}

	[TestMethod]
	public void TokenTypeIs_MatchesGroupAndItemSeparatesVariants()
	{
		var token = LexicalTokenType.Create(LexicalTokenType.SEQUENCE, DoubleEqual);

		Assert.IsTrue(token.Is(LexicalTokenType.SEQUENCE));
		Assert.IsTrue(token.Is(LexicalTokenType.SEQUENCE, DoubleEqual));
		Assert.IsFalse(token.Is(LexicalTokenType.SEQUENCE, Equal));
	}

	[TestMethod]
	public void OneBackFilter_ReturnsPreviousTokenAgain()
	{
		var back = new OneBackFilter();
		var scanner = new TokenScanner([back], new WhiteSpaceTokenRule(), new NameTokenRule());
		var stream = new CharStream("alpha beta");

		var first = scanner.Next(ref stream);
		back.Back();
		var replayed = scanner.Next(ref stream);
		var second = scanner.Next(ref stream);

		Assert.AreEqual(first.Position, replayed.Position);
		Assert.AreEqual(first.Length, replayed.Length);
		Assert.AreEqual("alpha", replayed.GetString(stream));
		Assert.AreEqual("beta", second.GetString(stream));
	}

	[TestMethod]
	public void IndentFilter_EmitsIndentAndUndentTokens()
	{
		var scanner = new TokenScanner([new IndentFilter()],
			new WhiteSpaceTokenRule(keepNewLine: true),
			new NameTokenRule());
		var stream = new CharStream("root\n\tchild\nnext");

		var tokens = ReadAll(scanner, ref stream);

		Assert.IsTrue(tokens.Exists(o => o.Is(LexicalTokenType.INDENT)));
		Assert.IsTrue(tokens.Exists(o => o.Is(LexicalTokenType.UNDENT)));
		Assert.AreEqual("root", tokens[0].GetString(stream));
		bool hasChild = false;
		foreach (var token in tokens)
		{
			hasChild |= token.Is(LexicalTokenType.IDENTIFIER) && token.GetString(stream) == "child";
		}
		Assert.IsTrue(hasChild);
	}

	private static List<LexicalToken> ReadAll(TokenScanner scanner, ref CharStream stream)
	{
		var result = new List<LexicalToken>();
		LexicalToken token;
		while (!(token = scanner.Next(ref stream)).IsEof)
		{
			result.Add(token);
		}
		return result;
	}
}
