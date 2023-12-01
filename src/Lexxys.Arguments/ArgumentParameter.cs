namespace Lexxys;

/// <summary>
/// Represents a command line parameter with its value.
/// </summary>
public class ArgumentParameter
{
	/// <summary>
	/// Creates a new instance of <see cref="ArgumentParameter"/> class with empty value.
	/// </summary>
	/// <param name="definition">Parameter definition.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentParameter(ParameterDefinition definition)
	{
		Definition = definition ?? throw new ArgumentNullException(nameof(definition));
		Value = new ParameterValue();
	}

	/// <summary>
	/// Creates a new instance of <see cref="ArgumentParameter"/> class with specified <paramref name="value"/>
	/// </summary>
	/// <param name="definition">Parameter definition.</param>
	/// <param name="value">Parameter value.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ArgumentParameter(ParameterDefinition definition, ParameterValue value)
	{
		Definition = definition ?? throw new ArgumentNullException(nameof(definition));
		Value = value;
	}

	/// <summary>
	/// Gets the parameter definition.
	/// </summary>
	public ParameterDefinition Definition { get; }

	/// <summary>
	/// Gets or sets the parameter value.
	/// </summary>
	public ParameterValue Value { get; set; }

	/// <summary>
	/// Gets the parameter name.
	/// </summary>
	public string Name => Definition.Name;
}