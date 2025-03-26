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
	public const char Nil = '\0';
	private readonly Func<string, string>? _macro;
	private (string? Start, string? End) _template;

	public StringTokenRule(char escapeChar, Func<string, string>? macro = default, (string? Start, string? End) template = default)
		: this(LexicalTokenType.STRING, escapeChar, macro, template)
	{
	}

	public StringTokenRule(LexicalTokenType? tokenType = default, char escapeChar = '\\', Func<string, string>? macro = default, (string? Start, string? End) template = default)
	{
		EscapeChar = escapeChar;
		TokenType = tokenType == default ? LexicalTokenType.STRING: tokenType;
		_macro = macro;
		_template = template.Start is { Length: >0 } && template.End is { Length: >0 } ? template: default;
	}

	public char EscapeChar { get; }
	public LexicalTokenType TokenType { get; }
	public override string BeginningChars => "\"'";

	public override bool TestBeginning(char value) => value is '"' or '\'';

	public override LexicalToken TryParse(ref CharStream stream)
		=> stream[0] is '"' or '\'' ? ParseString(TokenType, ref stream, EscapeChar, _macro, _template): LexicalToken.Empty;

	public static LexicalToken ParseString(LexicalTokenType tokenType, ref CharStream stream, char escapeChar, Func<string, string>? macro = null, (string? Start, string? End) template = default)
	{
		if (macro == null || !(template.Start is { Length: >0 } && template.End is { Length: >0 }))
			template = default;
		char c0 = stream[0];
		var sb = new StringBuilder();
		int i = 1;
		int j0 = stream.IndexOf(c0, i);
		int j1 = escapeChar == Nil ? -1: stream.IndexOf(escapeChar, i);
		int j2 = template.Start == null ? -1: stream.IndexOf(template.Start, i);

		if (j0 < 0)
			throw stream.SyntaxException(SR.EofInStringConstant());
		if ((j1 < 0 || j1 > j0) && (j2 < 0 || j2 > j0) && (escapeChar != Nil || stream[j0 + 1] != c0))
			return stream.Token(tokenType, j0 + 1, (t, s) => s[1..^1].ToString());

		while (true)
		{
			if (j0 < 0)
				throw stream.SyntaxException(SR.EofInStringConstant());
			var j = j0;
			if (j1 >= 0 && j1 < j)
				j = j1;
			if (j2 >= 0 && j2 < j)
				j = j2;
			sb.Append(stream.Slice(i, j - i));
			if (j == j0)    // end of string
			{
				if (escapeChar == Nil && stream[j + 1] == c0)   // double quote
				{
					i = j + 2;
					j0 = stream.IndexOf(c0, i);
					sb.Append(c0);
				}
				else
				{
					string value = sb.ToString();
					return stream.Token(tokenType, j + 1, (_, _) => value);
				}
			}
			else if (j == j1)   // escape sequence
			{
				i = j + 1;
				char ch = ParseEscape(stream.Slice(i, Math.Min(stream.Length - i, 5)), out var len);
				if (len < 0)
					throw stream.SyntaxException(SR.UnrecognizedEscapeSequence(stream.Substring(i, -len)));
				i += len;
				sb.Append(ch);
				j1 = stream.IndexOf(escapeChar, i);
				if (j0 < i)
					j0 = stream.IndexOf(c0, i);
				if (j2 >= 0 && j2 < i)
					j2 = stream.IndexOf(template.Start, i);
			}
			else // begin macro
			{
				i = j + template.Start!.Length;
				var k = stream.IndexOf(template.End, i);
				while (IsEscaped(stream, escapeChar, k))
					k = stream.IndexOf(template.End, k + 1);
				var k0 = stream.IndexOf(c0, i);
				while (IsEscaped(stream, escapeChar, k0))
					k0 = stream.IndexOf(c0, k0 + 1);
				if (k < 0 || k0 < k)
				{
					sb.Append(template.Start);
					j2 = -1;
					continue;
				}
				sb.Append(macro!(UnEscape(stream.Slice(i, k - i), escapeChar)));
				i = k + template.End!.Length;
				if (j0 < i)
					j0 = stream.IndexOf(c0, i);
				if (j1 >= 0 && j1 < i)
					j1 = stream.IndexOf(escapeChar, i);
				j2 = stream.IndexOf(template.Start, i);
			}
		}

		static bool IsEscaped(in CharStream stream, char escape, int index) => index > 0 && stream[index - 1] == escape && !IsEscaped(stream, escape, index - 1);
	}

	public static string UnEscape(ReadOnlySpan<char> value, char escapeChar)
	{
		int j = value.IndexOf(escapeChar);
		if (j < 0)
			return value.ToString();
		var sb = new StringBuilder();

		do
		{
			sb.Append(value[..j]);
			value = value.Slice(j + 1);
			char ch = ParseEscape(value, out var len);
			if (len < 0)
			{
				len = -len;
				sb.Append(escapeChar).Append(value.Slice(0, len));
			}
			sb.Append(ch);
			value = value.Slice(len);
			j = value.IndexOf(escapeChar);
		} while (j >= 0);
		sb.Append(value);
		return sb.ToString();
	}

	public static char ParseEscape(ReadOnlySpan<char> value, out int length)
	{
		if (value.Length == 0)
		{
			length = 0;
			return '\0';
		}	
		char c;
		length = 1;
		c = value[0];
		switch (c)
		{
			// escape sequence: \[abfnrtv] \[eN_LP] \c[A-Z] \0 \xHHHH \uHHHH 
			case 'a':
				return '\a';
			case 'b':
				return '\b';
			case 'f':
				return '\f';
			case 'n':
				return '\n';
			case 'r':
				return '\r';
			case 't':
				return '\t';
			case 'v':
				return '\v';
			case 'e':
				return '\x18';
			case 'N':
				return '\x85';
			case '_':
				return '\xA0';
			case 'L':
				return '\u2028';
			case 'P':
				return '\u2029';
			case '0':
				return '\0';

			// Ctrl+CHAR symbol: \c[A-Z]
			case 'c':
				if (value.Length < 2)
				{
					length = -value.Length;
					return '\0';
				}
				c = value[1];
				if (c is < 'A' or > 'Z')
				{
					length = -2;
					return '\0';
				}
				length = 2;
				return (char)(c - 'A' + 1);

			// Hexadecimal symbol: \x[0-9a-f][0-9a-f]([0-9a-f][0-9a-f])?
			case 'x':
				if (value.Length < 3)
				{
					length = -value.Length;
					return '\0';
				}
				var h1 = HexPair(value[1], value[2]);
				if (h1 < 0)
				{
					length = -3;
					return '\0';
				}
				var h2 = value.Length < 5 ? -1: HexPair(value[3], value[4]);
				if (h2 < 0)
				{
					length = 3;
					return (char)h1;
				}
				else
				{
					length = 5;
					return (char)((h1 << 8) + h2);
				}

			// Hexadecimal unicode symbol: \u[0-9a-f][0-9a-f][0-9a-f][0-9a-f]
			case 'u':
				if (value.Length < 5)
				{
					length = -value.Length;
					return '\0';
				}
				var h11 = HexPair(value[1], value[2]);
				if (h11 < 0)
				{
					length = -5;
					return '\0';
				}	
				var h12 = HexPair(value[3], value[4]);
				if (h12 < 0)
				{
					length = -5;
					return '\0';
				}
				length = 5;
				return (char)((h11 << 8) + h12);

			default:
				return c;
		}

		static int HexPair(char c1, char c2)
		{
			int i = HexDigit(c1);
			if (i < 0)
				return -1;
			int j = HexDigit(c2);
			if (j < 0)
				return -1;
			return (i << 4) + j;
		}

		static int HexDigit(char c) => c switch
		{
			>= '0' and <= '9' => c - '0',
			>= 'a' and <= 'f' => c - 'a' + 10,
			>= 'A' and <= 'F' => c - 'A' + 10,
			_ => -1
		};
	}
}
