#if NET

using System.Runtime.CompilerServices;

namespace Lexxys.Data;

[InterpolatedStringHandler]
public ref struct SqlInterpolatedHandler(int literalLength, int formattedCount)
{
	private DefaultInterpolatedStringHandler _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);

	public void AppendLiteral(string value) => _handler.AppendLiteral(value);

	public void AppendFormatted(SqlPart value) => Append(value);

	public void AppendFormatted(SqlPart? value) => Append(value);

	public void AppendFormatted(string? value) => Append(Dc.Value(value));

	public void AppendFormatted(DateTime? value) => Append(Dc.Value(value));
	public void AppendFormatted(DateTime value) => Append(Dc.Value(value));

	public void AppendFormatted(DateTimeOffset? value) => Append(Dc.Value(value));
	public void AppendFormatted(DateTimeOffset value) => Append(Dc.Value(value));

	public void AppendFormatted(TimeOnly? value) => Append(Dc.Value(value));
	public void AppendFormatted(TimeOnly value) => Append(Dc.Value(value));

	public void AppendFormatted(DateOnly? value) => Append(Dc.Value(value));
	public void AppendFormatted(DateOnly value) => Append(Dc.Value(value));

	public void AppendFormatted(TimeSpan? value) => Append(Dc.Value(value));
	public void AppendFormatted(TimeSpan value) => Append(Dc.Value(value));

	public void AppendFormatted(Guid? value) => Append(Dc.Value(value));
	public void AppendFormatted(Guid value) => Append(Dc.Value(value));

	public void AppendFormatted(bool? value) => Append(Dc.Value(value));
	public void AppendFormatted(bool value) => Append(Dc.Value(value));

	public void AppendFormatted(int? value) => Append(Dc.Value(value));
	public void AppendFormatted(int value) => Append(Dc.Value(value));

	public void AppendFormatted(long? value) => Append(Dc.Value(value));
	public void AppendFormatted(long value) => Append(Dc.Value(value));

	public void AppendFormatted(ulong? value) => Append(Dc.Value(value));
	public void AppendFormatted(ulong value) => Append(Dc.Value(value));

	public void AppendFormatted(double? value) => Append(Dc.Value(value));
	public void AppendFormatted(double value) => Append(Dc.Value(value));

	public void AppendFormatted(decimal? value) => Append(Dc.Value(value));
	public void AppendFormatted(decimal value) => Append(Dc.Value(value));

	public void AppendFormatted(Money? value) => Append(Dc.Value(value));
	public void AppendFormatted(Money value) => Append(Dc.Value(value));

	public void AppendFormatted(byte[]? value) => Append(Dc.Value(value));

	public void AppendFormatted(RowVersion? value) => Append(Dc.Value(value));
	public void AppendFormatted(RowVersion value) => Append(Dc.Value(value));

	public void AppendFormatted(IEnum? value) => Append(Dc.Value(value));
	public void AppendFormatted<T>(T value) where T: struct, Enum => Append(Dc.Value(value));
	public void AppendFormatted<T>(T? value) where T: struct, Enum => Append(Dc.Value(value));

	public void AppendFormatted(IEnumerable<int>? value) => Append(Dc.IdFilter(value));
	public void AppendFormatted(IEnumerable<long>? value) => Append(Dc.IdFilter(value));
	public void AppendFormatted(IEnumerable<Guid>? value) => Append(Dc.IdFilter(value));

	public void AppendFormatted(object? value) => Append(Dc.Value(value));

	public override string ToString() => _handler.ToString();

	public SqlPart ToSqlQuery() => new SqlPart(_handler.ToStringAndClear());

	public string ToStringAndClear() => _handler.ToStringAndClear();

	private void Append(SqlPart? value)
	{
		if (value is { IsEmpty: false })
			_handler.AppendLiteral(value.GetValueOrDefault().Value);
	}

	private void Append(SqlPart value)
	{
		if (!value.IsEmpty)
			_handler.AppendLiteral(value.Value);
	}
}

#endif