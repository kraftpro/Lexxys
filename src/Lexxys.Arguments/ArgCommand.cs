using System.Collections;

using static Lexxys.Arguments;

namespace Lexxys;

public class ArgumentCommand: IArguments
{
	internal readonly Arguments.ArgumentsDictionary Parameters;

	internal ArgumentCommand(string name, ArgumentsConfig config)
	{
		Name = name;
		Config = config;
		Parameters = new Arguments.ArgumentsDictionary(Config.IgnoreCase);
		Definition = new CommandDefinition(null, name, null, null, config);
	}

	internal ArgumentCommand(string name, ArgumentsConfig config, Arguments.ArgumentsDictionary parameters)
	{
		Name = name;
		Config = config;
		Parameters = parameters;
		Definition = new CommandDefinition(null, name, null, null, config);
	}

	internal ArgumentCommand(CommandDefinition root)
	{
		Definition = root;
		Name = root.Name;
		Config = root.Config;
		Parameters = new Arguments.ArgumentsDictionary(Config.IgnoreCase);
	}

	private ArgumentCommand(ArgumentCommand other, ArgumentCommand? outer)
	{
		Name = other.Name;
		Config = other.Config;
		Parameters = other.Parameters;
		Definition = other.Definition;
		Command = outer;
	}

	internal ArgumentCommand(CommandDefinition root, ArgumentCommand outer) : this(root) => Command = outer;

	//public ArgumentCommand(string name, ArgumentsConfig config, ArgumentCommand outer) : this(name, config) => Command = outer;

	public string Name { get; }

	public ArgumentCommand? Command { get; }

	IArguments? IArguments.Command => Command;

	public ArgumentsConfig Config { get; }

	public int Count => Parameters.Count;

	public ParameterValue this[string name] => Parameters[name];

	public ParameterValue Positional => this[Arguments.PositionalKey];

	public bool Contains(string name) => Parameters.ContainsKey(name);

	public Dictionary<string, ParameterValue>.Enumerator GetEnumerator() => Parameters.GetEnumerator();

	IEnumerator<KeyValuePair<string, ParameterValue>> IEnumerable<KeyValuePair<string, ParameterValue>>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public CommandDefinition Definition { get; }

	public bool IsAuto => Parameters is AutoArgumentsDictionary;

	internal ArgumentCommand WithOuterCommand(ArgumentCommand? outerCommand) => Command == outerCommand ? this : new ArgumentCommand(this, outerCommand);
}
