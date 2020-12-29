// Lexxys Infrastructural library.
// file: StringTokenRule.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys.Tokenizer2
{
	public class StringTokenRule: LexicalTokenRule
	{
		public const char NoEscape = Char.MaxValue;

		public StringTokenRule(char escapeChar = '\0', LexicalTokenType tokenType = default)
		{
			EscapeChar = escapeChar == '\0' ? '\\': escapeChar;
			TokenType = tokenType == default ? LexicalTokenType.STRING: tokenType;
		}

		public char EscapeChar { get; }
		public LexicalTokenType TokenType { get; }
		public override string BeginningChars => "\"'";

		public override bool TestBeginning(char value)
		{
			return value == '"' || value == '\'';
		}

		public override LexicalToken TryParse(CharStream stream)
		{
			if (stream[0] != '"' && stream[0] != '\'')
				return null;
			return ParseString(TokenType, stream, EscapeChar);
		}

		public static LexicalToken ParseString(LexicalTokenType tokenType, CharStream stream, char escapeChar)
		{
			char c0 = stream[0];
			StringBuilder sb = new StringBuilder();
			int i = 1;
			int j0 = stream.IndexOf(c0, i);
			int j1 = escapeChar == NoEscape ? -1: stream.IndexOf(escapeChar, i);

			while (true)
			{
				int j = j0 < j1 ? j0: j1;
				if (j < 0)
				{
					if (j0 < 0)
						throw stream.SyntaxException(SR.EofInStringConstant());
					j = j0;
				}
				sb.Append(stream.Substring(i, j - i));
				if (j == j0)
				{
					if (stream[j + 1] != c0)
						return stream.Token(tokenType, j + 1, sb.ToString());
					i = j + 2;
					j0 = stream.IndexOf(c0, i);
					sb.Append(c0);
				}
				else
				{
					char ch = ParseEscape(stream, j + 1, out i);
					sb.Append(ch);
					j1 = stream.IndexOf(escapeChar, i);
					if (j0 < i)
						j0 = stream.IndexOf(c0, i);
				}
			}
		}

		private static char ParseEscape(CharStream stream, int position, out int next)
		{
			int k;
			int i = position;
			char c;
			switch (stream[i])
			{
				// C++ standard escape sequence: \[abfnrtv]
				case 'a':
					c = '\a';
					break;
				case 'b':
					c = '\b';
					break;
				case 'f':
					c = '\f';
					break;
				case 'n':
					c = '\n';
					break;
				case 'r':
					c = '\r';
					break;
				case 't':
					c = '\t';
					break;
				case 'v':
					c = '\v';
					break;

				// Ctrl+CHAR symbol: \c[A-Z]
				case 'c':
					c = stream[i++];
					if (c < 'A' || c > 'Z')
						throw stream.SyntaxException(SR.UnrecognizedEscapeSequence(c));
					c = (char)(c - 'A' + 1);
					break;

				// Octal value: \0[0-7]?[0-7]?[0-7]?
				case '0':
					k = 0;
					for (int j = 0; j < 3; ++j)
					{
						c = stream[i++];
						if (!(c >= '0' && c <= '7'))
						{
							--i;
							break;
						}
						k = k * 8 + (c - '0');
					}
					c = (char)k;
					break;

				// Hexadecimal unicode symbol: \[ux][0-9a-f]?[0-9a-f]?[0-9a-f]?[0-9a-f]?
				case 'x':
				case 'u':
					k = 0;
					for (int j = 0; j < 4; ++j)
					{
						c = stream[i++];
						int l;
						if (c >= '0' && c <= '9')
						{
							l = (c - '0');
						}
						else
						{
							c |= ' ';
							if (c >= 'a' && c <= 'f')
							{
								l = (c - 'a' + 10);
							}
							else
							{
								--i;
								break;
							}
						}
						k = k * 16 + l;
					}
					c = (char)k;
					break;

				default:
					c = stream[i];
					break;
			}
			next = i + 1;
			return c;
		}
	}
}
