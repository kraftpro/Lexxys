using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Lexxys;

/// <summary>
/// Represents a collection of command line parameters with their values.
/// </summary>
public class ArgumentParameterCollection: IReadOnlyCollection<ArgumentParameter>
{
	private readonly Dictionary<ParameterDefinition, ArgumentParameter> _parameters;

	internal ArgumentParameterCollection(ParameterDefinitionCollection definition)
	{
		Definition = definition ?? throw new ArgumentNullException(nameof(definition));
		_parameters = new Dictionary<ParameterDefinition, ArgumentParameter>(definition.Count);
	}

	public ParameterDefinitionCollection Definition { get; }

	public int Count => _parameters.Count;

	internal ArgumentParameter? this[ParameterDefinition definition]
	{
		get => _parameters.GetValueOrDefault(definition);
		set => _parameters[definition] = value ?? throw new ArgumentNullException(nameof(value));
	}

	public ParameterDefinitionFindResult TryAdd(string name, string value, bool unknown = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));
		if (value == null) throw new ArgumentNullException(nameof(value));

		Definition.TryFind(name, out var pd, Definition.Comparison);
		if (pd is not null)
		{
			if (!_parameters.TryGetValue(pd, out var p1))
				_parameters[pd] = new ArgumentParameter(pd, value);
			else if (pd.IsCollection)
				p1.Value = p1.Value.Append(value);
			else
				return ParameterDefinitionFindResult.Ambiguous;				// Parameter is already set
			return ParameterDefinitionFindResult.Found;						// Parameter was added
		}
		_parameters.Keys.TryFind(name, out pd, Definition.Comparison);
		if (pd is not null)
		{
			var p1 = _parameters[pd];
			if (!pd.IsCollection)
				return ParameterDefinitionFindResult.Ambiguous;				// Parameter is already set
			p1.Value = p1.Value.Append(value);
			return ParameterDefinitionFindResult.Found;						// Parameter was added
		}

		if (!unknown)
			return ParameterDefinitionFindResult.NotFound;					// Parameter not found

		pd = new ParameterDefinition(null!, name, collection: true);
		_parameters.Add(pd, new ArgumentParameter(pd, value));
		return ParameterDefinitionFindResult.Found;							// Parameter was added
	}

    public string? Value(string name) => TryGet(name, out var value) ? value!.Value.StringValue: null;

	public bool Switch(string name) => Strings.GetBoolean(Value(name), false);

	public T Value<T>(string name, T defaultValue) => Strings.GetValue(Value(name), defaultValue);

	public string[] Collection(string name) => TryGet(name, out var value) ? value?.Value.ArrayValue ?? []: [];

	public T[] Collection<T>(string name, T defaultItem) => Array.ConvertAll(Collection(name), o => Strings.GetValue(o, defaultItem));

    public IEnumerable<ParameterValue> Positional => _parameters.Values.Where(o => o.Definition.IsPositional).Select(o => o.Value);

    public bool TryGet(string name, [NotNullWhen(true)] out ArgumentParameter? value, bool auto = false, bool ignoreDelimiters = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		_parameters.Keys.TryFind(name, out var pd, Definition.Comparison, auto: auto, ignoreDelimiters: ignoreDelimiters);
		if (pd is null)
		{
			value = null;
			return false;
		}

		value = _parameters[pd];
		return true;
	}

	internal void Add(ParameterDefinition definition, ParameterValue value)
	{
		if (definition is null) throw new ArgumentNullException(nameof(definition));

		//if (!Info.Any(o => o == definition))
		//	throw new ArgumentOutOfRangeException(nameof(definition), definition, null);
		_parameters.Add(definition, new ArgumentParameter(definition, value));
	}

	public IEnumerator<ArgumentParameter> GetEnumerator() => _parameters.Values.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}