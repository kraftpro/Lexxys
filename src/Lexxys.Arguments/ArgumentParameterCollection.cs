using Lexxys;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

	/// <summary>
	/// Returns the collection of parameter definitions.
	/// </summary>
	public ParameterDefinitionCollection Definition { get; }

	/// <summary>
	/// Returns the number of parameters in the collection.
	/// </summary>
	public int Count => _parameters.Count;

	/// <summary>
	/// Returns the parameter with the specified <paramref name="definition"/>.
	/// </summary>
	/// <param name="definition">Parameter definition.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	internal ArgumentParameter? this[ParameterDefinition definition]
	{
		get => _parameters.GetValueOrDefault(definition);
		set => _parameters[definition] = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Returns the collection of positional parameters.
	/// </summary>
	public IEnumerable<ParameterValue> Positional => _parameters.Values.Where(o => o.Definition.IsPositional).Select(o => o.Value);

	/// <summary>
	/// Adds a parameter to the collection.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="value">Value of the parameter.</param>
	/// <param name="unknown">Allow unknown (not defined) parameters.</param>
	/// <returns>Found, ambiguous or not found status <see cref="ParameterDefinitionFindResult"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
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

	/// <summary>
	/// Gets the parameter with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="value">A parameter with the specified <paramref name="name"/>.</param>
	/// <param name="extended">Enables extended search for similar parameters.</param>
	/// <param name="ignoreDelimiters"></param>
	/// <returns>true if parameter is found.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool TryGet(string name, [MaybeNullWhen(false)] out ArgumentParameter value, bool extended = false, bool ignoreDelimiters = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		_parameters.Keys.TryFind(name, out var pd, Definition.Comparison, extended: extended, ignoreDelimiters: ignoreDelimiters);
		if (pd is null)
		{
			value = null;
			return false;
		}

		value = _parameters[pd];
		return true;
	}

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <returns>Value of the parameter or <c>null</c> if parameter is not found.</returns>
	public string? Value(string name) => TryGet(name, out var value) ? value.Value.StringValue: null;

	/// <summary>
	/// Returns the collection of values of the parameter with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the parameter.</param>
	/// <returns>The array of the parameter values or empty array if parameter is not found.</returns>
	public string[]? Collection(string name) => TryGetCollection<string>(name, false, out var value, null) ? value: default;

	/// <summary>
	/// Checks if flag with the specified <paramref name="name"/> is set.
	/// </summary>
	/// <param name="name">Name of the flag.</param>
	/// <returns>true if flag is set.</returns>
	public bool Switch(string name) => Strings.GetBoolean(Value(name), false);

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> converted to the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="defaultValue">Default value to return if parameter is not found.</param>
	/// <param name="errors">The collection of errors found.</param>
	/// <returns>The value of the parameter or <paramref name="defaultValue"/> if parameter is not found.</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="name"/> is <c>null</c>.</exception>
	public T Value<T>(string name, T defaultValue, ICollection<string>? errors = null) => TryConvert<T>(name, Value(name), out var x, errors) ? x: defaultValue;

	/// <summary>
	/// Returns the collection of values of the parameter with the specified <paramref name="name"/> converted to the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the array elements.</typeparam>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="defaultItem">Default value of the array item if the item cannot be converted to the specified type <typeparamref name="T"/>.</param>
	/// <returns>An array of the parameter values or <c>null</c> if error occurs.</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="name"/> is <c>null</c>.</exception>
	public T[]? Collection<T>(string name, ICollection<string>? errors = null) => TryGetCollection<T>(name, false, out var value, errors) ? value: null;

	/// <summary>
	/// Returns the value of the parameter with the specified <paramref name="name"/> converted to the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="required">Indicates that parameter is required.</param>
	/// <param name="result">The value of the parameter.</param>
	/// <param name="errors">The collection of errors found.</param>
	/// <returns>true if the parameter is found and converted to the specified type <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="name"/> is <c>null</c>.</exception>
	public bool TryGetValue<T>(string name, bool required, [MaybeNullWhen(false)] out T result, ICollection<string>? errors = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		if (!TryGet(name, out var argument))
		{
			if (required)
				errors?.Add($"parameter {name} is required");
			result = default;
			return false;
		}
		return TryConvert<T>(name, argument.Value.StringValue!, out result, errors);
	}

	/// <summary>
	/// Returns the collection of values of the parameter with the specified <paramref name="name"/> converted to the specified type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the array elements.</typeparam>
	/// <param name="name">Name of the parameter.</param>
	/// <param name="required">Indicates that parameter is required.</param>
	/// <param name="result">The array of the parameter values.</param>
	/// <param name="errors">The collection of errors found.</param>
	/// <returns>true if the parameter is found and converted to the specified type <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="name"/> is <c>null</c>.</exception>
	public bool TryGetCollection<T>(string name, bool required, out T[] result, ICollection<string>? errors = null)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		if (!TryGet(name, out var argument))
		{
			if (required)
				errors?.Add($"parameter {name} is required");
			result = [];
			return false;
		}

		var x = argument.Value.ArrayValue!;

		if (typeof(T) == typeof(string))
		{
			result = Unsafe.As<string[], T[]>(ref x);
			return true;
		}

		T[] values = new T[x.Length];
		int i = 0;
		int j = 0;
		bool error = false;
		while (i < x.Length)
		{
			var v = x[i++];
			if (string.IsNullOrWhiteSpace(v))
				continue;
			if (TryConvert<T>($"{name}.{i}", v, out var value, errors))
				values[j++] = value;
			else
				error = true;
		}
		if (error)
		{
			result = [];
			return false;
		}

		if (j < values.Length)
			Array.Resize(ref values, j);
		result = values;
		return true;
	}

	private static bool TryConvert<T>(string name, string? value, [MaybeNullWhen(false)]out T result, ICollection<string>? errors)
	{
		if (value is null)
		{
			result = default;
			errors?.Add($"missing value for parameter {name}");
			return false;
		}

		if (typeof(T) == typeof(string))
		{
			result = Unsafe.As<string, T>(ref value);
			return true;
		}
		if (typeof(T) == typeof(FileInfo))
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				result = default;
				errors?.Add($"missing value for parameter {name}");
				return false;
			}
			var x = new FileInfo(value);
			result = Unsafe.As<FileInfo, T>(ref x);
			return true;
		}
		if (typeof(T) == typeof(DirectoryInfo))
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				result = default;
				errors?.Add($"missing value for parameter {name}");
				return false;
			}
			var x = new DirectoryInfo(value);
			result = Unsafe.As<DirectoryInfo, T>(ref x);
			return true;
		}
		if (typeof(T) == typeof(Uri))
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				result = default;
				errors?.Add($"missing value for parameter {name}");
				return false;
			}
			var x = new Uri(value, UriKind.RelativeOrAbsolute);
			result = Unsafe.As<Uri, T>(ref x);
			return true;
		}
		if (Strings.TryGetValue<T>(value, out result))
		{
			return true;
		}

		string message = $"invalid value for parameter {name}: {value}";
		if (typeof(T).IsEnum)
		{
			var names = Enum.GetNames(typeof(T));
			for (int i = 0; i < names.Length; ++i)
			{
				names[i] = names[i].ToLowerInvariant();
			}
			message += $". The valid values are: {String.Join(", ", names)}";
		}
		errors?.Add(message);
		return false;
	}

	internal void Add(ParameterDefinition definition, ParameterValue value)
	{
		if (definition is null) throw new ArgumentNullException(nameof(definition));

		_parameters.Add(definition, new ArgumentParameter(definition, value));
	}

	public IEnumerator<ArgumentParameter> GetEnumerator() => _parameters.Values.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}