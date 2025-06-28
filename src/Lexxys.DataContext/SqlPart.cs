namespace Lexxys.Data;

public readonly struct SqlPart
{
	public readonly static SqlPart Empty = default;
	private readonly string? _value;

	public SqlPart(string? value) => _value = value;

#if NET
	public SqlPart(ref SqlInterpolatedHandler value) => _value = value.ToStringAndClear();
#endif

	public string Value => _value ?? string.Empty;

	public bool IsEmpty => String.IsNullOrEmpty(_value);

	public static implicit operator string(SqlPart sql) => sql.Value;

	public static explicit operator SqlPart(string? value) => new SqlPart(value);

	public static SqlPart operator +(SqlPart left, SqlPart right) => new SqlPart(left._value + right._value);

	public override string ToString() => Value;
}
