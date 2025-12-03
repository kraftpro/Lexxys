using System.Text;

using Lexxys.Tokenizer;

namespace Lexxys.Configuration;

public ref partial struct ConfigNodeParser
{
	private readonly TokenScanner _scanner;
	private readonly OneBackFilter _back;

	public ConfigNodeParser()
	{
		_back = new OneBackFilter();
		_scanner = new TokenScanner([_back],
		[
			new WhiteSpaceTokenRule(),
			new CommentsTokenRule(LexicalTokenType.IGNORE, [("//", "\n"), ("/*", "*/"), ("<#", "#>"), ("#", "\n")]),
			new SequenceTokenRule(SEQUENCE,
				(BEGIN, "("),
				(END, ")"),
				(DELIMITER, ","),
				(DELIMITER, ";"),
				(EQUAL, "="),
				(EQUAL, ":")
			),
			new PlainValueTokenRule(VALUE),
			new StringTokenRule(STRING),
		]);
	}

	/// <summary>
	/// Parses a sequence of characters from the specified text stream into a collection of configuration nodes.
	/// </summary>
	/// <param name="stream">A reference to the character stream containing the text to parse. The stream position will be advanced as nodes are read.</param>
	/// <param name="macro"></param>
	/// <returns>A collection of configuration nodes parsed from the input text. The collection will be empty if no nodes are found.</returns>
	/// <remarks>
	/// The method uses the following BNF-like grammar to parse configuration nodes:
	/// <code>
	///	  config_node     ::= value | collection
	///	  collection      ::= '(' [ config_node { { delimiter config_node } ] ')'
	///	  element         ::= value | key_value_pair
	///	  key_value_pair  ::= name (':' | '=') [ value ]
	///	  name            ::= simple_value
	///	  delimiter       ::= ',' | ';' | SPACE | NEWLINE
	///	  delimiter_brace ::= delimiter | '(' | ')'
	///	  value           ::= simple_value [ collection ]
	///	  simple_value    ::= string_value | plain_value
	///	  string_value    ::= '"' { string_item } '"'
	///	  string_item     ::= macro | [^"`] | escape_sequence
	///	  escape_sequence ::= '`' ( 'n' | 'r' | 't' | '`' | [abfnrtveN_LP0] | 'c' [A-Z] | 'x' [0-9a-f][0-9a-f]([0-9a-f][0-9a-f])? | 'u' [0-9a-f][0-9a-f][0-9a-f][0-9a-f] )
	///	  macro           ::= '${{' ANY* '}}'
	///	  plain_value     ::= ([^delimiter_brace] | macro | '`' ANY)+
	///	  ANY             ::= [^\n\r]
	///	  SPACE           ::= ' ' | '\t'
	///	  NEWLINE         ::= '\n' | '\r\n'
	///	  COMMENT         ::= '#' ... '\n' | '&lt;#' ... '#&gt;' | '//' ... '\n' | '/*' ... '*/'
	/// </code>
	/// </remarks>
	public static ConfigNodeCollection ParseCollection(ref CharStream stream, Func<string, Func<string, string?>, string?>? macro = null)
	{
		var collection = new List<KeyValuePair<string?, ConfigNode>>();
		var parser = new ConfigNodeParser();
		var intern = GetMacro(collection);
		parser.ParseNodeCollection(ref stream, o => o.IsEof, collection, macro is null ? intern: o => macro(o, intern));
		return new ConfigNodeCollection(collection);
	}

	public static ConfigNodeCollection ParseCollection(string text, Func<string, Func<string, string?>, string?>? macro = null)
	{
		var collection = new List<KeyValuePair<string?, ConfigNode>>();
		var parser = new ConfigNodeParser();
		var stream = new CharStream(text);
		var intern = GetMacro(collection);
		parser.ParseNodeCollection(ref stream, o => o.IsEof, collection, macro is null ? intern: o => macro(o, intern));
		return new ConfigNodeCollection(collection);
	}

	public static List<ConfigNode> ParseList(ref CharStream stream, Func<string, Func<string, string?>, string?>? macro = null)
	{
		var nodes = new List<ConfigNode>();
		var parser = new ConfigNodeParser();
		var intern = GetMacro(nodes);
		parser.ParseNodeList(ref stream, nodes, macro is null ? intern: o => macro(o, intern));
		return nodes;
	}

	private static Func<string, string?> GetMacro(List<ConfigNode> nodes) =>
		name =>
		{
			for (int i = nodes.Count - 1; i >= 0; --i)
			{
				if (TryFindNode(nodes[i].Collection, name, out var found))
					return found.ToString();
			}
			return null;
		};

	private static Func<string, string?> GetMacro(List<KeyValuePair<string?, ConfigNode>> nodes) =>
		name =>
		{
			for (int i = nodes.Count - 1; i >= 0; --i)
			{
				if (TryFindNode(nodes[i].Value.Collection, name, out var found))
					return found.ToString();
			}
			int j = nodes.FindIndex(x => x.Key == name);
			return j < 0 ? null: nodes[j].Value.ToString();
		};

	private static bool TryFindNode(ConfigNodeCollection? collection, string name, out ConfigNode node)
	{
		node = default;
		if (collection is null)
			return false;
		if (collection.TryGetValue(name, out node))
			return true;

		for (int i = collection.Count - 1; i >= 0; --i)
		{
			if (TryFindNode(collection[i].Collection, name, out node))
				return true;
		}
		return false;
	}
	
	private readonly List<ConfigNode> ParseNodeList(ref CharStream stream, List<ConfigNode> nodes, Func<string, string?>? macro)
	{
		while (true)
		{
			var token = _scanner.Next(ref stream);
			if (token.IsEof)
				return nodes;
			if (token.Is(VALUE))
			{
				var next = _scanner.Next(ref stream);
				nodes.Add(new ConfigNode(token.GetString(in stream)));
				continue;
			}
			if (!token.Is(SEQUENCE))
				throw stream.SyntaxException("!0.2!");

			if (!token.Is(SEQUENCE, BEGIN, DELIMITER))
				throw stream.SyntaxException("!0.1!");

			if (token.Is(SEQUENCE, BEGIN))
			{
				var child = new List<KeyValuePair<string?, ConfigNode>>();
				nodes.Add(new ConfigNode(new ConfigNodeCollection(child)));
				ParseNodeCollection(ref stream, o => o.Is(SEQUENCE, END), child, macro);
			}
		}
	}

	private readonly void Back() => _back.Back();

	private readonly void ParseNodeCollection(ref CharStream stream, Func<LexicalToken, bool> testEnd, List<KeyValuePair<string?, ConfigNode>> collection, Func<string, string?>? macro)
	{
		bool delimiter = true;
		while (true)
		{
			var token = _scanner.Next(ref stream);
			if (testEnd(token))
				return;

			string? name = null;
			if (token.Is(VALUE))
			{
				var next = _scanner.Next(ref stream);
				if (next.Is(SEQUENCE, EQUAL))
				{
					var x = token.GetValue(in stream);
					name = macro is null || x is not ConfigValue cv ? x?.ToString(): cv.ToString(macro);
					token = _scanner.Next(ref stream);
					if (testEnd(token))
					{
						collection.Add(new KeyValuePair<string?, ConfigNode>(name, new ConfigNode()));
						return;
					}
				}
				else
				{
					Back();
				}
			}

			string? value = null;
			if (token.Is(VALUE))
			{
				var x = token.GetValue(in stream);
				value = macro is null || x is not ConfigValue cv ? x?.ToString() : cv.ToString(macro);
				token = _scanner.Next(ref stream);
				if (token.Is(VALUE) || testEnd(token))
				{
					collection.Add(new KeyValuePair<string?, ConfigNode>(name, new ConfigNode(value)));
					Back();
					delimiter = false;
					continue;
				}
			}

			if (token.Is(SEQUENCE, BEGIN))
			{
				delimiter = false;
				var child = new List<KeyValuePair<string?, ConfigNode>>();
				ParseNodeCollection(ref stream, o => o.Is(SEQUENCE, END), child, macro);
				collection.Add(new KeyValuePair<string?, ConfigNode>(name, new ConfigNode(value, new ConfigNodeCollection(child))));
			}
			else if (token.Is(SEQUENCE, DELIMITER))
			{
				if (name != null || value != null)
					collection.Add(new KeyValuePair<string?, ConfigNode>(name, new ConfigNode(value)));
				else if (delimiter)
					collection.Add(new KeyValuePair<string?, ConfigNode>(null, new ConfigNode()));
				delimiter = true;
			}
			else
			{
				throw stream.SyntaxException($"!{nameof(ParseNodeCollection)}.1! Delimiter or parameters are expected");
			}
		}
	}

	//internal readonly void ParseNameValue(ref CharStream stream, in LexicalToken start, ConfigNodeCollection collection)
	//{
	//	Debug.Assert(start.Is(VALUE));
	//	var next = _scanner.Next(ref stream);
	//	string? name = null;
	//	if (next.Is(SEQUENCE, EQUAL))
	//	{
	//		name = next.GetString(in stream);
	//		next = _scanner.Next(ref stream);
	//	}

	//	else if (!(next = _scanner.Next(ref stream)).Is(VALUE))
	//	{
	//		collection.Add(name, new ConfigNode());
	//		Back();
	//	}
	//	else
	//	{
	//		collection.Add(name, new ConfigNode(next.GetString(in stream)));
	//	}
	//}


	private const int BEGIN = 1;
	private const int END = 2;
	private const int DELIMITER = 3;
	private const int EQUAL = 4;
	private static readonly LexicalTokenType SEQUENCE = LexicalTokenType.SEQUENCE;
	private static readonly LexicalTokenType VALUE = LexicalTokenType.Create(21, 0, "value");
	private static readonly LexicalTokenType STRING = LexicalTokenType.Create(21, 1, "string");

	private class PlainValueTokenRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType;

		public PlainValueTokenRule(LexicalTokenType tokenType)
		{
			_tokenType = tokenType;
		}

		// public override string? BeginningChars => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_@$\"'`";

		public override bool TestBeginning(char ch) => !(IsWhiteSpace(ch) || ch is '[' or '{' or '(' or ')' or ']' or '}' or ':' or '=' or ',' or ';');

		private static bool IsWhiteSpace(char value) => value <= '\xFF' ? value is <= ' ' or >= '\x7f' and <= '\xa0': Char.IsWhiteSpace(value);

		public override LexicalToken TryParse(ref CharStream stream)
		{
			ReadOnlySpan<char> span = stream[0..];
			StringBuilder? text = null;
			ConfigValue? value = null;

			int i = 0;
			for (; i < span.Length; ++i)
			{
				char ch = span[i];
				if (IsWhiteSpace(ch))
					break;
				if (ch is '[' or '{' or '(' or ')' or ']' or '}' or ',' or ';')
					break;
				if (ch is ':' or '=' && span.Length > i + 1 && IsWhiteSpace(span[i + 1]))
					break;

				if (ch == '`' && i < span.Length - 1)
				{
					text ??= new StringBuilder().Append(span[..i]);
					text.Append(span[++i]);
				}
				else if (ch == '$' && i < span.Length - 5 && span[i + 1] == '{' && span[i + 2] == '{')
				{
					text ??= new StringBuilder().Append(span[..i]);
					int x = i + 3;
					int j;
					while ((j = span[x..].IndexOf("}}".AsSpan())) > 0 && IsEscaped(span, x + j))
					{
						x += j + 2;
					}
					if (j > 0)
					{
						string macro = UnEscape(span.Slice(i + 3, x + j - (i + 3)));
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
						i = x + j + 1;
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
				value.AppendValue(text.ToString());
			return stream.Token(_tokenType, i, (_, _) => value);

			static bool IsEscaped(ReadOnlySpan<char> span, int i)
			{
				int j = i;
				while (span[j - 1] == '`')
					--j;
				return (i - j) % 2 == 1;
			}

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

	private class StringTokenRule: LexicalTokenRule
	{
		private readonly LexicalTokenType _tokenType;

		public StringTokenRule(LexicalTokenType tokenType)
		{
			_tokenType = tokenType;
		}

		public override string? BeginningChars => "\"'";

		public override bool TestBeginning(char ch) => ch is '"' or '\'';

		public override LexicalToken TryParse(ref CharStream stream)
		{
			char c0 = stream[0];
			ReadOnlySpan<char> span = stream[1..];
			ReadOnlySpan<char> eol = [c0, '\r', '\n'];
			int j0 = span.IndexOf(eol);

			if (j0 < 0)
				throw stream.SyntaxException(SR.EofInStringConstant());

			int j1 = span.IndexOf('`');
			int j2 = span.IndexOf("${{");
			if ((j1 < 0 || j1 > j0) && (j2 < 0 || j2 > j0))
			{
				if (span[j0] != c0)
					throw stream.SyntaxException(SR.EolInStringConstant());
				//span = span.Slice(0, j0 - 1);
				//j1 = span.IndexOf(@"\\".AsSpan());
				//LexicalToken token;
				//if (j1 < 0)
				//{
				//	token = new LexicalToken(tokenType, stream.Position + 1, j0);
				//}
				//else
				//{
				//	string s = span.ToString().Replace(@"\\", @"\");
				//	token = new LexicalToken(tokenType, stream.Position, j0 + 2, (_, _) => s);
				//}
				LexicalToken token = new LexicalToken(_tokenType, stream.Position + 1, j0);
				stream.Forward(j0 + 2);
				return token;
			}

			int i = j2 < 0 ? j1 : j1 < 0 ? j2 : j1 < j2 ? j1 : j2;
			ConfigValue? value = null;
			var text = new StringBuilder().Append(span[..i]);
			bool macro = false;
			for (; i < span.Length; ++i)
			{
				char ch = span[i];

				if (ch == '`')
				{
					ch = Tokenizer.StringTokenRule.ParseEscape(stream.Slice(i + 1, Math.Min(span.Length - i - 1, 5)), out var len);
					text.Append(ch);
					i += len - 1;
					continue;
				}
				if (ch == c0)
				{
					if (value == null)
						value = new ConfigValue(text.ToString());
					else
						value.AppendValue(text.ToString());
					return stream.Token(_tokenType, i + 2, (_, _) => value);
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
								value.AppendValue(text.ToString());
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
			_chunk = [(value, macro)];
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

		public void AppendValue(string value)
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

		public string ToString(Func<string, string?> macro)
		{
			if (_chunk == null)
				return _value ?? String.Empty;
			var text = new StringBuilder();
			foreach (var (s, m) in _chunk)
			{
				text.Append(s);
				if (m != null)
					text.Append(macro.Invoke(m));
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
}
