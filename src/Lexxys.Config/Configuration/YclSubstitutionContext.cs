namespace Lexxys.Configuration;

public delegate string? YclSubstitutionSource(YclSubstitutionContext context);

public sealed class YclSubstitutionContext
{
	private readonly YclParser _parser;

	internal YclSubstitutionContext(YclParser parser, string sourceName, string reference, ConfigSourceLocation location, IReadOnlyList<string?> nodePath)
	{
		_parser = parser;
		SourceName = sourceName;
		Reference = reference;
		Location = location;
		NodePath = nodePath;
	}

	public string SourceName { get; }

	public string Reference { get; }

	public ConfigSourceLocation Location { get; }

	public IReadOnlyList<string?> NodePath { get; }

	public string? GetConfigurationValue(ConfigNodeCollection collection)
		=> _parser.GetConfigurationValue(collection, Reference);

	public string? GetCurrentDocumentValue()
		=> _parser.GetCurrentDocumentValue(Reference);
}
