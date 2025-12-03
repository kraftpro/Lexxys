namespace Lexxys;

/// <summary>
/// Marks a property or field as a command-line option or positional argument.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CliOptionAttribute: Attribute
{
	/// <summary>
	/// Gets or initializes alternative option names.
	/// </summary>
	public string[]? Alias { get; init; }
	/// <summary>
	/// Gets or initializes the option name. When omitted, the member name is used.
	/// </summary>
	public string? Name { get; init; }
	/// <summary>
	/// Gets or initializes the placeholder name used for the option value in usage output.
	/// </summary>
	public string? ValueName { get; init; }
	/// <summary>
	/// Gets or initializes text shown for the option in usage output.
	/// </summary>
	public string? Description { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether the option or positional argument is required.
	/// </summary>
	public bool Required { get; init; }
	/// <summary>
	/// Gets or initializes a value indicating whether the member is a positional argument instead of a named option.
	/// </summary>
	public bool Positional { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CliOptionAttribute"/> class.
	/// </summary>
	public CliOptionAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="CliOptionAttribute"/> class with option aliases.
	/// </summary>
	/// <param name="alias">Alternative option names.</param>
	public CliOptionAttribute(params string[] alias) => Alias = alias;
}
