// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Represents a value type of the <see cref="ParameterDefinition"/>.
/// </summary>
[Flags]
public enum ParameterValueType
{
	/// <summary>
	/// The parameter value type is not defined.
	/// </summary>
	None = 0,
	/// <summary>
	/// The parameter value is required.
	/// </summary>
	Required = 1,
	/// <summary>
	/// The parameter value is optional.
	/// </summary>
	Optional = 2,
	/// <summary>
	/// The parameter value is a collection.
	/// </summary>
	Collection = 4,
}
