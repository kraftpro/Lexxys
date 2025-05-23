using System.Text;

using Lexxys.Tokenizer;
using Lexxys.Xml;

namespace Lexxys.Configuration;

public ref partial struct CfgParser
{
	private readonly TokenScanner _nodeScanner;
	private readonly TokenScanner _nodeValueScanner;
	private readonly TokenScanner _paramValueScanner;
	private readonly TokenScanner _optionNameScanner;
	private readonly TokenScanner _optionValueScanner;
	private readonly TokenScanner _nodeArgumentsScanner;
	private readonly TokenScanner _arrayScanner;
	private readonly TokenScanner _objectScanner;

	private readonly string? _sourceName;
	private readonly SyntaxRuleCollection _syntaxRules;
	private readonly TextToXmlOptionHandler? _optionHandler;
	private readonly MacroSubstitution _macro;
	private readonly OneBackFilter _back;
	private readonly List<string> _nodePath;

	public CfgParser(): this(null)
	{
	}

	public readonly string? SourceName => _sourceName;

	public ConfigOptions Options => _options;

	private void Back() => _back.Back();

	private void Back(LexicalToken token, ref CharStream stream) => stream.Move(token.Position);

	public void Reset()
	{
		_back.Reset();
		_nodeScanner.ResetParser();
		_nodeValueScanner.ResetParser();
		_paramValueScanner.ResetParser();
		_optionNameScanner.ResetParser();
		_nodeArgumentsScanner.ResetParser();
		_objectScanner.ResetParser();
		_arrayScanner.ResetParser();

		_syntaxRules.Clear();
		_options.Clear();
		_nodePath.Clear();
	}

	public void Convert(ref CharStream stream, Action<Node> action)
	{
		if (action is null) throw new ArgumentNullException(nameof(action));
		if (stream[0] == ':') return;

		Node? node = ParseNode(ref stream);
		while (node != null)
		{
			action(node);
			node = ParseNode(ref stream);
		}
	}

	private static string GetVarValue(string name, ConfigOptions options)
	{
		int i = name.IndexOf('|');
		return i >= 0 ?
			options.GetVariableText(name[..i]) ?? name[(i + 1)..]:
			options.GetVariableText(name) ?? throw new SyntaxException($"Cannot find macro \"{name}\"");
	}

	[return: NotNullIfNotNull(nameof(value))]
	private static string? SubstituteMacro(string? value, IMacroProvider macros)
	{
		if (value is null) return value;

		var s = value.AsSpan();
		int i = s.IndexOf(BeginMacro);
		if (i < 0) return value;

		var text = new StringBuilder(value.Length * 2);
		do
		{
			text.Append(s[..i]);
			s = s[i..];
			int j = s.IndexOf(EndMacro);
			if (j < 0)
				break;
			var macro = s[3..j].Trim();
			ReadOnlySpan<char> defaultValue = default;
			int k = macro.IndexOf('|');
			if (k >= 0)
			{
				defaultValue = macro[(k + 1)..].TrimStart();
				macro = macro[..k].TrimEnd();
			}
			string? subst = macro.Length > 0 ? macros.GetValue(macro.ToString()): null;
			if (subst != null)
				text.Append(subst);
			else if (defaultValue.Length > 0)
				text.Append(defaultValue);
			else
				text.Append(s[..(j + 2)]);
			s = s[(j + 2)..];
			i = s.IndexOf(BeginMacro);
		} while (i >= 0);
		text.Append(s);
		return text.ToString();
	}
	private static readonly char[] BeginMacro = ['$', '{', '{'];
	private static readonly char[] EndMacro = ['}', '}'];

	public static List<Node> ParseConfig(string text)
	{
		var parser = new CfgParser();
		var cs = new CharStream(text);
		return parser.ParseNodeList(ref cs);
	}

	public static JsonBuilder? ParseToJson(string text)
	{
		var node = ParseConfig(text);
		return node.Count == 0 ? null: node.Count == 1 ? ConvertToJson(node[0]): ConvertToJson(node);
	}

	public static JsonBuilder ConvertToJson(IEnumerable<Node> node, bool nameValuePair = false)
	{
		if (node is null) throw new ArgumentNullException(nameof(node));
		var json = JsonBuilder.Create(new StringBuilder());

		json.Arr();
		foreach (var item in node)
		{
			if (nameValuePair)
				item.BuildJsonObject(json);
			else
				item.BuildJson(json);
		}
		json.End();
		return json;
	}

	private static JsonBuilder ConvertToJson(Node node)
		=> node.BuildJsonObject(JsonBuilder.Create(new StringBuilder()));

	//public static IXmlReadOnlyNode ConvertToXmlLite(Node node, bool ignoreCase)
	//{
	//	if (node is null) throw new ArgumentNullException(nameof(node));

	//	List<XmlLiteNode>? child = [];
	//	List<KeyValuePair<string, string>>? attrib = [];
	//	if (node.IsArray)
	//	{
	//		foreach (var item in node.Items!)
	//		{
	//			child.Add((XmlLiteNode)ConvertToXmlLite(item, ignoreCase));
	//		}
	//	}
	//	else if (!node.IsValue)
	//	{
	//		foreach (var item in node.Items!)
	//		{
	//			if (item.AttributeMark && item.IsValue)
	//				attrib.Add(new KeyValuePair<string, string>(item.Name, SubstituteMacro(item.Value) ?? ""));
	//			else
	//				child.Add((XmlLiteNode)ConvertToXmlLite(item, ignoreCase));
	//		}
	//	}
	//	return new XmlLiteNode(node.Name, SubstituteMacro(node.Value), ignoreCase, attrib, child);
	//}

	private Exception SyntaxException(in LexicalToken token, in CharStream stream, string? message)
	{
		return stream.SyntaxException(message, _sourceName, token.Position);
	}

	private Exception SyntaxException(in CharStream stream, string? message)
	{
		return stream.SyntaxException(message, _sourceName);
	}

	private Exception SyntaxException(in CharStream stream, int position, string? message)
	{
		return stream.SyntaxException(message, _sourceName, position);
	}

	private void PushNode(Node value)
	{
		if (value == null) throw new ArgumentNullException(nameof(value));
		_nodePath.Add(value.Name);
	}

	public List<Node> ParseNodeList(ref CharStream stream)
	{
		var result = new List<Node>();
		Node? node = ParseNode(ref stream);
		while (node != null)
		{
			result.Add(node);
			node = ParseNode(ref stream);
		}
		return result;
	}

	private void PopNode()
	{
		_nodePath.RemoveAt(_nodePath.Count - 1);
	}

	private readonly List<string> CurrentNodePath => _nodePath;

	//private LexicalToken CheckOptions(ref CharStream stream)
	//{
	//	LexicalToken token;
	//	while ((token = _nodeScanner.Next(ref stream)).TokenType.Is(TOKEN, OPTION) || token.TokenType.Is(LexicalTokenType.NEWLINE))
	//	{
	//		if (token.TokenType.Is(TOKEN))
	//			ScanOptions(token.TokenType.Is(TOKEN, CONFIG), ref stream);
	//	}

	//	return token;
	//}

	private ConfigOptions _options;

	internal static bool IsIdentifier(LexicalToken token) => token.Is(LexicalTokenType.IDENTIFIER, LexicalTokenType.STRING);


	private Node? ParseNode(ref CharStream stream)
	{
		LexicalToken token;
		// Skip empty lines and parse options
		while ((token = _nodeScanner.Next(ref stream)).Is(LexicalTokenType.NEWLINE, LexicalTokenType.INDENT, LexicalTokenType.UNDENT) || token.TokenType.Is(TOKEN, OPTION))
		{
			if (token.TokenType.Is(TOKEN, OPTION))
			{
				var node = ParseOptions(ref stream);
				if (node != null)
					return node;
			}
		}
		if (stream.Eof)
			return null;

		return ParseNode(token, ref stream);
	}

	private string GetStringValue(LexicalToken token, in CharStream stream)
	{
		var v = token.GetValue(stream);
		if (v is ConfigValue c)
		{
			var options = _options;
			return c.ToString(o => GetVarValue(o, options));
		}
		return v?.ToString() ?? String.Empty;
	}

	private Node? ParseNode(LexicalToken token, ref CharStream stream)
	{
		if (token.IsEof || token.Is(LexicalTokenType.INDENT, LexicalTokenType.UNDENT))
			return null;
		bool dash = token.Is(TOKEN, DASH);
		if (!dash && !token.Is(LexicalTokenType.IDENTIFIER))
			throw SyntaxException(token, stream, SR.ExpectedNodeName());

		string nodeName = GetStringValue(token, stream);
		bool attribute = false;
		int i = SkipSpace(stream);
		if (stream[i] is '=' or ':')
		{
			attribute = stream[i] == ':';
			stream.Forward(i + 1);
		}

		Node node = new Node(nodeName, position: GetPosition(token.Position, stream), attribute: attribute) { IsArrayItem = dash };

		SyntaxRule? rule = _syntaxRules.Find(CurrentNodePath, dash, _options.Comparer);

		ParseStartNode(node, rule, ref stream);

		if (dash && node.Name == "-")
			node.Name = "item";

		token = _nodeScanner.Next(ref stream);
		if (token.TokenType == LexicalTokenType.INDENT)
			ParseNodeTree(node, ref stream);
		else
			Back();
		return node;
	}

	private void ParseStartNode(Node node, SyntaxRule? rule, ref CharStream stream)
	{
		if (rule == null)
		{
			ParseNodeValue(node, ref stream);
		}
		else
		{
			throw new NotImplementedException(nameof(ParseStartNode));
		}
	}

	private void ParseNodeValue(Node node, ref CharStream stream)
	{
		var token = _nodeValueScanner.Next(ref stream);
		if (token.IsEof || token.Is(LexicalTokenType.NEWLINE)) return;

		if (token.Is(TOKEN, PARAMETER, OBJECT))
			ParseObject(node, token.Is(TOKEN, PARAMETER), ref stream);
		else if (token.Is(TOKEN, ARRAY))
			ParseArray(node, ref stream);
		else
			node.AppendValue(GetStringValue(token, stream));
		if (token.Is(LexicalTokenType.STRING))
			node.IsQuoted = true;
		token = _nodeValueScanner.Next(ref stream);
		if (!token.IsEof && !token.Is(LexicalTokenType.NEWLINE))
			throw SyntaxException(token, in stream, SR.ExpectedEndOfLine());
	}

	private void ParseObject(Node node, bool parameter, ref CharStream stream)
	{
		var offRule = parameter ? "Object": "Param";
		for (;;)
		{
			var at = stream.Position - 1;
			LexicalToken token = _objectScanner.Next(ref stream);
			if (token.TokenType.Is(TOKEN, END))
				return;

			if (stream.Eof)
				throw SyntaxException(stream, at, $"The closing {(parameter ? "parenthesis": "brace")} is not found until end of file");
			if (!token.Is(TEXT))
				throw SyntaxException(token, in stream, "Name of argument is expected");
			at = token.Position;
			var name = GetStringValue(token, stream);
			if (!(token = _objectScanner.Next(ref stream)).Is(TOKEN, ATTRIB, EQUAL))
				throw SyntaxException(token, in stream, "Assign symbol is expected");

			var inner = new Node(name, position: GetPosition(at, stream), attribute: parameter || token.Is(TOKEN, ATTRIB));

			int i = SkipSpace(stream);
			var ch = stream[i];
			if (ch is ARRAY_MARK or OBJECT_MARK or PARAM_MARK)
				stream.Forward(i + 1);

			_objectScanner.EnableRule(o => o.RuleName == "Name" || o.RuleName == offRule, false);

			if (ch == ARRAY_MARK)
				ParseArray(inner, ref stream);
			else if (ch is OBJECT_MARK or PARAM_MARK)
				ParseObject(inner, ch == PARAM_MARK, ref stream);
			else if ((token = _objectScanner.Next(ref stream)).Is(TEXT, LexicalTokenType.STRING))
				inner.Value = GetStringValue(token, stream);
			else
				throw SyntaxException(token, in stream, "Argument value is expected");

			_objectScanner.EnableRule(o => o.RuleName == "Name" || o.RuleName == offRule, true);

			node.Add(inner);
		}
	}

	private void ParseArray(Node node, ref CharStream stream)
	{
		var at = stream.Position - 1;

		for (;;)
		{
			int i = SkipSpace(stream);
			var ch = stream[i];
			if (ch is ARRAY_MARK or OBJECT_MARK or PARAM_MARK)
				stream.Forward(i + 1);


			LexicalToken token = _arrayScanner.Next(ref stream);
			if (token.Is(TOKEN, END_ARRAY))
				return;
			if (stream.Eof)
				throw SyntaxException(stream, at, "The array closing brace is not found before the end of the file");

			var item = new Node("item", position: GetPosition(token.Position, stream), arrayItem: true);
			if (token.Is(TEXT))
				item.AppendValue(GetStringValue(token, stream));
			else if (token.Is(TOKEN, PARAMETER, OBJECT))
				ParseObject(item, token.Is(TOKEN, PARAMETER), ref stream);
			else if (token.Is(TOKEN, ARRAY))
				ParseArray(item, ref stream);
			else
				throw SyntaxException(token, in stream, "Array item is expected");
			node.Add(item);
		}
	}

	private void ParseNodeTree(Node node, ref CharStream stream)
	{
		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).TokenType != LexicalTokenType.UNDENT)
		{
			if (token.TokenType.Is(TOKEN))
			{
				Node? child = ParseToken(token, node, ref stream);
				node.Add(child);
			}
			else if (token.TokenType == LexicalTokenType.IDENTIFIER)
			{
				Node? child = ParseNode(token, ref stream);
				node.Add(child);
			}
			else if (token.TokenType.Is(TEXT))
			{
				node.AppendValue(GetStringValue(token, stream));
			}
			else if (!token.IsEof && !token.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				throw SyntaxException(token, in stream, SR.ExpectedEndOfLine());
			}
		}

		token = _nodeScanner.Next(ref stream);
		if (!token.TokenType.Is(TOKEN, ENDNODE))
		{
			Back();
			return;
		}

		token = _nodeScanner.Next(ref stream);
		if (!token.GetSpan(stream).Equals(node.Name.AsSpan(), StringComparison.Ordinal))
			throw SyntaxException(token, stream, SR.ExpectedEndOfNode(node.Name));
		ParseNodeValue(node, ref stream);
	}

	private Node? ParseToken(LexicalToken token, Node node, ref CharStream stream)
	{
		switch (token.TokenType.Item)
		{
			case OPTION:
				return ParseOptions(ref stream);

			case LINE:
				node.AppendNewLine();
				ParseNodeValue(node, ref stream);
				return null;

			case DASH:
				return ParseNode(token, ref stream);

			case PARAMETER:
				ParseObject(node, true, ref stream);
				return null;

			case OBJECT:
				ParseObject(node, false, ref stream);
				return null;

			case ARRAY:
				ParseArray(node, ref stream);
				return null;

			default:
				throw SyntaxException(token, stream, null);
		}
	}

	private Node? ParseOptions(ref CharStream stream)
	{
		int i = SkipSpace(stream);
		var ch = stream[i];
		if (ch is VAR_MARK or ACTION_MARK)
			stream.Forward(i + 1);

		LexicalToken token = _optionNameScanner.Next(ref stream);
		if (!token.Is(TEXT, LexicalTokenType.STRING))
			throw SyntaxException(token, in stream, SR.ExpectedNodePattern());

		var name = GetStringValue(token, stream);
		i = SkipSpace(stream);
		if (stream[i] is ':' or '=')
			stream.Forward(i + 1);

		List<Node> parameters = ParseOptionValue(name, ref stream);

		Node? result = null;

		if (ch == VAR_MARK)
			_options.AddVariable(name, parameters);
		else if (ch != ACTION_MARK)
			_syntaxRules.Add(CurrentNodePath, name, parameters, _options.Comparer);
		else if (!_options.TryExecuteExternalAction(name, ref this, parameters, out result))
			throw SyntaxException(token, in stream, SR.ActionNotFound(name));

		return result;

	}

	private List<Node> ParseOptionValue(string name, ref CharStream stream)
	{
		var parameters = new List<Node>();
		for (;;)
		{
			var token = _optionValueScanner.Next(ref stream);
			if (token.IsEof || token.Is(LexicalTokenType.NEWLINE))
				return parameters;
			if (token.Is(TOKEN, PARAMETER, OBJECT))
			{
				var node = new Node(name, position: GetPosition(token.Position, stream), arrayItem: true);
				ParseObject(node, token.Is(TOKEN, PARAMETER), ref stream);
				parameters.Add(node);
			}
			else if (token.Is(TOKEN, ARRAY))
			{
				var node = new Node(name, position: GetPosition(token.Position, stream), arrayItem: true);
				ParseArray(node, ref stream);
				parameters.Add(node);
			}
			else if (token.Is(TEXT, LexicalTokenType.STRING))
			{
				parameters.Add(new Node(name, token.GetString(stream), position: GetPosition(token.Position, stream), arrayItem: true));
			}
			else
			{
				throw SyntaxException(token, in stream, SR.ExpectedNodePattern());
			}
		}
	}

	private FilePosition GetPosition(int position, in CharStream stream)
	{
		return new FilePosition(stream.GetCharPosition(position), _sourceName);
	}
}

internal interface IMacroProvider
{
	string? GetValue(string name);
}