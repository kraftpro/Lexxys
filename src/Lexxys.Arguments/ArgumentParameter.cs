namespace Lexxys;

public class ArgumentParameter
{
	public ArgumentParameter(ParameterDefinition definition)
	{
		Definition = definition;
		Value = new ParameterValue();
	}

	public ArgumentParameter(ParameterDefinition definition, ParameterValue value)
	{
		Definition = definition;
		Value = value;
	}

	public ParameterDefinition Definition { get; }

	public ParameterValue Value { get; set; }

	public string Name => Definition.Name;
}