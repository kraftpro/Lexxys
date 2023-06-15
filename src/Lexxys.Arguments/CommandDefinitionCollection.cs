using System.Collections;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Implements a collection of <see cref="CommandDefinition"/> instances.
/// </summary>
public class CommandDefinitionCollection: IEnumerable<CommandDefinition>
{
	private readonly List<CommandDefinition> _commands;
	private readonly StringComparison _comparison;

	/// <summary>
	/// Construct a new <see cref="CommandDefinitionCollection"/> instance with the specified <see cref="StringComparison"/> as a command names comparison rule.
	/// Adds a default, no-name command.
	/// </summary>
	/// <param name="comparison"><see cref="StringComparison"/> rule to compare command names.</param>
	public CommandDefinitionCollection(StringComparison comparison)
	{
		_comparison = comparison;
		_commands = new List<CommandDefinition> { new(String.Empty, null, comparison) };
	}

	/// <summary>
	/// Creates a copy of <see cref="CommandDefinitionCollection"/> with the specified <see cref="StringComparison"/> as a command names comparison rule.
	/// </summary>
	/// <param name="other">The <see cref="CommandDefinitionCollection"/> to copy from.</param>
	/// <param name="comparison"><see cref="StringComparison"/> rule to compare command names.</param>
	internal CommandDefinitionCollection(CommandDefinitionCollection other, StringComparison comparison)
	{
		_comparison = comparison;
		_commands = new List<CommandDefinition>(other._commands.Select(o => new CommandDefinition(o, comparison)));
	}

	// public CommandDefinition this[string name] => _commands[name];

	/// <summary>
	/// Number or commands in the collection.
	/// </summary>
	public int Count => _commands.Count;
	
	/// <summary>
	/// Adds a new command to the collection.
	/// </summary>
	/// <param name="command">The command to add</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentException"></exception>
	public CommandDefinitionCollection Add(CommandDefinition command)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		var name = command.Name;
		if (_commands.Exists(o => String.Equals(o.Name, name, _comparison)))
			throw new ArgumentException($"Command '{command.Name}' already exists", nameof(command));
		_commands.Add(command);
		return this;
	}
	
	/// <summary>
	/// Gets or creates a command with the specified <paramref name="name"/> and <paramref name="description"/>.
	/// </summary>
	/// <param name="name">Command name.</param>
	/// <param name="description">Command description.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public CommandDefinition GetOrCreate(string name, string? description = null)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		var command = _commands.Find(o => String.Equals(o.Name, name, _comparison));
		if (command is null)
			_commands.Add(command = new CommandDefinition(name, description));
		return command;
	}

	/// <summary>
	/// Returns the default, no-name command.
	/// </summary>
	public CommandDefinition Default => _commands[0];

	/// <summary>
	/// Returns the command with the specified <paramref name="name"/> or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">Name to find the command.</param>
	/// <returns></returns>
	public CommandDefinition? TryGetCommand(string name)
		=> _commands.Find(o => String.Equals(o.Name, name, _comparison));

	/// <inheritdoc/>
	public IEnumerator<CommandDefinition> GetEnumerator() => _commands.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();
}
