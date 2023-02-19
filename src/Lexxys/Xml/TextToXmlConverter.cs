// Lexxys Infrastructural library.
// file: TextToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;

#pragma warning disable CA1845 // Use span-based 'string.Concat'
#pragma warning disable CA1307 // Specify StringComparison for clarity

namespace Lexxys.Xml
{
	using Tokenizer;

	public delegate IEnumerable<XmlLiteNode>? TextToXmlOptionHandler(TextToXmlConverter converter, string option, IReadOnlyCollection<string> parameters);
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
	/// 					named_ruile [parameters]
	/// pattern			:=	[TEXT_WITH_STAR '/']* (TEXT_WITH_STAR | NAME)
	/// named_ruile		:=	NAME ':' pattern
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
	///	text_value		:=	'&lt;&lt;' [NAME] [comment] EOL TEXT EOL [SPACE] '&gt;&gt;' [NAME] [comment] EOL
	///	parameter_value	:=	[simple_value [delimiter simple_value]*] [TEXT_WITHOUT_EOL] EOL
	///	array_value		:=	'[' [array_items] ']'
	///	array_items		:=	array_item [',' array_items]
	///	array_item		:=	sumple_value
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
	///	subnode			:=	node
	/// 
	///	eq_name			:= ['=' | '@' | ':'] NAME
	/// 
	///	endnode			:=	'/' NAME
	///	
	/// delimiter		:=	' ' | ',' | ';' | '=' | '=>'
	/// 
	/// macro_element	:= '${{' reference ['|' default_value ] '}}'
	/// </code>
	/// </remarks>
	public class TextToXmlConverter
	{
		private readonly CharStream _stream;
		private readonly TokenScanner5 _nodeScanner;
		private readonly TokenScanner _nodeValueScanner;
		private readonly TokenScanner _attribValueScanner;
		private readonly TokenScanner _optionsScanner;
		private readonly TokenScanner _nodeArgumentsScanner;
		private readonly TokenScanner _parametersScanner;
		private readonly TokenScanner _arrayScanner;

		private readonly string? _sourceName;
		private readonly StringBuilder _currentNodePath;
		private readonly SyntaxRuleCollection _syntaxRules;
		private readonly TextToXmlOptionHandler? _optionHandler;
		private readonly Stack<Node> _nodesStack;


		private TextToXmlConverter(CharStream stream, string? sourceName, TextToXmlOptionHandler? optionHandler, MacroSubstitution? macro = null)
		{
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			_nodeScanner = new TokenScanner5(_stream,
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new InlineTextTokenRule(),
				new SequenceTokenRule(TOKEN)
					.Add(LINE, "..")			// continue of value mark
					.Add(ANONYMOUS, ".")		// predefined node name mark
					.Add(ANONYMOUS, "-")		// predefined node name mark
					.Add(ATTRIB, ":")			// attributte mark
					.Add(ATTRIB, "=")			// attributte mark
					.Add(ATTRIB, "@")			// attributte mark
					.Add(ARRAY, "[")			// array mark
					.Add(ENDNODE, "/")			// endnode mark
					.Add(CONFIG, "%%")			// configuration mark
					.Add(OPTION, "%"),			// option mark (not affected to indentation)
				new UniversalTokenRule(LexicalTokenType.IDENTIFIER, ""));
			_nodeValueScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new StringTokenRule('`'),
				new InlineTextTokenRule(),
				new SequenceTokenRule(TOKEN)
					.Add(PARAMETER, "(")        // begin of parameters list
					.Add(ARRAY, "["),           // begin of array
				new TextLineTokenRule());
			_attribValueScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new StringTokenRule('`'),
				new InlineTextTokenRule(),
				new TextLineTokenRule());
			_parametersScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, false),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new SequenceTokenRule(LexicalTokenType.IGNORE, ",", ";"),
				new SequenceTokenRule(TOKEN).Add(PARAMETER, ")"),
				new SequenceTokenRule(ASSIGN, ":", "="),
				new UniversalTokenRule(TEXT, ",=:()"));
			_arrayScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, false),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new SequenceTokenRule(LexicalTokenType.IGNORE, ",", ";"),
				new SequenceTokenRule(TOKEN).Add(ARRAY, "]"),
				new UniversalTokenRule(TEXT, ",=[]"));
			_optionsScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new SequenceTokenRule(LexicalTokenType.IGNORE, ",", "=", "=>"),
				new UniversalTokenRule(TEXT, ",="));
			_nodeArgumentsScanner = new TokenScanner(_stream,
				new WhiteSpaceTokenRule(false, true),
				new CommentsTokenRule(LexicalTokenType.IGNORE, Comments),
				new SequenceTokenRule(SEPARATOR, ",", "=", "=>"),
				new InlineTextTokenRule(),
				new UniversalTokenRule(TEXT, ",="));

			_sourceName = sourceName;
			_currentNodePath = new StringBuilder(128);
			_syntaxRules = new SyntaxRuleCollection();
			_optionHandler = optionHandler;
			_nodesStack = new Stack<Node>();
		}
		private static readonly (string, string)[] Comments = new[] { ("//", "\n"), ("/*", "*/"), ("<#", "#>"), ("#", "\n") };

		class TokenScanner5: TokenScanner
		{
			private readonly OneBackFilter _push;
			public TokenScanner5(CharStream stream, params LexicalTokenRule[] rules)
				: base(stream, rules)
			{
				_push = new OneBackFilter();
				SetFilter(new IndentFilter());
				SetFilter(_push);
			}

			public void Back()
			{
				_push.Back();
			}
		}

		public string? SourceName => _sourceName;

		public string CurrentNodePath => _currentNodePath.ToString();

		public static string Convert(string text, string? sourceName = null)
			=> Convert(text, null, sourceName);

		public static string Convert(string text, TextToXmlOptionHandler? optionHandler, string? sourceName = null)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			var sb = new StringBuilder(1024);
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

			var cs = new CharStream(text);
			var converter = new TextToXmlConverter(cs, sourceName, optionHandler);
			converter.Convert(o=> ConvertToXml(writer, o));
			writer.Flush();
		}

		public static List<XmlLiteNode> ConvertLite(string text, string? sourceName = null, bool ignoreCase = false)
			=> ConvertLite(text, null, sourceName, ignoreCase);

		public static List<XmlLiteNode> ConvertLite(string text, TextToXmlOptionHandler? optionHandler, string? sourceName = null, bool ignoreCase = false)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			var converter = new TextToXmlConverter(new CharStream(text), sourceName, optionHandler);
			var result = new List<XmlLiteNode>();
			converter.Convert(o=> result.Add(ConvertToXmlLite(o, ignoreCase)));
			return result;
		}

		private void Convert(Action<Node> action)
		{
			if (_stream[0] == ':')
				return;

			Node? node = ScanNode();
			while (node != null)
			{
				action(node);
				node = ScanNode();
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
				string? subst = string.IsNullOrWhiteSpace(macro) ? defaultValue : config.GetValue(macro, defaultValue).Value;
				if (subst != null)
					value = value.Substring(0, i) + subst + value.Substring(j + 2);
				i = value.IndexOf("${{", j + 2, StringComparison.Ordinal);
			}
			return value;
		}

		private static XmlLiteNode ConvertToXmlLite(Node node, bool ignoreCase)
		{
			XmlLiteNode[]? child = null;
			if (node.HasChildren)
			{
				var cc = node.Children!;
				child = new XmlLiteNode[cc.Count];
				for (int i = 0; i < child.Length; ++i)
				{
					child[i] = ConvertToXmlLite(cc[i], ignoreCase);
				}
			}

			KeyValuePair<string, string>[]? attrib = null;
			if (node.HasAttributes)
			{
				attrib = node.Attributes!.ToArray();
				for (int i = 0; i < attrib.Length; ++i)
				{
					attrib[i] = KeyValue.Create(attrib[i].Key, SubstituteMacro(attrib[i].Value));
				}
			}
			return new XmlLiteNode(node.Name, String.IsNullOrEmpty(node.Value) ? null: SubstituteMacro(node.Value!), ignoreCase || node.IgnoreCase, attrib, child);
		}

		private static void ConvertToXml(XmlWriter writer, Node node)
		{
			writer.WriteStartElement(XmlConvert.EncodeName(node.Name));
			if (node.HasAttributes)
			{
				foreach (var item in node.Attributes!)
				{
					writer.WriteAttributeString(XmlConvert.EncodeName(item.Key), SubstituteMacro(item.Value));
				}
			}
			if (!String.IsNullOrEmpty(node.Value))
				writer.WriteValue(SubstituteMacro(node.Value!));
			if (node.HasChildren)
			{
				foreach (var item in node.Children!)
				{
					ConvertToXml(writer, item);
				}
			}
			writer.WriteEndElement();
		}


		#region Lexical Parser
		private static readonly LexicalTokenType TEXT = new LexicalTokenType(20, 1, "text");
		private static readonly LexicalTokenType TOKEN = new LexicalTokenType(21, 0, "token");
		private static readonly LexicalTokenType SEPARATOR = new LexicalTokenType(22, 0, "separator");
		private static readonly LexicalTokenType ASSIGN = new LexicalTokenType(23, 0, "assign");
		private const int ANONYMOUS = 1;
		private const int ATTRIB = 2;
		private const int LINE = 3;
		private const int ENDNODE = 5;
		private const int OPTION = 6;
		private const int CONFIG = 7;
		private const int PARAMETER = 8;
		private const int ARRAY = 9;

		private static readonly char[] CrLf = { '\r', '\n' };
		
		private class UniversalTokenRule: LexicalTokenRule
		{
			private readonly char[] _asep;
			public UniversalTokenRule(LexicalTokenType tokenType, string separators)
			{
				TokenType = tokenType;
				_asep = ((separators ?? "") + " \t\n\f").ToCharArray();
			}

			public LexicalTokenType TokenType { get; private set; }

			public override bool TestBeginning(char ch) => Array.IndexOf(_asep, ch) < 0;

			public override LexicalToken? TryParse(CharStream stream)
			{
				if (stream[0] == '"' || stream[0] == '\'')
					return StringTokenRule.ParseString(TokenType, stream, '`');

				int i = stream.IndexOfAny(_asep);
				return i <= 0 ? null: stream.Token(TokenType, i);
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

			public override bool TestBeginning(char ch) => ch is not ('\r' or '\n');

			public override LexicalToken? TryParse(CharStream stream)
			{
				int n = stream.IndexOfAny(CrLf);
				if (n < 0)
					n = stream.Length;
				if (n == 0)
					return null;

				if (stream[0] == '`')
					return stream.Token(TokenType, n, stream.Substring(1, n - 1).Trim());

				string s = stream.Substring(0, n);
				int i = s.IndexOfAny(BeginComments);
				while (i >= 0)
				{
					if (i == 0 || IsWhiteSpace(s[i - 1]))
					{
						var c = s[i];
						if (c == '#')
						{
							s = s.Substring(0, i);
							break;
						}
						if (i + 1 >= s.Length)
							break;

						if (c == '<')
						{
							if (s[i + 1] == '#')
							{
								int k = s.IndexOf("#>", i + 2, StringComparison.Ordinal);
								if (k < 0)  // Multiline comments in value
								{
									n -= s.Length - i;
									s = s.Substring(0, i);
									break;
								}
								s = s.Substring(0, i) + s.Substring(k + 2);
								--i;
							}
						}
						else // if (c == '/')
						{
							var c2 = s[i + 1];
							if (c2 == '/')
							{
								s = s.Substring(0, i);
								break;
							}
							if (c2 == '*')
							{
								int k = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
								if (k < 0)  // Multiline comments in value
								{
									n -= s.Length - i;
									s = s.Substring(0, i);
									break;
								}
								s = s.Substring(0, i) + s.Substring(k + 2);
								--i;
							}
						}
					}
					i = s.IndexOfAny(BeginComments, i + 1);
				}

				return stream.Token(TokenType, n, s.Trim());
			}
			private static readonly char[] BeginComments = new[] { '/', '#', '<' };

			private static bool IsWhiteSpace(char value)
			{
				return value <= '\xFF' ? value <= ' ' || value == '\xa0' || value == '\x85': Char.IsWhiteSpace(value);
			}
		}

		private class InlineTextTokenRule: LexicalTokenRule
		{
			readonly LexicalTokenRule _comments;

			public InlineTextTokenRule()
				: this(TEXT)
			{
			}

			private InlineTextTokenRule(LexicalTokenType textTokenType)
			{
				TokenType = textTokenType;
				_comments = new CppCommentsTokenRule();
			}

			public override string BeginningChars
			{
				get { return "<"; }
			}

			public LexicalTokenType TokenType { get; private set; }

			public override bool TestBeginning(char ch)
			{
				return ch == '<';
			}

			public override LexicalToken? TryParse(CharStream stream)
			{
				if (stream[0] != '<' || stream[1] != '<')
					return null;

				static bool NonSpace(char c) => c == '\n' || !Char.IsWhiteSpace(c) && !Char.IsControl(c);

				int at = stream.Position;
				stream.Forward(stream.IndexOf(NonSpace, 2));

				string pattern = ">>";
				if (stream[0] != '\n')
				{
					int k = stream.Match(Char.IsLetterOrDigit);
					if (k > 0)
						pattern += stream.Substring(0, k);
					stream.Forward(stream.IndexOf(NonSpace, k));

					while (stream[0] != '\n')
					{
						if (_comments.TryParse(stream) == null)
						{
							stream.Move(at);
							return null;
						}
						stream.Forward(stream.IndexOf(NonSpace));
					}
				}

				int start = -1;
				int index = 0;
				char[] pat = pattern.ToCharArray();
				var lines = new List<string>();
				var rest = new StringBuilder();
				bool PatMatch(char ch, int pos)
				{
					if (ch == '\n')
					{
						if (start >= 0 && index == pat.Length)
							return true;
						lines.Add(rest.ToString().TrimEnd());
						rest.Length = 0;
						start = pos;
						index = 0;
						return false;
					}
					rest.Append(ch);
					if (start >= 0)
					{
						if (!(Char.IsWhiteSpace(ch) || Char.IsControl(ch)) && (index == pat.Length || ch != pat[index++]))
						{
							index = 0;
							start = -1;
						}
					}
					return false;
				}

				int i = stream.IndexOf(PatMatch, 1);
				if (i < 0)
					return null;
				stream.Forward(i);
				string text = lines.Count == 0 ? String.Empty: Strings.CutIndents(lines.ToArray(), stream.TabSize);
				return new LexicalToken(TokenType, text, at, stream.Culture);
			}
		}
		#endregion

		private Exception SyntaxException(TokenScanner scanner, string? message)
		{
			return scanner.SyntaxException(message, _sourceName);
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

		private void CheckOptions()
		{
			while (_nodeScanner.Next().TokenType.Is(TOKEN, OPTION, CONFIG) || _nodeScanner.Current.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				if (_nodeScanner.Current.TokenType.Is(TOKEN))
					ScanOptions(_nodeScanner.Current.TokenType.Is(TOKEN, CONFIG));
			}
		}

		private struct Options
		{
			public bool IgnoreCase;
		}

		private Options _options;

		private Node? ScanNode()
		{
			Options opt = _options;

			while (_nodeScanner.Next().TokenType.Is(LexicalTokenType.INDENT) || _nodeScanner.Current.TokenType.Is(LexicalTokenType.NEWLINE))
				;
			_nodeScanner.Back();

			// Assume _nodeContext
			CheckOptions();

			if (_nodeScanner.EOF)
			{
				_options = opt;
				return null;
			}

			Node node = ScanBeginNode();

			CheckOptions();

			if (_nodeScanner.EOF)
			{
				_options = opt;
				return node;
			}

			if (_nodeScanner.Current.TokenType != LexicalTokenType.INDENT)
			{
				if (_nodeScanner.Current.TokenType.Is(TOKEN, ENDNODE))
				{
					if (_nodeScanner.Next().Text != node.Name)
						throw SyntaxException(_nodeScanner, SR.ExpectedEndOfNode(node.Name));
					ScanNodeValue(node);
				}
				else
				{
					_nodeScanner.Back();
				}
				_options = opt;
				return node;
			}

			PushNode(node);

			while (_nodeScanner.Next().TokenType != LexicalTokenType.UNDENT)
			{
				LexicalToken token = _nodeScanner.Current;
				if (token.TokenType.Is(TOKEN))
				{
					ScanToken(node);
				}
				else if (token.TokenType == LexicalTokenType.IDENTIFIER)
				{
					_nodeScanner.Back();
					Node? child = ScanNode();
					if (child != null)
						node.AddChild(child);
				}
				else if (token.TokenType.Is(TEXT))
				{
					node.AppendNlValue(token.Text);
				}
				else if (!token.TokenType.Is(LexicalTokenType.NEWLINE))
				{
					throw SyntaxException(_nodeScanner, SR.ExpectedEndOfLine());
				}
			}
			PopNode();

			if (_nodeScanner.MoveNext())
				if (_nodeScanner.Current.TokenType.Is(TOKEN, ENDNODE))
				{
					if (_nodeScanner.Next().Text != node.Name)
						throw SyntaxException(_nodeScanner, SR.ExpectedEndOfNode(node.Name));
					ScanAttribValue();
				}
				else
				{
					_nodeScanner.Back();
				}
			_options = opt;
			return node;
		}

		private Node ScanBeginNode()
		{
			LexicalToken token = _nodeScanner.Current;
			Node node;
			SyntaxRuleCollection? rules = _syntaxRules.GetApplicatbleRules(CurrentNodePath);

			if (token.TokenType.Is(LexicalTokenType.IDENTIFIER))
			{
				node = new Node(token.Text, _options.IgnoreCase);
				SyntaxRule? rule = rules?.Find(CurrentNodePath, node.Name);
				if (rule == null)
					ScanNodeValue(node);
				else
					ScanNodeArguments(node, rule);
			}
			else if (token.TokenType.Is(TOKEN, ANONYMOUS))
			{
				SyntaxRule? rule = rules?.Find(CurrentNodePath);
				if (rule == null || rule.NodeName == null)
					throw SyntaxException(_nodeScanner, SR.RuleNotFound());
				node = new Node(rule.NodeName, _options.IgnoreCase);
				if (rule.Attrib.Length == 0)
					ScanNodeValue(node);
				else
					ScanNodeArguments(node, rule);
			}
			else
			{
				throw SyntaxException(_nodeScanner, SR.ExpectedNodeName());
			}
			return node;
		}

		private void ScanToken(Node node)
		{
			switch (_nodeScanner.Current.TokenType.Item)
			{
				case OPTION:
					ScanOptions(false);
					break;

				case CONFIG:
					ScanOptions(true);
					break;

				case ATTRIB:
					ScanAttributtePair(node);
					break;

				case LINE:
					node.AppendNlValue();
					ScanNodeValue(node);
					break;

				case ANONYMOUS:
					_nodeScanner.Back();
					Node? child = ScanNode();
					if (child != null)
						node.AddChild(child);
					break;

				case ARRAY:
					ScanArray(node);
					break;

				default:
					throw SyntaxException(_nodeScanner, null);
			}
		}

		private string ScanAttribValue()
		{
			var at = _attribValueScanner.Stream.Position;
			if (_attribValueScanner.Next().TokenType.Is(LexicalTokenType.NEWLINE))
			{
				_attribValueScanner.Stream.Move(at);
				return "";
			}
			var value = _attribValueScanner.Current.Text;
			if (_attribValueScanner.Next().TokenType.Is(LexicalTokenType.NEWLINE))
				return value;
			throw SyntaxException(_attribValueScanner, SR.ExpectedEndOfLine());
		}

		private void ScanNodeValue(Node node)
		{
			var at = _nodeValueScanner.Stream.Position;
			var token = _nodeValueScanner.Next();
			if (token.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				_nodeValueScanner.Stream.Move(at);
				return;
			}
			if (token.Is(TOKEN, PARAMETER))
				ScanParameters(node);
			else if (token.Is(TOKEN, ARRAY))
				ScanArray(node);
			else
				node.AppendValue(token.Text);
			if (!_nodeValueScanner.Next().TokenType.Is(LexicalTokenType.NEWLINE) && !_nodeValueScanner.EOF)
				throw SyntaxException(_nodeValueScanner, SR.ExpectedEndOfLine());
		}

		private void ScanParameters(Node node)
		{
			while (!_parametersScanner.Next().TokenType.Is(TOKEN, PARAMETER) && !_arrayScanner.EOF)
			{
				if (!_parametersScanner.Current.Is(TEXT))
					throw SyntaxException(_parametersScanner, "Name of argument is expected");
				var name = _parametersScanner.Current.Text;
				if (!_parametersScanner.Next().Is(ASSIGN))
					throw SyntaxException(_parametersScanner, "Assing symbol is expected");
				if (!_parametersScanner.Next().Is(TEXT))
					throw SyntaxException(_parametersScanner, "Argument value is expected");
				var value = _parametersScanner.Current.Text;
				node.AddAttrib(name, value);
			}
		}

		private void ScanArray(Node node)
		{
			while (!_arrayScanner.Next().TokenType.Is(TOKEN, ARRAY) && !_arrayScanner.EOF)
			{
				var n = new Node("item", _options.IgnoreCase);
				n.AppendValue(_arrayScanner.Current.Text);
				node.AddChild(n);
			}
		}

		/// <summary>
		/// Parse attributte name, value pair
		/// </summary>
		/// <param name="node">Xml node where the attribute will be saved</param>
		/// 
		/// <remarks>
		/// Syntax:
		/// <code>
		///		attribute	:=	NAME [node_value] [INDENT inline_value+ UNDENT]
		/// </code>
		/// </remarks>
		private void ScanAttributtePair(Node node)
		{
			if (_nodeScanner.Next().TokenType != LexicalTokenType.IDENTIFIER)
				throw SyntaxException(_nodeScanner, SR.ExpectedAttributeName());
			string name = _nodeScanner.Current.Text;
			var value = new StringBuilder(ScanAttribValue());
			if (_nodeScanner.Next().TokenType == LexicalTokenType.NEWLINE)
				_nodeScanner.MoveNext();
			if (_nodeScanner.Current.TokenType != LexicalTokenType.INDENT)
			{
				_nodeScanner.Back();
			}
			else
			{
				var tt = new Stack<LexicalToken>();
				tt.Push(_nodeScanner.Current);
				while (_nodeScanner.Next().TokenType != LexicalTokenType.UNDENT)
				{
					tt.Push(_nodeScanner.Current);
					string text;
					if (_nodeScanner.Current.TokenType.Is(TEXT))
						text = _nodeScanner.Current.Text;
					else if (_nodeScanner.Current.TokenType.Is(TOKEN, LINE))
						text = ScanAttribValue();
					else
						throw SyntaxException(_nodeScanner, SR.ExpectedMultilineAttribute());
					if (value.Length > 0)
						value.Append('\n');
					value.Append(text);
				}
			}
			node.AddAttrib(name, value.ToString());
		}

		/// <summary>
		/// Parse syntax options
		/// </summary>
		/// 
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
		private void ScanOptions(bool isConfig)
		{
			if (!_optionsScanner.Next().TokenType.Is(TEXT))
				throw SyntaxException(_optionsScanner, SR.ExpectedNodePattern());

			string pattern = _optionsScanner.Current.Text;

			var items = new List<string>();
			while (_optionsScanner.Next().TokenType == TEXT)
			{
				items.Add(_optionsScanner.Current.Text);
			}
			if (_optionsScanner.Current.TokenType != LexicalTokenType.NEWLINE)
				throw SyntaxException(_optionsScanner, SR.ExpectedNewLine());

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
					var top = _nodesStack.Peek();
					if (_options.IgnoreCase)
						items.Add(XmlTools.OptionIgnoreCase);
					var xx = _optionHandler?.Invoke(this, pattern, items);
					if (xx != null)
						top.AddChild(xx.Select(o => Node.FromXml(o)));

					// string text = _optionHandler?.Invoke(pattern, items.ToArray());
					// if (text == null)
					// 	return;
					// var cs = new CharStream(text);
					// string fileName = $"{_sourceName}({_optionsScanner.Stream.GetPosition(at).Line + 1})[%{pattern}{(" " + String.Join(", ", items)).TrimEnd()}]";
					// var converter = new TextToXmlConverter(cs, fileName, _optionHandler);
					// Node node;
					// do
					// {
					// 	node = converter.ScanNode();
					// 	if (node != null)
					// 		_nodesStack.Peek().Child.Add(node);
					// } while (node != null);
				}
			}
			else // if (!(items.Count == 0 && _syntaxRules.Add(CurrentNodePath, pattern)))
			{
				_syntaxRules.Add(CurrentNodePath, pattern, _options.IgnoreCase, items.ToArray());
			}
		}

		///	parameters	:=	[[value] delimiter]* [rest] EOL
		private void ScanNodeArguments(Node node, SyntaxRule rule)
		{
			int i;
			bool comma = false;
			for (i = 0; i < rule.Attrib.Length - 1; ++i)
			{
				if (!_nodeArgumentsScanner.MoveNext() || _nodeArgumentsScanner.Current.TokenType.Is(LexicalTokenType.NEWLINE))
					return;
				if (_nodeArgumentsScanner.Current.TokenType.Is(SEPARATOR))
				{
					if (comma)
					{
						--i;
						comma = false;
					}
				}
				else
				{
					if (rule.Attrib[i] == "*")
						node.AppendValue(_nodeArgumentsScanner.Current.Text);
					else
						node.AddAttrib(rule.Attrib[i], _nodeArgumentsScanner.Current.Text);
					comma = true;
				}
			}

			int at = _nodeArgumentsScanner.Stream.Position;
			if (_nodeArgumentsScanner.MoveNext() && !_nodeArgumentsScanner.Current.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				if (!_nodeArgumentsScanner.Current.TokenType.Is(SEPARATOR))
					_nodeArgumentsScanner.Stream.Move(at);
				if (i >= rule.Attrib.Length || rule.Attrib[i] == "*")
					ScanNodeValue(node);
				else
					node.AddAttrib(rule.Attrib[i], ScanAttribValue());
			}
		}

		private class Node
		{
			private List<KeyValuePair<string, string>>? _attribs;
			private List<Node>? _child;
			private string? _value;

			public Node(string name, bool ignoreCase)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				IgnoreCase = ignoreCase;
			}

			public string Name { get; }

			public bool IgnoreCase { get; }

			public string? Value => _value;

			public List<KeyValuePair<string, string>>? Attributes => _attribs;

			public void AddAttrib(string name, string value) => (_attribs ??= new()).Add(KeyValue.Create(name, value));

			public bool HasAttributes => _attribs?.Count > 0;
			public bool HasChildren => _child?.Count > 0;

			public List<Node>? Children => _child;

			public void AddChild(Node value) => (_child ??= new()).Add(value);

			public void AddChild(IEnumerable<Node> value) => (_child ??= new()).AddRange(value);

			public void AppendValue(string value)
			{
				if (String.IsNullOrEmpty(_value))
					_value = value;
				else
					_value += value;
			}

			public void AppendNlValue(string value)
			{
				if (String.IsNullOrEmpty(_value))
					_value = value;
				else
					_value += "\n" + value;
			}

			public void AppendNlValue()
			{
				if (!String.IsNullOrEmpty(_value))
					_value += "\n";
			}

			public static Node FromXml(XmlLiteNode xml)
			{
				if (xml is null)
					throw new ArgumentNullException(nameof(xml));
				var x = new Node(xml.Name, xml.Comparer.Equals("x", "X")) { _value = xml.Value };
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

			public void Add(string path, string pattern, bool ignoreCase, string[] attribs)
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

				if (attribs.Length == 0)
				{
					List<SyntaxRule> temp = _rule.FindAll(o=> String.Equals(o.RuleName, pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal));
					if (temp.Count > 0)
					{
						_rule.AddRange(temp.Select(o=> o.CreateFromTemplate(path, ignoreCase)).Where(o=> o != null)!);
						return;
					}
				}

				var rule = SyntaxRule.Create(path, pattern, attribs, ignoreCase);
				if (rule != null)
					_rule.Add(rule);
			}

			public SyntaxRuleCollection? GetApplicatbleRules(string root)
			{
				SyntaxRuleCollection? colletion = null;
				foreach (var rule in _rule)
				{
					if (rule.IsApplicatble(root))
					{
						(colletion ??= new())._rule.Add(rule);
					}
				}
				return colletion;
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
					if (_rule[i].TryMatch(root))
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
		}

		private class SyntaxRule
		{
			Regex? _pattern;
			string? _start;
			string? _ruleName;
			string? _nodeName;
			string[] _attrib;
			bool _permanent;
			bool _ignoreCase;

			public SyntaxRule(string? nodeName = null, string? ruleName = null, string? start = null, Regex? pattern = null, string[]? attrib = null, bool permanent = false, bool ignoreCase = false)
			{
				_pattern = pattern;
				_start = start;
				_ruleName = ruleName;
				_nodeName = nodeName.TrimToNull();
				_attrib = attrib ?? Array.Empty<string>();
				_permanent = permanent;
				_ignoreCase = ignoreCase;
			}

			public static SyntaxRule? Create(string path, string node, string[] attribs, bool ignoreCase)
			{
				return Create(path, node, attribs, ignoreCase, true);
			}

			private static SyntaxRule? Create(string path, string? node, string[]? attribs, bool ignoreCase, bool extractName)
			{
				if (path is null)
					throw new ArgumentNullException(nameof(path));
				if (node == null || (node = node.Trim(TrimedChars)).Length == 0)
					return null;

				string name;
				if (extractName && (name = NameRex.Match(node).Value).Length > 1)
				{
					node = node.Substring(name.Length).Trim();
					if (node.Length == 0)
						node = ".";

					return new SyntaxRule
					(
						ignoreCase: ignoreCase,
						ruleName: name.Substring(0, name.Length - 1),
						nodeName: node,
						attrib: attribs
					);
				}

				if ((path = path.Trim(TrimedChars)).Length == 0)
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
					name = path.Substring(i + 1).TrimStart(TrimedChars);
					path = path.Substring(0, i + 1).TrimEnd(TrimedChars);
				}
				else
				{
					name = "";
				}

				string start;
				Regex? pattern;
				bool permanent = false;
				if (path.IndexOf('*') < 0)
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
					start = path.Substring(0, i).TrimEnd(TrimedChars);
				}

				return new SyntaxRule
				(
					ignoreCase: ignoreCase,
					nodeName: name,
					attrib: attribs,
					start: start,
					pattern: pattern,
					permanent: permanent
				);
			}
			private static readonly char[] StarOrSlash = { '*', '/' };
			private static readonly char[] TrimedChars = { '/', ' ', '\t' };
			private static readonly Regex NameRex = new Regex(@"^[a-zA-Z0-9~!@$&+=_-]+:");
			private static readonly Regex PrepareRex = new Regex(@"(\\\*)+|\\\((\\\*)+\\\)");


			public SyntaxRule? CreateFromTemplate(string path, bool ignoreCase)
			{
				return _ruleName == null ? null: Create(path, _nodeName, _attrib, ignoreCase, false);
			}

			public string? RuleName => _ruleName;

			public string? NodeName => _nodeName;

			public string[] Attrib => _attrib;

			public void Append(params string[] value)
			{
				if (value == null || value.Length == 0)
					return;
				string[] attrib = new string[_attrib.Length + value.Length];
				Array.Copy(_attrib, attrib, _attrib.Length);
				Array.Copy(value, 0, attrib, _attrib.Length, value.Length);
				_attrib = attrib;
			}

			public bool IsApplicatble(string path)
			{
				if (path == null)
					throw new ArgumentNullException(nameof(path));
				if (_start == null)
					return false;
				path = path.Trim(TrimedChars);
				if (_pattern == null)
					return String.Equals(path, _start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);

				return _start.Length < path.Length ?
					path.StartsWith(_start, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal):
					_start.StartsWith(path, _ignoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal);
			}

			public bool TryMatch(string path, string? node = null)
			{
				if (path == null)
					throw new ArgumentNullException(nameof(path));

				if (node == null && _nodeName == null)
					return false;

				path = path.Trim(TrimedChars);
				string name = node?.Trim(TrimedChars) ?? "";

				string test;
				if (_nodeName == null || node == null)
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
					if (_nodeName == null && node != null)
						_nodeName = name;
				}
				else if (m.Groups.Count > 1)
				{
					int i = 0;
					string patternString = Regex.Replace(_pattern.ToString(), @"\(\[\^/]\*\)|\(\.\*\)", o => m.Groups[++i].Value);
					_pattern = new Regex(patternString, _ignoreCase ? RegexOptions.IgnoreCase: RegexOptions.None);
				}

				return true;
			}
		}
	}
}
