// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Represents a command line command.
/// </summary>
public class CommandDefinition
{
	private readonly ParameterDefinitionCollection _parameters;

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
	/// Creates a new instance of <see cref="CommandDefinition"/>.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="description"></param>
	/// <param name="comparison"></param>
	/// <exception cref="ArgumentNullException"></exception>
	internal CommandDefinition(string name, string? description = null, StringComparison? comparison = null)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Description = description;
		_parameters = new ParameterDefinitionCollection(comparison);
	}

	internal CommandDefinition(CommandDefinition other, StringComparison comparison)
	{
		if (other is null) throw new ArgumentNullException(nameof(other));
		Name = other.Name;
		Description = other.Description;
		_parameters = new ParameterDefinitionCollection(other._parameters, comparison);
	}

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
}
