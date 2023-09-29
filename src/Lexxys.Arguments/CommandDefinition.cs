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
	/// <param name="description">Description of the command</param>
	/// <param name="comparison">A <see cref="StringComparison"/></param>
	/// <exception cref="ArgumentNullException"></exception>
	internal CommandDefinition(CommandDefinition? parent, string name, string? description = null, StringComparison? comparison = null)
	{
		Parent = parent;
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Description = description;
		_comparison = comparison ?? StringComparison.Ordinal;
		_parameters = new ParameterDefinitionCollection(_comparison);
		parent?.Add(this);
	}

	/// <summary>
	/// Creates a copy of <see cref="CommandDefinition"/> with the specified <paramref name="comparison"/>.
	/// </summary>
	/// <param name="other">An instance of <see cref="CommandDefinition"/> to copy</param>
	/// <param name="comparison">A <see cref="StringComparison"/> to use for the new instance</param>
	/// <exception cref="ArgumentNullException"></exception>
	internal CommandDefinition(CommandDefinition other, StringComparison comparison)
	{
		if (other is null) throw new ArgumentNullException(nameof(other));
		Parent = other.Parent;
		Name = other.Name;
		Description = other.Description;
		_comparison = comparison;
		_parameters = new ParameterDefinitionCollection(other._parameters, comparison);
		_commands = other._commands is null ? null : new CommandDefinitionCollection(other._commands, comparison);
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

	/// <summary>
	/// Indicates that the command has subcommands.
	/// </summary>
	public bool HasCommands => _commands?.Count > 0;

	/// <summary>
	/// Indicates that the command has parameters.
	/// </summary>
	public bool HasParameters => _parameters.Count > 0;

	internal StringComparison Comparison => _comparison;

	/// <summary>
	/// Adds a parameter to the command.
	/// </summary>
	/// <param name="parameter">Parameter to add.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public void Add(ParameterDefinition parameter)
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
