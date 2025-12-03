using System.Text;

using Lexxys;
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

	public void Convert(ref CharStream stream, Action<Node> action, Action<ErrorResult> fail)
	{
		if (action is null) throw new ArgumentNullException(nameof(action));
		if (stream[0] == ':') return;

		ResultValue<Node> node = ParseNode(ref stream);
		while (node.IsSuccess)
		{
			action(node.Value);
			node = ParseNode(ref stream);
		}
		node.ActOnError(fail, 400);
	}

	private static ResultValue<string> GetVarValue(string name, ConfigOptions options)
	{
		int i = name.IndexOf('|');
		return i >= 0 ?
			options.GetVariableText(name[..i]) ?? name[(i + 1)..]:
			options.GetVariableText(name) ?? ErrorResult.Problem($"Cannot find macro \"{name}\"").AsResultValue<string>();
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

	public static (List<Node> Nodes, ErrorResult? Error) ParseConfig(string text)
	{
		var parser = new CfgParser();
		var cs = new CharStream(text);
		return parser.ParseNodeList(ref cs);
	}

	public static (JsonDumpWriter? Json, ErrorResult? Error) ParseToJson(string text)
	{
		var (nodes, error) = ParseConfig(text);
		return
			nodes.Count == 0 ?
				(null, error):
			nodes.Count == 1 ?
				(ConvertToJson(nodes[0]), error):
				(ConvertToJson(nodes), error);
	}

	public static ErrorResult? ParseTo(IDumpWriter writer, string text)
	{
		var (nodes, error) = ParseConfig(text);
		if (nodes.Count == 0 || error != null)
			return error;
		if (nodes.Count == 1)
			writer.Write("config", nodes[0]);
		else
			writer.Write("config", nodes);
		return null;
	}

	public static JsonDumpWriter ConvertToJson(IEnumerable<Node> node, bool nameValuePair = false)
	{
		if (node is null) throw new ArgumentNullException(nameof(node));
		var json = JsonDumpWriter.Create(new StringBuilder());

		json.BeginArray();
		foreach (var item in node)
		{
			ConvertToJson(item, json);
		}
		json.End();
		return json;
	}

	private static JsonDumpWriter ConvertToJson(Node node)
	{
		var json = JsonDumpWriter.Create(new StringBuilder());
		ConvertToJson(node, json);
		return json;
	}

	private static JsonDumpWriter ConvertToJson(Node node, JsonDumpWriter json)
	{
		bool obj = node.Items is { Count: > 0 };
		if (obj)
			json.BeginObject();
		node.DumpContent(json);
		if (obj)
			json.End();
		return json;
	}

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

	private ErrorResult SyntaxError(in LexicalToken token, in CharStream stream, string? message)
		=> SyntaxError(stream, token.Position, message);

	private ErrorResult SyntaxError(in CharStream stream, int position, string? message)
	{
		var at = stream.GetCharPosition(position);
		message ??= SR.SyntaxException();
		string error = _sourceName is null ?
			$"{message} at ({at.Line,+1}, {at.Column + 1})" :
			$"{message} in {_sourceName} at ({at.Line + 1}, {at.Column + 1})";
		var result = ErrorResult.Problem(error, "Syntax error").With(
			("line", at.Line + 1),
			("column", at.Column + 1));
		if (_sourceName != null)
			result.With("file", _sourceName);
		return result;
	}

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

	public (List<Node> Nodes, ErrorResult? Error) ParseNodeList(ref CharStream stream)
	{
		var result = new List<Node>();
		ResultValue<Node> node = ParseNode(ref stream);
		while (node.IsSuccess)
		{
			result.Add(node.Value);
			node = ParseNode(ref stream);
		}
		return (result, GetError(node));
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


	private ResultValue<Node> ParseNode(ref CharStream stream)
	{
		LexicalToken token;
		// Skip empty lines and parse options
		while ((token = _nodeScanner.Next(ref stream)).Is(LexicalTokenType.NEWLINE, LexicalTokenType.INDENT, LexicalTokenType.UNDENT) || token.TokenType.Is(TOKEN, OPTION))
		{
			if (token.TokenType.Is(TOKEN, OPTION))
			{
				var node = ParseOptions(ref stream);
				if (HasResult(node))
					return node;
			}
		}
		return stream.Eof ? NoResult(): ParseNode(token, ref stream);
	}

	private ResultValue<string> GetStringValue(LexicalToken token, in CharStream stream)
	{
		var v = token.GetValue(stream);
		if (v is ConfigValue c)
		{
			var options = _options;
			return c.ToString(o => GetVarValue(o, options));
		}
		return v?.ToString() ?? String.Empty;
	}

	private static ErrorResult NoResult() => new ErrorResult("", errorCode: 0);

	private static ErrorResult? GetError(ResultValue<Node> node)
		=> node.IsFailure && node.Error.ErrorCode != 0 ? node.Error : null;

	private static bool HasError(ResultValue<Node> node)
		=> node.IsFailure && node.Error.ErrorCode != 0;

	private static bool HasResult(ResultValue<Node> node)
		=> node.IsSuccess || node.Error.ErrorCode != 0;

	private ResultValue<Node> ParseNode(LexicalToken token, ref CharStream stream)
	{
		if (token.IsEof || token.Is(LexicalTokenType.INDENT, LexicalTokenType.UNDENT))
			return NoResult();
		bool dash = token.Is(TOKEN, DASH);
		if (!dash && !token.Is(LexicalTokenType.IDENTIFIER))
			return SyntaxError(token, stream, SR.ExpectedNodeName());

		var nodeNameValue = GetStringValue(token, stream);
		if (nodeNameValue.IsFailure)
			return nodeNameValue.Error;
		string nodeName = nodeNameValue.Value;
		bool attribute = false;
		int i = SkipSpace(stream);
		if (stream[i] is '=' or ':')
		{
			attribute = stream[i] == ':';
			stream.Forward(i + 1);
		}

		Node node = new Node(nodeName, position: GetPosition(token.Position, stream), attribute: attribute) { IsArrayItem = dash };

		SyntaxRule? rule = _syntaxRules.Find(CurrentNodePath, dash, _options.Comparer);

		var result = ParseStartNode(node, rule, ref stream);
		if (result.IsFailure) return result;

		if (dash && node.Name == "-")
			node.Name = "item";

		token = _nodeScanner.Next(ref stream);
		if (token.TokenType == LexicalTokenType.INDENT)
			return ParseNodeTree(node, ref stream);

		Back();
		return result;
	}

	private ResultValue<Node> ParseStartNode(Node node, SyntaxRule? rule, ref CharStream stream)
	{
		if (rule == null)
			return ParseNodeValue(node, ref stream);

		throw new NotImplementedException(nameof(ParseStartNode));
	}

	private ResultValue<Node> ParseNodeValue(Node node, ref CharStream stream)
	{
		var token = _nodeValueScanner.Next(ref stream);
		ResultValue<Node> result = node;

		if (token.IsEof || token.Is(LexicalTokenType.NEWLINE)) return result;

		if (token.Is(TOKEN, PARAMETER, OBJECT))
			result = ParseObject(node, token.Is(TOKEN, PARAMETER), ref stream);
		else if (token.Is(TOKEN, ARRAY))
			result = ParseArray(node, ref stream);
		else
		{
			var value = GetStringValue(token, stream);
			if (value.IsFailure)
				return value.Error;
			node.AppendValue(value.Value);
		}
		if (result.IsFailure) return result;

		if (token.Is(LexicalTokenType.STRING))
			node.IsQuoted = true;
		token = _nodeValueScanner.Next(ref stream);

		return token.IsEof || token.Is(LexicalTokenType.NEWLINE) ?
			result:
			SyntaxError(token, in stream, SR.ExpectedEndOfLine());
	}

	private ResultValue<Node> ParseObject(Node node, bool parameter, ref CharStream stream)
	{
		var offRule = parameter ? "Object": "Param";
		for (;;)
		{
			var at = stream.Position - 1;
			LexicalToken token = _objectScanner.Next(ref stream);
			if (token.TokenType.Is(TOKEN, END))
				return node;

			if (stream.Eof)
				return SyntaxError(stream, at, $"The closing {(parameter ? "parenthesis" : "brace")} is not found until end of file");
			if (!token.Is(TEXT))
				return SyntaxError(token, in stream, "Name of argument is expected");
			at = token.Position;
			var nameValue = GetStringValue(token, stream);
			if (nameValue.IsFailure)
				return nameValue.Error;
			var name = nameValue.Value;
			if (!(token = _objectScanner.Next(ref stream)).Is(TOKEN, ATTRIB, EQUAL))
				return SyntaxError(token, in stream, "Assign symbol is expected");
			var inner = new Node(name, position: GetPosition(at, stream), attribute: parameter || token.Is(TOKEN, ATTRIB));

			int i = SkipSpace(stream);
			var ch = stream[i];
			if (ch is ARRAY_MARK or OBJECT_MARK or PARAM_MARK)
				stream.Forward(i + 1);

			_objectScanner.EnableRule(o => o.RuleName == "Name" || o.RuleName == offRule, false);

			ResultValue<Node> result;
			if (ch == ARRAY_MARK)
				result = ParseArray(inner, ref stream);
			else if (ch is OBJECT_MARK or PARAM_MARK)
				result = ParseObject(inner, ch == PARAM_MARK, ref stream);
			else if ((token = _objectScanner.Next(ref stream)).Is(TEXT, LexicalTokenType.STRING))
			{
				var value = GetStringValue(token, stream);
				if (value.IsFailure)
					return value.Error;
				inner.Value = value.Value;
			}
			else
				return SyntaxError(token, in stream, "Argument value is expected");

			_objectScanner.EnableRule(o => o.RuleName == "Name" || o.RuleName == offRule, true);

			node.Add(inner);
		}
	}

	private ResultValue<Node> ParseArray(Node node, ref CharStream stream)
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
				return node;
			if (stream.Eof)
				return SyntaxError(stream, at, "The array closing brace is not found before the end of the file");

			var item = new Node("item", position: GetPosition(token.Position, stream), arrayItem: true);
			if (token.Is(TEXT))
			{
				var value = GetStringValue(token, stream);
				if (value.IsFailure)
					return value.Error;
				item.AppendValue(value.Value);
			}
			else if (token.Is(TOKEN, PARAMETER, OBJECT))
			{
				var value = ParseObject(item, token.Is(TOKEN, PARAMETER), ref stream);
				if (value.IsFailure)
					return value.Error;
			}
			else if (token.Is(TOKEN, ARRAY))
			{
				var value = ParseArray(item, ref stream);
				if (value.IsFailure)
					return value.Error;
			}
			else
			{
				return SyntaxError(token, in stream, "Array item is expected");
			}
			node.Add(item);
		}
	}

	private ResultValue<Node> ParseNodeTree(Node node, ref CharStream stream)
	{
		LexicalToken token;
		while ((token = _nodeScanner.Next(ref stream)).TokenType != LexicalTokenType.UNDENT)
		{
			if (token.TokenType.Is(TOKEN))
			{
				var child = ParseToken(token, node, ref stream);
				if (child.IsSuccess)
					node.Add(child.Value);
				else if (HasError(child))
					return child.Error;
			}
			else if (token.TokenType == LexicalTokenType.IDENTIFIER)
			{
				var child = ParseNode(token, ref stream);
				if (child.IsSuccess)
					node.Add(child.Value);
				else if (HasError(child))
					return child.Error;
			}
			else if (token.TokenType.Is(TEXT))
			{
				var value = GetStringValue(token, stream);
				if (value.IsFailure)
					return value.Error;
				node.AppendValue(value.Value);
			}
			else if (!token.IsEof && !token.TokenType.Is(LexicalTokenType.NEWLINE))
			{
				return SyntaxError(token, in stream, SR.ExpectedEndOfLine());
			}
		}

		token = _nodeScanner.Next(ref stream);
		if (!token.TokenType.Is(TOKEN, ENDNODE))
		{
			Back();
			return node;
		}

		token = _nodeScanner.Next(ref stream);
		if (!token.GetSpan(stream).Equals(node.Name.AsSpan(), StringComparison.Ordinal))
			return SyntaxError(token, in stream, SR.ExpectedEndOfNode(node.Name));
		return ParseNodeValue(node, ref stream);
	}

	private ResultValue<Node> ParseToken(LexicalToken token, Node node, ref CharStream stream)
	{
		ResultValue<Node> value;
		switch (token.TokenType.Item)
		{
			case OPTION:
				return ParseOptions(ref stream);

			case LINE:
				node.AppendNewLine();
				value = ParseNodeValue(node, ref stream);
				return value.IsFailure ? value.Error: NoResult();

			case DASH:
				return ParseNode(token, ref stream);

			case PARAMETER:
				value = ParseObject(node, true, ref stream);
				return value.IsFailure ? value.Error : NoResult();

			case OBJECT:
				value = ParseObject(node, false, ref stream);
				return value.IsFailure ? value.Error : NoResult();

			case ARRAY:
				value = ParseArray(node, ref stream);
				return value.IsFailure ? value.Error : NoResult();

			default:
				return SyntaxError(token, stream, null);
		}
	}

	private ResultValue<Node> ParseOptions(ref CharStream stream)
	{
		int i = SkipSpace(stream);
		var ch = stream[i];
		if (ch is VAR_MARK or ACTION_MARK)
			stream.Forward(i + 1);

		LexicalToken token = _optionNameScanner.Next(ref stream);
		if (!token.Is(TEXT, LexicalTokenType.STRING))
			return SyntaxError(token, in stream, SR.ExpectedNodePattern());

		var nameValue = GetStringValue(token, stream);
		if (nameValue.IsFailure)
			return nameValue.Error;
		var name = nameValue.Value;

		i = SkipSpace(stream);
		if (stream[i] is ':' or '=')
			stream.Forward(i + 1);

		var parms = ParseOptionValue(name, ref stream);
		if (parms.IsFailure)
			return parms.Error;

		var parameters = parms.Value;
		Node? result = null;

		if (ch == VAR_MARK)
			_options.AddVariable(name, parameters);
		else if (ch != ACTION_MARK)
			_syntaxRules.Add(CurrentNodePath, name, parameters, _options.Comparer);
		else if (!_options.TryExecuteExternalAction(name, ref this, parameters, out result))
			return SyntaxError(token, in stream, SR.ActionNotFound(name));

		return result == null ? NoResult(): result;
	}

	private ResultValue<List<Node>> ParseOptionValue(string name, ref CharStream stream)
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
				var value = GetStringValue(token, stream);
				if (value.IsFailure)
					return value.Error;
				parameters.Add(new Node(name, value.Value, position: GetPosition(token.Position, stream), arrayItem: true));
			}
			else
			{
				return SyntaxError(token, in stream, SR.ExpectedNodePattern());
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