// Lexxys Infrastructural library.
// file: WhiteSpaceTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.Tokenizer
{
	public class WhiteSpaceTokenRule: LexicalTokenRule
	{
		public WhiteSpaceTokenRule(LexicalTokenType whiteSpaceToken, LexicalTokenType newLineToken)
		{
			WhiteSpaceTokenType = whiteSpaceToken;
			NewLineTokenType = newLineToken;
		}

		public WhiteSpaceTokenRule(bool keepWhiteSpace = false, bool keepNewLine = false)
			: this(keepWhiteSpace ? LexicalTokenType.WHITESPACE : LexicalTokenType.IGNORE, keepNewLine ? LexicalTokenType.NEWLINE : LexicalTokenType.IGNORE)
		{
		}

		public LexicalTokenType WhiteSpaceTokenType { get; }
		public LexicalTokenType NewLineTokenType { get; }

		public override string BeginningChars => "\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F \x7F";

		public override bool HasExtraBeginning => true;

		public override bool TestBeginning(char value)
		{
			return Char.IsWhiteSpace(value) || Char.IsControl(value);
		}

		public override LexicalToken? TryParse(CharStream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			int nl = 0;
			var len = stream.Match(Space);
			if (len == 0)
				return null;

			string text = stream.Substring(0, len);
			int position = stream.Position;
			stream.Forward(len);

#pragma warning disable CA1508 // Avoid dead conditional code.  Value of nl increased in Space fragment.
			return NewLineTokenType == LexicalTokenType.IGNORE || nl == 0 ?
				new LexicalToken(WhiteSpaceTokenType, text, position, stream.CultureInfo):
				new LexicalToken(NewLineTokenType, text, position, nl, stream.CultureInfo);
#pragma warning restore CA1508

			bool Space(char c)
			{
				if (c != '\n')
					return c <= 127 ? c <= ' ' : Char.IsWhiteSpace(c) || Char.IsControl(c);
				++nl;
				return true;
			}
		}
	}
}


