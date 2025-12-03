namespace Lexxys;

public interface IArguments: IReadOnlyCollection<KeyValuePair<string, ParameterValue>>
{
	string Name { get; }

	IArguments? Command { get; }

	CommandDefinition Definition { get; }

	ParameterValue this[string name] { get; }

#if NET
	ParameterValue Positional => this[Arguments.PositionalKey];
#else
	ParameterValue Positional { get; }
#endif

	bool Contains(string name);
}