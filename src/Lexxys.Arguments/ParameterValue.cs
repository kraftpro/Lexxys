using Microsoft.Extensions.Primitives;

using System.Collections;
using System.Runtime.CompilerServices;

namespace Lexxys;

/// <summary>
/// Represents a command line parameter value supporting both string and array of string values.
/// </summary>
public readonly struct ParameterValue: IReadOnlyCollection<string>
{
	public static readonly ParameterValue Empty = new ParameterValue();

	private readonly object? _value;

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> with the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">String value</param>
	public ParameterValue(string? value)
	{
		_value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> with the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">Array value</param>
	public ParameterValue(string[]? value)
	{
		_value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ParameterValue"/> with the specified <paramref name="value"/>.
	/// </summary>
	/// <param name="value">Array value</param>
	public ParameterValue(IReadOnlyCollection<string>? value)
	{
		_value = value switch
		{
			null => null,
			{ Count: 0 } => null,
			{ Count: 1 } => value.FirstOrDefault(),
			string[] s => s,
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

	public int Count => _value switch
	{
		null => 0,
		string => 1,
		_ => Unsafe.As<string[]>(_value).Length,
	};

	/// <summary>
	/// Appends the specified <paramref name="value"/> to the current value.
	/// </summary>
	/// <param name="value">The value to append</param>
	public ParameterValue Append(string? value)
	{
		if (value is null) return this;
		return _value switch
		{
			null => new ParameterValue(value),
			string s => new ParameterValue(new[] { s, value }),
			_ => new ParameterValue(Unsafe.As<string[]>(_value).Append(value)),
		};
	}

	/// <summary>
	/// Appends the specified collection of values to the current value.
	/// </summary>
	/// <param name="value">A collection of values to append</param>
	public ParameterValue Append(IReadOnlyCollection<string>? value)
	{
		if (value is null || value.Count == 0) return this;
		if (value.Count == 1)
			return Append(value.First());
		return _value switch
		{
			null => new ParameterValue(value is string[] array ? array: value.ToArray()),
			string s => new ParameterValue(Append1(s, value)),
			_ => new ParameterValue(Append2(Unsafe.As<string[]>(_value), value)),
		};

		static string[] Append1(string s, IReadOnlyCollection<string> value)
		{
			var tmp = new string[value.Count + 1];
			tmp[0] = s;
			value.CopyTo(tmp, 1);
			return tmp;
		}

		static string[] Append2(string[] array, IReadOnlyCollection<string> value)
		{
			var tmp = new string[value.Count + array.Length];
			Array.Copy(array, 0, tmp, 0, array.Length);
			value.CopyTo(tmp, array.Length);
			return tmp;
		}
	}

	/// <summary>
	/// Returns the string representation of the current value.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => StringValue ?? String.Empty;

	public string[] ToArray() => ArrayValue ?? [];

	public IEnumerator<string> GetEnumerator() => new Enumerator(this);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <summary>
	/// Implicitly converts the specified <see cref="ParameterValue"/> to a <see cref="String"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator string?(ParameterValue value) => value.StringValue;

	/// <summary>
	/// Implicitly converts the specified <see cref="ParameterValue"/> to a <see cref="T:String[]"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator string[]?(ParameterValue value) => value.ArrayValue;

	/// <summary>
	/// Implicitly converts the specified <see cref="String"/> to a <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator ParameterValue(string? value) => new ParameterValue(value);

	/// <summary>
	/// Implicitly converts the specified <see cref="T:String[]"/> to a <see cref="ParameterValue"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static implicit operator ParameterValue(string[]? value) => new ParameterValue(value);

	private struct Enumerator: IEnumerator<string>
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
					_index = -1;
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
				if (_index < 0) return false;
				_index = -1;
				return true;
			}
			if (_index < _array.Length)
			{
				_current = _array[_index++];
				return true;
			}
			return false;
		}

		public void Reset() => throw new NotImplementedException();
	}
}
