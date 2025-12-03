using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Lexxys;

/// <summary>
/// Implements a collection of <see cref="CommandDefinition"/> instances.
/// </summary>
public class CommandDefinitionCollection: IReadOnlyCollection<CommandDefinition>
{
	private readonly List<CommandDefinition> _commands;
	private readonly StringComparison _comparison;

	/// <summary>
	/// Constructs a new <see cref="CommandDefinitionCollection"/> instance.
	/// </summary>
	/// <param name="parent">The parent command where this collection is defined.</param>
	/// <param name="comparison"><see cref="StringComparison"/> rule to compare command names.</param>
	/// <exception cref="ArgumentNullException"><paramref name="parent"/> is <c>null</c>.</exception>
	internal CommandDefinitionCollection(CommandDefinition parent, StringComparison comparison)
	{
		Command = parent ?? throw new ArgumentNullException(nameof(parent));
		_comparison = comparison;
		_commands = [];
	}

	/// <summary>
	/// The parent command where this collection is defined.
	/// </summary>
	public CommandDefinition Command { get; }

	/// <summary>
	/// Gets the number of commands in the collection.
	/// </summary>
	public int Count => _commands.Count;
	
	/// <summary>
	/// Adds a new command to the collection.
	/// </summary>
	/// <param name="command">The command to add.</param>
	/// <returns>The current collection.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="command"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentException">A command with the same name or alias already exists.</exception>
	internal CommandDefinitionCollection Add(CommandDefinition command)
	{
		if (command is null) throw new ArgumentNullException(nameof(command));
		var name = command.Name;
		if (_commands.Exists(o => String.Equals(o.Name, name, _comparison) || o.Alias.Any(a => String.Equals(a, name, _comparison))))
			throw new ArgumentException($"Command '{command.Name}' already exists", nameof(command));
		foreach (var abbr in command.Alias)
		{
			if (_commands.Exists(o => String.Equals(o.Name, abbr, _comparison) || o.Alias.Any(a => String.Equals(a, abbr, _comparison))))
				throw new ArgumentException($"Command abbreviation '{abbr}' already exists", nameof(command));
		}
		_commands.Add(command);
		return this;
	}

	/// <summary>
	/// Returns the command with the specified <paramref name="name"/> or <c>null</c> if not found.
	/// </summary>
	/// <param name="name">Name to find the command.</param>
	/// <param name="result">The command found or <c>null</c>.</param>
	/// <returns><c>true</c> when a command with the specified name or alias is found; otherwise, <c>false</c>.</returns>
	public bool TryGetCommand(string? name, [MaybeNullWhen(false)] out CommandDefinition result)
	{
		result = _commands.Find(o => String.Equals(o.Name, name, _comparison) || o.Alias.Any(a => String.Equals(a, name, _comparison)));
		return result != null;
	}

	/// <inheritdoc/>
	public IEnumerator<CommandDefinition> GetEnumerator() => _commands.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();
}
