using System.Collections;
using System.Diagnostics.CodeAnalysis;

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
	/// <param name="parent">The parent command where this collection is defined.</param>
	/// <param name="comparison"><see cref="StringComparison"/> rule to compare command names.</param>
	public CommandDefinitionCollection(CommandDefinition parent, StringComparison comparison)
	{
		Command = parent ?? throw new ArgumentNullException(nameof(parent));
		_comparison = comparison;
		_commands = new List<CommandDefinition>();
	}

	/// <summary>
	/// Creates a copy of <see cref="CommandDefinitionCollection"/> with the specified <see cref="StringComparison"/> as a command names comparison rule.
	/// </summary>
	/// <param name="other">The <see cref="CommandDefinitionCollection"/> to copy from.</param>
	/// <param name="comparison"><see cref="StringComparison"/> rule to compare command names.</param>
	internal CommandDefinitionCollection(CommandDefinitionCollection other, StringComparison comparison)
	{
		if (other is null)
			throw new ArgumentNullException(nameof(other));

		Command = other.Command;
		_comparison = comparison;
		_commands = new List<CommandDefinition>(other._commands.Select(o => new CommandDefinition(o, comparison)));
	}

	/// <summary>
	/// The parent command where this collection is defined.
	/// </summary>
	public CommandDefinition Command { get; }

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
			_commands.Add(command = new CommandDefinition(Command, name, description: description));
		return command;
	}

	/// <summary>
	/// Gets or creates a command with the specified <paramref name="name"/> and <paramref name="description"/>.
	/// </summary>
	/// <param name="name">Command name.</param>
	/// <param name="description">Command description.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public CommandDefinition GetOrCreate(string name, string[]? abbreviation, string? description = null)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		var command = _commands.Find(o => String.Equals(o.Name, name, _comparison));
		if (command is null)
			_commands.Add(command = new CommandDefinition(Command, name, abbreviation, description));
		return command;
	}

	/// <summary>
	/// Returns the command with the specified <paramref name="name"/> or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">Name to find the command.</param>
	/// <param name="result">The command found or <c>null</c>.</param>
	/// <returns></returns>
	public bool TryGetCommand(string? name, [MaybeNullWhen(false)] out CommandDefinition result)
	{
		result = _commands.Find(o => String.Equals(o.Name, name, _comparison));
		return result != null;
	}

	/// <inheritdoc/>
	public IEnumerator<CommandDefinition> GetEnumerator() => _commands.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();
}
