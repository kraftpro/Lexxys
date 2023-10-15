using System.Collections;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

/// <summary>
/// Implements a collection of <see cref="ParameterDefinition"/> instances.
/// </summary>
public class ParameterDefinitionCollection: IReadOnlyList<ParameterDefinition>
{
	private readonly List<ParameterDefinition> _parameters;
	private readonly StringComparison _comparison;

	/// <summary>
	/// Constructs a new instance of <see cref="ParameterDefinitionCollection"/> with the specified <see cref="StringComparison"/> as a parameter names comparison rule.
	/// </summary>
	/// <param name="comparison">Comparison rule to compare parameter names.</param>
	public ParameterDefinitionCollection(StringComparison? comparison = null)
	{
		_parameters = new List<ParameterDefinition>();
		_comparison = comparison ?? StringComparison.Ordinal;
	}

	/// <summary>
	/// Constructs a new instance of <see cref="ParameterDefinitionCollection"/> with the specified <see cref="ParameterDefinition"/> instances.
	/// </summary>
	/// <param name="parameters">Collection of the <see cref="ParameterDefinition"/>s</param>
	/// <param name="comparison">Comparison rule to compare parameter names.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterDefinitionCollection(IEnumerable<ParameterDefinition> parameters, StringComparison? comparison = null)
	{
		if (parameters == null) throw new ArgumentNullException(nameof(parameters));
		_parameters = new List<ParameterDefinition>(parameters.Where(o => o is not null));
		_comparison = comparison ?? StringComparison.Ordinal;
	}

	///// <summary>
	///// Selects the parameters missing in the command line arguments.
	///// </summary>
	///// <param name="required">Indicates that only required parameters should be returned.</param>
	///// <returns></returns>
	//public IEnumerable<ParameterDefinition> GetMissingParameters(bool required = false) => _parameters.Where(p => !p.HasValue && (!required || p.IsRequired));

	///// <summary>
	///// Returns the next positional parameter that is not set.
	///// </summary>
	///// <returns></returns>
	//internal ParameterDefinition? GetNextPositionalParameter() => _parameters.FirstOrDefault(o => o is { IsPositional: true, HasValue: false });

	/// <summary>
	/// Returns the last positional parameter.
	/// </summary>
	/// <returns></returns>
	internal ParameterDefinition? GetLastPositionalParameter() => _parameters.LastOrDefault(o => o is { IsPositional: true });

	///// <summary>
	///// Returns the positional parameters that are set.
	///// </summary>
	///// <returns></returns>
	//public IReadOnlyList<ParameterValue> GetPositionalArguments() => _parameters.Where(o => o is { IsPositional: true, HasValue: true }).Select(o => o.Value).ToIReadOnlyList();

	/// <inheritdoc />
	public ParameterDefinition this[int index] => _parameters[index];

	/// <summary>
	/// Gets the parameter with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name"></param>
	public ParameterDefinition? this[string name] => _parameters.FirstOrDefault(o => String.Equals(o.Name, name, _comparison));
	
	/// <inheritdoc />
	public int Count => _parameters.Count;

	public bool HasNamedParameters => _parameters.Any(o => !o.IsPositional);

	internal StringComparison Comparison => _comparison;

	/// <summary>
	/// Adds the given <paramref name="parameter"/> to the end of this collection.
	/// </summary>
	/// <param name="parameter">The parameter to be added to the collection</param>
	public void Add(ParameterDefinition parameter)
	{
		if (parameter is null) throw new ArgumentNullException(nameof(parameter));

		if (_parameters.Contains(parameter)) return;

		var pd = _parameters.FindExact(parameter.Name, _comparison);
		if (pd != null) throw new ArgumentException($"Parameter with name '{parameter.Name}' already exists.", nameof(parameter));

		foreach (var abbr in parameter.Abbreviations)
		{
			if (_parameters.FindExact(abbr, _comparison) != null)
				throw new ArgumentException($"Parameter with abbreviation '{abbr}' already exists.", nameof(parameter));
		}
		_parameters.Add(parameter);
	}

	/// <summary>
	/// Combines this collection with the given <paramref name="parameters"/> and returns a new collection.
	/// </summary>
	/// <param name="parameters">Collection of parameters to be added to the current collection.</param>
	/// <returns></returns>
	public ParameterDefinitionCollection Combine000(ParameterDefinitionCollection parameters)
	{
		if (parameters is null) throw new ArgumentNullException(nameof(parameters));

		List<ParameterDefinition> collection = new List<ParameterDefinition>(_parameters);
		collection.AddRange(parameters._parameters);
		return new ParameterDefinitionCollection(collection, _comparison);
	}

	/// <inheritdoc/>
	public IEnumerator<ParameterDefinition> GetEnumerator() => ((IEnumerable<ParameterDefinition>)_parameters).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_parameters).GetEnumerator();
}
