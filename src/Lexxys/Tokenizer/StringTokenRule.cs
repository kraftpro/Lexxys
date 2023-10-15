// Lexxys Infrastructural library.
// file: StringTokenRule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys.Tokenizer;

[Serializable]
public class StringTokenRule: LexicalTokenRule
{
	public const char NoEscape = Char.MaxValue;

	public StringTokenRule(char escapeChar = '\0', LexicalTokenType? tokenType = default)
	{
		EscapeChar = escapeChar == '\0' ? '\\': escapeChar;
		TokenType = tokenType == default ? LexicalTokenType.STRING: tokenType;
	}

	public char EscapeChar { get; }
	public LexicalTokenType TokenType { get; }
	public override string BeginningChars => "\"'";

	public override bool TestBeginning(char value) => value is '"' or '\'';

	public override LexicalToken TryParse(ref CharStream stream)
		=> stream[0] is '"' or '\'' ? ParseString(TokenType, ref stream, EscapeChar): LexicalToken.Empty;

	public static LexicalToken ParseString(LexicalTokenType tokenType, ref CharStream stream, char escapeChar)
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
			sb.Append(stream.Slice(i, j - i));
			if (j == j0)
			{
				if (stream[j + 1] != c0)
				{
					LexicalToken result;
					if (sb.Length == j - i)
					{
						result = new LexicalToken(tokenType, stream.Position + i, j - i);
					}
					else
					{
						string value = sb.ToString();
						result = new LexicalToken(tokenType, stream.Position + i, j - i, (_, _) => value);
					}
					stream.Forward(j + 1);
					return result;
				}
				i = j + 2;
				j0 = stream.IndexOf(c0, i);
				sb.Append(c0);
			}
			else
			{
				char ch = ParseEscape(ref stream, j + 1, out i);
				sb.Append(ch);
				j1 = stream.IndexOf(escapeChar, i);
				if (j0 < i)
					j0 = stream.IndexOf(c0, i);
			}
		}
	}

	private static char ParseEscape(ref CharStream stream, int position, out int next)
	{
		int k;
		int i = position;
		int j;
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
			case 'e':
				c = '\x18';
				break;
			case 'N':
				c = '\x85';
				break;
			case '_':
				c = '\xA0';
				break;
			case 'L':
				c = '\u2028';
				break;
			case 'P':
				c = '\u2029';
				break;

			// Ctrl+CHAR symbol: \c[A-Z]
			case 'c':
				c = stream[++i];
				if (c is < 'A' or > 'Z')
					throw stream.SyntaxException(SR.UnrecognizedEscapeSequence(c));
				c = (char)(c - 'A' + 1);
				break;

			// Octal value: \0[0-7]?[0-7]?[0-7]?
			case '0':
				k = 0;
				for (j = 0; j < 3; ++j)
				{
					c = stream[++i];
					if (c is not (>= '0' and <= '7'))
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
				for (j = 0; j < 4; ++j)
				{
					c = stream[++i];
					int l;
					if (c is >= '0' and <= '9')
					{
						l = (c - '0');
					}
					else
					{
						c |= ' ';
						if (c is >= 'a' and <= 'f')
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
				if (j == 0)
					throw stream.SyntaxException(SR.UnrecognizedEscapeSequence(stream.Substring(0, 2)));
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
