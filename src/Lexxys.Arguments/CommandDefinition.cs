namespace Lexxys;

/// <summary>
/// Represents a command line command.
/// </summary>
public class CommandDefinition
{
	private readonly ParameterDefinitionCollection _parameters;
	private readonly StringComparison _comparison;
	private CommandDefinitionCollection? _commands;

	/// <summary>
	/// Creates a new instance of <see cref="CommandDefinition"/>.
	/// </summary>
	/// <param name="parent">A parent command</param>
	/// <param name="name">Name of the command</param>
	/// <param name="alias">Alternative command names</param>
	/// <param name="description">Description of the command</param>
	/// <param name="comparison">A <see cref="StringComparison"/></param>
	/// <param name="namingStyle">The naming style used for members without explicit CLI names.</param>
	/// <exception cref="ArgumentNullException"></exception>
	internal CommandDefinition(CommandDefinition? parent, string name, string[]? alias = null, string? description = null, ArgumentsConfig? config = null)
	{
		Parent = parent;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Alias = alias ?? [];
		Description = description;
		Config = config ?? parent?.Config ?? ArgumentsConfig.Default;
		_comparison = Config.IgnoreCase ? StringComparison.OrdinalIgnoreCase: StringComparison.Ordinal;
		_parameters = new ParameterDefinitionCollection(this);
		parent?.Add(this);
	}

	/// <summary>
	/// Parent command or <c>null</c> if the command is a root command.
	/// </summary>
	public CommandDefinition? Parent { get; }

	/// <summary>
	/// Name of the command.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Alternative names for the command.
	/// </summary>
	public string[] Alias { get; }

	/// <summary>
	/// Description of the command.
	/// </summary>
	public string? Description { get; }

	/// <summary>
	/// Parameters related to the command.
	/// </summary>
	public ParameterDefinitionCollection Parameters => _parameters;

	/// <summary>
	/// Subcommands of the command.
	/// </summary>
	public CommandDefinitionCollection? Commands => _commands;

	public ArgumentsConfig Config { get; }

	internal StringComparison Comparison => _comparison;

	/// <summary>
	/// Adds a parameter to the command.
	/// </summary>
	/// <param name="parameter">Parameter to add.</param>
	/// <exception cref="ArgumentNullException"></exception>
	internal void Add(ParameterDefinition parameter)
	{
		if (parameter is null) throw new ArgumentNullException(nameof(parameter));
		_parameters.Add(parameter);
	}

	/// <summary>
	/// Adds a subcommand to the command.
	/// </summary>
	/// <param name="command">Command to add.</param>
	/// <exception cref="ArgumentNullException"></exception>
	private void Add(CommandDefinition command)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		_commands ??= new CommandDefinitionCollection(this, _comparison);
		_commands.Add(command);
	}
}
