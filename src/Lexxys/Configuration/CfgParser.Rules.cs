using Lexxys.Tokenizer;
using Lexxys.Xml;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Configuration;

public ref partial struct CfgParser
{
	public CfgParser(string? sourceName, TextToXmlOptionHandler? optionHandler = null, MacroSubstitution? macro = null)
	{
		_sourceName = sourceName;
		_syntaxRules = new SyntaxRuleCollection();
		_optionHandler = optionHandler;
		_macro = macro ?? (o => o);
		_options = new ConfigOptions();
		_nodePath = new List<string>(4);

		var options = _options;
		Func<string, ResultValue<string>> Macro = o => GetVarValue(o, options);

		_back = new OneBackFilter();

		_nodeScanner = new TokenScanner([new IndentFilter(), _back],
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new InlineTextTokenRule(),
			new SequenceTokenRule(TOKEN,
				(LINE, ".."),           // continue of value mark
				(DASH, "-"),            // predefined node name mark
				(PARAMETER, "("),       // parameters list mark
				(OBJECT, "{"),          // object mark
				(ARRAY, "["),           // array mark
				(ENDNODE, "/"),         // end node mark
				(OPTION, "%")),         // option mark (not affected to indentation)
			new NodeNameRule(LexicalTokenType.IDENTIFIER, Macro));

		_nodeValueScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new InlineTextTokenRule(),
			new SequenceTokenRule(TOKEN,
				(PARAMETER, "("),               // begin of parameters list
				(OBJECT, "{"),                  // begin of parameters list
				(ARRAY, "[")),                  // begin of array
			new PlainValueTokenRule());

		_paramValueScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new StringTokenRule('`'),
			new InlineTextTokenRule(),
			new PlainValueTokenRule());

		_objectScanner = new TokenScanner(
			new WhiteSpaceTokenRule(),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(LexicalTokenType.IGNORE, CommaSemicolon),
			new SequenceTokenRule(TOKEN,
				(ATTRIB, ":"),
				(EQUAL, "="),
				(PARAMETER, "("),               // begin of parameters list
				(OBJECT, "{"),                  // begin of parameters list
				(ARRAY, "[")),                  // begin of array
			new SequenceTokenRule(TOKEN, (END, ")")) { RuleName = "Param" },
			new SequenceTokenRule(TOKEN, (END, "}")) { RuleName = "Object" },
			new NodeNameRule(TEXT, Macro) { RuleName = "Name" },
			new PlainItemTokenRule(TEXT, ";,)") { RuleName = "Param" },
			new PlainItemTokenRule(TEXT, ";,}") { RuleName = "Object" });

		_arrayScanner = new TokenScanner(
			new WhiteSpaceTokenRule(),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(LexicalTokenType.IGNORE, CommaSemicolon),
			new SequenceTokenRule(TOKEN,
				(PARAMETER, "("),               // begin of parameters list
				(OBJECT, "{"),                  // begin of parameters list
				(ARRAY, "["),                   // begin of array
				(END_ARRAY, "]")
				),
			new PlainItemTokenRule(TEXT, ";,]"));

		// parse option name: ['$' | '#' ] name [ ':' | '=' ]
		_optionNameScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),                       // ignore spaces except new line
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),   // ignore comments
			new NodeNameRule(TEXT, Macro));                             // use text as a token using space, colon, and equal as separators

		// parse option value: (inline_value | text)*
		_optionValueScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),                       // ignore spaces except new line
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),   // ignore comments
			new SequenceTokenRule(LexicalTokenType.IGNORE, ","),        // ignore comma separator
			new SequenceTokenRule(TOKEN,                                // allow inline arrays and objects
				(ARRAY, "["),
				(PARAMETER, "("),
				(OBJECT, "{")),
			new PlainItemTokenRule(TEXT, ","));                              // use text as a token except using comma as a separator

		_nodeArgumentsScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),                       // ignore spaces except new line
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),   // ignore comments
			new SequenceTokenRule(SEPARATOR, ","),                      // use comma as a separator
			new InlineTextTokenRule(),                                  // allow multiline text
			new PlainItemTokenRule(TEXT, ","));                              // use text as a token except using comma as a separator
	}

	private static readonly string[] CommaSemicolon = [",", ";"];
	private static readonly string[] CommaEqual = [",", "=", "=>"];
	private static readonly string[] ColonEqual = [":", "="];
	private static readonly (string, string)[] Comments = [("//", "\n"), ("/*", "*/"), ("<#", "#>"), ("#", "\n")];

	private static readonly LexicalTokenType TEXT = LexicalTokenType.Create(20, 1, "text");
	private static readonly LexicalTokenType TOKEN = LexicalTokenType.Create(21, 0, "token");
	private static readonly LexicalTokenType SEPARATOR = LexicalTokenType.Create(22, 0, "separator");
	private const int DASH = 1;             // - or .
	private const int ATTRIB = 2;           // :
	private const int EQUAL = 3;            // =
	private const int LINE = 4;             // ..
	private const int ENDNODE = 5;          // /
	private const int OPTION = 6;           // %

	private const int PARAMETER = 11;       // (
	private const int ARRAY = 12;           // [
	private const int OBJECT = 13;          // {
	private const int END_PARAMETER = 14;   // )
	private const int END_ARRAY = 15;       // ]
	private const int END_OBJECT = 16;      // }
	private const int END = 17;             // ], )

	private const char ACTION_MARK = '%';
	private const char VAR_MARK = '$';
	private const char ARRAY_MARK = '[';
	private const char OBJECT_MARK = '{';
	private const char PARAM_MARK = '(';

	private static int SkipSpace(in CharStream stream, int index = 0)
	{
		while (IsSpace(stream[index]))
			++index;
		return index;
	}

	private static bool IsSpace(char ch) => ch < 127 ?
		ch is <= ' ' and not ('\r' or '\n'):
		ch is >='\x7F' and <='\xA0' or '\u1680' or >= '\u2000' and <= '\u200A' or '\u202F' or '\u205F' or '\u3000';

	private LexicalToken SkipWhile(ref CharStream stream, LexicalTokenType type1)
	{
		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).Is(type1) && !token.IsEof)
		{
		}
		return token;
	}

	private LexicalToken SkipWhile(ref CharStream stream, LexicalTokenType type1, LexicalTokenType type2)
	{
		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).Is(type1, type2) && !token.IsEof)
		{
		}
		return token;
	}

	private class ConfigValue
	{
		private List<(string Value, string? Macro)>? _chunk;
		private string? _value;

		public ConfigValue()
		{
		}

		public ConfigValue(string value)
		{
			_value = value;
		}

		public ConfigValue(string value, string macro)
		{
			_chunk = [ (value, macro) ];
		}

		public void Append(string value, string macro)
		{
			if (_chunk != null)
			{
				_chunk.Add((value, macro));
			}
			else if (_value == null)
			{
				_chunk = [];
				_chunk.Add((value, macro));
			}
			else
			{
				_chunk = [];
				_chunk.Add((_value + value, macro));
				_value = null;
			}
		}

		public void AppendString(string value)
		{
			if (_chunk != null)
				_chunk.Add((value, null));
			else if (_value == null)
				_value = value;
			else
				_value += value;
		}

		public void AppendMacro(string macro)
		{
			if (_chunk != null)
			{
				if (_chunk[^1].Macro == null)
					_chunk[^1] = (_chunk[^1].Value, macro);
				else
					_chunk.Add((String.Empty, macro));
			}
			else
			{
				_chunk = [];
				_chunk.Add((_value ?? String.Empty, macro));
				_value = null;
			}
		}

		public ResultValue<string> ToString(Func<string, ResultValue<string>> macro)
		{
			if (_chunk == null)
				return _value ?? String.Empty;
			var text = new StringBuilder();
			foreach (var (s, m) in _chunk)
			{
				text.Append(s);
				if (m != null)
				{
					var r = macro(m);
					if (r.IsFailure)
						return r.Error;
					text.Append(r.Value);
				}
			}
			return text.ToString();
		}

		public override string ToString()
		{
			if (_chunk == null)
				return _value ?? String.Empty;
			var text = new StringBuilder();
			foreach (var (s, m) in _chunk)
			{
				text.Append(s);
				if (m != null)
					text.Append("${{").Append(m).Append("}}");
			}
			return text.ToString();
		}
	}

	private static bool IsWhiteSpace(char value) => value <= '\xFF' ? value is <= ' ' or >= '\x7f' and <= '\xa0': Char.IsWhiteSpace(value);

	/// <summary>
	/// Rule to parse node name.
	/// Name can be a string or starts with a letter or underscore, '@', '$' and may contain letters, digits, underscores, and special characters except one of "[](){}:=,;".
	/// '`' is used as an escape character.
	/// </summary>
	private class NodeNameRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType;
		private readonly Func<string, ResultValue<string>>? _macro;

		public NodeNameRule(LexicalTokenType tokenType, Func<string, ResultValue<string>>? macro = null)
		{
			_tokenType = tokenType;
			_macro = macro;
		}

		public override string? BeginningChars => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_@$\"'`";

		public override bool TestBeginning(char ch) => Char.IsLetter(ch) || ch is '_' or '@' or '$' or '"' or '\'' or '`';

		public override LexicalToken TryParse(ref CharStream stream)
		{
			if (stream[0] == '"' || stream[0] == '\'')
				return ParseString(_tokenType, ref stream);

			ReadOnlySpan<char> span = stream[0..];
			StringBuilder? text = null;
			ConfigValue? value = null;

			int i = 0;
			for (; i < span.Length; ++i)
			{
				char ch = span[i];
				if (IsWhiteSpace(ch))
					break;
				if (ch is '[' or '{' or '(' or ')' or ']' or '}' or ':' or '=' or ',' or ';')
					break;

				if (ch == '`' && i < span.Length - 1)
				{
					if (text == null)
					{
						text = new StringBuilder();
						text.Append(span[..i]);
					}
					text.Append(span[++i]);
				}
				else if (ch == '$' && i < span.Length - 5 && span[i + 1] == '{' && span[i + 2] == '{')
				{
					if (text == null)
						text = new StringBuilder().Append(span[..i]);
					int x = 3;
					int j;
					while ((j = span[x..].IndexOf("}}".AsSpan())) > 0 && IsEscaped(span, x + j))
					{
						x += j + 2;
					}
					if (j > 0)
					{
						string macro = UnEscape(span.Slice(i + 3, x + j - 3));
						if (text == null)
						{
							(value ??= new()).Append(span[..i].ToString(), macro);
							text = new StringBuilder();
						}
						else
						{
							(value ??= new()).Append(text.ToString(), macro);
							text.Clear();
						}
						i += x + j + 1;
					}
					else
					{
						i += 2;
						text?.Append("${{");
					}
				}
				else
				{
					text?.Append(ch);
				}
			}
			if (value == null && text == null)
				return stream.Token(_tokenType, i);
			if (value == null)
				return stream.Token(_tokenType, i, (_, _) => text!.ToString());
			if (text!.Length > 0)
				value.AppendString(text.ToString());
			return stream.Token(_tokenType, i, (_, _) => value);

			static string UnEscape(ReadOnlySpan<char> text)
			{
				if (text.IndexOf('`') < 0)
					return text.ToString();
				var sb = new StringBuilder();
				for (int k = 0; k < text.Length; ++k)
				{
					char c = text[k];
					if (c == '`' && k < text.Length - 1)
					{
						sb.Append(text[++k]);
					}
					else
					{
						sb.Append(c);
					}
				}
				return sb.ToString();
			}
		}
	}

	private static bool IsEscaped(ReadOnlySpan<char> span, int i)
	{
		int j = i;
		while (span[j - 1] == '`')
			--j;
		return (i - j) % 2 == 1;
	}

	private static LexicalToken ParseString(LexicalTokenType tokenType, ref CharStream stream)
	{
		char c0 = stream[0];
		ReadOnlySpan<char> span = stream[1..];
		ReadOnlySpan<char> eol = [c0, '\r', '\n'];
		int j0 = span.IndexOfAny(eol);

		if (j0 < 0)
			throw stream.SyntaxException(SR.EofInStringConstant());

		int j1 = span.IndexOf('`');
		int j2 = span.IndexOf("${{");
		if ((j1 < 0 || j1 > j0) && (j2 < 0 || j2 > j0))
		{
			if (span[j0] != c0)
				throw stream.SyntaxException(SR.EolInStringConstant());
			var token = new LexicalToken(tokenType, stream.Position + 1, j0);
			stream.Forward(j0 + 2);
			return token;
		}

		int i = j2 < 0 ? j1: j1 < 0 ? j2: j1 < j2 ? j1: j2;
		ConfigValue? value = null;
		var text = new StringBuilder().Append(span[..i]);
		bool macro = false;
		for (; i < span.Length; ++i)
		{
			char ch = span[i];

			if (ch == '`')
			{
				ch = StringTokenRule.ParseEscape(stream.Slice(i + 1, Math.Min(span.Length - i - 1, 5)), out var len);
				text.Append(ch);
				i += len - 1;
				continue;
			}
			if (ch == c0)
			{
				if (value == null)
					value = new ConfigValue(text.ToString());
				else
					value.AppendString(text.ToString());
				return stream.Token(tokenType, i + 2, (_, _) => value);
			}
			if (ch == '\r' || ch == '\n')
				throw stream.SyntaxException(SR.EolInStringConstant());

			if (macro)
			{
				if (ch == '}' && i < span.Length - 1 && span[i + 1] == '}')
				{
					macro = false;
					value ??= new ConfigValue();
					value.AppendMacro(text.ToString());
					text.Clear();
					i += 1;
					continue;
				}
			}
			else
			{
				if (ch == '$' && i < span.Length - 5 && span[i + 1] == '{' && span[i + 2] == '{')
				{
					macro = true;
					if (text.Length > 0)
					{
						if (value == null)
							value = new ConfigValue(text.ToString());
						else
							value.AppendString(text.ToString());
						text.Clear();
					}
					i += 2;
					continue;
				}
			}
			text.Append(ch);
		}
		throw stream.SyntaxException(SR.EofInStringConstant());
	}

	//private static LexicalToken ParseString(LexicalTokenType tokenType, ref CharStream stream)
	//{
	//	char c0 = stream[0];
	//	ReadOnlySpan<char> span = stream[1..];
	//	ReadOnlySpan<char> eol = [c0, '\r', '\n'];
	//	int j0 = span.IndexOfAny(eol);
	//	int j1 = span.IndexOf('`');
	//	int j2 = span.IndexOf("${{");

	//	if (j0 < 0)
	//		throw stream.SyntaxException(SR.EofInStringConstant());

	//	if ((j1 < 0 || j1 > j0) && (j2 < 0 || j2 > j0))
	//	{
	//		if (span[j0] != c0)
	//			throw stream.SyntaxException(SR.EolInStringConstant());
	//		var token = new LexicalToken(tokenType, stream.Position + 1, j0);
	//		stream.Forward(j0 + 2);
	//		return token;
	//	}

	//	ConfigValue value = new();
	//	var text = new StringBuilder();
	//	int i = 0;

	//	while (true)
	//	{
	//		// j1 >= 0 || j2 >= 0
			
	//		// int j = j1 < 0 || j1 > j2 ? j2: j1;

	//		if (j2 < 0 || j2 > j1) // j1 >= 0 && j0 > j1
	//		{
	//			text.Append(span[i..j1]);
	//			i = j1 + 1;
	//			char ch = StringTokenRule.ParseEscape(span[i..], out var len);
	//			text.Append(ch);
	//			var rest = span.Slice(i + len);
	//			j1 = i + len + rest.IndexOf('`');
	//			j0 = i + len + rest.IndexOfAny(eol);
	//		}
	//		else // j2 >= 0 && j0 > j2
	//		{
	//			var rest = span.Slice(j2 + 3);
	//			int j = rest.IndexOf("}}");
	//			if (j < 0 || j0 < j)
	//			{
	//				if (j1 < 0 || j0 < j1)
	//				{
	//					text.Append(span[i..j0]);
	//					value.AppendString(text.ToString());
	//					return stream.Token(tokenType, j0 + 1, (_, _) => value);
	//				}
	//				// J1 >= 0 && J0 > J1
	//				j2 = -1;
	//			}
	//			else
	//			{
	//				text.Append(stream.Slice(i, j2 - i));
	//				i = j2 + 3;
	//				value.Append(text.ToString(), rest.Slice(0, j).ToString());
	//				text.Clear();
	//				i = j + 2;
	//				j2 = stream.IndexOf("${{", i);
	//				j0 = stream.IndexOfAny(eol, i);
	//				j1 = stream.IndexOf('`', i);
	//			}
	//		}


	//		if (j0 < 0)
	//			throw stream.SyntaxException(SR.EofInStringConstant());
	//		var j = j0;
	//		if (j1 >= 0 && j1 < j)
	//			j = j1;
	//		if (j2 >= 0 && j2 < j)
	//			j = j2;
	//		sb.Append(stream.Slice(i, j - i));
	//		if (j == j0)    // end of string
	//		{
	//			if (escapeChar == Nil && stream[j + 1] == c0)   // double quote
	//			{
	//				i = j + 2;
	//				j0 = stream.IndexOf(c0, i);
	//				sb.Append(c0);
	//			}
	//			else
	//			{
	//				string value = sb.ToString();
	//				return stream.Token(tokenType, j + 1, (_, _) => value);
	//			}
	//		}
	//		else if (j == j1)   // escape sequence
	//		{
	//			i = j + 1;
	//			char ch = ParseEscape(stream.Slice(i, Math.Min(stream.Length - i, 5)), out var len);
	//			if (len < 0)
	//				throw stream.SyntaxException(SR.UnrecognizedEscapeSequence(stream.Substring(i, -len)));
	//			i += len;
	//			sb.Append(ch);
	//			j1 = stream.IndexOf(escapeChar, i);
	//			if (j0 < i)
	//				j0 = stream.IndexOf(c0, i);
	//			if (j2 >= 0 && j2 < i)
	//				j2 = stream.IndexOf(template.Start, i);
	//		}
	//		else // begin macro
	//		{
	//			i = j + template.Start!.Length;
	//			var k = stream.IndexOf(template.End, i);
	//			while (IsEscaped(stream, escapeChar, k))
	//				k = stream.IndexOf(template.End, k + 1);
	//			var k0 = stream.IndexOf(c0, i);
	//			while (IsEscaped(stream, escapeChar, k0))
	//				k0 = stream.IndexOf(c0, k0 + 1);
	//			if (k < 0 || k0 < k)
	//			{
	//				sb.Append(template.Start);
	//				j2 = -1;
	//				continue;
	//			}
	//			sb.Append(macro!(UnEscape(stream.Slice(i, k - i), escapeChar)));
	//			i = k + template.End!.Length;
	//			if (j0 < i)
	//				j0 = stream.IndexOf(c0, i);
	//			if (j1 >= 0 && j1 < i)
	//				j1 = stream.IndexOf(escapeChar, i);
	//			j2 = stream.IndexOf(template.Start, i);
	//		}
	//	}
	//}


	private class PlainItemTokenRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType;
		private readonly string? _separators;

		public PlainItemTokenRule(LexicalTokenType tokenType, string? separators)
		{
			_tokenType = tokenType;
			_separators = separators;
		}

		public override bool TestBeginning(char ch) => !(_separators != null && _separators.Contains(ch) || IsWhiteSpace(ch));

		//public override LexicalToken TryParse(ref CharStream stream)
		//{
		//	if (stream[0] == '"' || stream[0] == '\'')
		//		return ParseString(_tokenType, ref stream);

		//	ReadOnlySpan<char> span = stream[0..];
		//	StringBuilder? text = null;
		//	ConfigValue? value = null;

		//	int i = 0;
		//	for (; i < span.Length; ++i)
		//	{
		//		char ch = span[i];
		//		if (_separators != null && _separators.Contains(ch) || IsWhiteSpace(ch))
		//			break;

		//		if (ch == '`' && i < span.Length - 1)
		//		{
		//			if (text == null)
		//			{
		//				text = new StringBuilder();
		//				text.Append(span[..i]);
		//			}
		//			text.Append(span[++i]);
		//		}
		//		else if (ch == '$' && i < span.Length - 5 && span[i + 1] == '{' && span[i + 2] == '{')
		//		{
		//			if (text == null)
		//				text = new StringBuilder().Append(span[..i]);
		//			int x = 3;
		//			int j;
		//			while ((j = span[x..].IndexOf("}}".AsSpan())) > 0 && IsEscaped(span, x + j))
		//			{
		//				x += j + 2;
		//			}
		//			if (j <= 0)
		//			{
		//				text?.Append(ch);
		//				continue;
		//			}
		//			string macro = UnEscape(span.Slice(i + 3, x + j - 3));
		//			if (text == null)
		//			{
		//				(value ??= new()).Append(span[..i].ToString(), macro);
		//				text = new StringBuilder();
		//			}
		//			else
		//			{
		//				(value ??= new()).Append(text.ToString(), macro);
		//				text.Clear();
		//			}
		//			i += x + j + 1;
		//		}
		//		else
		//		{
		//			text?.Append(ch);
		//		}
		//	}
		//	if (value == null && text == null)
		//		return stream.Token(_tokenType, i);
		//	if (value == null)
		//		return stream.Token(_tokenType, i, (_, _) => text!.ToString());
		//	if (text!.Length > 0)
		//		value.AppendString(text.ToString());
		//	return stream.Token(_tokenType, i, (_, _) => value);

		//	static string UnEscape(ReadOnlySpan<char> text)
		//	{
		//		if (text.IndexOf('`') < 0)
		//			return text.ToString();
		//		var sb = new StringBuilder();
		//		for (int k = 0; k < text.Length; ++k)
		//		{
		//			char c = text[k];
		//			if (c == '`' && k < text.Length - 1)
		//			{
		//				sb.Append(text[++k]);
		//			}
		//			else
		//			{
		//				sb.Append(c);
		//			}
		//		}
		//		return sb.ToString();
		//	}
		//}

		public override LexicalToken TryParse(ref CharStream stream)
		{
			if (stream[0] == '"' || stream[0] == '\'')
				return ParseString(LexicalTokenType.STRING, ref stream);
			int i = _separators == null ?
				stream.IndexOf(c => IsWhiteSpace(c)):
				stream.IndexOf(c => _separators.Contains(c) || IsWhiteSpace(c));
			return i <= 0 ? stream.Token(_tokenType, stream.Length): stream.Token(_tokenType, i);
		}
	}

	/// <summary>
	/// Scans a plain value token.
	/// </summary>
	private class PlainValueTokenRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType = TEXT;

		public override bool HasExtraBeginning => true;

		public override bool TestBeginning(char ch) => ch is not ('\n' or '\r');

		public override LexicalToken TryParse(ref CharStream stream)
		{
			if (stream[0] == '"' || stream[0] == '\'')
				return ParseString(LexicalTokenType.STRING, ref stream);

			int n = stream.IndexOfAny(CrLf);
			if (n < 0)
				n = stream.Length;
			if (n == 0)
				return LexicalToken.Empty;

			var s = stream.Slice(0, n);
			int i = BeginOfComment(s);
			if (i < 0)
			{
				s = s.TrimEnd();
				var token = new LexicalToken(_tokenType, stream.Position, s.Length);
				stream.Forward(n);
				return token;
			}
			if (s[i] == '#')
			{
				if (i == 0)
					return LexicalToken.Empty;
				s = s[..i].TrimEnd();
				var token = new LexicalToken(_tokenType, stream.Position, s.Length);
				stream.Forward(n);
				return token;
			}

			string left = s[..i].ToString();
			var position = stream.Position;
			while (true)
			{
				stream.Forward(i);
				i = stream.IndexOf("#>", 2);
				if (i < 0)
					return new LexicalToken(_tokenType, position, n, (_, _) => left);
				stream.Forward(i + 2);
				n = stream.IndexOfAny(CrLf);
				if (n < 0)
					n = stream.Length;
				s = stream.Slice(0, n);
				i = BeginOfComment(s);
				if (i < 0)
				{
					left += s.TrimEnd().ToString();
					left = left.TrimEnd();
					var token = new LexicalToken(_tokenType, position, stream.Position - position + n, (_, _) => left);
					stream.Forward(n);
					return token;
				}
				left += s[..i].ToString();
				if (s[i] == '#')
				{
					left = left.TrimEnd();
					var token = new LexicalToken(_tokenType, position, stream.Position - position + i, (_, _) => left);
					stream.Forward(n);
					return token;
				}
			}

			static int BeginOfComment(ReadOnlySpan<char> s)
			{
				int k = 0;
				while (true)
				{
					int i = s.IndexOfAny(BeginComments);
					if (i < 0)
						return -1;
					if (i > 0 && IsWhiteSpace(s[i - 1]))
					{
						if (s[i] == '#')
							return k + i;
						if (s.Length < i + 2)
							return -1;
						if (s[i + 1] == '#')
							return k + i;
					}
					s = s.Slice(i + 1);
					k += i + 1;
				}
			}
		}
		private static readonly char[] BeginComments = ['#', '<'];
	}

	private class InlineTextTokenRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType;

		public InlineTextTokenRule(): this(TEXT)
		{
		}

		private InlineTextTokenRule(LexicalTokenType textTokenType)
		{
			_tokenType = textTokenType;
		}

		public override string BeginningChars => "<";

		public override bool TestBeginning(char ch) => ch == '<';

		public override LexicalToken TryParse(ref CharStream stream)
		{
			const char Begin = '<';
			const char End = '>';
			int width = 0;
			while (stream[width] == Begin)
				++width;
			if (!IsWhiteSpace(stream[width]))
				return LexicalToken.Empty;

			int at = stream.Position;

			int i = stream.IndexOfAny(CrLf);
			if (i < 0)
				return LexicalToken.Empty;
			var s = stream.Slice(width, i - width).Trim();
			if (s.Length > 0 && s[0] != '#')
			{
				if (!s.StartsWith("<#"))
					return LexicalToken.Empty;

				int j = stream.IndexOf("<#");
				for (;;)
				{
					Debug.Assert(j > 0);
					j = stream.IndexOf("#>", j + 2);
					j = SkipSpace(in stream, j);
					if (stream[j] is '\n' or '\r')
					{
						i = j;
						break;
					}
					if (stream[j] == '#')
					{
						i = stream.IndexOfAny(CrLf, j);
						if (i < 0)
							return LexicalToken.Empty;
						break;
					}
					if (stream.Slice(j, 2) != "<#")
						return LexicalToken.Empty;
				}
			}

			stream.Forward(i + stream.NewLineSize(i));
			Span<char> end = stackalloc char[width];
			for (int j = 0; j < width; ++j)
				end[j] = End;

			i = 0;
			int k = 0;
			for (;;)
			{
				k = stream.IndexOf(end, i);
				if (k < 0)
					return new LexicalToken(LexicalTokenType.ERROR, stream.Position, i, (_, _) => "End of file reached in multiline text");
				i = k;
				while (i > 0 && IsSpace(stream[i - 1]))
					--i;
				k += width;
				if (stream[i] is '\n' or '\r' && IsWhiteSpace(stream[k]))
					break;
				i = k;
			}
			--i;
			if (stream[i] == '\r' ? stream[i + 1] == '\n': stream[i + 1] == '\r')
				--i;
			var text = Strings.CutIndents(stream.Slice(0, i), stream.TabSize);
			stream.Forward(k);
			return new LexicalToken(_tokenType, at, stream.Position - at, (_, _) => text);
		}
	}

	private static readonly char[] CrLf = ['\r', '\n'];
}
