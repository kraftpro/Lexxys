namespace Lexxys;

public class ArgumentCommand
{
	public ArgumentCommand(CommandDefinition definition)
	{
		Parameters = new ArgumentParameterCollection(definition.Parameters);
		Definition = definition;
	}

	public CommandDefinition Definition { get; }

	public ArgumentParameterCollection Parameters { get; }

	public ArgumentCommand? Command { get; internal set; }

	public string Name => Definition.Name;
}