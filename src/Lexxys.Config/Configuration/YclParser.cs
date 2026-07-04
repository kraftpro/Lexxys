using System.Text;

using Lexxys.Tokenizer;

namespace Lexxys.Configuration;

public sealed class YclParser
{
	private readonly Dictionary<string, YclType> _types = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);
	private readonly List<YclTypeApplication> _typeApplications = [];
	private readonly List<string> _itemSeparators = [",", ";"];
	private readonly List<YclParseMessage> _messages = [];
	private readonly Dictionary<string, YclMetaStatementHandler> _metaHandlers = new(StringComparer.Ordinal);
	private readonly Dictionary<string, YclSubstitutionSource> _substitutionSources = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _arguments = new(StringComparer.OrdinalIgnoreCase);
	private string _basePath = Environment.CurrentDirectory;
	private string _includeRootPath = Environment.CurrentDirectory;
	private string? _sourceName;
	private List<KeyValuePair<string?, ConfigNode>>? _rootItems;
	private string[]? _lastSchemaPath;
	private readonly HashSet<string> _includeStack = new(StringComparer.OrdinalIgnoreCase);
	private char _escapeCharacter = '`';
	private YclType? _currentSchemaType;
	private bool _recursiveSubstitutionWarningIssued;

	public YclParser()
	{
		SetMetaStatementHandler("include", context => context.ParseYclInclude());
		SetMetaStatementHandler("set-separator", context =>
		{
			SetSeparator(context.RawValue);
			return null;
		});
		SetMetaStatementHandler("set-escape", context =>
		{
			SetEscape(context.RawValue);
			return null;
		});
		SetSubstitutionSource("env", context => Environment.GetEnvironmentVariable(context.Reference));
		SetSubstitutionSource("arg", context => _arguments.TryGetValue(context.Reference, out var value) ? value: null);
		SetSubstitutionSource("", context => context.GetCurrentDocumentValue());
	}

	public static ConfigNodeCollection Parse(string text)
	{
		var parser = new YclParser();
		return parser.ParseDocument(text);
	}

	public static ConfigNodeCollection Parse(string text, Action<YclParser> configure)
	{
		var parser = new YclParser();
		configure?.Invoke(parser);
		return parser.ParseDocument(text);
	}

	public static YclParseResult ParseResult(string text)
	{
		var parser = new YclParser();
		return parser.ParseDocumentResult(text);
	}

	public static YclParseResult ParseResult(string text, Action<YclParser> configure)
	{
		var parser = new YclParser();
		configure?.Invoke(parser);
		return parser.ParseDocumentResult(text);
	}

	public static ConfigNodeCollection ParseFile(string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));
		var parser = new YclParser();
		return parser.ParseFileContent(path);
	}

	public static ConfigNodeCollection ParseFile(string path, Action<YclParser> configure)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));
		var parser = new YclParser();
		configure?.Invoke(parser);
		return parser.ParseFileContent(path);
	}

	private ConfigNodeCollection ParseFileContent(string path)
	{
		var fullPath = Path.GetFullPath(path);
		_basePath = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
		_includeRootPath = _basePath;
		_sourceName = fullPath;
		_includeStack.Add(fullPath);
		return ParseDocument(File.ReadAllText(fullPath));
	}

	public static YclParseResult ParseFileResult(string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));
		var parser = new YclParser();
		return parser.ParseFileContentResult(path);
	}

	public static YclParseResult ParseFileResult(string path, Action<YclParser> configure)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));
		var parser = new YclParser();
		configure?.Invoke(parser);
		return parser.ParseFileContentResult(path);
	}

	private YclParseResult ParseFileContentResult(string path)
	{
		var fullPath = Path.GetFullPath(path);
		_basePath = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
		_includeRootPath = _basePath;
		_sourceName = fullPath;
		_includeStack.Add(fullPath);
		try
		{
			return ParseDocumentResult(File.ReadAllText(fullPath));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AddError(ex.Message);
			return new YclParseResult(null, _messages.ToArray());
		}
	}

	public static ConfigNodeCollection Parse(ref CharStream stream)
		=> Parse(stream.Slice(0).ToString());

	public void SetMetaStatementHandler(string name, YclMetaStatementHandler handler)
	{
		if (String.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Meta statement name is required.", nameof(name));
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));
		name = name.Trim();
		if (ReadNameLength(name) != name.Length)
			throw new ArgumentException($"Invalid meta statement name '{name}'.", nameof(name));
		_metaHandlers[name] = handler;
	}

	public bool RemoveMetaStatementHandler(string name)
	{
		if (String.IsNullOrWhiteSpace(name))
			return false;
		return _metaHandlers.Remove(name.Trim());
	}

	public void SetSubstitutionSource(string name, YclSubstitutionSource source)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		name = name.Trim();
		if (name.Length > 0 && ReadNameLength(name) != name.Length)
			throw new ArgumentException($"Invalid substitution source name '{name}'.", nameof(name));
		_substitutionSources[name] = source;
	}

	public bool RemoveSubstitutionSource(string name)
		=> name is not null && _substitutionSources.Remove(name.Trim());

	public void SetArguments(IEnumerable<string> arguments)
	{
		if (arguments is null)
			throw new ArgumentNullException(nameof(arguments));
		_arguments.Clear();
		foreach (var argument in arguments)
			AddArgument(argument);
	}

	public void SetArguments(IReadOnlyDictionary<string, string> arguments)
	{
		if (arguments is null)
			throw new ArgumentNullException(nameof(arguments));
		_arguments.Clear();
		foreach (var item in arguments)
			_arguments[item.Key] = item.Value;
	}

	public void SetConfigurationSource(ConfigNodeCollection collection)
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));
		SetSubstitutionSource("config", context => context.GetConfigurationValue(collection));
	}

	private void AddArgument(string argument)
	{
		if (String.IsNullOrWhiteSpace(argument))
			return;
		var text = argument.Trim();
		if (text.StartsWith("--", StringComparison.Ordinal))
			text = text.Substring(2);
		else if (text.StartsWith("-", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal))
			text = text.Substring(1);

		int split = text.IndexOf('=');
		if (split < 0)
			split = text.IndexOf(':');
		if (split < 0)
			_arguments[text] = "true";
		else
			_arguments[text.Substring(0, split)] = text.Substring(split + 1);
	}

	internal string? GetCurrentDocumentValue(string reference)
	{
		if (_rootItems is null)
			return null;
		return GetConfigurationValue(new ConfigNodeCollection(_rootItems), reference);
	}

	internal string? GetConfigurationValue(ConfigNodeCollection collection, string reference)
	{
		if (collection is null || String.IsNullOrWhiteSpace(reference))
			return null;
		if (!TryGetConfigurationNode(collection, reference, out var node))
			return null;
		return node.GetValue();
	}

	private static bool TryGetConfigurationNode(ConfigNodeCollection collection, string reference, out ConfigNode node)
	{
		node = default;
		var parts = reference.Split(['/', '.'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return false;
		ConfigNodeCollection? current = collection;
		for (int i = 0; i < parts.Length; ++i)
		{
			if (current is null)
				return false;
			var part = parts[i];
			if (Int32.TryParse(part, out var index))
			{
				if (index < 0 || index >= current.Count)
					return false;
				node = current[index];
			}
			else if (!current.TryGetValue(part, out node))
			{
				return false;
			}
			current = i + 1 == parts.Length ? null: node.Collection;
		}
		return true;
	}

	public ConfigNodeCollection ParseDocument(string text)
	{
		var lines = ReadLines(text);
		int index = 0;
		var previousRootItems = _rootItems;
		var rootItems = new List<KeyValuePair<string?, ConfigNode>>();
		_rootItems = rootItems;
		try
		{
		return ParseBlock(lines, ref index, 0, [], rootItems);
		}
		finally
		{
			_rootItems = previousRootItems;
		}
	}

	public YclParseResult ParseDocumentResult(string text)
	{
		try
		{
			return new YclParseResult(ParseDocument(text), _messages.ToArray());
		}
		catch (Exception ex) when (ex is SyntaxException or IOException or UnauthorizedAccessException)
		{
			AddError(ex.Message);
			return new YclParseResult(null, _messages.ToArray());
		}
	}

	private ConfigNodeCollection ParseBlock(List<YclLine> lines, ref int index, int indent, List<string?> path, List<KeyValuePair<string?, ConfigNode>>? existingItems = null, YclType? structuralType = null)
	{
		var items = existingItems ?? new List<KeyValuePair<string?, ConfigNode>>();
		var collectionLocation = default(ConfigSourceLocation);
		while (index < lines.Count)
		{
			var line = lines[index];
			if (line.IsBlank)
			{
				++index;
				continue;
			}
			var text = NormalizeDirective(line.Text);
			if (IsDirective(text))
			{
				if (line.Indent < indent)
					break;
				++index;
				ProcessDirective(text, items, path, line.Location);
				continue;
			}
			if (line.Indent < indent)
				break;
			if (line.Indent > indent)
				throw new SyntaxException($"Unexpected indentation at line {line.Number}.");
			if (collectionLocation.IsEmpty)
				collectionLocation = line.Location;

			++index;
			var item = ParseNodeItem(line.Text, line.Number);
			var appliedType = ResolveType(item, path, structuralType);
			var nodeName = appliedType.Name ?? item.Name;
			if (TryGetMultilineCloseMarker(item.RawValue, out var closeMarker))
			{
				var value = index < lines.Count && lines[index].Indent > indent ?
					ReadMultilineValue(lines, ref index, lines[index].Indent, closeMarker):
					String.Empty;
				items.Add(new KeyValuePair<string?, ConfigNode>(nodeName, new ConfigNode(value, line.Location)));
				continue;
			}
			ConfigNodeCollection? children = null;
			if (item.RawValue is not null && HasFloatingDirective(lines, index, indent))
			{
				items.Add(new KeyValuePair<string?, ConfigNode>(nodeName, BuildNode(item, null, appliedType, line.Location, path)));
				ProcessFloatingDirectives(lines, ref index, indent, items, path);
				continue;
			}
			SkipBlankLines(lines, ref index);
			if (index < lines.Count && lines[index].Indent > indent)
			{
				path.Add(nodeName);
				children = ParseBlock(lines, ref index, lines[index].Indent, path, structuralType: appliedType.Type);
				path.RemoveAt(path.Count - 1);
			}

			items.Add(new KeyValuePair<string?, ConfigNode>(nodeName, BuildNode(item, children, appliedType, line.Location, path)));
		}
		WarnIfRepeatedNodesAreSplit(items, path);
		return new ConfigNodeCollection(items, collectionLocation);
	}

	private static bool IsDirective(string text)
		=> text.StartsWith("%", StringComparison.Ordinal);

	private void AddWarning(string message)
		=> _messages.Add(new YclParseMessage(YclParseMessageSeverity.Warning, message));

	private void AddError(string message)
		=> _messages.Add(new YclParseMessage(YclParseMessageSeverity.Error, message));

	private void WarnIfRepeatedNodesAreSplit(List<KeyValuePair<string?, ConfigNode>> items, List<string?> path)
	{
		var last = new Dictionary<string, int>(StringComparer.Ordinal);
		var warned = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < items.Count; ++i)
		{
			var key = items[i].Key;
			if (key is null)
				continue;
			if (last.TryGetValue(key, out var previous) && previous + 1 != i && warned.Add(key))
				AddWarning($"Repeated node '{key}' is split by other nodes at '{FormatPath(path)}'. It will be grouped during collection construction.");
			last[key] = i;
		}
	}

	private static string FormatPath(List<string?> path)
		=> path.Count == 0 ? "/" : "/" + String.Join("/", path.Select(o => o ?? "-"));

	private void ProcessDirective(string text, List<KeyValuePair<string?, ConfigNode>> items, List<string?> path, ConfigSourceLocation location)
	{
		if (TryHandleMetaStatement(text, items, path, location))
			return;
		if (TryHandleTypeDeclaration(text))
			return;
		HandleDirective(text, path);
	}

	private void ProcessFloatingDirectives(List<YclLine> lines, ref int index, int indent, List<KeyValuePair<string?, ConfigNode>> items, List<string?> path)
	{
		while (index < lines.Count)
		{
			var line = lines[index];
			if (line.IsBlank || line.Indent <= indent)
				return;
			var text = NormalizeDirective(line.Text);
			if (!IsDirective(text))
				return;
			++index;
			ProcessDirective(text, items, path, line.Location);
		}
	}

	private static void SkipBlankLines(List<YclLine> lines, ref int index)
	{
		while (index < lines.Count && lines[index].IsBlank)
			++index;
	}

	private static bool HasFloatingDirective(List<YclLine> lines, int index, int indent)
	{
		if (index >= lines.Count)
			return false;
		var line = lines[index];
		return !line.IsBlank && line.Indent > indent && IsDirective(NormalizeDirective(line.Text));
	}

	private static bool TryGetMultilineCloseMarker(string? value, out string closeMarker)
	{
		if (value is "\"\"\"")
		{
			closeMarker = "\"\"\"";
			return true;
		}
		if (value is { Length: >= 2 } && value.All(o => o == '<'))
		{
			closeMarker = new String('>', value.Length);
			return true;
		}
		closeMarker = String.Empty;
		return false;
	}

	private static string ReadMultilineValue(List<YclLine> lines, ref int index, int indent, string closeMarker)
	{
		var result = new StringBuilder();
		while (index < lines.Count)
		{
			var line = lines[index];
			if (line.IsBlank)
			{
				if (result.Length > 0)
					result.Append('\n');
				++index;
				continue;
			}
			if (line.Indent < indent)
				throw new SyntaxException($"Missing multiline terminator '{closeMarker}'.");
			if (line.Text == closeMarker)
			{
				++index;
				return result.ToString();
			}
			if (result.Length > 0)
				result.Append('\n');
			result.Append(line.Text);
			++index;
		}
		throw new SyntaxException($"Missing multiline terminator '{closeMarker}'.");
	}

	internal ConfigNodeCollection ParseInclude(string includePath)
		=> ParseInclude(includePath, (text, _) => ParseDocument(text));

	internal ConfigNodeCollection ParseInclude(string includePath, Func<string, string, ConfigNodeCollection> parser)
	{
		if (parser is null)
			throw new ArgumentNullException(nameof(parser));
		var fullPath = ResolveIncludePath(includePath);
		if (!_includeStack.Add(fullPath))
			throw new SyntaxException($"Recursive include detected for '{includePath}'.");
		if (!File.Exists(fullPath))
			throw new SyntaxException($"Include file '{includePath}' was not found.");

		var previousBasePath = _basePath;
		var previousSourceName = _sourceName;
		try
		{
			_basePath = Path.GetDirectoryName(fullPath) ?? previousBasePath;
			_sourceName = fullPath;
			return parser(File.ReadAllText(fullPath), fullPath);
		}
		finally
		{
			_basePath = previousBasePath;
			_sourceName = previousSourceName;
			_includeStack.Remove(fullPath);
		}
	}

	internal string ResolveIncludePath(string includePath)
	{
		var fullPath = Path.IsPathRooted(includePath) ? includePath: Path.Combine(_basePath, includePath);
		fullPath = Path.GetFullPath(fullPath);
		if (!IsPathUnderRoot(fullPath, _includeRootPath))
			throw new SyntaxException($"Include path '{includePath}' escapes the include root.");
		return fullPath;
	}

	private static bool IsPathUnderRoot(string path, string root)
	{
		var fullRoot = Path.GetFullPath(root);
		if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
			!fullRoot.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
			fullRoot += Path.DirectorySeparatorChar;
		return path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeDirective(string text)
	{
		if (!text.StartsWith("%", StringComparison.Ordinal))
			return text;
		int i = 1;
		while (i < text.Length && Char.IsWhiteSpace(text[i]))
			++i;
		if (i >= text.Length)
			return "%";
		var result = new StringBuilder();
		result.Append('%');
		result.Append(text[i++]);
		if (result[1] is '$' or '!' or ':' or '.')
		{
			while (i < text.Length && Char.IsWhiteSpace(text[i]))
				++i;
		}
		result.Append(text.Substring(i));
		return result.ToString();
	}

	private ConfigNode BuildNode(YclNodeItem item, ConfigNodeCollection? children, ResolvedType resolvedType, ConfigSourceLocation location, List<string?> path)
	{
		var type = resolvedType.Type;
		if (type is not null)
		{
			return ApplyType(type, item.RawValue, children, location, path);
		}

		if (item.RawValue is null)
			return children is null ? new ConfigNode((string?)null, location): new ConfigNode(children, location);

		var value = ParseValue(item.RawValue, location, path);
		if (children is null)
			return value;

		if (value.Collection is not null && value.Value is null)
			value = new ConfigNode(Merge(value.Collection, children), location);
		else
			value = new ConfigNode(value.Value, children, location);
		return resolvedType.Field is null ? value: ApplyField(resolvedType.Field, value);
	}

	private ResolvedType ResolveType(YclNodeItem item, List<string?> parentPath, YclType? structuralType)
	{
		var path = new List<string?>(parentPath.Count + 1);
		path.AddRange(parentPath);
		path.Add(item.Name);
		for (int i = _typeApplications.Count - 1; i >= 0; --i)
		{
			var application = _typeApplications[i];
			if (TryMatchTypeApplication(application, path, item.Name, out var nodeName))
				return new ResolvedType(application.Type, nodeName, null);
		}
		if (structuralType is not null && TryResolveStructuralField(structuralType, item.Name, out var field, out var structuralName))
			return new ResolvedType(field.Type, structuralName ?? item.Name, field.Type is null ? field: null);
		return new ResolvedType(null, item.Name, null);
	}

	private bool TryMatchTypeApplication(YclTypeApplication application, List<string?> path, string? nodeName, out string? effectiveName)
	{
		effectiveName = null;
		if (!PathStartsWith(path, application.DeclarationPath))
			return false;
		var relativePath = path.Skip(application.DeclarationPath.Length).ToList();
		if (relativePath.Count == 0)
			return false;

		if (application.OneShot)
		{
			if (application.Consumed || relativePath.Count != 1)
				return false;
			application.Consumed = true;
			effectiveName = nodeName;
			return true;
		}

		if (application.IsActivated)
		{
			if (!PathEquals(application.CapturedParentPath, path, path.Count - 1))
				return false;
			if (!TryMatchPattern(application, 0, relativePath, 0))
				return false;
			effectiveName = GetAppliedNodeName(application, nodeName);
			return true;
		}

		if (!TryMatchPattern(application, 0, relativePath, 0))
			return false;
		application.CapturedParentPath = path.Take(path.Count - 1).ToArray();
		application.IsActivated = true;
		effectiveName = GetAppliedNodeName(application, nodeName);
		return true;
	}

	private static bool PathStartsWith(List<string?> path, string?[] prefix)
	{
		if (path.Count < prefix.Length)
			return false;
		for (int i = 0; i < prefix.Length; ++i)
		{
			if (path[i] != prefix[i])
				return false;
		}
		return true;
	}

	private static bool PathEquals(string?[]? expected, List<string?> path, int count)
	{
		if (expected is null || expected.Length != count)
			return false;
		for (int i = 0; i < count; ++i)
		{
			if (expected[i] != path[i])
				return false;
		}
		return true;
	}

	private static bool TryMatchPattern(YclTypeApplication application, int patternIndex, List<string?> path, int pathIndex)
	{
		if (patternIndex == application.Pattern.Length)
			return pathIndex == path.Count;
		if (pathIndex >= path.Count)
			return false;

		var pattern = application.Pattern[patternIndex];
		if (pattern == "**")
		{
			for (int count = 1; pathIndex + count <= path.Count; ++count)
			{
				if (TryMatchPattern(application, patternIndex + 1, path, pathIndex + count))
					return true;
			}
			return false;
		}
		if (!MatchPatternSegment(application, pattern, path[pathIndex], patternIndex == application.Pattern.Length - 1))
			return false;
		return TryMatchPattern(application, patternIndex + 1, path, pathIndex + 1);
	}

	private static bool MatchPatternSegment(YclTypeApplication application, string pattern, string? value, bool isLast)
	{
		if (pattern == "-")
			return value is null;
		if (pattern == "*")
			return true;
		if (pattern == ".")
			return true;
		return value == pattern || value is null && isLast && application.LiteralPatternMatchesUnnamed;
	}

	private static bool TryResolveStructuralField(YclType type, string? nodeName, out YclField field, out string? effectiveName)
	{
		if (nodeName is not null)
		{
			var exact = type.Fields.FirstOrDefault(o => o.Name == nodeName);
			if (exact is not null)
			{
				field = MergeWildcardField(type, exact);
				effectiveName = nodeName;
				return true;
			}
			var wildcard = type.Fields.FirstOrDefault(o => o.Name == "*" && o.Type is not null);
			if (wildcard is not null)
			{
				field = wildcard;
				effectiveName = nodeName;
				return true;
			}
		}

		var candidates = type.Fields
			.Where(o => o.Name != "*" && o.Type is not null)
			.ToList();
		if (nodeName is null && candidates.Count == 1)
		{
			field = MergeWildcardField(type, candidates[0]);
			effectiveName = field.Name;
			return true;
		}

		field = null!;
		effectiveName = null;
		return false;
	}

	private static YclField MergeWildcardField(YclType parent, YclField field)
	{
		if (field.Type is null)
			return field;
		var wildcard = parent.Fields.FirstOrDefault(o => o.Name == "*" && o.Type is not null);
		if (wildcard?.Type is null)
			return field;
		var fields = new List<YclField>(field.Type.Fields);
		foreach (var item in wildcard.Type.Fields)
		{
			if (!fields.Any(o => o.Name == item.Name))
				fields.Add(item);
		}
		return field with { Type = new YclType(field.Type.Name, fields) };
	}

	private static string? GetAppliedNodeName(YclTypeApplication application, string? nodeName)
	{
		if (nodeName is not null)
			return nodeName;
		if (!application.NameUnnamedNodes)
			return null;
		var last = application.Pattern[application.Pattern.Length - 1];
		if (last != "*" && last != "**" && last != "-")
			return last;
		return application.Type.Name;
	}

	private ConfigNode ApplyType(YclType type, string? rawValue, ConfigNodeCollection? children, ConfigSourceLocation location, List<string?> path)
	{
		var values = rawValue is null ? new List<ConfigNode>(): ParsePositionalValues(rawValue, location, path);
		var result = new List<KeyValuePair<string?, ConfigNode>>();
		var outputFields = type.Fields.Where(o => o.Name != "*").ToList();

		for (int i = 0; i < outputFields.Count; ++i)
		{
			var field = outputFields[i];
			ConfigNode value;
			if (i < values.Count && !values[i].IsEmpty)
			{
				value = ApplyField(field, values[i]);
			}
			else
			{
				value = new ConfigNode((string?)null, location);
			}
			result.Add(new KeyValuePair<string?, ConfigNode>(field.Name, value));
		}

		if (children is not null)
		{
			foreach (var child in children)
			{
				int index = result.FindIndex(o => o.Key == child.Key);
				if (index >= 0)
				{
					var field = type.Fields[index];
					if (result[index].Value.IsEmpty)
						result[index] = new KeyValuePair<string?, ConfigNode>(child.Key, ApplyField(field, child.Value));
					else
						result.Add(child);
				}
				else
					result.Add(child);
			}
		}

		for (int i = 0; i < outputFields.Count; ++i)
		{
			var field = outputFields[i];
			var value = result[i].Value;
			if (value.IsEmpty && field.DefaultValue is not null)
				value = ApplyField(field, ParseValue(field.DefaultValue, location, path));
			if (field.Required && value.IsEmpty)
				throw new SyntaxException($"Required field '{field.Name}' is missing.");
			result[i] = new KeyValuePair<string?, ConfigNode>(field.Name, value);
		}
		result.RemoveAll(o => o.Key == "*");

		return new ConfigNode(new ConfigNodeCollection(result, location), location);
	}

	private ConfigNode ApplyField(YclField field, ConfigNode value)
	{
		var result = field.Type is null ? value: ApplyStructuredType(field.Type, value);
		ValidateField(field, result);
		return result;
	}

	private ConfigNode ApplyStructuredType(YclType type, ConfigNode value)
	{
		if (value.Collection is null)
			return ApplyType(type, value.Value, null, value.Location, []);

		if (value.Collection.All(o => o.Key is null))
			return ApplyStructuredTypePositionally(type, value);

		var result = new List<KeyValuePair<string?, ConfigNode>>();
		var outputFields = type.Fields.Where(o => o.Name != "*").ToList();
		for (int i = 0; i < outputFields.Count; ++i)
		{
			var field = outputFields[i];
			result.Add(new KeyValuePair<string?, ConfigNode>(field.Name, new ConfigNode((string?)null, value.Location)));
		}
		foreach (var child in value.Collection)
		{
			int index = result.FindIndex(o => o.Key == child.Key);
			if (index >= 0)
			{
				var field = outputFields[index];
				result[index] = new KeyValuePair<string?, ConfigNode>(child.Key, child.Value.IsEmpty ? child.Value: ApplyField(field, child.Value));
			}
			else if (child.Key != "*")
			{
				result.Add(child);
			}
		}
		for (int i = 0; i < outputFields.Count; ++i)
		{
			var field = outputFields[i];
			var item = result[i].Value;
			if (item.IsEmpty && field.DefaultValue is not null)
				item = ApplyField(field, ParseValue(field.DefaultValue, value.Location));
			if (field.Required && item.IsEmpty)
				throw new SyntaxException($"Required field '{field.Name}' is missing.");
			result[i] = new KeyValuePair<string?, ConfigNode>(field.Name, item);
		}
		return new ConfigNode(new ConfigNodeCollection(result, value.Location), value.Location);
	}

	private ConfigNode ApplyStructuredTypePositionally(YclType type, ConfigNode value)
	{
		var values = value.Collection!.Select(o => o.Value).ToList();
		var outputFields = type.Fields.Where(o => o.Name != "*").ToList();
		var result = new List<KeyValuePair<string?, ConfigNode>>();
		for (int i = 0; i < outputFields.Count; ++i)
		{
			var field = outputFields[i];
			var item = i < values.Count ? values[i]:
				field.DefaultValue is null ? new ConfigNode((string?)null, value.Location): ParseValue(field.DefaultValue, value.Location);
			item = item.IsEmpty ? item: ApplyField(field, item);
			if (field.Required && item.IsEmpty)
				throw new SyntaxException($"Required field '{field.Name}' is missing.");
			result.Add(new KeyValuePair<string?, ConfigNode>(field.Name, item));
		}
		return new ConfigNode(new ConfigNodeCollection(result, value.Location), value.Location);
	}

	private static void ValidateField(YclField field, ConfigNode value)
	{
		if (field.ValueType is null || value.IsEmpty)
			return;
		if (field.IsArray)
		{
			if (value.Collection is null || value.Collection.Any(o => o.Key is not null))
				throw new SyntaxException($"Field '{field.Name}' must be an array of {field.ValueType}.");
			foreach (var item in value.Collection)
				ValidateScalar(field.Name, field.ValueType, item.Value);
			return;
		}
		ValidateScalar(field.Name, field.ValueType, value);
	}

	private static void ValidateScalar(string name, string valueType, ConfigNode value)
	{
		if (value.Collection is not null)
			throw new SyntaxException($"Field '{name}' must be {valueType}; got collection with {value.Collection.Count} item(s).");
		var text = value.Value;
		if (text is null)
			return;
		bool valid = valueType switch
		{
			"string" => true,
			"int" or "integer" => Int64.TryParse(text, out _),
			"number" or "decimal" => Decimal.TryParse(text, out _),
			"bool" or "boolean" => Strings.TryGetBoolean(text, out _),
			"date" => IsDate(text),
			"datetime" or "date-time" or "timestamp" => Strings.TryGetDateTimeOffset(text, out _),
			"period" or "duration" or "timespan" or "time-span" => Strings.TryGetTimeSpan(text, out _),
			_ => true
		};
		if (!valid)
			throw new SyntaxException($"Field '{name}' must be {valueType}; got '{text}'.");
	}

	private static bool IsDate(string text)
	{
		return text.IndexOf('T') < 0 && text.IndexOf('t') < 0 && text.IndexOf(' ') < 0 && text.IndexOf(':') < 0 &&
			Strings.TryGetDateTime(text, out var value) && value.TimeOfDay == TimeSpan.Zero;
	}

	private static ConfigNodeCollection Merge(ConfigNodeCollection left, ConfigNodeCollection right)
	{
		var items = new List<KeyValuePair<string?, ConfigNode>>();
		foreach (var item in left)
			items.Add(item);
		foreach (var item in right)
			items.Add(item);
		return new ConfigNodeCollection(items, left.Location);
	}

	private bool HandleDirective(string text, List<string?> path)
	{
		if (text.Length == 0)
			return true;

		if (text.StartsWith("%$", StringComparison.Ordinal))
		{
			var rest = text.Substring(2).TrimStart();
			int n = ReadNameLength(rest);
			if (n == 0)
				throw new SyntaxException("Variable name expected.");
			var name = rest.Substring(0, n);
			var value = TrimAssignment(rest.Substring(n).TrimStart());
			_variables[name] = ParseScalar(value, substitute: false);
			return true;
		}

		if (text[0] == '%' && text.Length > 1)
		{
			var rest = text.Substring(1).TrimStart();
			int n = ReadDirectivePatternLength(rest);
			if (n == 0)
				return true;
			var pattern = ParseTypeApplicationPattern(rest.Substring(0, n));
			var tail = rest.Substring(n).TrimStart();
			if (tail.StartsWith(":", StringComparison.Ordinal) || tail.StartsWith("=", StringComparison.Ordinal))
			{
				var typeName = TrimAssignment(tail);
				if (typeName.StartsWith(":", StringComparison.Ordinal))
					typeName = typeName.Substring(1).TrimStart();
				AddTypeApplication(path, pattern, GetType(typeName), nameUnnamedNodes: pattern.Length != 1 || pattern[0] != ".", oneShot: pattern.Length == 1 && pattern[0] == ".");
				return true;
			}
			AddTypeApplication(path, pattern, new YclType(GetTypeApplicationName(pattern), ParseTypeFields(tail)), nameUnnamedNodes: PatternNamesUnnamedNodes(pattern), literalPatternMatchesUnnamed: SingleLiteralPattern(pattern));
			return true;
		}

		return false;
	}

	private bool TryHandleMetaStatement(string text, List<KeyValuePair<string?, ConfigNode>> items, List<string?> path, ConfigSourceLocation location)
	{
		if (!text.StartsWith("%!", StringComparison.Ordinal))
			return false;

		var rest = text.Substring(2).TrimStart();
		int n = ReadNameLength(rest);
		if (n == 0)
			throw new SyntaxException("Meta statement name expected.");
		var name = rest.Substring(0, n);
		var value = rest.Substring(n).TrimStart();
		if (!_metaHandlers.TryGetValue(name, out var handler))
			throw new SyntaxException($"Unknown meta statement '{name}'.");
		var context = new YclMetaStatementContext(this, name, value, location, path.ToArray());
		var nodes = handler(context);
		if (nodes is not null)
		{
			foreach (var node in nodes)
				items.Add(node);
		}
		return true;
	}

	private void AddTypeApplication(List<string?> declarationPath, string[] pattern, YclType type, bool nameUnnamedNodes = true, bool literalPatternMatchesUnnamed = false, bool oneShot = false)
	{
		if (pattern.Length == 0)
			throw new SyntaxException("Type application pattern expected.");
		_typeApplications.Add(new YclTypeApplication(declarationPath.ToArray(), pattern, type, nameUnnamedNodes, literalPatternMatchesUnnamed, oneShot));
	}

	private string[] ParseTypeApplicationPattern(string text)
	{
		var items = text.Split(['/'], StringSplitOptions.RemoveEmptyEntries)
			.Select(o => UnquoteName(o.Trim()))
			.Where(o => o.Length > 0)
			.ToArray();
		if (items.Length == 0)
			throw new SyntaxException("Type application pattern expected.");
		return items;
	}

	private static int ReadDirectivePatternLength(string text)
	{
		int i = 0;
		while (i < text.Length && !Char.IsWhiteSpace(text[i]) && text[i] != ':' && text[i] != '=')
			++i;
		return i;
	}

	private static string GetTypeApplicationName(string[] pattern)
	{
		for (int i = pattern.Length - 1; i >= 0; --i)
		{
			if (pattern[i] != "*" && pattern[i] != "**" && pattern[i] != "-")
				return pattern[i];
		}
		return "item";
	}

	private static bool PatternNamesUnnamedNodes(string[] pattern)
	{
		var last = pattern[pattern.Length - 1];
		return last != "-" && last != "**";
	}

	private static bool SingleLiteralPattern(string[] pattern)
		=> pattern.Length == 1 && pattern[0] != "*" && pattern[0] != "**" && pattern[0] != "-";

	private bool TryHandleTypeDeclaration(string text)
	{
		if (!text.StartsWith("%:", StringComparison.Ordinal))
			return false;
		var rest = text.Substring(2).TrimStart();
		if (rest.StartsWith("./", StringComparison.Ordinal))
		{
			AddSchemaPathField(rest);
			return true;
		}

		int n = ReadNameLength(rest);
		if (n == 0)
			throw new SyntaxException("Type name expected.");
		var name = rest.Substring(0, n);
		if (name.IndexOf('/') >= 0 || name == ".")
		{
			AddSchemaPathField(rest);
			return true;
		}
		var body = rest.Substring(n).TrimStart();
		var fields = ParseTypeFields(body);
		_currentSchemaType = new YclType(name, fields);
		_types[name] = _currentSchemaType;
		_lastSchemaPath = [name];
		return true;
	}

	private void AddSchemaPathField(string text)
	{
		var split = ReadNameLength(text);
		if (split == 0)
			throw new SyntaxException("Schema field path expected.");
		var path = ResolveSchemaDeclarationPath(text.Substring(0, split));
		if (path.Length < 2)
			throw new SyntaxException("Schema field path expected.");
		var rootName = path[0];
		if (!_types.TryGetValue(rootName, out var rootType))
		{
			rootType = new YclType(rootName, []);
			_types[rootName] = rootType;
		}
		_currentSchemaType = rootType;
		var body = text.Substring(split).TrimStart();
		var fieldPath = path.Skip(1).ToArray();
		var field = CreateSchemaPathField(fieldPath[fieldPath.Length - 1], body);
		AddSchemaField(rootType.Fields, fieldPath, 0, field);
		_lastSchemaPath = path;
	}

	private YclField CreateSchemaPathField(string name, string body)
	{
		if (body.Length == 0)
			return new YclField(name, null, null, null, false, false);
		if (body.StartsWith(":", StringComparison.Ordinal) ||
			body.StartsWith("(", StringComparison.Ordinal) ||
			body.StartsWith("=", StringComparison.Ordinal) ||
			body.StartsWith("required", StringComparison.OrdinalIgnoreCase) ||
			LooksLikeFieldSpec(body))
			return ParseSchemaField(name + " " + body, 0);
		return new YclField(name, new YclType(name, ParseTypeFields(body)), null, null, false, false);
	}

	private bool LooksLikeFieldSpec(string body)
	{
		var first = SplitTopLevel(body, allowWhitespaceSeparator: true)
			.Select(o => o.Trim())
			.FirstOrDefault(o => o.Length > 0);
		if (first is null)
			return false;
		if (first.EndsWith("[]", StringComparison.Ordinal))
			first = first.Substring(0, first.Length - 2);
		return IsScalarType(first) || _types.ContainsKey(first);
	}

	private static bool IsScalarType(string valueType)
		=> valueType.ToLowerInvariant() is "string" or "int" or "integer" or "number" or "decimal" or "bool" or "boolean" or
			"date" or "datetime" or "date-time" or "timestamp" or "period" or "duration" or "timespan" or "time-span";

	private string[] ResolveSchemaDeclarationPath(string text)
	{
		var parts = text.Split(['/'], StringSplitOptions.RemoveEmptyEntries)
			.Select(o => UnquoteName(o.Trim()))
			.Where(o => o.Length > 0)
			.ToArray();
		if (parts.Length == 0)
			throw new SyntaxException("Schema field path expected.");
		if (parts.Any(o => o == ".") && _lastSchemaPath is null)
			throw new SyntaxException("Relative schema path requires a preceding declaration.");
		var result = new string[parts.Length];
		for (int i = 0; i < parts.Length; ++i)
		{
			if (parts[i] == ".")
			{
				if (_lastSchemaPath is null || i >= _lastSchemaPath.Length)
					throw new SyntaxException("Relative schema path exceeds the preceding declaration path.");
				result[i] = _lastSchemaPath[i];
			}
			else
			{
				result[i] = parts[i];
			}
		}
		return result;
	}

	private void SetSeparator(string text)
	{
		var raw = TrimAssignment(text);
		if (raw.Length == 0)
		{
			_itemSeparators.Clear();
			_itemSeparators.Add(",");
			_itemSeparators.Add(";");
			return;
		}
		var separator = ParseScalar(raw);
		if (String.IsNullOrEmpty(separator))
			throw new SyntaxException("Separator expected.");
		if (!_itemSeparators.Contains(separator, StringComparer.Ordinal))
		{
			_itemSeparators.Add(separator);
			_itemSeparators.Sort((x, y) => y.Length.CompareTo(x.Length));
		}
	}

	private void SetEscape(string text)
	{
		if (TrimAssignment(text).Length == 0)
		{
			_escapeCharacter = '`';
			return;
		}
		if (!TryParseEscapeSetting(text, _escapeCharacter, out var escape))
			throw new SyntaxException("Escape character expected.");
		_escapeCharacter = escape;
	}

	private YclType GetType(string name)
	{
		if (_types.TryGetValue(name.Trim(), out var type))
			return type;
		throw new SyntaxException($"Type '{name}' is not declared.");
	}

	private static string TrimAssignment(string value)
	{
		value = value.Trim();
		if (value.Length > 0 && (value[0] == ':' || value[0] == '='))
			value = value.Substring(1).TrimStart();
		return value;
	}

	internal string ParseMetaScalarValue(string text)
		=> ParseScalar(TrimAssignment(text));

	private YclNodeItem ParseNodeItem(string text, int lineNumber)
	{
		if (text == "-")
			return new YclNodeItem(null, null);
		if (text.StartsWith("- ", StringComparison.Ordinal) || text.StartsWith("-\t", StringComparison.Ordinal))
			return new YclNodeItem(null, text.Substring(1).TrimStart());

		int sep = FindBlockSeparator(text);
		if (sep >= 0)
		{
			var name = text.Substring(0, sep).Trim();
			if (name.Length == 0)
				throw new SyntaxException($"Name expected at line {lineNumber}.");
			var value = text.Substring(sep + 1).TrimStart();
			return new YclNodeItem(UnquoteName(name), value.Length == 0 ? null: value);
		}

		int split = FindNameValueSplit(text);
		if (split < 0)
			return new YclNodeItem(UnquoteName(text.Trim()), null);
		return new YclNodeItem(UnquoteName(text.Substring(0, split).Trim()), text.Substring(split).TrimStart());
	}

	private ConfigNode ParseValue(string text, ConfigSourceLocation location = default, IReadOnlyList<string?>? path = null)
	{
		text = text.Trim();
		if (text.Length == 0)
			return new ConfigNode(String.Empty, location);
		if (text[0] != '"' && text[0] != '\'')
			text = Substitute(text, location, path);
		if (text[0] == '[' || text[0] == '{' || text[0] == '(')
		{
			var stream = new CharStream(text);
			return new ConfigNode(ParseFlowCollection(ref stream, location, path), location);
		}
		return new ConfigNode(ParseScalar(text, location: location, path: path), location);
	}

	private ConfigNodeCollection ParseFlowCollection(ref CharStream stream, ConfigSourceLocation location = default, IReadOnlyList<string?>? path = null)
	{
		char open = stream[0];
		char close = open switch
		{
			'[' => ']',
			'{' => '}',
			'(' => ')',
			_ => throw stream.SyntaxException("Flow collection expected.")
		};
		stream.Forward(1);

		var items = new List<KeyValuePair<string?, ConfigNode>>();
		while (true)
		{
			SkipSpaceAndComments(ref stream);
			if (stream[0] == close)
			{
				stream.Forward(1);
				return new ConfigNodeCollection(items, location);
			}
			if (stream.Eof)
				throw stream.SyntaxException($"Missing '{close}'.");
			var separator = MatchItemSeparator(ref stream, requireTrailingWhitespace: true);
			if (separator is not null)
			{
				items.Add(new KeyValuePair<string?, ConfigNode>(null, new ConfigNode((string?)null, location)));
				stream.Forward(separator.Length);
				continue;
			}

			if (open == '{')
			{
				var name = ReadFlowName(ref stream);
				SkipSpaceAndComments(ref stream);
			if (!IsFlowAssignmentSeparator(ref stream))
				throw stream.SyntaxException("Object item assignment expected.");
			stream.Forward(1);
				items.Add(new KeyValuePair<string?, ConfigNode>(name, ReadFlowValue(ref stream, close, location, path)));
			}
			else
			{
				int at = stream.Position;
				var name = TryReadFlowNameValue(ref stream, close, location, path, out var node);
				if (name is null)
				{
					stream.Move(at);
					node = ReadFlowValue(ref stream, close, location, path);
				}
				items.Add(new KeyValuePair<string?, ConfigNode>(name, node));
			}

			SkipSpaceAndComments(ref stream);
			separator = MatchItemSeparator(ref stream, requireTrailingWhitespace: true);
			if (separator is not null)
				stream.Forward(separator.Length);
		}
	}

	private string? TryReadFlowNameValue(ref CharStream stream, char close, ConfigSourceLocation location, IReadOnlyList<string?>? path, out ConfigNode node)
	{
		node = default;
		if (stream[0] == '"' || stream[0] == '\'')
			return null;
		var name = ReadFlowName(ref stream);
		SkipSpaceAndComments(ref stream);
		if (!IsFlowAssignmentSeparator(ref stream))
			return null;
		stream.Forward(1);
		node = ReadFlowValue(ref stream, close, location, path);
		return name;
	}

	private ConfigNode ReadFlowValue(ref CharStream stream, char close, ConfigSourceLocation location = default, IReadOnlyList<string?>? path = null)
	{
		SkipSpaceAndComments(ref stream);
		if (stream[0] == '[' || stream[0] == '{' || stream[0] == '(')
			return new ConfigNode(ParseFlowCollection(ref stream, location, path), location);
		if (stream[0] == '"' || stream[0] == '\'')
			return new ConfigNode(Substitute(ReadQuotedString(ref stream), location, path), location);

		int start = stream.Position;
		int depth = 0;
		while (!stream.Eof)
		{
			char ch = stream[0];
			if (depth == 0 && (ch == close || MatchItemSeparator(ref stream, requireTrailingWhitespace: true) is not null))
				break;
			if (ch == '[' || ch == '{' || ch == '(')
				++depth;
			else if (ch == ']' || ch == '}' || ch == ')')
			{
				if (depth == 0)
					break;
				--depth;
			}
			else if (Char.IsWhiteSpace(ch) && depth == 0)
			{
				break;
			}
			stream.Forward(1);
		}
		return new ConfigNode(Substitute(stream.Chunk(start, stream.Position - start).ToString().Trim(), location, path), location);
	}

	private List<ConfigNode> ParsePositionalValues(string text, ConfigSourceLocation location = default, IReadOnlyList<string?>? path = null)
	{
		var stream = new CharStream(text);
		var result = new List<ConfigNode>();
		bool previousWasSeparator = true;
		while (true)
		{
			SkipSpaceAndComments(ref stream);
			if (stream.Eof)
				break;
			var separator = MatchItemSeparator(ref stream, requireTrailingWhitespace: true);
			if (separator is not null)
			{
				if (previousWasSeparator)
					result.Add(new ConfigNode((string?)null, location));
				previousWasSeparator = true;
				stream.Forward(separator.Length);
				continue;
			}
			result.Add(ReadFlowValue(ref stream, '\0', location, path));
			previousWasSeparator = false;
		}
		return result;
	}

	private string ParseScalar(string text, bool substitute = true, ConfigSourceLocation location = default, IReadOnlyList<string?>? path = null)
	{
		text = text.Trim();
		if (text.Length >= 2 && (text[0] == '"' && text[text.Length - 1] == '"' || text[0] == '\'' && text[text.Length - 1] == '\''))
		{
			var stream = new CharStream(text);
			var value = ReadQuotedString(ref stream);
			return substitute ? Substitute(value, location, path): value;
		}
		return substitute ? Substitute(text, location, path): text;
	}

	private string ReadQuotedString(ref CharStream stream)
	{
		var token = Tokenizer.StringTokenRule.ParseString(LexicalTokenType.STRING, ref stream, _escapeCharacter);
		return token.GetString(stream);
	}

	private string Substitute(string text)
		=> Substitute(text, default, null, []);

	private string Substitute(string text, ConfigSourceLocation location, IReadOnlyList<string?>? path)
		=> Substitute(text, location, path, []);

	private string Substitute(string text, ConfigSourceLocation location, IReadOnlyList<string?>? path, HashSet<string> stack)
	{
		if (text.IndexOf("${", StringComparison.Ordinal) < 0)
			return text;
		var result = new StringBuilder();
		int i = 0;
		while (i < text.Length)
		{
			int j = text.IndexOf("${", i, StringComparison.Ordinal);
			if (j < 0)
			{
				result.Append(text.Substring(i));
				break;
			}
			result.Append(text.Substring(i, j - i));
			int k = text.IndexOf('}', j + 2);
			if (k < 0)
			{
				result.Append(text.Substring(j));
				break;
			}
			var expr = text.Substring(j + 2, k - j - 2);
			var pipe = expr.IndexOf('|');
			var name = pipe < 0 ? expr: expr.Substring(0, pipe);
			if (_variables.TryGetValue(name, out var value) && stack.Add(name))
			{
				result.Append(Substitute(value, location, path, stack));
				stack.Remove(name);
			}
			else if (_variables.ContainsKey(name))
			{
				if (!_recursiveSubstitutionWarningIssued)
				{
					AddWarning($"Recursive substitution detected for variable '{name}'.");
					_recursiveSubstitutionWarningIssued = true;
				}
				result.Append(text.Substring(j, k - j + 1));
			}
			else if (TryGetSubstitutionSourceValue(name, location, path, out var sourceValue))
				result.Append(sourceValue);
			else if (pipe >= 0)
				result.Append(Substitute(expr.Substring(pipe + 1), location, path, stack));
			else
				result.Append(text.Substring(j, k - j + 1));
			i = k + 1;
		}
		return result.ToString();
	}

	private bool TryGetSubstitutionSourceValue(string expression, ConfigSourceLocation location, IReadOnlyList<string?>? path, out string value)
	{
		value = String.Empty;
		var colon = expression.IndexOf(':');
		if (colon < 0)
			return false;
		var sourceName = expression.Substring(0, colon);
		var reference = expression.Substring(colon + 1);
		if (!_substitutionSources.TryGetValue(sourceName, out var source))
			return false;
		var context = new YclSubstitutionContext(this, sourceName, reference, location, path ?? []);
		var result = source(context);
		if (result is null)
			return false;
		value = result;
		return true;
	}

	private List<YclField> ParseTypeFields(string text)
	{
		var fields = new List<YclField>();
		bool hasExplicitSeparator = HasTopLevelItemSeparator(text);
		var items = SplitTopLevel(text, allowWhitespaceSeparator: !hasExplicitSeparator);
		if (!hasExplicitSeparator)
			items = CombineWhitespaceSeparatedFieldSpecs(items);
		foreach (var item in items)
		{
			if (String.IsNullOrWhiteSpace(item))
				continue;
			fields.Add(ParseTypeField(item.Trim()));
		}
		return fields;
	}

	private static List<string> CombineWhitespaceSeparatedFieldSpecs(List<string> items)
	{
		var result = new List<string>();
		foreach (var item in items)
		{
			var text = item.Trim();
			if (text.Length == 0)
				continue;
			if (result.Count > 0 && IsFieldSpecContinuation(text))
			{
				result[result.Count - 1] += " " + text;
				continue;
			}
			result.Add(text);
		}
		return result;
	}

	private static bool IsFieldSpecContinuation(string text)
		=> text.StartsWith(":", StringComparison.Ordinal) ||
			text.StartsWith("=", StringComparison.Ordinal) ||
			text.Equals("required", StringComparison.OrdinalIgnoreCase);

	private YclField ParseTypeField(string text)
	{
		int shape = FindTopLevel(text, ':');
		int assign = FindTopLevel(text, '=');
		int structure = FindTopLevelStructuredType(text);
		int split = MinPositive(shape, assign, structure);
		var name = split < 0 ? text.Trim(): text.Substring(0, split).Trim();
		string? defaultValue = null;

		if (assign >= 0)
			defaultValue = text.Substring(assign + 1).Trim();
		var spec = structure >= 0 && structure == split ?
			text.Substring(structure, (assign > structure ? assign: text.Length) - structure).Trim():
			shape < 0 ? String.Empty:
			text.Substring(shape + 1, (assign > shape ? assign: text.Length) - shape - 1).Trim();
		if (shape >= 0 && spec.StartsWith("(", StringComparison.Ordinal))
			throw new SyntaxException($"Structured type for field '{name}' must not use ':' prefix.");
		var (type, valueType, isArray, required) = ParseFieldTypeSpec(name, spec);

		return new YclField(name, type, defaultValue, valueType, isArray, required);
	}

	private int FindTopLevelStructuredType(string text)
	{
		int depth = 0;
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
			{
				quote = ch;
				continue;
			}
			if (ch == '(')
			{
				if (depth == 0 && i > 0 && Char.IsWhiteSpace(text[i - 1]))
					return i;
				++depth;
			}
			else if (ch == ')' && depth > 0)
			{
				--depth;
			}
		}
		return -1;
	}

	private static int MinPositive(params int[] values)
	{
		var result = -1;
		foreach (var value in values)
		{
			if (value >= 0 && (result < 0 || value < result))
				result = value;
		}
		return result;
	}

	private void AddSchemaField(List<YclField> fields, string text)
	{
		int split = FindNameValueSplit(text);
		var pathText = split < 0 ? text.Trim(): text.Substring(0, split).Trim();
		var body = split < 0 ? String.Empty: text.Substring(split).TrimStart();
		var path = pathText.Split(['/'], StringSplitOptions.RemoveEmptyEntries)
			.Select(o => UnquoteName(o.Trim()))
			.Where(o => o.Length > 0)
			.ToArray();
		if (path.Length == 0)
			throw new SyntaxException("Schema field path expected.");

		var fieldText = path[path.Length - 1] + (body.Length == 0 ? String.Empty: " " + body);
		var field = ParseSchemaField(fieldText, 0);
		AddSchemaField(fields, path, 0, field);
	}

	private static void AddSchemaField(List<YclField> fields, string[] path, int index, YclField field)
	{
		if (index == path.Length - 1)
		{
			int existing = fields.FindIndex(o => o.Name == field.Name);
			if (existing < 0)
				fields.Add(field);
			else
				fields[existing] = field;
			return;
		}

		var name = path[index];
		int at = fields.FindIndex(o => o.Name == name);
		if (at < 0)
		{
			var child = new YclField(name, new YclType(name, []), null, null, false, false);
			fields.Add(child);
			at = fields.Count - 1;
		}
		else if (fields[at].Type is null)
		{
			fields[at] = fields[at] with { Type = new YclType(name, []) };
		}

		AddSchemaField(fields[at].Type!.Fields, path, index + 1, field);
	}

	private YclField ParseSchemaField(string text, int lineNumber)
	{
		int split = FindNameValueSplit(text);
		var name = split < 0 ? text.Trim(): text.Substring(0, split).Trim();
		if (name.EndsWith(":", StringComparison.Ordinal))
			name = name.Substring(0, name.Length - 1).TrimEnd();
		name = UnquoteName(name);
		if (name.Length == 0)
			throw new SyntaxException($"Schema field name expected at line {lineNumber}.");

		var body = split < 0 ? String.Empty: text.Substring(split).TrimStart();
		if (body.StartsWith(":", StringComparison.Ordinal) && body.Substring(1).TrimStart().StartsWith("(", StringComparison.Ordinal))
			throw new SyntaxException($"Structured type for field '{name}' must not use ':' prefix.");
		if (body.StartsWith(":", StringComparison.Ordinal))
			body = body.Substring(1).TrimStart();
		int assign = FindTopLevel(body, '=');
		var spec = assign < 0 ? body.Trim(): body.Substring(0, assign).Trim();
		var defaultValue = assign < 0 ? null: body.Substring(assign + 1).Trim();

		var (nestedType, valueType, isArray, required) = ParseFieldTypeSpec(name, spec);
		return new YclField(name, nestedType, defaultValue, valueType, isArray, required);
	}

	private (YclType? Type, string? ValueType, bool IsArray, bool Required) ParseFieldTypeSpec(string fieldName, string spec)
	{
		string? valueType = null;
		bool isArray = false;
		bool required = false;
		YclType? nestedType = null;
		spec = spec.Trim();
		if (spec.StartsWith(":", StringComparison.Ordinal) && spec.Substring(1).TrimStart().StartsWith("(", StringComparison.Ordinal))
			throw new SyntaxException($"Structured type for field '{fieldName}' must not use ':' prefix.");
		if (spec.StartsWith("(", StringComparison.Ordinal) && spec.EndsWith(")", StringComparison.Ordinal))
			return (new YclType(fieldName, ParseSchemaFieldList(spec.Substring(1, spec.Length - 2))), null, false, false);
		if (spec.Length > 0)
		{
			var parts = SplitTopLevel(spec, allowWhitespaceSeparator: true)
				.Select(o => o.Trim())
				.Where(o => o.Length > 0)
				.ToList();
			foreach (var part in parts)
			{
				var item = part;
				if (item.Equals("required", StringComparison.OrdinalIgnoreCase))
				{
					required = true;
					continue;
				}
				if (item.StartsWith(":", StringComparison.Ordinal))
				{
					item = item.Substring(1).Trim();
					if (item.Length == 0)
						continue;
				}
				if (item.StartsWith("(", StringComparison.Ordinal) && item.EndsWith(")", StringComparison.Ordinal))
				{
					if (part.StartsWith(":", StringComparison.Ordinal))
						throw new SyntaxException($"Structured type for field '{fieldName}' must not use ':' prefix.");
					nestedType = new YclType(fieldName, ParseSchemaFieldList(item.Substring(1, item.Length - 2)));
					continue;
				}
				if (item.StartsWith("(", StringComparison.Ordinal))
					throw new SyntaxException($"Structured type for field '{fieldName}' must not use ':' prefix.");
				if (_types.TryGetValue(item, out var declaredType))
				{
					nestedType = declaredType;
					continue;
				}
				if (valueType is not null)
					continue;
				valueType = item;
				if (valueType.EndsWith("[]", StringComparison.Ordinal))
				{
					isArray = true;
					valueType = valueType.Substring(0, valueType.Length - 2);
				}
				valueType = valueType.ToLowerInvariant();
			}
		}
		return (nestedType, valueType, isArray, required);
	}

	private List<YclField> ParseSchemaFieldList(string text)
	{
		var fields = new List<YclField>();
		foreach (var item in SplitTopLevel(text, allowWhitespaceSeparator: false))
		{
			if (String.IsNullOrWhiteSpace(item))
				continue;
			fields.Add(ParseSchemaField(item.Trim(), 0));
		}
		return fields;
	}

	private List<string> SplitTopLevel(string text, bool allowWhitespaceSeparator)
	{
		var result = new List<string>();
		int start = 0;
		int depth = 0;
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
			{
				quote = ch;
				continue;
			}
			if (ch == '(' || ch == '[' || ch == '{')
				++depth;
			else if (ch == ')' || ch == ']' || ch == '}')
				--depth;
			var separator = depth == 0 ? MatchItemSeparator(text, i, requireTrailingWhitespace: true): null;
			if (depth == 0 && (separator is not null || allowWhitespaceSeparator && Char.IsWhiteSpace(ch)))
			{
				if (i > start)
					result.Add(text.Substring(start, i - start));
				if (separator is not null)
					i += separator.Length - 1;
				while (i + 1 < text.Length)
				{
					if (Char.IsWhiteSpace(text[i + 1]))
					{
						++i;
						continue;
					}
					var nextSeparator = MatchItemSeparator(text, i + 1, requireTrailingWhitespace: true);
					if (nextSeparator is null)
						break;
					i += nextSeparator.Length;
				}
				start = i + 1;
			}
		}
		if (start < text.Length)
			result.Add(text.Substring(start));
		return result;
	}

	private int FindTopLevel(string text, char value)
	{
		int depth = 0;
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if (ch == '(' || ch == '[' || ch == '{')
				++depth;
			else if (ch == ')' || ch == ']' || ch == '}')
				--depth;
			else if (depth == 0 && ch == value)
				return i;
		}
		return -1;
	}

	private int FindBlockSeparator(string text)
	{
		char quote = '\0';
		bool afterNameSpace = false;
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if ((ch == ':' || ch == '=') && (i + 1 == text.Length || Char.IsWhiteSpace(text[i + 1])))
				return i;
			else if (Char.IsWhiteSpace(ch))
				afterNameSpace = true;
			else if (afterNameSpace)
				return -1;
		}
		return -1;
	}

	private int FindNameValueSplit(string text)
	{
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if (Char.IsWhiteSpace(ch))
				return i;
		}
		return -1;
	}

	private static int ReadNameLength(string text)
	{
		int i = 0;
		while (i < text.Length && !Char.IsWhiteSpace(text[i]) && text[i] != ':' && text[i] != '=' && text[i] != ',' && text[i] != ';')
			++i;
		return i;
	}

	private string UnquoteName(string name)
	{
		name = name.Trim();
		if (name.Length >= 2 && (name[0] == '"' && name[name.Length - 1] == '"' || name[0] == '\'' && name[name.Length - 1] == '\''))
		{
			var stream = new CharStream(name);
			return ReadQuotedString(ref stream);
		}
		return name;
	}

	private string ReadFlowName(ref CharStream stream)
	{
		SkipSpaceAndComments(ref stream);
		if (stream[0] == '"' || stream[0] == '\'')
			return ReadQuotedString(ref stream);
		int start = stream.Position;
		while (!stream.Eof)
		{
			char ch = stream[0];
			if (Char.IsWhiteSpace(ch) || IsFlowAssignmentSeparator(ref stream) || MatchItemSeparator(ref stream, requireTrailingWhitespace: true) is not null || ch == ')' || ch == ']' || ch == '}')
				break;
			stream.Forward(1);
		}
		return Substitute(stream.Chunk(start, stream.Position - start).ToString());
	}

	private bool HasTopLevelItemSeparator(string text)
	{
		int depth = 0;
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == _escapeCharacter)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if (ch == '(' || ch == '[' || ch == '{')
				++depth;
			else if (ch == ')' || ch == ']' || ch == '}')
				--depth;
			else if (depth == 0 && MatchItemSeparator(text, i, requireTrailingWhitespace: true) is not null)
				return true;
		}
		return false;
	}

	private string? MatchItemSeparator(ref CharStream stream, bool requireTrailingWhitespace)
	{
		foreach (var separator in _itemSeparators)
		{
			if (stream.StartsWith(separator) && IsValidItemSeparator(ref stream, separator, requireTrailingWhitespace))
				return separator;
		}
		return null;
	}

	private string? MatchItemSeparator(string text, int index, bool requireTrailingWhitespace)
	{
		foreach (var separator in _itemSeparators)
		{
			if (index + separator.Length <= text.Length && text.AsSpan(index, separator.Length).SequenceEqual(separator.AsSpan()) &&
				IsValidItemSeparator(text, index, separator, requireTrailingWhitespace))
				return separator;
		}
		return null;
	}

	private static bool IsValidItemSeparator(ref CharStream stream, string separator, bool requireTrailingWhitespace)
	{
		if (separator == "," || separator == ";")
		{
			if (!requireTrailingWhitespace)
				return true;
			int end = separator.Length;
			while (stream.Slice(end).StartsWith(separator.AsSpan()))
				end += separator.Length;
			return end >= stream.Length || Char.IsWhiteSpace(stream[end]);
		}
		return stream.Position > 0 && Char.IsWhiteSpace(stream.Chunk(stream.Position - 1, 1)[0]) &&
			(separator.Length >= stream.Length || Char.IsWhiteSpace(stream[separator.Length]));
	}

	private static bool IsFlowAssignmentSeparator(ref CharStream stream)
	{
		char ch = stream[0];
		if (ch != ':' && ch != '=')
			return false;
		return stream.Length > 1 && Char.IsWhiteSpace(stream[1]);
	}

	private static bool IsValidItemSeparator(string text, int index, string separator, bool requireTrailingWhitespace)
	{
		if (separator == "," || separator == ";")
		{
			if (!requireTrailingWhitespace)
				return true;
			int end = index + separator.Length;
			while (end < text.Length && text.AsSpan(end, separator.Length).SequenceEqual(separator.AsSpan()))
				end += separator.Length;
			return end >= text.Length || Char.IsWhiteSpace(text[end]);
		}
		return index > 0 && Char.IsWhiteSpace(text[index - 1]) &&
			(index + separator.Length >= text.Length || Char.IsWhiteSpace(text[index + separator.Length]));
	}

	private static void SkipSpaceAndComments(ref CharStream stream)
	{
		while (!stream.Eof)
		{
			if (Char.IsWhiteSpace(stream[0]))
			{
				stream.Forward(1);
				continue;
			}
			if (stream.StartsWith("//"))
			{
				int i = stream.IndexOf('\n');
				stream.Forward(i < 0 ? stream.Length: i + 1);
				continue;
			}
			if (stream.StartsWith("/*"))
			{
				int i = stream.IndexOf("*/", 2);
				if (i < 0)
					throw stream.SyntaxException("EOF in comment.");
				stream.Forward(i + 2);
				continue;
			}
			if (stream.StartsWith("<#"))
			{
				int i = stream.IndexOf("#>", 2);
				if (i < 0)
					throw stream.SyntaxException("EOF in comment.");
				stream.Forward(i + 2);
				continue;
			}
			if (stream[0] == '#')
			{
				int i = stream.IndexOf('\n');
				stream.Forward(i < 0 ? stream.Length: i + 1);
				continue;
			}
			break;
		}
	}

	private List<YclLine> ReadLines(string text)
	{
		var result = new List<YclLine>();
		var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
		var lines = normalized.Split('\n');
		char escape = '`';
		string? blockClose = null;
		string? multilineClose = null;
		for (int i = 0; i < lines.Length; ++i)
		{
			string raw = (multilineClose is null ? StripComments(lines[i], ref blockClose, escape): lines[i]).TrimEnd();
			if (String.IsNullOrWhiteSpace(raw))
			{
				result.Add(new YclLine(0, String.Empty, i + 1, true, new ConfigSourceLocation(_sourceName, i + 1, 1)));
				continue;
			}
			var indentation = ReadIndentation(raw);
			var logical = new StringBuilder(raw.Substring(indentation.Length).TrimEnd());
			int balance = FlowBalance(logical.ToString(), escape);
			while (balance > 0 && i + 1 < lines.Length)
			{
				string next = StripComments(lines[++i], ref blockClose, escape).Trim();
				if (next.Length == 0)
					continue;
				logical.Append(' ').Append(next);
				balance += FlowBalance(next, escape);
			}
			var line = logical.ToString();
			result.Add(new YclLine(indentation.Width, line, i + 1, false, new ConfigSourceLocation(_sourceName, i + 1, indentation.Length + 1)));
			if (multilineClose is null && TryGetMultilineCloseMarkerFromLine(line, out var closeMarker))
				multilineClose = closeMarker;
			else if (multilineClose is not null && line == multilineClose)
				multilineClose = null;
			else if (multilineClose is null && TryReadEscapeDirective(line, escape, out var nextEscape))
				escape = nextEscape;
		}
		if (blockClose is not null)
			throw new SyntaxException("EOF in comment.");
		if (multilineClose is not null)
			throw new SyntaxException($"Missing multiline terminator '{multilineClose}'.");
		return result;
	}

	private static bool TryGetMultilineCloseMarkerFromLine(string line, out string closeMarker)
	{
		var value = String.Empty;
		int sep = -1;
		for (int i = 0; i < line.Length; ++i)
		{
			if ((line[i] == ':' || line[i] == '=') && (i + 1 == line.Length || Char.IsWhiteSpace(line[i + 1])))
			{
				sep = i;
				break;
			}
			if (Char.IsWhiteSpace(line[i]))
			{
				value = line.Substring(i + 1).TrimStart();
				break;
			}
		}
		if (sep >= 0)
			value = line.Substring(sep + 1).TrimStart();
		return TryGetMultilineCloseMarker(value, out closeMarker);
	}

	private static (int Width, int Length) ReadIndentation(string text)
	{
		int width = 0;
		int length = 0;
		while (length < text.Length)
		{
			if (text[length] == ' ')
			{
				++width;
				++length;
			}
			else if (text[length] == '\t')
			{
				width += 4 - width % 4;
				++length;
			}
			else
			{
				break;
			}
		}
		return (width, length);
	}

	private static int FlowBalance(string text, char escape)
	{
		int balance = 0;
		char quote = '\0';
		for (int i = 0; i < text.Length; ++i)
		{
			char ch = text[i];
			if (quote != '\0')
			{
				if (ch == escape)
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if (ch == '[' || ch == '{' || ch == '(')
				++balance;
			else if (ch == ']' || ch == '}' || ch == ')')
				--balance;
		}
		return balance;
	}

	private static string StripComments(string line, ref string? blockClose, char escape)
	{
		var result = new StringBuilder(line.Length);
		char quote = '\0';
		for (int i = 0; i < line.Length;)
		{
			if (blockClose is not null)
			{
				int end = line.IndexOf(blockClose, i, StringComparison.Ordinal);
				if (end < 0)
					return result.ToString();
				i = end + blockClose.Length;
				blockClose = null;
				continue;
			}

			char ch = line[i];
			if (quote != '\0')
			{
				result.Append(ch);
				if (ch == escape && i + 1 < line.Length)
					result.Append(line[++i]);
				else if (ch == quote)
					quote = '\0';
				++i;
				continue;
			}

			if (ch == '"' || ch == '\'')
			{
				quote = ch;
				result.Append(ch);
				++i;
			}
			else if (i + 1 < line.Length && ch == '/' && line[i + 1] == '*' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
			{
				blockClose = "*/";
				i += 2;
			}
			else if (i + 1 < line.Length && ch == '<' && line[i + 1] == '#' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
			{
				blockClose = "#>";
				i += 2;
			}
			else if (ch == '#' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
			{
				break;
			}
			else if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
			{
				break;
			}
			else
			{
				result.Append(ch);
				++i;
			}
		}
		return result.ToString();
	}

	private static bool TryReadEscapeDirective(string line, char currentEscape, out char escape)
	{
		escape = '\0';
		line = NormalizeDirective(line);
		if (!TryReadMetaStatement(line, out var name, out var value) || name != "set-escape")
			return false;
		return TryParseEscapeSetting(value, currentEscape, out escape);
	}

	private static bool TryReadMetaStatement(string text, out string name, out string value)
	{
		name = String.Empty;
		value = String.Empty;
		if (!text.StartsWith("%!", StringComparison.Ordinal))
			return false;
		var rest = text.Substring(2).TrimStart();
		int n = ReadNameLength(rest);
		if (n == 0)
			return false;
		name = rest.Substring(0, n);
		value = rest.Substring(n).TrimStart();
		return true;
	}

	private static bool TryParseEscapeSetting(string text, char currentEscape, out char escape)
	{
		escape = '\0';
		var value = TrimAssignment(text);
		if (value.Length >= 2 &&
			(value[0] == '"' && value[value.Length - 1] == '"' ||
			 value[0] == '\'' && value[value.Length - 1] == '\''))
		{
			value = value.Substring(1, value.Length - 2);
		}
		if (value.Length == 1)
		{
			escape = value[0];
			return true;
		}
		if (value.Length > 1 && currentEscape != StringTokenRule.Nil && value[0] == currentEscape)
		{
			escape = StringTokenRule.ParseEscape(value.AsSpan(1), out var length);
			return length == value.Length - 1;
		}
		return false;
	}

	private static string RemoveBlockComments(string text)
	{
		var result = new StringBuilder();
		char quote = '\0';
		for (int i = 0; i < text.Length;)
		{
			if (quote != '\0')
			{
				result.Append(text[i]);
				if (text[i] == '`' && i + 1 < text.Length)
					result.Append(text[++i]);
				else if (text[i] == quote)
					quote = '\0';
				++i;
			}
			else if (text[i] == '"' || text[i] == '\'')
			{
				quote = text[i];
				result.Append(text[i++]);
			}
			else if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
			{
				int j = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
				if (j < 0)
					throw new SyntaxException("EOF in comment.");
				i = j + 2;
			}
			else if (i + 1 < text.Length && text[i] == '<' && text[i + 1] == '#')
			{
				int j = text.IndexOf("#>", i + 2, StringComparison.Ordinal);
				if (j < 0)
					throw new SyntaxException("EOF in comment.");
				i = j + 2;
			}
			else
			{
				result.Append(text[i++]);
			}
		}
		return result.ToString();
	}

	private static string StripLineComment(string line)
	{
		char quote = '\0';
		for (int i = 0; i < line.Length; ++i)
		{
			char ch = line[i];
			if (quote != '\0')
			{
				if (ch == '`')
					++i;
				else if (ch == quote)
					quote = '\0';
				continue;
			}
			if (ch == '"' || ch == '\'')
				quote = ch;
			else if (ch == '#' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
				return line.Substring(0, i);
			else if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/' && (i == 0 || Char.IsWhiteSpace(line[i - 1])))
				return line.Substring(0, i);
		}
		return line;
	}

	private readonly record struct YclLine(int Indent, string Text, int Number, bool IsBlank, ConfigSourceLocation Location);
	private readonly record struct YclNodeItem(string? Name, string? RawValue);
	private readonly record struct ResolvedType(YclType? Type, string? Name, YclField? Field);
	private sealed record YclType(string Name, List<YclField> Fields);
	private sealed record YclField(string Name, YclType? Type, string? DefaultValue, string? ValueType, bool IsArray, bool Required);
	private sealed record YclTypeApplication(string?[] DeclarationPath, string[] Pattern, YclType Type, bool NameUnnamedNodes, bool LiteralPatternMatchesUnnamed, bool OneShot)
	{
		public bool IsActivated { get; set; }
		public string?[]? CapturedParentPath { get; set; }
		public bool Consumed { get; set; }
	}
}
