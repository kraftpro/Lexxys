using System.Collections;

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
	internal ParameterDefinitionCollection(CommandDefinition command)
	{
		Command = command ?? throw new ArgumentNullException(nameof(command));
		_parameters = [];
		_comparison = command.Comparison;
	}

	/// <summary>
	/// Constructs a new instance of <see cref="ParameterDefinitionCollection"/> with the specified <see cref="ParameterDefinition"/> instances.
	/// </summary>
	/// <param name="parameters">Collection of the <see cref="ParameterDefinition"/>s</param>
	/// <param name="comparison">Comparison rule to compare parameter names.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ParameterDefinitionCollection(CommandDefinition command, IEnumerable<ParameterDefinition> parameters)
	{
		Command = command ?? throw new ArgumentNullException(nameof(command));
		if (parameters == null) throw new ArgumentNullException(nameof(parameters));
		_parameters = [.. parameters.Where(o => o is not null)];
		_comparison = command.Comparison;
	}

	///// <summary>
	///// Returns the positional parameters that are set.
	///// </summary>
	///// <returns></returns>
	//public IReadOnlyList<ParameterValue> GetPositionalArguments() => _parameters.Where(o => o is { IsPositional: true, HasValue: true }).Select(o => o.Value).ToIReadOnlyList();

	/// <inheritdoc />
	public ParameterDefinition this[int index] => _parameters[index];

	/// <summary>
	/// Gets the parameter with the specified <paramref name="name"/> or <c>null</c> if the parameter is not found.
	/// </summary>
	/// <param name="name"></param>
	public ParameterDefinition? this[string name] => _parameters.FirstOrDefault(o => String.Equals(o.Name, name, _comparison));

	/// <inheritdoc />
	public int Count => _parameters.Count;

	public CommandDefinition Command { get; }

	internal StringComparison Comparison => _comparison;

	/// <summary>
	/// Adds the given <paramref name="parameter"/> to the end of this collection.
	/// </summary>
	/// <param name="parameter">The parameter to be added to the collection</param>
	internal void Add(ParameterDefinition parameter)
	{
		if (parameter is null) throw new ArgumentNullException(nameof(parameter));

		if (_parameters.Contains(parameter)) return;

		var found = _parameters.Any(o => Contains(o, parameter.Name, _comparison));
		if (found) throw new ArgumentException($"Parameter with name '{parameter.Name}' already exists.", nameof(parameter));

		found = parameter.Abbreviation.Any(a => _parameters.Any(o => Contains(o, a, _comparison)));
		if (found) throw new ArgumentException($"Parameter with abbreviation '{parameter.Abbreviation}' already exists.", nameof(parameter));

		_parameters.Add(parameter);

		static bool Contains(ParameterDefinition p, string name, StringComparison comparison) =>
			String.Equals(name, p.Name, comparison) ||
			p.Abbreviation.Any(a => String.Equals(name, a, comparison));
	}

	public IEnumerator<ParameterDefinition> GetEnumerator() => ((IEnumerable<ParameterDefinition>)_parameters).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_parameters).GetEnumerator();
}
