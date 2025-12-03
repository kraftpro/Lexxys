namespace Lexxys;

/// <summary>
/// Marks a property or field as a subcommand in a command-line option model.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CliCommandAttribute: Attribute
{
	/// <summary>
	/// Gets or initializes the command name. When omitted, the member name is used.
	/// </summary>
	public string? Name { get; init; }
	/// <summary>
	/// Gets or initializes alternative command names.
	/// </summary>
	public string[]? Alias { get; init; }
	/// <summary>
	/// Gets or initializes text shown for the command in usage output.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CliCommandAttribute"/> class.
	/// </summary>
	public CliCommandAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="CliCommandAttribute"/> class with command aliases.
	/// </summary>
	/// <param name="alias">Alternative command names.</param>
	public CliCommandAttribute(params string[] alias) => Alias = alias;
}
