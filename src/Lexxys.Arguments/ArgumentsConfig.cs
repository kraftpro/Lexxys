namespace Lexxys;

public enum ParameterNaming
{
	/// <summary>
	/// Kebab-case (e.g. <c>--output-directory</c>)
	/// </summary>
	KebabCase,
	/// <summary>
	/// CamelCase (e.g. <c>--outputDirectory</c>) or PascalCase (e.g. <c>--OutputDirectory</c>), depending on the case of the first character.
	/// </summary>
	CamelCase,
	/// <summary>
	/// PascalCase (e.g. <c>--OutputDirectory</c>)
	/// </summary>
	PascalCase,
	/// <summary>
	/// Snake_case (e.g. <c>--output_directory</c>)
	/// </summary>
	SnakeCase,
	/// <summary>
	/// Dot.case (e.g. <c>--output.directory</c>)
	/// </summary>
	DotCase,
}

public enum ParameterMatching
{
	/// <summary>
	/// Partial matching for single-dashs, ignores name separators, and allows name variations. (e.g. <c>-verb</c> can match <c>--verbose</c>, <c>-outdir</c> can match <c>--output-directory</c>)
	/// </summary>
	Fluent,
	/// <summary>
	/// Partial matching for single-dash, requires name separators. (e.g. <c>-verb</c> can match <c>--verbose</c>, <c>-out-dir</c> can match <c>--output-directory</c>, but <c>-outdir</c> would not match <c>--output-directory</c>)
	/// </summary>
	Flexible,
	/// <summary>
	/// Exact name matching.
	/// </summary>
	Strict,
}

/// <summary>
/// Represents the configuration for command-line arguments parsing and matching rules.
/// </summary>
public class ArgumentsConfig: IDump
{
	public static readonly ArgumentsConfig Default = new();
	public static readonly ArgumentsConfig CaseInsensitive = new() { IgnoreCase = true };
	public static readonly ArgumentsConfig Windows = new() { IgnoreCase = true, AllowSlash = true };
	public static readonly ArgumentsConfig Posix = new() { Strict = true, MatchingType = ParameterMatching.Strict, ColonSeparator = false };

	/// <summary>
	/// Gets or initializes a value indicating whether command and option names are matched case-insensitively.
	/// </summary>
	public bool IgnoreCase { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether slashes are allowed as option prefixes.
	/// </summary>
	public bool AllowSlash { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether POSIX+GNU-style strict parsing rules are applied,
	/// which require long options to use double-dash prefix, allow combined short options, and do not allow option name variations.
	/// </summary>
	public bool Strict { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether unknown options are allowed.
	/// </summary>
	public bool AllowUnknown { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether colon separators are allowed. (default: true)
	/// </summary>
	public bool ColonSeparator { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether equal separators are allowed. (default: true)
	/// </summary>
	public bool EqualSeparator { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether blank separators are allowed. (default: true)
	/// </summary>
	public bool BlankSeparator { get; init; }
	/// <summary>
	/// Gets or initializes the naming style for parameters. (default: <see cref="ParameterNaming.KebabCase"/>)
	/// </summary>
	public ParameterNaming NamingStyle { get; init; }
	/// <summary>
	/// Gets or initializes the matching type for parameters. (default: <see cref="ParameterMatching.Fluent"/>)
	/// </summary>
	public ParameterMatching MatchingType { get; init; }

	public ArgumentsConfig()
	{
		ColonSeparator = true;
		EqualSeparator = true;
		BlankSeparator = true;
	}

	public ArgumentsConfig(ArgumentsConfig? other)
	{
		if (other == null) return;
		IgnoreCase = other.IgnoreCase;
		AllowSlash = other.AllowSlash;
		Strict = other.Strict;
		AllowUnknown = other.AllowUnknown;
		ColonSeparator = other.ColonSeparator;
		EqualSeparator = other.EqualSeparator;
		BlankSeparator = other.BlankSeparator;
		NamingStyle = other.NamingStyle;
		MatchingType = other.MatchingType;
	}

	public bool IsOption(ReadOnlySpan<char> c) => c.Length > 1 && (c[0] == '-' || (AllowSlash && c[0] == '/'));

	public static ArgumentsConfig FromAttribute(CliParametersAttribute? attr) => attr == null ? Default : new ArgumentsConfig(attr);

	public ArgumentsConfig(CliParametersAttribute attr)
	{
		IgnoreCase = attr.IgnoreCase;
		AllowSlash = attr.AllowSlash;
		Strict = attr.Strict;
		AllowUnknown = attr.AllowUnknown;
		ColonSeparator = attr.ColonSeparator;
		EqualSeparator = attr.EqualSeparator;
		BlankSeparator = attr.BlankSeparator;
		NamingStyle = attr.NamingStyle;
		MatchingType = attr.MatchingType;
	}

	public void DumpContent(IDumpWriter writer)
	{
		if (IgnoreCase)
			writer.Field(IgnoreCase);
		if (AllowSlash)
			writer.Field(AllowSlash);
		if (Strict)
			writer.Field(Strict);
		if (AllowUnknown)
			writer.Field(AllowUnknown);
		if (ColonSeparator)
			writer.Field(ColonSeparator);
		if (EqualSeparator)
			writer.Field(EqualSeparator);
		if (BlankSeparator)
			writer.Field(BlankSeparator);
		writer.Field(NamingStyle);
		writer.Field(MatchingType);
	}
}