namespace Lexxys.Configuration;

public delegate ConfigNodeCollection? YclMetaStatementHandler(YclMetaStatementContext context);

public sealed class YclMetaStatementContext
{
	private readonly YclParser _parser;

	internal YclMetaStatementContext(YclParser parser, string name, string rawValue, ConfigSourceLocation location, IReadOnlyList<string?> nodePath)
	{
		_parser = parser;
		Name = name;
		RawValue = rawValue;
		Location = location;
		NodePath = nodePath;
	}

	public string Name { get; }

	public string RawValue { get; }

	public ConfigSourceLocation Location { get; }

	public IReadOnlyList<string?> NodePath { get; }

	public string ReadScalarValue() => _parser.ParseMetaScalarValue(RawValue);

	public string ReadRequiredScalarValue(string message)
	{
		var value = ReadScalarValue();
		if (String.IsNullOrWhiteSpace(value))
			throw new SyntaxException(message);
		return value;
	}

	public string ResolveIncludePath(string includePath) => _parser.ResolveIncludePath(includePath);

	public ConfigNodeCollection ParseYclInclude()
		=> _parser.ParseInclude(ReadRequiredScalarValue("Include path expected."));

	public ConfigNodeCollection ParseInclude(Func<string, string, ConfigNodeCollection> parser)
		=> _parser.ParseInclude(ReadRequiredScalarValue("Include path expected."), parser);

	public ConfigNodeCollection ParseInclude(string includePath, Func<string, string, ConfigNodeCollection> parser)
		=> _parser.ParseInclude(includePath, parser);
}
