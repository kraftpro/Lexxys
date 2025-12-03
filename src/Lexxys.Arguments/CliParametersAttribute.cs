namespace Lexxys;

/// <summary>
/// Marks a type as a command-line option model for generated parser support.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CliParametersAttribute: Attribute
{
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
	public bool ColonSeparator { get; init; } = true;
	/// <summary>
	/// Gets or initializes a value indicating whether equal separators are allowed. (default: true)
	/// </summary>
	public bool EqualSeparator { get; init; } = true;
	/// <summary>
	/// Gets or initializes a value indicating whether blank separators are allowed. (default: true)
	/// </summary>
	public bool BlankSeparator { get; init; } = true;
	/// <summary>
	/// Gets or initializes the naming style for parameters. (default: <see cref="ParameterNaming.KebabCase"/>)
	/// </summary>
	public ParameterNaming NamingStyle { get; init; } = ParameterNaming.KebabCase;
	/// <summary>
	/// Gets or initializes the matching type for parameters. (default: <see cref="ParameterMatching.Fluent"/>)
	/// </summary>
	public ParameterMatching MatchingType { get; init; } = ParameterMatching.Fluent;
}
