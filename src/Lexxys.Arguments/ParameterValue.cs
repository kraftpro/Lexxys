using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lexxys;

/// <summary>
/// Converts a command-line string value to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The target value type.</typeparam>
/// <param name="value">The command-line value to convert.</param>
/// <param name="result">The converted value when the conversion succeeds.</param>
/// <param name="error">An optional conversion error message. Set this when returning <c>false</c> to provide more detail.</param>
/// <returns><c>true</c> when the value was converted; otherwise, <c>false</c>.</returns>
public delegate bool ParameterValueConverter<T>(string value, out T result, ref string? error);

/// <summary>
/// Represents a command line parameter value supporting both string and array of string values.
/// </summary>
public readonly struct ParameterValue: IReadOnlyCollection<string>
{
	/// <summary>
	/// Represents an empty parameter value.
	/// </summary>
	public static readonly ParameterValue Empty = new ParameterValue();

	public static readonly ParameterValue EmptyArray = new ParameterValue([]);

	private readonly object? _value;

	/// <summary>
	/// Returns <c>true</c> when this value represents a switch (an empty array), as produced for valueless options.
	/// </summary>
	/// <remarks>
	/// This is a structural test and must be used instead of comparing with <see cref="EmptyArray"/> via <c>==</c>:
	/// <see cref="ParameterValue"/> has no equality operator, so <c>==</c> silently degrades to string-value comparison
	/// and would also match an explicit empty-string value.
	/// </remarks>
	public bool IsSwitch => _value is string[] { Length: 0 };

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> from the specified string.
	/// </summary>
	/// <param name="value">The parameter value, or <c>null</c> for an empty value.</param>
	public ParameterValue(string value)
	{
		_value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> from the specified array.
	/// </summary>
	/// <param name="value">The parameter values, or <c>null</c> for an empty value.</param>
	public ParameterValue(string[] value)
	{
		_value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> from the specified collection.
	/// </summary>
	/// <param name="value">The parameter values, or <c>null</c> for an empty value.</param>
	public ParameterValue(IReadOnlyCollection<string>? value)
	{
		_value = value switch
		{
			null => null,
			{ Count: 0 } => null,
			{ Count: 1 } => value is IReadOnlyList<string> irl ? irl[0]: value is IList<string> il ? il[0]: value.FirstOrDefault(),
			string[] array => array,
			_ => value.ToArray(),
		};
	}

	/// <summary>
	/// Returns <c>true</c> if the value is <c>null</c>.
	/// </summary>
	public bool IsEmpty => _value is null;

	/// <summary>
	/// Returns <c>true</c> if the value has been defined.
	/// </summary>
	public bool HasValue => _value is not null;

	/// <summary>
	/// Returns <c>true</c> if the value is a string.
	/// </summary>
	public bool IsString => _value is string;
	
	/// <summary>
	/// Returns <c>true</c> if the value is an array.
	/// </summary>
	public bool IsArray => _value is string[];

	/// <summary>
	/// Returns the parameter value as an array of strings.
	/// </summary>
	public string[]? ArrayValue => _value switch
	{
		null => null,
		string s => [s],
		_ => Unsafe.As<string[]>(_value),
	};

	/// <summary>
	/// Returns the parameter value as a string.
	/// </summary>
	public string? StringValue => _value switch
	{
		null => null,
		string s => s,
		_ => String.Join(",", Unsafe.As<string[]>(_value)),
	};

	/// <summary>
	/// Returns the number of values in the parameter.
	/// </summary>
	public int Count => _value switch
	{
		null => 0,
		string => 1,
		_ => Unsafe.As<string[]>(_value).Length,
	};

	/// <summary>
	/// Appends the specified value to the current value and returns a new <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value">The value to append.</param>
	/// <returns>A value containing the current items followed by <paramref name="value"/>.</returns>
	public ParameterValue Append(string? value)
	{
		if (value is null) return this;
		return _value switch
		{
			null => new ParameterValue(value),
			string s => new ParameterValue([s, value]),
			_ => new ParameterValue(AppendValue(Unsafe.As<string[]>(_value), value)),
		};

		static string[] AppendValue(string[] array, string value)
		{
			var tmp = new string[array.Length + 1];
			Array.Copy(array, tmp, array.Length);
			tmp[array.Length] = value;
			return tmp;
		}

	}

	/// <summary>
	/// Appends the specified collection of values to the current value and returns a new <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value">The values to append.</param>
	/// <returns>A value containing the current items followed by <paramref name="value"/>.</returns>
	public ParameterValue Append(IReadOnlyCollection<string>? value)
	{
		if (value is null || value.Count == 0) return this;
		if (value.Count == 1)
			return Append(value.First());
		return _value switch
		{
			null => new ParameterValue(value is string[] array ? array: value.ToArray()),
			string s => new ParameterValue(PrependValue(s, value)),
			_ => new ParameterValue(JoinCollections(Unsafe.As<string[]>(_value), value)),
		};

		static string[] PrependValue(string s, IReadOnlyCollection<string> value)
		{
			var tmp = new string[value.Count + 1];
			tmp[0] = s;
			return CopyItems(value, tmp, 1);
		}

		static string[] JoinCollections(string[] array, IReadOnlyCollection<string> value)
		{
			var tmp = new string[value.Count + array.Length];
			Array.Copy(array, 0, tmp, 0, array.Length);
			return CopyItems(value, tmp, array.Length);
		}

		static string[] CopyItems(IReadOnlyCollection<string> value, string[] array, int index)
		{
			if (value is ICollection<string> collection)
			{
				collection.CopyTo(array, index);
			}
			else
			{
				int i = index;
				foreach (var item in value)
				{
					array[i++] = item;
				}
			}
			return array;
		}

	}

	/// <summary>
	/// Appends the specified value to the current value and returns a new <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value">The value to append.</param>
	/// <returns>A value containing the current items followed by <paramref name="value"/>.</returns>
	public ParameterValue Append(ParameterValue value) => value._value is null ? this: _value switch
	{
		null => value,
		string s when value._value is string s2 => new ParameterValue([s, s2]),
		string s => new ParameterValue([s, .. Unsafe.As<string[]>(value._value)]),
		_ when value._value is string s2 => new ParameterValue([.. Unsafe.As<string[]>(_value), s2]),
		_ => new ParameterValue([.. Unsafe.As<string[]>(_value), .. Unsafe.As<string[]>(value._value)]),
	};

	/// <summary>
	/// Returns the string representation of the current value.
	/// </summary>
	/// <returns>The string value, a comma-separated list for arrays, or an empty string when no value is present.</returns>
	public override string ToString() => StringValue ?? String.Empty;

	/// <summary>
	/// Returns the array representation of the current value.
	/// </summary>
	/// <returns>An array of values, or an empty array when no value is present.</returns>
	public string[] ToArray() => ArrayValue ?? [];

	public Enumerator GetEnumerator() => new Enumerator(this);

	IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <summary>
	/// Converts a <see cref="ParameterValue"/> to its string representation.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The string value, a comma-separated list for arrays, or <c>null</c> when no value is present.</returns>
	public static implicit operator string?(ParameterValue value) => value.StringValue;

	/// <summary>
	/// Converts a <see cref="ParameterValue"/> to an array of strings.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The value as an array, or <c>null</c> when no value is present.</returns>
	public static implicit operator string[]?(ParameterValue value) => value.ArrayValue;

	/// <summary>
	/// Converts a string to a <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value">The string value.</param>
	/// <returns>A parameter value containing <paramref name="value"/>.</returns>
	public static implicit operator ParameterValue(string? value) => new ParameterValue(value!);

	/// <summary>
	/// Converts an array of strings to a <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value">The string values.</param>
	/// <returns>A parameter value containing <paramref name="value"/>.</returns>
	public static implicit operator ParameterValue(string[]? value) => new ParameterValue(value!);

	/// <summary>
	/// Converts this value to a single value of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The target value type.</typeparam>
	/// <param name="result">The converted value when conversion succeeds.</param>
	/// <param name="required">If <c>true</c>, an empty value is reported as an error.</param>
	/// <param name="errors">An optional collection that receives conversion errors.</param>
	/// <param name="name">The parameter name used in error messages.</param>
	/// <returns><c>true</c> when conversion succeeds; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// <list type="bullet">
	/// <item>When the value is an array, it is converted to a comma-separated string before conversion.</item>
	/// <item>Empty values are considered invalid when <paramref name="required"/> is <c>true</c>.</item>
	/// <item>When converting to <c>bool</c>, an empty array is treated as <c>true</c>.</item>
	/// <item>To convert each array item separately, use <see cref="TryConvert{T}(out T[], bool, ICollection{string}?, string?)"/> instead.</item>
	/// </list>
	/// </remarks>
	public bool TryConvert<T>([MaybeNullWhen(false)] out T result, bool required = false, ICollection<string>? errors = null, string? name = null)
	{
		if (IsEmpty)
		{
			if (required)
				errors?.Add(name is null ? "parameter is required" : $"parameter {name} is required");
			result = default;
			return false;
		}
		if (typeof(T) == typeof(bool) && IsSwitch)
		{
			result = (T)(object)true;
			return true;
		}

		var value = StringValue!;
		return TryConvertValue(value, out result, errors, name);
	}

	/// <summary>
	/// Converts this value to an array of values of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The target array element type.</typeparam>
	/// <param name="result">The converted values when conversion succeeds.</param>
	/// <param name="name">The parameter name used in error messages.</param>
	/// <param name="required">If <c>true</c>, an empty value is reported as an error.</param>
	/// <param name="errors">An optional collection that receives conversion errors.</param>
	/// <returns><c>true</c> when all values convert successfully; otherwise, <c>false</c>.</returns>
	public bool TryConvert<T>(out T[] result, bool required = false, ICollection<string>? errors = null, string? name = null)
	{
		if (IsEmpty)
		{
			if (required)
				errors?.Add(name is null ? "parameter is required" : $"parameter {name} is required");
			result = [];
			return false;
		}

		var value = ArrayValue!;

		if (typeof(T) == typeof(string))
		{
			result = (T[])(object)value;
			return true;
		}

		T[] rr = new T[value.Length];
		int i = 0;
		int j = 0;
		bool error = false;
		while (i < value.Length)
		{
			var v = value[i++];
			if (string.IsNullOrWhiteSpace(v))
				continue;
			if (TryConvertValue<T>(v, out var r, errors, name is null ? null : $"{name}.{i}"))
				rr[j++] = r;
			else
				error = true;
		}
		if (error)
		{
			result = [];
			return false;
		}

		if (j < rr.Length)
			Array.Resize(ref rr, j);
		result = rr;
		return true;
	}

	private static bool TryConvertValue<T>(string value, [MaybeNullWhen(false)] out T result, ICollection<string>? errors, string? name)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = default;
			errors?.Add(name is null ? "missing value" : $"missing value for parameter {name}");
			return false;
		}
		if (__converters.TryGetValue(typeof(T), out var obj))
		{
			var converter = (ParameterValueConverter<T>)obj;
			string? error = null;
			try
			{
				if (converter(value, out result, ref error))
					return true;
			}
			catch (Exception ex)
			{
				result = default;
				error = ex.Message;
			}
			if (error == null)
				errors?.Add(name is null ? $"invalid value \"{value}\"": $"invalid value \"{value}\" for parameter {name}");
			else
				errors?.Add(name is null ? $"invalid value \"{value}\". {error}": $"invalid value \"{value}\" for parameter {name}. {error}");
			return false;
		}

		if (Strings.TryGetValue(value, out result))
			return true;

		if (errors == null)
			return false;

		string message = name is null ? $"invalid value \"{value}\"" : $"invalid value \"{value}\" for parameter {name}";
		if (typeof(T).IsEnum)
		{
			var names = Enum.GetNames(typeof(T));
			for (int i = 0; i < names.Length; ++i)
			{
				names[i] = names[i].ToLowerInvariant();
			}
			message += $". The valid values are: {String.Join(", ", names)}";
		}
		errors.Add(message);
		return false;
	}

	/// <summary>
	/// A non-generic converter that converts a string <paramref name="value"/> to the specified <paramref name="type"/>.
	/// </summary>
	/// <param name="value">The command-line value to convert.</param>
	/// <param name="type">The target type.</param>
	/// <param name="result">The converted value when the conversion succeeds.</param>
	/// <returns><c>true</c> when the value was converted; otherwise, <c>false</c>.</returns>
	public delegate bool TryConvertDelegate(string value, Type type, out object? result);

	/// <summary>
	/// Returns a non-generic converter for the specified <paramref name="type"/>, or <c>null</c> when no converter is registered.
	/// </summary>
	/// <param name="type">The target type.</param>
	/// <remarks>
	/// The converter is derived from the same registry used by the generic <see cref="TryConvert{T}(out T, bool, ICollection{string}?, string?)"/>,
	/// so converters added with <see cref="AddConverter{T}"/> are visible here as well.
	/// </remarks>
	public static TryConvertDelegate? GetConverter(Type type)
		=> __converters.ContainsKey(type) ? __delegateCache.GetOrAdd(type, BuildConverterDelegate): null;

	private static readonly ConcurrentDictionary<Type, TryConvertDelegate> __delegateCache = new ConcurrentDictionary<Type, TryConvertDelegate>();

	private static TryConvertDelegate BuildConverterDelegate(Type type)
	{
		var method = typeof(ParameterValue)
			.GetMethod(nameof(InvokeConverter), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(type);
		return (TryConvertDelegate)method.CreateDelegate(typeof(TryConvertDelegate));
	}

	private static bool InvokeConverter<T>(string value, Type type, out object? result)
	{
		if (TryConvertValue<T>(value, out var r, null, null))
		{
			result = r;
			return true;
		}
		result = null;
		return false;
	}

	/// <summary>
	/// Registers or replaces a converter for values of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The target value type handled by the converter.</typeparam>
	/// <param name="converter">The converter to use for subsequent conversions.</param>
	/// <exception cref="ArgumentNullException"><paramref name="converter"/> is <c>null</c>.</exception>
	public static void AddConverter<T>(ParameterValueConverter<T> converter) => __converters[typeof(T)] = converter;

	private static readonly ConcurrentDictionary<Type, object> __converters = new ConcurrentDictionary<Type, object>()
	{
		[typeof(Uri)] = (ParameterValueConverter<Uri>)UriConverter,
		[typeof(FileInfo)] = (ParameterValueConverter<FileInfo>)FileInfoConverter,
		[typeof(DirectoryInfo)] = (ParameterValueConverter<DirectoryInfo>)DirectoryInfoConverter,
		[typeof(bool)] = (ParameterValueConverter<bool>)BooleanConverter,

		[typeof(int)] = (ParameterValueConverter<int>)((value, out result, ref error) => int.TryParse(value, out result)),
		[typeof(long)] = (ParameterValueConverter<long>)((value, out result, ref error) => long.TryParse(value, out result)),
		[typeof(double)] = (ParameterValueConverter<double>)((value, out result, ref error) => double.TryParse(value, out result)),
	};

	private static bool BooleanConverter(string value, out bool result, ref string? error)
	{
		var v = value.ToUpperInvariant();
		if (v is "1" or "Y" or "YES" or "TRUE" or "ON")
		{
			result = true;
			return true;
		}
		else if (v is "0" or "N" or "NO" or "FALSE" or "OFF")
		{
			result = false;
			return true;
		}
		error = $"invalid boolean value \"{value}\". The valid values are: 1, 0, y, n, yes, no, true, false, on, off.";
		result = false;
		return false;
	}

	private static bool FileInfoConverter(string value, out FileInfo result, ref string? error)
	{
		result = new FileInfo(value);
		return true;
	}

	private static bool DirectoryInfoConverter(string value, out DirectoryInfo result, ref string? error)
	{
		result = new DirectoryInfo(value);
		return true;
	}

	private static bool UriConverter(string value, out Uri result, ref string? error)
	{
		result = new Uri(value, UriKind.RelativeOrAbsolute);
		return true;
	}

	public struct Enumerator: IEnumerator<string>
	{
		private readonly string[]? _array;
		private string? _current;
		private int _index;

		public Enumerator(ParameterValue value)
		{
			switch (value._value)
			{
				case null:
					_array = null;
					_current = null;
					_index = 1;
					break;
				case string s:
					_array = null;
					_current = s;
					_index = 0;
					break;
				default:
					_array = Unsafe.As<string[]>(value._value);
					_current = null;
					_index = 0;
					break;
			}
		}

		public readonly string Current => _current ?? String.Empty;

		readonly object IEnumerator.Current => Current;

		public readonly void Dispose() { }

		public bool MoveNext()
		{
			if (_array is null)
			{
				if (_index != 0) return false;
				_index = 1;
			}
			else
			{
				if (_index >= _array.Length) return false;
				_current = _array[_index++];
			}
			return true;
		}

		public void Reset()
		{
			if (_array == null)
			{
				_index = _current == null ? 1: 0;
			}
			else
			{
				_current = null;
				_index = 0;
			}
		}
	}
}
