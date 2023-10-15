// Lexxys Infrastructural library.
// file: WhiteSpaceTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

[Serializable]
public class WhiteSpaceTokenRule: LexicalTokenRule
{
	public WhiteSpaceTokenRule(LexicalTokenType whiteSpaceToken, LexicalTokenType newLineToken)
	{
		WhiteSpaceTokenType = whiteSpaceToken;
		NewLineTokenType = newLineToken;
	}

	public WhiteSpaceTokenRule(bool keepWhiteSpace = false, bool keepNewLine = false, bool keepNewLineCount = false)
		: this(
			keepWhiteSpace ? LexicalTokenType.WHITESPACE : LexicalTokenType.IGNORE, 
			keepNewLine ? keepNewLineCount ? LexicalTokenType.NEWLINE2: LexicalTokenType.NEWLINE : LexicalTokenType.IGNORE)
	{
	}

	public LexicalTokenType WhiteSpaceTokenType { get; }
	public LexicalTokenType NewLineTokenType { get; }

	public override string BeginningChars => "\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0a\x0b\x0c\x0d\x0e\x0f\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f\x20\x7f";

	public override bool HasExtraBeginning => true;

	public override bool TestBeginning(char value)
	{
		return Char.IsControl(value) || Char.IsWhiteSpace(value);
	}

	public override LexicalToken TryParse(ref CharStream stream)
	{
		int nl = 0;
		var rest = stream.Slice(0);
		int len;
		for (len = 0; len < rest.Length; ++len)
		{
			var c = rest[len];
			if (c > 255)
				if (Char.IsControl(c) || Char.IsWhiteSpace(c))
					continue;
				else
					break;
			if (c > ' ')
				if (c is >= '\x7f' and <= '\xa0')
					continue;
				else
					break;
			if (c == '\n')
				++nl;
			else if (c == '\r' && !(len + 1 < rest.Length && rest[len + 1] == '\n'))
				++nl;
		}
		if (len == 0)
			return LexicalToken.Empty;

		int position = stream.Position;
		stream.Forward(len);

		return
			NewLineTokenType == LexicalTokenType.IGNORE || nl == 0 ? new LexicalToken(WhiteSpaceTokenType, position, len):
			NewLineTokenType == LexicalTokenType.NEWLINE ? new LexicalToken(NewLineTokenType, position, len):
				new LexicalToken(NewLineTokenType, position, len, (_, _) => nl);
	}
}
