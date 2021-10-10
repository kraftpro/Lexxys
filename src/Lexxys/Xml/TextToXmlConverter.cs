// Lexxys Infrastructural library.
// file: TextToXmlConverter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
using Lexxys.Tokenizer;
using System.Globalization;

namespace Lexxys.Xml
{
	public delegate IEnumerable<XmlLiteNode> TextToXmlOptionHandler(string option, IReadOnlyCollection<string> parameters);
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
	///						'#&lt;' TEXT '&gt;#'
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

		private readonly string _sourceName;
		private readonly StringBuilder _currentNodePath;
		private readonly SyntaxRuleCollection _syntaxRules;
		private readonly TextToXmlOptionHandler _optionHandler;

		private TextToXmlConverter(CharStream stream, string sourceName, TextToXmlOptionHandler optionHandler, MacroSubstitution macro = null)
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
					.Add(ENDNODE, "/")          // endnode mark
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
		}
		private static readonly string[] Comments = new[] { "//", "\n", "/*", "*/", "<#", "#>", "#", "\n" };

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


		public static string Convert(string text, string sourceName = null)
		{
			return Convert(text, null, sourceName);
		}

		public static string Convert(string text, TextToXmlOptionHandler optionHandler, string sourceName = null)
		{
			var sb = new StringBuilder(1024);
			var ws = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, OmitXmlDeclaration = true };
			using (var writer = XmlWriter.Create(sb, ws))
			{
				Convert(text, writer, optionHandler, sourceName);
			}
			return sb.ToString();
		}

		public static void Convert(string text, XmlWriter writer, TextToXmlOptionHandler optionHandler, string sourceName = null)
		{
			var cs = new CharStream(text);
			var converter = new TextToXmlConverter(cs, sourceName, optionHandler);
			converter.Convert(o=> ConvertToXml(writer, o));
			writer.Flush();
		}

		//public static string Convert(TextReader reader, string fileName = null)
		//{
		//    return Convert(reader.ReadToEnd(), null, fileName);
		//}

		//public static string Convert(TextReader reader, TextToXmlOptionHandler optionHandler, string fileName = null)
		//{
		//    return Convert(reader.ReadToEnd(), optionHandler, fileName);
		//}

		//public static void Convert(TextReader reader, XmlWriter writer, TextToXmlOptionHandler optionHandler, string fileName = null)
		//{
		//    Convert(reader.ReadToEnd(), writer, optionHandler, fileName);
		//}

		public static List<XmlLiteNode> ConvertLite(string text, string sourceName = null, bool ignoreCase = false)
		{
			return ConvertLite(text, null, sourceName, ignoreCase);
		}

		public static List<XmlLiteNode> ConvertLite(string text, TextToXmlOptionHandler optionHandler, string sourceName = null, bool ignoreCase = false)
		{
			var converter = new TextToXmlConverter(new CharStream(text), sourceName, optionHandler);
			var result = new List<XmlLiteNode>();
			converter.Convert(o=> result.Add(ConvertToXmlLite(o, ignoreCase)));
			return result;
		}

		private void Convert(Action<Node> action)
		{
			if (_stream[0] == ':')
				return;

			Node node = ScanNode();
			while (node != null)
			{
				action(node);
				node = ScanNode();
			}
		}

		private static string SubstituteMacro(string value)
		{
			int i = 0;
			while (i < value.Length)
			{
				i = value.IndexOf("${{", i, StringComparison.Ordinal);
				if (i < 0)
					break;
				int j = value.IndexOf("}}", i + 3, StringComparison.Ordinal);
				if (j < 0)
					break;
				string macro = value.Substring(i + 3, j - i - 3);
				string defaultValue = null; 
				int k = macro.IndexOf('|');
				if (k >= 0)
				{
					defaultValue = macro.Substring(k + 1).Trim();
					macro = macro.Substring(0, k);
				}
				string subst = String.IsNullOrWhiteSpace(macro) ? defaultValue: Config.Default.GetValue<string>(macro) ?? defaultValue;
				if (subst != null)
					value = value.Substring(0, i) + subst + value.Substring(j + 2);
				i = j + 2;
			}
			return value;
		}

		private static XmlLiteNode ConvertToXmlLite(Node node, bool ignoreCase)
		{
			XmlLiteNode[] child = null;
			if (node.Child.Count > 0)
			{
				child = new XmlLiteNode[node.Child.Count];
				for (int i = 0; i < child.Length; ++i)
				{
					child[i] = ConvertToXmlLite(node.Child[i], ignoreCase);
				}
			}
			return new XmlLiteNode(node.Name, SubstituteMacro(node.Value.ToString()), ignoreCase || node.IgnoreCase, node.Attrib.Select(o => KeyValue.Create(o.Key, SubstituteMacro(o.Value))), child);
		}

		private static void ConvertToXml(XmlWriter writer, Node node)
		{
			writer.WriteStartElement(XmlConvert.EncodeName(node.Name));
			foreach (var item in node.Attrib)
			{
				writer.WriteAttributeString(XmlConvert.EncodeName(item.Key), SubstituteMacro(item.Value));
			}
			if (node.Value != null && node.Value.Length > 0)
				writer.WriteValue(SubstituteMacro(node.Value.ToString()));
			foreach (var item in node.Child)
			{
				ConvertToXml(writer, item);
			}
			writer.WriteEndElement();
		}


		//public static string IncludeOptionHandler(string option, params string[] parameters)
		//{
		//    if (option != "include" || parameters == null || parameters.Length == 0 || parameters[0] == null || parameters[0].Length == 0)
		//        return null;
		//    FileInfo f = Tools.FindFile(parameters[0], null);
		//    if (!f.Exists)
		//        return null;
		//    return File.ReadAllText(f.FullName);
		//}


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

		private class UniversalTokenRule: LexicalTokenRule
		{
			private readonly char[] _asep;
			public UniversalTokenRule(LexicalTokenType tokenType, string separators)
			{
				TokenType = tokenType;
				_asep = ((separators ?? "") + " \t\n\f").ToCharArray();
			}

			public LexicalTokenType TokenType { get; private set; }

			public override bool TestBeginning(char ch)
			{
				return Array.IndexOf(_asep, ch) < 0;
			}

			public override LexicalToken TryParse(CharStream stream)
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

			public override bool HasExtraBeginning
			{
				get { return true; }
			}

			public LexicalTokenType TokenType { get; private set; }

			public override bool TestBeginning(char ch)
			{
				return ch != '\n';
			}

			public override LexicalToken TryParse(CharStream stream)
			{
				int n = stream.IndexOf('\n', 0);
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
							if (i + 1 < s.Length && s[i + 1] == '<')
							{
								int k = s.IndexOf(">#", i + 2, StringComparison.Ordinal);
								if (k < 0)  // Multiline comments in value
								{
									n -= s.Length - i;
									s = s.Substring(0, i);
									break;
								}
								s = s.Substring(0, i) + s.Substring(k + 2);
								--i;
							}
							else
							{
								s = s.Substring(0, i);
								break;
							}

						}
						else // if (c == '/')
						{
							if (i + 1 >= s.Length)
								break;
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
			private static readonly char[] BeginComments = new[] { '/', '#' };

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

			public override LexicalToken TryParse(CharStream stream)
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
				return new LexicalToken(TokenType, text, at, stream.CultureInfo);
			}
		}
		#endregion

		private Exception SyntaxException(TokenScanner scanner, string message)
		{
			return scanner.SyntaxException(message, _sourceName);
		}

		readonly Stack<Node> _nodesStack = new Stack<Node>(4);

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

		private string CurrentNodePath
		{
			get { return _currentNodePath.ToString(); }
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

		private Node ScanNode()
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
					Node child = ScanNode();
					if (child != null)
						node.Child.Add(child);
				}
				else if (token.TokenType.Is(TEXT))
				{
					if (node.Value.Length > 0)
						node.Value.Append('\n');
					node.Value.Append(token.Text);
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
			SyntaxRuleCollection rules = _syntaxRules.GetApplicatbleRules(CurrentNodePath);

			if (token.TokenType.Is(LexicalTokenType.IDENTIFIER))
			{
				node = new Node(token.Text, _options.IgnoreCase);
				SyntaxRule rule = rules?.Find(CurrentNodePath, node.Name);
				if (rule == null)
					ScanNodeValue(node);
				else
					ScanNodeArguments(node, rule);
			}
			else if (token.TokenType.Is(TOKEN, ANONYMOUS))
			{
				SyntaxRule rule = rules?.Find(CurrentNodePath);
				if (rule == null)
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
					if (node.Value.Length > 0)
						node.Value.Append('\n');
					ScanNodeValue(node);
					break;

				case ANONYMOUS:
					_nodeScanner.Back();
					Node child = ScanNode();
					if (child != null)
						node.Child.Add(child);
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
				node.Value.Append(token.Text);
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
				node.Attrib[name] = value;
			}
		}

		private void ScanArray(Node node)
		{
			while (!_arrayScanner.Next().TokenType.Is(TOKEN, ARRAY) && !_arrayScanner.EOF)
			{
				var n = new Node("item", _options.IgnoreCase);
				n.Value.Append(_arrayScanner.Current.Text);
				node.Child.Add(n);
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
			node.Attrib.Add(name, value.ToString());
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
					var xx = _optionHandler?.Invoke(pattern, items);
					if (xx != null)
						top.Child.AddRange(xx.Select(o => Node.FromXml(o)));

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
						node.Value.Append(_nodeArgumentsScanner.Current.Text);
					else
						node.Attrib.Add(rule.Attrib[i], _nodeArgumentsScanner.Current.Text);
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
					node.Attrib.Add(rule.Attrib[i], ScanAttribValue());
			}
		}

		public class Node
		{
			public readonly string Name;
			public readonly Dictionary<string, string> Attrib = new Dictionary<string, string>();
			public readonly List<Node> Child = new List<Node>();
			public readonly StringBuilder Value = new StringBuilder();
			public readonly bool IgnoreCase;

			public Node(string name, bool ignoreCase)
			{
				Name = name;
				IgnoreCase = ignoreCase;
			}

			public static Node FromXml(XmlLiteNode xml)
			{
				var x = new Node(xml.Name, xml.Comparer.Equals("x", "X"));
				x.Value.Append(xml.Value);
				foreach (var item in xml.Attributes)
				{
					x.Attrib[item.Key] = item.Value;
				}
				foreach (var item in xml.Elements)
				{
					x.Child.Add(FromXml(item));
				}
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
						_rule.AddRange(temp.Select(o=> o.CreateFromTemplate(path, ignoreCase)).Where(o=> o != null));
						return;
					}
				}

				var rule = SyntaxRule.Create(path, pattern, attribs, ignoreCase);
				if (rule != null)
					_rule.Add(rule);
			}

			public SyntaxRuleCollection GetApplicatbleRules(string root)
			{
				SyntaxRuleCollection colletion = null;
				foreach (var rule in _rule)
				{
					if (rule.IsApplicatble(root))
					{
						if (colletion == null)
							colletion = new SyntaxRuleCollection();
						colletion._rule.Add(rule);
					}
				}
				return colletion;
			}

			/// <summary>
			/// Find the first rule that can be applied to specified <paramref name="root"/> path and has predefined node name.
			/// </summary>
			/// <param name="root">The root path to find rule.</param>
			/// <returns>First found <see cref="SyntaxRule"/>.</returns>
			public SyntaxRule Find(string root)
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
			public SyntaxRule Find(string root, string name)
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
			Regex _pattern;
			string _start;
			string _ruleName;
			string _nodeName;
			string[] _attrib;
			bool _permanent;
			bool _ignoreCase;

			private SyntaxRule()
			{
			}

			public static SyntaxRule Create(string path, string node, string[] attribs, bool ignoreCase)
			{
				return Create(path, node, attribs, ignoreCase, true);
			}

			private static SyntaxRule Create(string path, string node, string[] attribs, bool ignoreCase, bool extractName)
			{
				if (node == null || (node = node.Trim(TrimedChars)).Length == 0)
					return null;
				if (attribs == null)
					attribs = EmptyArray<string>.Value;
				if (path == null)
					path = "";

				string name = null;
				if (extractName && (name = NameRex.Match(node).Value).Length > 1)
				{
					node = node.Substring(name.Length).Trim();
					if (node.Length == 0)
						node = ".";

					return new SyntaxRule
					{
						_ignoreCase = ignoreCase,
						_ruleName = name.Substring(0, name.Length - 1),
						_nodeName = node,
						_attrib = attribs
					};
				}

				if ((path = path.Trim(TrimedChars)).Length == 0)
					path = node;
				else
					path += "/" + node;

				int i = path.LastIndexOfAny(StarsAndSlash);
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

				string start;
				Regex pattern;
				bool permanent = false;
				if (path.IndexOfAny(Stars) < 0)
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
				{
						_ignoreCase = ignoreCase,
						_nodeName = name == null || name.Length == 0 ? null: name,
						_attrib = attribs,
						_start = start,
						_pattern = pattern,
						_permanent = permanent
					};
			}
			private static readonly char[] Stars = { '*' };
			private static readonly char[] StarsAndSlash = { '*', '/' };
			private static readonly char[] TrimedChars = { '/', ' ', '\t' };
			private static readonly Regex NameRex = new Regex(@"^[a-zA-Z0-9~!@$&+=_-]+:");
			private static readonly Regex PrepareRex = new Regex(@"(\\\*)+|\\\((\\\*)+\\\)");


			public SyntaxRule CreateFromTemplate(string path, bool ignoreCase)
			{
				return _ruleName == null ? null: Create(path, _nodeName, _attrib, ignoreCase, false);
			}

			public string RuleName => _ruleName;

			public string NodeName => _nodeName;

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

			public bool TryMatch(string path, string node = null)
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
