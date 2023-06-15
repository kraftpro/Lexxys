// Lexxys Infrastructural library.
// file: TextToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Lexxys.Xml;

using Tokenizer;

public delegate IEnumerable<XmlLiteNode>? TextToXmlOptionHandler(ref TextToXmlConverter converter, string option, IReadOnlyCollection<string> parameters);
public delegate string MacroSubstitution(string value);

/// <summary>
/// Converts user friendly configuration text to plain XML
/// </summary>
/// <remarks>
/// The Syntax:
/// <code>
/// 
/// document		:=	node [node]*
/// 
///	node			:=	[options]* (array_value | node_definition)
///	node_definition	:=	node_name [node_value] [INDENT (line_value | attribute | subnode | comment)+ UNDENT] [endnode]
/// node_name		:=	(anonymous_name | NAME)
/// anonymous_name	:=	'.' | '-'
/// 
/// options			:=	'%' option EOL
/// 					'%%' 'include' text EOL
/// 					'%%' 'ignore-case' EOL
/// 					'%%' '&lt;&lt;' 'include' text ... '%%&gt;&gt;'
/// option			:=	pattern [parameters]
/// 					named_rule [parameters]
/// pattern			:=	[TEXT_WITH_STAR '/']* (TEXT_WITH_STAR | NAME)
/// named_rule		:=	NAME ':' pattern
///	parameters		:=	NAME [delimiter NAME]* ['*'] [delimiter NAME]*
///						'*' [delimiter NAME]*
/// 
/// node_value		:=	[ arguments ] node_value2
/// arguments		:=	'(' [arguments_list] ')'
/// arguments_list	:=	argument [',' arguments_list]
/// argument		:=	name (':' | '=') simple_value
/// node_value2		:=	eol_value
///						text_value
///						parameter_value
///						array_value
///	eol_value		:=	TEXT_WITHOUT_EOL [comment] EOL
///	text_value		:=	bot_mark [comment] EOL TEXT EOL [SPACE] eot_mark
///	bot_mark		:=	'&lt;' '&lt;' ['&lt;']*
///	eot_mark		:=	'&gt;' '&gt;' ['&gt;']*
///	parameter_value	:=	[simple_value [delimiter simple_value]*] [TEXT_WITHOUT_EOL] EOL
///	array_value		:=	'[' [array_items] ']'
///	array_items		:=	array_item [',' array_items]
///	array_item		:=	simple_value
///	simple_value	:=	STRING
///						TOKEN
///	
///	comment			:=	'//' TEXT_WITHOUT_EOL
///	comment			:=	'#' TEXT_WITHOUT_EOL
///	comment			:=	comment
///						'/*' TEXT '*/'
///						'&lt;#' TEXT '#&gt;'
/// 
/// line_value		:=	'..' eol_value
///						text_value
///	
///	attribute		:=	eq_name [attrib_value] [INDENT line_value+ UNDENT]
///	
///	attrib_value	:=	eol_value
///						text_value
///	
///	sub_node		:=	node
/// 
///	eq_name			:= ['=' | '@' | ':'] NAME
/// 
///	end_node		:=	'/' NAME
///	
/// delimiter		:=	' ' | ',' | ';' | '=' | '=>'
/// 
/// macro_element	:= '${{' reference ['|' default_value ] '}}'
/// </code>
/// </remarks>
public ref struct TextToXmlConverter
{
	private TokenScanner _nodeScanner;
	private TokenScanner _nodeValueScanner;
	private TokenScanner _attribValueScanner;
	private TokenScanner _optionsScanner;
	private TokenScanner _nodeArgumentsScanner;
	private TokenScanner _parametersScanner;
	private TokenScanner _arrayScanner;

	private readonly string? _sourceName;
	private readonly StringBuilder _currentNodePath;
	private readonly SyntaxRuleCollection _syntaxRules;
	private readonly TextToXmlOptionHandler? _optionHandler;
	private readonly MacroSubstitution _macro;
	private readonly OneBackFilter _back;
	private readonly Stack<Node> _nodesStack;

	public TextToXmlConverter(): this(null)
	{
	}

	public TextToXmlConverter(string? sourceName, TextToXmlOptionHandler? optionHandler = null, MacroSubstitution? macro = null)
	{
		_back = new OneBackFilter();
		_nodeScanner = new TokenScanner(
			new ITokenFilter[]
			{
				new IndentFilter(),
				_back
			}, new LexicalTokenRule[]
			{
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new InlineTextTokenRule(),
				new SequenceTokenRule(TOKEN)
					.Add(LINE, "..")			// continue of value mark
					.Add(ANONYMOUS, ".")		// predefined node name mark
					.Add(ANONYMOUS, "-")		// predefined node name mark
					.Add(ATTRIB, ":")			// attribute mark
					.Add(ATTRIB, "=")			// attribute mark
					.Add(ATTRIB, "@")			// attribute mark
					.Add(ARRAY, "[")			// array mark
					.Add(ENDNODE, "/")		  // end node mark
					.Add(CONFIG, "%%")			// configuration mark
					.Add(OPTION, "%"),			// option mark (not affected to indentation)
				new UniversalTokenRule(LexicalTokenType.IDENTIFIER, "")
			});

		_nodeValueScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new StringTokenRule('`'),
			new InlineTextTokenRule(),
			new SequenceTokenRule(TOKEN)
				.Add(PARAMETER, "(")		// begin of parameters list
				.Add(ARRAY, "["),		   // begin of array
			new TextLineTokenRule());
		_attribValueScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new StringTokenRule('`'),
			new InlineTextTokenRule(),
			new TextLineTokenRule());
		_parametersScanner = new TokenScanner(
			new WhiteSpaceTokenRule(),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(LexicalTokenType.IGNORE, CommaSemicolon),
			new SequenceTokenRule(TOKEN).Add(PARAMETER, ")"),
			new SequenceTokenRule(ASSIGN, ColonEqual),
			new UniversalTokenRule(TEXT, ",=:()"));
		_arrayScanner = new TokenScanner(
			new WhiteSpaceTokenRule(),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(LexicalTokenType.IGNORE, CommaSemicolon),
			new SequenceTokenRule(TOKEN).Add(ARRAY, "]"),
			new UniversalTokenRule(TEXT, ",=[]"));
		_optionsScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(LexicalTokenType.IGNORE, CommaEqual),
			new UniversalTokenRule(TEXT, ",="));
		_nodeArgumentsScanner = new TokenScanner(
			new WhiteSpaceTokenRule(false, true),
			new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
			new SequenceTokenRule(SEPARATOR, CommaEqual),
			new InlineTextTokenRule(),
			new UniversalTokenRule(TEXT, ",="));

		_sourceName = sourceName;
		_currentNodePath = new StringBuilder(128);
		_syntaxRules = new SyntaxRuleCollection();
		_optionHandler = optionHandler;
		_macro = macro ?? (o => o);
		_options = default;
		_nodesStack = new Stack<Node>(4);
	}

	private static readonly string[] CommaSemicolon = { ",", ";" };
	private static readonly string[] CommaEqual = { ",", "=", "=>" };
	private static readonly string[] ColonEqual = { ":", "=" };
	private static readonly (string, string)[] Comments = { ("//", "\n"), ("/*", "*/"), ("<#", "#>"), ("#", "\n") };

	public static string Convert(string text, string? sourceName = null)
	{
		return Convert(text, null, sourceName);
	}

	public static string Convert(string text, TextToXmlOptionHandler? optionHandler, string? sourceName = null)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		var sb = new StringBuilder(text.Length * 4);
		var ws = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, OmitXmlDeclaration = true };
		using (var writer = XmlWriter.Create(sb, ws))
		{
			Convert(text, writer, optionHandler, sourceName);
		}
		return sb.ToString();
	}

	public static void Convert(string text, XmlWriter writer, TextToXmlOptionHandler? optionHandler, string? sourceName = null)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		var converter = new TextToXmlConverter(sourceName, optionHandler);
		var cs = new CharStream(text);
		converter.Convert(ref cs, o=> ConvertToXml(writer, o));
		writer.Flush();
	}

	//public static string Convert(TextReader reader, string fileName = null)
	//{
	//	return Convert(reader.ReadToEnd(), null, fileName);
	//}

	//public static string Convert(TextReader reader, TextToXmlOptionHandler optionHandler, string fileName = null)
	//{
	//	return Convert(reader.ReadToEnd(), optionHandler, fileName);
	//}

	//public static void Convert(TextReader reader, XmlWriter writer, TextToXmlOptionHandler optionHandler, string fileName = null)
	//{
	//	Convert(reader.ReadToEnd(), writer, optionHandler, fileName);
	//}

	public static List<XmlLiteNode> ConvertLite(string text, string? sourceName = null, bool ignoreCase = false)
	{
		return ConvertLite(text, null, sourceName, ignoreCase);
	}

	public static List<XmlLiteNode> ConvertLite(string text, TextToXmlOptionHandler? optionHandler, string? sourceName = null, bool ignoreCase = false)
	{
		var converter = new TextToXmlConverter(sourceName, optionHandler);
		var result = new List<XmlLiteNode>();
		var cs = new CharStream(text);
		converter.Convert(ref cs, o=> result.Add(ConvertToXmlLite(o, ignoreCase)));
		return result;
	}

	public string? SourceName => _sourceName;

	private void Back()
	{
		_back.Back();
	}

	public void Reset()
	{
		_back.Reset();
		_nodeScanner.Reset();
		_nodeValueScanner.Reset();
		_attribValueScanner.Reset();
		_optionsScanner.Reset();
		_nodeArgumentsScanner.Reset();
		_parametersScanner.Reset();
		_arrayScanner.Reset();
		_currentNodePath.Clear();

		_syntaxRules.Clear();
		_options = default;
		_nodesStack.Clear();
	}

	public void Convert(ref CharStream stream, Action<Node> action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));
		if (stream[0] == ':')
			return;

		Node? node = ScanNode(ref stream);
		while (node != null)
		{
			action(node);
			node = ScanNode(ref stream);
		}
	}

	private static string SubstituteMacro(string value)
	{
		int i = value.IndexOf("${{", StringComparison.Ordinal);
		if (i < 0)
			return value;
		var config = Statics.TryGetService<Configuration.IConfigSection>();
		if (config == null)
			return value;
		while (i >= 0)
		{
			int j = value.IndexOf("}}", i + 3, StringComparison.Ordinal);
			if (j < 0)
				break;
			string macro = value.Substring(i + 3, j - i - 3);
			string? defaultValue = null; 
			int k = macro.IndexOf('|');
			if (k >= 0)
			{
				defaultValue = macro.Substring(k + 1).Trim();
				macro = macro.Substring(0, k);
			}
			string? subst = string.IsNullOrWhiteSpace(macro) ? defaultValue: config.GetValue(macro, defaultValue).Value;
			if (subst != null)
				value = value.Substring(0, i) + subst + value.Substring(j + 2);
			i = value.IndexOf("${{", j + 2, StringComparison.Ordinal);
		}
		return value;
	}

	public static XmlLiteNode ConvertToXmlLite(Node node, bool ignoreCase)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		var child = Array.Empty<XmlLiteNode>();
		if (node.Children is { Count: >0 } cc)
		{
			child = new XmlLiteNode[cc.Count];
			for (int i = 0; i < child.Length; ++i)
			{
				child[i] = ConvertToXmlLite(cc[i], ignoreCase);
			}
		}

		var attrib = Array.Empty<KeyValuePair<string, string>>();
		if (node.Attribs is { Count: >0 } aa)
		{
			attrib = aa.ToArray();
			for (int i = 0; i < attrib.Length; ++i)
			{
				attrib[i] = KeyValue.Create(attrib[i].Key, SubstituteMacro(attrib[i].Value));
			}
		}
		return new XmlLiteNode(node.Name, SubstituteMacro(node.Value), ignoreCase || node.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, attrib, child);
	}

	private static void ConvertToXml(XmlWriter writer, Node node)
	{
		writer.WriteStartElement(XmlConvert.EncodeName(node.Name)!);
		if (node.Attribs is { Count: >0 } aa)
		{
			foreach (var item in aa)
			{
				writer.WriteAttributeString(XmlConvert.EncodeName(item.Key)!, SubstituteMacro(item.Value));
			}
		}
		if (node.Value.Length > 0)
			writer.WriteValue(SubstituteMacro(node.Value));
		if (node.Children is { Count: >0 } cc)
		{
			foreach (var item in cc)
			{
				ConvertToXml(writer, item);
			}
		}
		writer.WriteEndElement();
	}


	#region Lexical Parser
	private static readonly LexicalTokenType TEXT = LexicalTokenType.Create(20, 1, "text");
	private static readonly LexicalTokenType TOKEN = LexicalTokenType.Create(21, 0, "token");
	private static readonly LexicalTokenType SEPARATOR = LexicalTokenType.Create(22, 0, "separator");
	private static readonly LexicalTokenType ASSIGN = LexicalTokenType.Create(23, 0, "assign");
	private const int ANONYMOUS = 1;
	private const int ATTRIB = 2;
	private const int LINE = 3;
	private const int ENDNODE = 5;
	private const int OPTION = 6;
	private const int CONFIG = 7;
	private const int PARAMETER = 8;
	private const int ARRAY = 9;

	private class UniversalTokenRule: LexicalTokenRule
	{
		private readonly char[] _asep;
		public UniversalTokenRule(LexicalTokenType tokenType, string? separators)
		{
			TokenType = tokenType;
			_asep = ((separators ?? "") + " \t\r\n\f").ToCharArray();
		}

		public LexicalTokenType TokenType { get; private set; }

		public override bool TestBeginning(char ch) => Array.IndexOf(_asep, ch) < 0;

		public override LexicalToken TryParse(ref CharStream stream)
		{
			if (stream[0] == '"' || stream[0] == '\'')
				return StringTokenRule.ParseString(TokenType, ref stream, '`');

			int i = stream.IndexOfAny(_asep);
			return i <= 0 ? stream.Token(TokenType, stream.Length) : stream.Token(TokenType, i);
		}
	}

	private class TextLineTokenRule: LexicalTokenRule
	{
		public TextLineTokenRule()
		{
			TokenType = TEXT;
		}

		public override bool HasExtraBeginning => true;

		public LexicalTokenType TokenType { get; }

		public override bool TestBeginning(char ch) => ch is not ('\n' or '\r');

		public override LexicalToken TryParse(ref CharStream stream)
		{
			int n = stream.IndexOfAny(CrLf);
			if (n < 0)
				n = stream.Length;
			if (n == 0)
				return LexicalToken.Empty;

			if (stream[0] == '`')
			{
				stream.Forward(1);
			return stream.Token(TokenType, n - 1);
			}

			LexicalToken token;
			var s = stream.Slice(0, n);
			int i = s.IndexOfAny(BeginComments);
			if (i < 0)
			{
				var t = s.TrimEnd();
				token = new LexicalToken(TokenType, stream.Position, t.Length);
				stream.Forward(n);
				return token;
			}

			string? left = null;
			ReadOnlySpan<char> beginComments = BeginComments;
			do
			{
				if (i != 0 && !IsWhiteSpace(s[i - 1]))
				{
					int k = s.Slice(i + 1).IndexOfAny(beginComments);
					if (k < 0)
						break;
					i += k + 1;
					continue;
				}

				var c = s[i];
				if (c == '#')
				{
					s = s.Slice(0, i);
					break;
				}

				if (i + 1 >= s.Length)
					break;

				if (c == '<')
				{
					if (s[i + 1] == '#')
					{
						int j = s.Slice(i + 2).IndexOf("#>".AsSpan());
						if (j < 0) // Multiline comments in value
						{
							n -= s.Length - i;
							s = s.Slice(0, i);
							break;
						}

						if (left == null)
							left = s.Slice(0, i).ToString();
						else
#if NET5_0_OR_GREATER
							left = String.Concat(left, s.Slice(0, i));
#else
							left += s.Slice(0, i).ToString();
#endif

						s = s.Slice(i + 2 + j + 2);
						i = -1;
					}
				}
				else // if (c == '/')
				{
					var c2 = s[i + 1];
					if (c2 == '/')
					{
						s = s.Slice(0, i);
						break;
					}

					if (c2 == '*')
					{
						int j = s.Slice(i + 2).IndexOf("*/".AsSpan());
						if (j < 0) // Multiline comments in value
						{
							n -= s.Length - i;
							s = s.Slice(0, i);
							break;
						}

						if (left == null)
							left = s.Slice(0, i).ToString();
						else
#if NET5_0_OR_GREATER
							left = String.Concat(left, s.Slice(0, i));
#else
							left += s.Slice(0, i).ToString();
#endif
						s = s.Slice(i + 2 + j + 2);
						--i;
					}
				}

				{
					int k = s.Slice(i + 1).IndexOfAny(beginComments);
					if (k < 0)
						break;
					i += k + 1;
				}
			} while (i >= 0);

			s = s.TrimEnd();
			if (left is null)
			{
				token = new LexicalToken(TokenType, stream.Position, s.Length);
			}
			else
			{
#if NET5_0_OR_GREATER
				string value = String.Concat(left, s);
#else
				string value = left + s.ToString();
#endif
				token = new LexicalToken(TokenType, stream.Position, n, (_, _) => value);
			}
			stream.Forward(n);
			return token;
		}
		private static readonly char[] BeginComments = { '/', '#', '<' };

		private static bool IsWhiteSpace(char value) => value <= '\xFF' ? value is <= ' ' or >= '\x7f' and <= '\xa0' : Char.IsWhiteSpace(value);
	}

	private class InlineTextTokenRule: LexicalTokenRule
	{
		public InlineTextTokenRule(): this(TEXT)
		{
		}

		private InlineTextTokenRule(LexicalTokenType textTokenType)
		{
			TokenType = textTokenType;
		}

		public override string BeginningChars => "<";

		public LexicalTokenType TokenType { get; }

		public override bool TestBeginning(char ch) => ch == '<';

		public override LexicalToken TryParse(ref CharStream stream)
		{
			const char Begin = '<';
			const char End = '>';
			int width = 0;
			while (stream[width] == Begin)
				++width;
			if (width < 2)
				return LexicalToken.Empty;

			int at = stream.Position;

			int i = stream.IndexOfAny(CrLf, 2);
			if (i < 0)
				return LexicalToken.Empty;
			stream.Forward(i + stream.NewLineSize(i));

			bool nl = true;
			int count = 0;

			i = stream.IndexOf(ch =>
			{
				if (Char.IsWhiteSpace(ch))
				{
					if (ch is not ('\n' or '\r'))
						return false;
					nl = true;
					count = 0;
					return false;
				}
				if (ch == End)
				{
					if (nl)
						return ++count == width;
					count = 0;
					return false;
				}

				nl = false;
				return false;
			});
			if (i < 0)
			{
				stream.Move(at);
				return LexicalToken.Empty;
			}

			var slice = stream.Slice(0, i - width + 1);
			stream.Forward(i + 1);
			i = slice.LastIndexOfAny(CrLf);
			if (i <= 0)
				return new LexicalToken(TokenType, at, stream.Position - at, (_, _) => String.Empty);
			if (slice[i - 1] == (slice[i] == '\n' ? '\r': '\n'))
				--i;
			var text = Strings.CutIndents(slice.Slice(0, i), stream.TabSize);
			return new LexicalToken(TokenType,at, stream.Position - at, (_, _) => text);
		}
	}
	private static readonly char[] CrLf = { '\r', '\n' };
	#endregion

	private Exception SyntaxException(in LexicalToken token, in CharStream stream, string? message)
	{
		return stream.SyntaxException(message, _sourceName, token.Position);
	}

	private void PushNode(Node value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		_nodesStack.Push(value);
		_currentNodePath.Append('/').Append(value.Name);
	}

	private void PopNode()
	{
		Node node = _nodesStack.Pop();
		_currentNodePath.Length -= node.Name.Length + 1;
	}

	private string CurrentNodePath => _currentNodePath.ToString();

	private LexicalToken CheckOptions(ref CharStream stream)
	{
		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).TokenType.Is(TOKEN, OPTION, CONFIG) || token.TokenType.Is(LexicalTokenType.NEWLINE))
		{
			if (token.TokenType.Is(TOKEN))
				ScanOptions(token.TokenType.Is(TOKEN, CONFIG), ref stream);
		}

		return token;
	}

	private struct Options
	{
		public bool IgnoreCase;
	}

	private Options _options;

	private Node? ScanNode(ref CharStream stream)
	{
		Options opt = _options;

		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).Is(LexicalTokenType.INDENT) || token.Is(LexicalTokenType.NEWLINE))
		{
		}
		if (stream.Eof)
			return null;
		Back();

		// Assume _nodeContext
		token = CheckOptions(ref stream);

		if (token.IsEof)
		{
			_options = opt;
			return null;
		}

		ScanBeginNode(ref stream, token, out var node);

		token = CheckOptions(ref stream);

		if (stream.Eof)
		{
			_options = opt;
			return node;
		}

		if (token.TokenType != LexicalTokenType.INDENT)
		{
			if (token.TokenType.Is(TOKEN, ENDNODE))
			{
				token = _nodeScanner.Next(ref stream);
				if (!token.GetSpan(stream).Equals(node.Name.AsSpan(), StringComparison.Ordinal))
					throw SyntaxException(token, stream, SR.ExpectedEndOfNode(node.Name));
				ScanNodeValue(node, ref stream);
			}
			else if (token.TokenType.Is(TEXT))
			{
				if (node.Value is { Length: >0 })
					node.AppendNewLine();
				node.AppendValue(token.GetString(stream));
			}
			else
			{
				Back();
			}
			_options = opt;
			return node;
		}

		PushNode(node);

		while ((token = _nodeScanner.Next(ref stream)).TokenType != LexicalTokenType.UNDENT)
		{
			if (token.TokenType.Is(TOKEN))
			{
				ScanToken(node, ref stream, token);
			}
			else if (token.TokenType == LexicalTokenType.IDENTIFIER)
			{
				Back();
				Node? child = ScanNode(ref stream);
				if (child != null)
					node.AddChild(child);
			}
			else if (token.TokenType.Is(TEXT))
			{
				node.AppendValue(token.GetString(stream));
			}
			else if (!token.IsEof && !token.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				throw SyntaxException(token, stream, SR.ExpectedEndOfLine());
			}
		}
		PopNode();

		token = _nodeScanner.Next(ref stream);
		if (!token.IsEof)
		{
			if (token.TokenType.Is(TOKEN, ENDNODE))
			{
				token = _nodeScanner.Next(ref stream);
				if (!token.GetSpan(stream).Equals(node.Name.AsSpan(), StringComparison.Ordinal))
					throw SyntaxException(token, stream, SR.ExpectedEndOfNode(node.Name));
				ScanAttribValue(ref stream);
			}
			else
			{
				Back();
			}
		}
		_options = opt;
		return node;
	}

	private void ScanBeginNode(ref CharStream stream, LexicalToken token, out Node node)
	{
		SyntaxRuleCollection? rules = _syntaxRules.GetApplicableRules(CurrentNodePath);

		if (token.TokenType.Is(LexicalTokenType.IDENTIFIER))
		{
			node = new Node(token.GetString(stream), _options.IgnoreCase);
			SyntaxRule? rule = rules?.Find(CurrentNodePath, node.Name);
			if (rule == null)
				ScanNodeValue(node, ref stream);
			else
				ScanNodeArguments(node, rule, ref stream);
		}
		else if (token.TokenType.Is(TOKEN, ANONYMOUS))
		{
			SyntaxRule? rule = rules?.Find(CurrentNodePath);
			if (rule == null)
				throw SyntaxException(token, stream, SR.RuleNotFound());
			node = new Node(rule.NodeName!, _options.IgnoreCase);
			if (rule.Attrib.Length == 0)
				ScanNodeValue(node, ref stream);
			else
				ScanNodeArguments(node, rule, ref stream);
		}
		else
		{
			throw SyntaxException(token, stream, SR.ExpectedNodeName());
		}
	}

	private void ScanToken(Node node, ref CharStream stream, LexicalToken token)
	{
		switch (token.TokenType.Item)
		{
			case OPTION:
				ScanOptions(false, ref stream);
				break;

			case CONFIG:
				ScanOptions(true, ref stream);
				break;

			case ATTRIB:
				ScanAttributePair(node, ref stream);
				break;

			case LINE:
				node.AppendNewLine();
				ScanNodeValue(node, ref stream);
				break;

			case ANONYMOUS:
				Back();
				Node? child = ScanNode(ref stream);
				if (child != null)
					node.AddChild(child);
				break;

			case ARRAY:
				ScanArray(node, ref stream);
				break;

			default:
				throw SyntaxException(token, stream, null);
		}
	}

	private LexicalToken ScanAttribValue(ref CharStream stream)
	{
		var at = stream.Position;
		LexicalToken token = _attribValueScanner.Next(ref stream);
		if (token.IsEof || token.Is(LexicalTokenType.NEWLINE))
		{
			stream.Move(at);
			return LexicalToken.Empty;
		}
		LexicalToken next;
		if ((next = _attribValueScanner.Next(ref stream)).Is(LexicalTokenType.NEWLINE, LexicalTokenType.EOF))
			return token;
		throw SyntaxException(next, in stream, SR.ExpectedEndOfLine());
	}

	private void ScanNodeValue(Node node, ref CharStream stream)
	{
		var at = stream.Position;
		var token = _nodeValueScanner.Next(ref stream);
		if (token.IsEof || token.Is(LexicalTokenType.NEWLINE))
		{
			stream.Move(at);
			return;
		}
		if (token.Is(TOKEN, PARAMETER))
			ScanParameters(node, ref stream);
		else if (token.Is(TOKEN, ARRAY))
			ScanArray(node, ref stream);
		else
			node.AppendValue(token.GetString(stream));
		token = _nodeValueScanner.Next(ref stream);
		if (!token.IsEof && !token.Is(LexicalTokenType.NEWLINE))
			throw SyntaxException(token, in stream, SR.ExpectedEndOfLine());
	}

	private void ScanParameters(Node node, ref CharStream stream)
	{
		LexicalToken token = _parametersScanner.Next(ref stream);
		while (!token.TokenType.Is(TOKEN, PARAMETER) && !stream.Eof)
		{
			if (!token.Is(TEXT))
				throw SyntaxException(token, in stream, "Name of argument is expected");
			var name = token.GetString(stream);
			if (!_parametersScanner.Next(ref stream).Is(ASSIGN))
				throw SyntaxException(token, in stream, "Assign symbol is expected");
			if (!(token = _parametersScanner.Next(ref stream)).Is(TEXT))
				throw SyntaxException(token, in stream, "Argument value is expected");
			var value = token.GetString(stream);
			node.AddAttrib(name, value);
			token = _parametersScanner.Next(ref stream);
		}
	}

	private void ScanArray(Node node, ref CharStream stream)
	{
		LexicalToken current = _arrayScanner.Next(ref stream);
		while (!current.TokenType.Is(TOKEN, ARRAY) && !stream.Eof)
		{
			var n = new Node("item", _options.IgnoreCase);
			n.AppendValue(current.GetString(stream));
			node.AddChild(n);
			current = _arrayScanner.Next(ref stream);
		}
	}

	///  <summary>
	///  Parse attribute name, value pair
	///  </summary>
	///  <param name="node">Xml node where the attribute will be saved</param>
	///  <param name="stream"></param>
	///  <remarks>
	///  Syntax:
	///  <code>
	/// 		attribute	:=	NAME [node_value] [INDENT inline_value+ UNDENT]
	///  </code>
	///  </remarks>
	private void ScanAttributePair(Node node, ref CharStream stream)
	{
		LexicalToken token;
		if ((token = _nodeScanner.Next(ref stream)).TokenType != LexicalTokenType.IDENTIFIER)
			throw SyntaxException(token, stream, SR.ExpectedAttributeName());
		string name = token.GetString(stream);
		var value = ScanAttribValue(ref stream).GetString(stream);
		if ((token = _nodeScanner.Next(ref stream)).TokenType == LexicalTokenType.NEWLINE)
			token = _nodeScanner.Next(ref stream);
		if (token.TokenType != LexicalTokenType.INDENT)
		{
			Back();
			node.AddAttrib(name, value);
			return;
		}

		var text = new StringBuilder(value);
		while ((token = _nodeScanner.Next(ref stream)).TokenType != LexicalTokenType.UNDENT)
		{
			string txt;
			if (token.TokenType.Is(TEXT))
				txt = token.GetString(stream);
			else if (token.TokenType.Is(TOKEN, LINE))
				txt = ScanAttribValue(ref stream).GetString(stream);
			else
				throw SyntaxException(token, stream, SR.ExpectedMultilineAttribute());
			if (text.Length > 0)
				text.Append('\n');
			text.Append(txt);
		}
		node.AddAttrib(name, text.ToString());
	}

	/// <summary>
	/// Parse syntax options
	/// </summary>
	/// <remarks>
	/// Syntax:
	/// <code>
	/// 	options			:=	'%' option EOL
	/// 						'%%' 'include' text EOL
	/// 						'%%' 'ignore-case' text EOL
	/// 						'%%' '&lt;&lt;' 'include' text ... '%%&gt;&gt;'
	///		option			:=	node_pattern [NAME]* ['*'] [NAME]*
	///		node_pattern	:=	'..'
	///							pattern ['.' [node_name]]
	///		pattern			:=	PATTERN
	///							RULENAME ':' PATTERN
	///		WILD_CHAR		:=	'*' | '(*)' | '**' | '(**)'
	/// </code>
	/// </remarks>
	private LexicalToken ScanOptions(bool isConfig, ref CharStream stream)
	{
		LexicalToken token = _optionsScanner.Next(ref stream);
		if (!token.Is(TEXT))
			throw SyntaxException(token, in stream, SR.ExpectedNodePattern());

		string pattern = token.GetString(stream);

		var items = new List<string>();
		while ((token = _optionsScanner.Next(ref stream)).TokenType == TEXT)
		{
			items.Add(token.GetString(stream));
		}
		if (token.TokenType != LexicalTokenType.NEWLINE)
			throw SyntaxException(token, in stream, SR.ExpectedNewLine());

		if (isConfig)
		{
			string s = pattern.Replace("-", "").Replace(" ", "");
			if (String.Equals(s, "IgnoreCase", StringComparison.OrdinalIgnoreCase) ||
				String.Equals(s, "CaseInsensitive", StringComparison.OrdinalIgnoreCase))
			{
				_options.IgnoreCase = true;
			}
			else if (String.Equals(s, "CaseSensitive", StringComparison.OrdinalIgnoreCase) ||
				String.Equals(s, "DontIgnoreCase", StringComparison.OrdinalIgnoreCase) ||
				String.Equals(s, "DoNotIgnoreCase", StringComparison.OrdinalIgnoreCase))
			{
				_options.IgnoreCase = false;
			}
			else
			{
				Node top = _nodesStack.Peek();
				if (_options.IgnoreCase)
					items.Add(XmlTools.OptionIgnoreCase);
				IEnumerable<XmlLiteNode>? xx = _optionHandler?.Invoke(ref this, pattern, items);
				if (xx != null)
					top.AddChild(xx.Select(Node.FromXml));
			}
		}
		else // if (!(items.Count == 0 && _syntaxRules.Add(CurrentNodePath, pattern)))
		{
			_syntaxRules.Add(CurrentNodePath, pattern, _options.IgnoreCase, items);
		}

		return token;
	}

	///	parameters	:=	[[value] delimiter]* [rest] EOL
	private void ScanNodeArguments(Node node, SyntaxRule rule, ref CharStream stream)
	{
		int i;
		bool comma = false;
		LexicalToken token;
		for (i = 0; i < rule.Attrib.Length - 1; ++i)
		{
			token = _nodeArgumentsScanner.Next(ref stream);
			if (token.IsEof || token.TokenType.Is(LexicalTokenType.NEWLINE))
				return;
			if (token.TokenType.Is(SEPARATOR))
			{
				if (!comma)
					continue;
				--i;
				comma = false;
			}
			else
			{
				if (rule.Attrib[i] == "*")
					node.AppendValue(token.GetString(stream));
				else
					node.AddAttrib(rule.Attrib[i], token.GetString(stream));
				comma = true;
			}
		}

		int at = stream.Position;
		token = _nodeArgumentsScanner.Next(ref stream);
		if (token.IsEof || token.Is(LexicalTokenType.NEWLINE))
			return;
		if (!token.TokenType.Is(SEPARATOR))
			stream.Move(at);
		if (i >= rule.Attrib.Length || rule.Attrib[i] == "*")
			ScanNodeValue(node, ref stream);
		else
			node.AddAttrib(rule.Attrib[i], ScanAttribValue(ref stream).GetString(stream));
	}

	public class Node
	{
		private List<KeyValuePair<string, string>>? _attrib;
		private List<Node>? _child;
		private string? _value;

		public Node(string name, bool ignoreCase)
		{
			Name = name;
			IgnoreCase = ignoreCase;
		}

		public string Name { get; }
		public bool IgnoreCase { get; }

		public string Value => _value ?? String.Empty;

		public IReadOnlyList<KeyValuePair<string, string>>? Attribs => _attrib;

		public List<Node>? Children => _child;

		public void AddAttrib(string name, string value)
		{
			(_attrib ??= new(2)).Add(KeyValue.Create(name, value));
		}

		public void AppendValue(string value)
		{
			if (_value is { Length: >0 })
				_value += value;
			else
				_value = value;
		}

		public void AppendNewLine()
		{
			if (_value is { Length: >0 })
				_value += "\n";
			else
				_value = "\n";
		}

		public void AddChild(Node node)
		{
			(_child ??= new()).Add(node);
		}

		public void AddChild(IEnumerable<Node> nodes)
		{
			(_child ??= new()).AddRange(nodes);
		}

		public static Node FromXml(XmlLiteNode xml)
		{
			if (xml is null)
				throw new ArgumentNullException(nameof(xml));

			var x = new Node(xml.Name, xml.Comparer.Equals("x", "X"))
			{
				_value = xml.Value
			};
			foreach (var item in xml.Attributes)
			{
				x.AddAttrib(item.Key, item.Value);
			}
			if (xml.Elements.Count > 0)
				x.AddChild(xml.Elements.Select(FromXml));
			return x;
		}
	}

	private class SyntaxRuleCollection
	{
		private readonly List<SyntaxRule> _rule = new List<SyntaxRule>();

		public void Add(string path, string pattern, bool ignoreCase, List<string> attribs)
		{
			if (pattern.StartsWith("..", StringComparison.Ordinal))
			{
				if (_rule.Count == 0)
					throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null);
				if (pattern.Length > 2)
					_rule[_rule.Count - 1].Append(pattern.Substring(2));
				_rule[_rule.Count - 1].Append(attribs);
				return;
			}

			if (attribs.Count == 0)
			{
				List<SyntaxRule> temp = _rule.FindAll(o=> String.Equals(o.RuleName, pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal));
				if (temp.Count > 0)
				{
					foreach (var item in temp)
					{
						var r = item.CreateFromTemplate(path, ignoreCase);
						if (r != null)
							_rule.Add(r);
					}
					return;
				}
			}

			var rule = SyntaxRule.Create(path, pattern, attribs.ToArray(), ignoreCase);
			if (rule != null)
				_rule.Add(rule);
		}

		public SyntaxRuleCollection? GetApplicableRules(string root)
		{
			SyntaxRuleCollection? collection = null;
			foreach (var rule in _rule)
			{
				if (rule.IsApplicable(root))
					(collection ??= new SyntaxRuleCollection())._rule.Add(rule);
			}
			return collection;
		}

		/// <summary>
		/// Find the first rule that can be applied to specified <paramref name="root"/> path and has predefined node name.
		/// </summary>
		/// <param name="root">The root path to find rule.</param>
		/// <returns>First found <see cref="SyntaxRule"/>.</returns>
		public SyntaxRule? Find(string root)
		{
			for (int i = _rule.Count - 1; i >= 0; --i)
			{
				if (_rule[i].TryMatch(root, ""))
					return _rule[i];
			}
			return null;
		}

		/// <summary>
		/// Find the first rule which can be used with specified root path and node name.
		/// </summary>
		/// <param name="root">Root path to find rule</param>
		/// <param name="name">Node name</param>
		/// <returns>The first applicable rule</returns>
		public SyntaxRule? Find(string root, string name)
		{
			for (int i = _rule.Count - 1; i >= 0; --i)
			{
				if (_rule[i].TryMatch(root, name))
					return _rule[i];
			}
			return null;
		}

		public void Clear() => _rule.Clear();
	}

	private class SyntaxRule
	{
		Regex? _pattern;
		string? _start;
		readonly string? _ruleName;
		string? _nodeName;
		string[] _attrib;
		readonly bool _permanent;
		readonly bool _ignoreCase;

		private SyntaxRule(string ruleName, string nodeName, string[] attrib, bool ignoreCase)
		{
			_ruleName = ruleName;
			_nodeName = nodeName.TrimToNull();
			_attrib = attrib;
			_ignoreCase = ignoreCase;
		}

		private SyntaxRule(string? nodeName, string[] attrib, string start, Regex? pattern, bool ignoreCase, bool permanent)
		{
			_nodeName = nodeName.TrimToNull();
			_attrib = attrib;
			_start = start;
			_pattern = pattern;
			_ignoreCase = ignoreCase;
			_permanent = permanent;
		}

		public static SyntaxRule? Create(string? path, string? node, string[]? attribs, bool ignoreCase)
		{
			return Create(path, node, attribs, ignoreCase, true);
		}

		private static SyntaxRule? Create(string? path, string? node, string[]? attribs, bool ignoreCase, bool extractName)
		{
			if (node == null || (node = node.Trim(TrimmerChars)).Length == 0)
				return null;
			attribs ??= Array.Empty<string>();
			path ??= "";

			string? name = null;
			if (extractName && (name = NameRex.Match(node).Value).Length > 1)
			{
				node = node.Substring(name.Length).Trim();
				if (node.Length == 0)
					node = ".";

				return new SyntaxRule(name.Substring(0, name.Length - 1), node, attribs, ignoreCase);
			}

			if ((path = path.Trim(TrimmerChars)).Length == 0)
				path = node;
			else
				path += "/" + node;

			int i = path.LastIndexOfAny(StarOrSlash);
			if (i < 0)
			{
				name = path;
				path = "";
			}
			else if (i < path.Length - 1 && path[i] == '/')
			{
				name = path.Substring(i + 1).TrimStart(TrimmerChars);
				path = path.Substring(0, i + 1).TrimEnd(TrimmerChars);
			}

			string start;
			Regex? pattern;
			bool permanent = false;
			if (path.IndexOfAny(Star) < 0)
			{
				pattern = null;
				start = path;
			}
			else
			{
				i = -1;
				// (\*)+|\((\*+)\)
				string patternString = PrepareRex.Replace(Regex.Escape(path), m =>
					{
						if (i == -1)
							i = m.Index;
						string s;
						if (m.Value[1] == '(')
						{
							s = m.Value.Length == 6 ? "[^/]*": ".*";
							permanent = true;
						}
						else
						{
							s = m.Value.Length == 2 ? "([^/]*)": "(.*)";
						}
						return s;
					});
				pattern = new Regex(@"\A" + patternString + @"\z", ignoreCase ? RegexOptions.IgnoreCase: RegexOptions.None);
				start = path.Substring(0, i).TrimEnd(TrimmerChars);
			}

			return new SyntaxRule(name, attribs, start, pattern, ignoreCase, permanent);
		}
		private static readonly char[] Star = { '*' };
		private static readonly char[] StarOrSlash = { '*', '/' };
		private static readonly char[] TrimmerChars = { '/', ' ', '\t' };
		private static readonly Regex NameRex = new Regex(@"^[a-zA-Z0-9~!@$&+=_-]+:");
		private static readonly Regex PrepareRex = new Regex(@"(\\\*)+|\\\((\\\*)+\\\)");


		public SyntaxRule? CreateFromTemplate(string path, bool ignoreCase)
		{
			return _ruleName == null ? null: Create(path, _nodeName, _attrib, ignoreCase, false);
		}

		public string? RuleName => _ruleName;

		public string? NodeName => _nodeName;

		public string[] Attrib => _attrib;

		public void Append(string value)
		{
			if (value is not { Length: >0 })
				return;
			string[] attrib = new string[_attrib.Length + 1];
			Array.Copy(_attrib, attrib, _attrib.Length);
			attrib[attrib.Length - 1] = value;
			_attrib = attrib;
		}

		public void Append(List<string> value)
		{
			if (value is not { Count: >0 })
				return;
			string[] attrib = new string[_attrib.Length + value.Count];
			Array.Copy(_attrib, attrib, _attrib.Length);
			value.CopyTo(attrib, _attrib.Length);
			_attrib = attrib;
		}

		public bool IsApplicable(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (_start == null)
				return false;
			path = path.Trim(TrimmerChars);
			if (_pattern == null)
				return String.Equals(path, _start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);

			return _start.Length < path.Length ?
				path.StartsWith(_start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal):
				_start.StartsWith(path, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);
		}

		public bool TryMatch(string path, string node)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			path = path.Trim(TrimmerChars);
			string name = node.Trim(TrimmerChars);

			if (name.Length == 0 && _nodeName == null) return false;

			string test;
			if (_nodeName == null || node.Length == 0)
				test = name.Length == 0 ? path: path.Length == 0 ? name: path + "/" + name;
			else if (String.Equals(_nodeName, name, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal))
				test = path;
			else
				return false;

			if (_pattern == null)
				return String.Equals(_start, test, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);

			Match m = _pattern.Match(test);
			if (!m.Success)
				return false;

			if (!_permanent)
			{
				_start = path;
				_pattern = null;
				_nodeName ??= name;
			}
			else if (m.Groups.Count > 1)
			{
				int i = 0;
				string patternString = Regex.Replace(_pattern.ToString(), @"\(\[\^/]\*\)|\(\.\*\)", _ => m.Groups[++i].Value);
				_pattern = new Regex(patternString, _ignoreCase ? RegexOptions.IgnoreCase: RegexOptions.None);
			}

			return true;
		}
	}
}
