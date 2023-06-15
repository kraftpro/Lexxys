using System.Diagnostics;

#pragma warning disable CA1720 // Identifier contains type name

namespace Lexxys.Validation;

public readonly struct ErrorDataType: IEquatable<ErrorDataType>
{
	public const int ErrorDataTypesCapacity = 256;

	private static readonly string?[] __items = new string[ErrorDataTypesCapacity];

	public static readonly ErrorDataType Object = Create(0, "Object");

	public static readonly ErrorDataType Numeric = Create(Object + 1, "Numeric");
	public static readonly ErrorDataType String = Create(Numeric + 1, "String");
	public static readonly ErrorDataType Date = Create(String + 1, "Date");
	public static readonly ErrorDataType Time = Create(Date + 1, "Time");
	public static readonly ErrorDataType Binary = Create(Time + 1, "Binary");

	public static readonly ErrorDataType Phone = Create(10, "Phone");
	public static readonly ErrorDataType Email = Create(Phone + 1, "Email");
	public static readonly ErrorDataType Url = Create(Email + 1, "Url");
	public static readonly ErrorDataType UsZip = Create(Url + 1, "UsZip");
	public static readonly ErrorDataType PostalCode = Create(UsZip + 1, "PostalCode");
	public static readonly ErrorDataType UsState = Create(PostalCode + 1, "UsState");
	public static readonly ErrorDataType Country = Create(UsState + 1, "Country");

	public static readonly ErrorDataType Ein = Create(Country + 1, "Ein");
	public static readonly ErrorDataType Ssn = Create(Ein + 1, "Ssn");

	//public static readonly ErrorDataType LastSpecial = (ErrorDataType)(99);

	public int TypeCode { get; }

	public ErrorDataType(int typeCode)
	{
		if (typeCode < 0 || typeCode >= __items.Length || __items[typeCode] == null)
			throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);

		TypeCode = typeCode;
	}

	public string Name => __items[TypeCode]!;

	/// <inheritdoc />
	public override bool Equals([NotNullWhen(true)] object? obj) => obj is ErrorDataType error && Equals(error);

	/// <inheritdoc />
	public override int GetHashCode() => TypeCode.GetHashCode();

	/// <inheritdoc />
	public bool Equals(ErrorDataType other) => TypeCode == other.TypeCode;

	public static bool operator ==(ErrorDataType left, ErrorDataType right) => left.TypeCode == right.TypeCode;
	public static bool operator !=(ErrorDataType left, ErrorDataType right) => left.TypeCode != right.TypeCode;

	public static ErrorDataType FromInt32(int value) => new ErrorDataType(value);
	/// <summary>
	/// Converts <see cref="ErrorDataType"/> to <see cref="int"/>.
	/// </summary>
	/// <returns></returns>
	public int ToInt32() => TypeCode;

	/// <summary>
	/// Converts <see cref="ErrorDataType"/> to <see cref="int"/>.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public static explicit operator ErrorDataType(int code) => new ErrorDataType(code);
	/// <summary>
	/// Converts <see cref="int"/> to <see cref="ErrorDataType"/>.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public static implicit operator int(ErrorDataType code) => code.TypeCode;

	/// <summary>
	/// Creates a new <see cref="ErrorDataType"/> with the specified <paramref name="value"/> and <paramref name="name"/>.
	/// The function doesn't check the <paramref name="name"/> for uniqueness.
	/// </summary>
	/// <param name="value">Integer value of the created <see cref="ErrorDataType"/></param>
	/// <param name="name">Name of the created <see cref="ErrorDataType"/></param>
	public static ErrorDataType Create(int value, string name)
	{
		if (value < 0 || value > __items.Length)
			throw new ArgumentOutOfRangeException(nameof(value), value, null);

		if (!TryCreate(value, name, out var dataType))
			throw new ArgumentOutOfRangeException(nameof(name), name, null);
		return dataType;
	}

	/// <summary>
	/// Creates a new <see cref="ErrorDataType"/> with the specified <paramref name="value"/> and <paramref name="name"/>.
	/// The function doesn't check the <paramref name="name"/> for uniqueness.
	/// </summary>
	/// <param name="value">Integer value of the created <see cref="ErrorDataType"/></param>
	/// <param name="name">Name of the created <see cref="ErrorDataType"/></param>
	/// <param name="result">Created <see cref="ErrorDataType"/> or default value</param>
	/// <returns>True if the <see cref="ErrorDataType"/> has been created.</returns>
	public static bool TryCreate(int value, string? name, out ErrorDataType result)
	{
		if (value < 0 || value >= __items.Length || name == null || (name = name.Trim()).Length == 0)
		{
			result = default;
			return false;
		}
		if (Interlocked.CompareExchange(ref __items[value], name, null) == null || string.Equals(name, __items[value], StringComparison.OrdinalIgnoreCase))
		{
			result = new ErrorDataType(value);
			return true;
		}
		result = default;
		return false;
	}

	/// <summary>
	/// Converts the string representation of an error-data-type to its <see cref="ErrorDataType"/> equivalent or default value of <see cref="ErrorDataType"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static ErrorDataType Parse(string value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (TryParse(value, out var result))
			return result;
		throw new FormatException($"'{value}' does not contain a valid string representation of an error code");
	}

	/// <summary>
	/// Converts the string representation of an error-data-type to its <see cref="ErrorDataType"/> equivalent or default value of <see cref="ErrorDataType"/>.
	/// </summary>
	/// <param name="value">A string containing an error-data-type to convert</param>
	public static ErrorDataType ParseOrDefault(string value)
	{
		_ = TryParse(value, out var code);
		return code;
	}

	/// <summary>
	/// Converts the string representation of an error-data-type to its <see cref="ErrorDataType"/> equivalent.
	/// A return value indicates whether the conversion succeeded.
	/// </summary>
	/// <param name="value">A string containing an error-data-type to convert</param>
	/// <param name="code">Result of the conversion or default value of <see cref="ErrorDataType"/></param>
	/// <returns>true if the s parameter was converted successfully; otherwise, false.</returns>
	public static bool TryParse(string? value, out ErrorDataType code)
	{
		if (value is not { Length: > 0 })
		{
			code = default;
			return false;
		}
		Debug.Assert(value != null);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		value = value.Replace(" ", "").Replace("-", "").Replace("_", "");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		if (Int32.TryParse(value, out var i))
		{
			if (i < 0 || i >= __items.Length || __items[i] == null)
			{
				code = default;
				return false;
			}
		}
		else
		{
			var v = value;
			i = Array.FindIndex(__items, o => string.Equals(o, v, StringComparison.OrdinalIgnoreCase));
			if (i < 0)
			{
				code = default;
				return false;
			}
		}
		code = new ErrorDataType(i);
		return true;
	}
}
