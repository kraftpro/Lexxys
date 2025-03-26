// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Lexxys.Xml;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexxys.Data;

public static class Dc
{
	public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
	public const string ConfigSection = "database.connection";

	public static ILogger Log => __log ??= Statics.TryGetLogger("Dc") ?? NullLogger.Instance;
	private static ILogger? __log;

	public static ILogger Timing => __logTrace ??= Statics.TryGetLogger("Dc-Timing") ?? NullLogger.Instance;
	private static ILogger? __logTrace;

	private static readonly IValue<ConnectionStringInfo> __connectionInfo = Config.Current.GetValue<ConnectionStringInfo>(ConfigSection);
	private static IDataContextFactory __staticDataFactory = new SimpleDataFactory();
	[ThreadStatic]
	private static IDataContext? __instance;

	public static IDataContextFactory StaticDataFactory
	{
		get => __staticDataFactory;
		set => __staticDataFactory = value ?? throw new ArgumentNullException(nameof(value));
	}

	private class SimpleDataFactory: IDataContextFactory
	{
		public IDataContext CreateContext(ConnectionStringInfo connectionInfo) => new MsSqlDataContext(connectionInfo);
	}

	public static IDataContext Instance => __instance ??= StaticDataFactory.CreateContext(__connectionInfo.Value);


	#region Tools

	const int MaxNStrLen = 4000;
	const int MaxStrLen = 8000;

	#region Parameters

	public static DataParameter Parameter(string name, DbType type, ParameterDirection direction)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, null, type) { Direction = direction };
	}

	public static DataParameter Parameter(string name, object? value, DbType type, int size)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? DBNull.Value, type, size);
	}

	public static DataParameter Parameter(string name, object? value, DbType type)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? DBNull.Value, type);
	}

	public static DataParameter Parameter(string name, object? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? DBNull.Value);
	}

	public static DataParameter Parameter(string name, bool value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Boolean);
	}

	public static DataParameter Parameter(string name, bool? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Boolean);
	}

	public static DataParameter Parameter(string name, byte value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Byte);
	}

	public static DataParameter Parameter(string name, byte? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Byte);
	}

	public static DataParameter Parameter(string name, short value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Int16);
	}

	public static DataParameter Parameter(string name, short? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Int16);
	}

	public static DataParameter Parameter(string name, int value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Int32);
	}

	public static DataParameter Parameter(string name, int? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Int32);
	}

	public static DataParameter Parameter(string name, long value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Int64);
	}

	public static DataParameter Parameter(string name, long? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Int64);
	}

	public static DataParameter Parameter(string name, decimal value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Decimal);
	}

	public static DataParameter Parameter(string name, decimal? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Decimal);
	}

	public static DataParameter Parameter(string name, Money value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value.Amount, DbType.Currency);
	}

	public static DataParameter Parameter(string name, Money? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value?.Amount ?? (object)DBNull.Value, DbType.Currency);
	}

	public static DataParameter Parameter(string name, float value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Single);
	}

	public static DataParameter Parameter(string name, float? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Single);
	}

	public static DataParameter Parameter(string name, double value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Double);
	}

	public static DataParameter Parameter(string name, double? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Double);
	}

	public static DataParameter Parameter(string name, DateTime value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.DateTime2);
	}

	public static DataParameter Parameter(string name, DateTime? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.DateTime2);
	}

	public static DataParameter Parameter(string name, TimeSpan value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Time);
	}

	public static DataParameter Parameter(string name, TimeSpan? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Time);
	}

	public static DataParameter Parameter(string name, Guid value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value, DbType.Guid);
	}

	public static DataParameter Parameter(string name, Guid? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Guid);
	}

	public static DataParameter Parameter(string name, RowVersion value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value.ToByteArray(), DbType.Binary);
	}

	public static DataParameter Parameter(string name, RowVersion? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value == null ? DBNull.Value: value.Value.ToByteArray(), DbType.Binary);
	}

	public static DataParameter Parameter(string name, string? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.String);
	}

	public static DataParameter Parameter(string name, byte[]? value)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));
		return new DataParameter(name.StartsWith("@", StringComparison.Ordinal) ? name: "@" + name, value ?? (object)DBNull.Value, DbType.Binary);
	}

	#endregion

	public static string NullValue => "null";

	public static string IsNullValue => " is null";

	public static string IsNotNullValue => " is not null";

	public static string EmptySqlFilter => "(-1.18E-38)";

	public static string Equal(string? value) => value == null ? IsNullValue: "=" + Value(value);
	public static string Equal(DateTime value) => "=" + Value(value);
	public static string Equal(DateTime? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(Guid value) => "=" + Value(value);
	public static string Equal(Guid? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(bool value) => "=" + Value(value);
	public static string Equal(bool? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(int value) => "=" + value.ToString();
	public static string Equal(int? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(long value) => "=" + value.ToString();
	public static string Equal(long? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(double value) => "=" + value.ToString(CultureInfo.InvariantCulture);
	public static string Equal(double? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(decimal value) => "=" + value.ToString(CultureInfo.InvariantCulture);
	public static string Equal(decimal? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static string Equal(byte[]? value) => value == null ? IsNullValue: Strings.ToHexString(value, "=0x".AsSpan());

	public static string NotEqual(string? value) => value == null ? IsNotNullValue: "<>" + Value(value);
	public static string NotEqual(DateTime value) => "<>" + Value(value);
	public static string NotEqual(DateTime? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(Guid value) => "<>" + Value(value);
	public static string NotEqual(Guid? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(bool value) => "<>" + Value(value);
	public static string NotEqual(bool? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(int value) => "<>" + value.ToString();
	public static string NotEqual(int? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(long value) => "<>" + value.ToString();
	public static string NotEqual(long? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(double value) => "<>" + value.ToString(CultureInfo.InvariantCulture);
	public static string NotEqual(double? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(decimal value) => "<>" + value.ToString(CultureInfo.InvariantCulture);
	public static string NotEqual(decimal? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static string NotEqual(byte[]? value) => value == null ? IsNotNullValue: "<>0x" + new String(Strings.ToHexCharArray(value));

	public static string IdFilter(IEnumerable<int>? ids)
	{
		if (ids == null)
			return EmptySqlFilter;
		string result = string.Join(",", ids.Where(o => o > 0));
		return result.Length == 0 ? EmptySqlFilter: "(" + result + ")";
	}

	public static string Id(int value) => value > 0 ? value.ToString(): "0";
	public static string Id(int? value) => value.GetValueOrDefault() > 0 ? value.GetValueOrDefault().ToString(): "0";
	public static string Id(string? value) => Int32.TryParse(value, out int id) && id > 0 ? id.ToString(): "0";
	public static string IdOrNull(int? value) => value.GetValueOrDefault() > 0 ? value.GetValueOrDefault().ToString(): NullValue;
	public static string IdOrNull(string? value) => Int32.TryParse(value, out int id) && id > 0 ? id.ToString(): NullValue;
	public static string DateValue(DateTime? value) => value == null ? NullValue: DateValue(value.GetValueOrDefault());
	public static string DateValue(DateTime value) => value.ToString(@"\'yyyyMMdd\'", CultureInfo.InvariantCulture);
	public static string TimeValue(DateTime value) => value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static string Name(string? value)
	{
		if (value == null)
			return String.Empty;
		Match m = __objectPartsRex.Match(value.Trim());
		return String.Join("", m.Groups[1].Captures.Cast<Capture>().Select(o => NamePart(o.Value) + ".")) + NamePart(m.Groups[2].Value);

		static string NamePart(string name)
			=> name.Length == 0 || (name[0] == '[' && name[name.Length - 1] == ']') ? name: "[" + name.Replace("]", "]]") + "]";

	}
	private static readonly Regex __objectPartsRex = new Regex(@"\A(?:(?<a>\[(?:[^\]]|]])*\]|[^\.\[]*)\.){0,3}(?<b>.*)?\z", RegexOptions.IgnoreCase);

	public static string TextValue(string? value, bool unicode = false) => value == null ? NullValue: (unicode ? "'": "N'") + value.Replace("'", "''") + "'";

	public static string EscapeLike(string? value)
	{
		if (value == null)
			return String.Empty;
		if (value.Length > 2000)
			value = value.Substring(0, 2000);
		return value.Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]");
	}

	public static string Value(string? value)
	{
		if (value == null)
			return NullValue;
		if (value.Length > MaxStrLen)
			value = value.Substring(0, MaxStrLen);
		return "'" + value.Replace("'", "''") + "'";
	}
	public static string Value(string? value, bool unicode)
	{
		if (value == null)
			return NullValue;
		if (unicode)
		{
			if (value.Length > MaxNStrLen)
				value = value.Substring(0, MaxNStrLen);
			return "N'" + value.Replace("'", "''") + "'";
		}
		if (value.Length > MaxStrLen)
			value = value.Substring(0, MaxStrLen);
		return "'" + value.Replace("'", "''") + "'";
	}
	public static string Value(DateTime? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(DateTime value) => value.ToString(value.TimeOfDay == default ? @"\'yyyyMMdd\'": @"\'yyyyMMdd HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static string Value(TimeSpan? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(TimeSpan value) => value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static string Value(Guid value) => "cast ('" + value.ToString("D") + "' as uniqueidentifier)";
	public static string Value(bool value) => value ? "1": "0";
	public static string Value(int? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(int value) => value.ToString();
	public static string Value(long? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(long value) => value.ToString();
	public static string Value(ulong value) => value.ToString();
	public static string Value(double? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(double value) => value.ToString(CultureInfo.InvariantCulture);
	public static string Value(decimal? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(decimal value) => value.ToString(CultureInfo.InvariantCulture);
	public static string Value(Money? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static string Value(Money value) => value.Amount.ToString(CultureInfo.InvariantCulture);
	public static string Value(byte[]? value) => value == null ? NullValue: Strings.ToHexString(value, "0x".AsSpan());
	public static string Value(IEnum? value) => value == null ? NullValue: Value(value.Value);
	public static string Value(Enum? value)
	{
		if (value == null)
			return NullValue;

		return (value.GetTypeCode()) switch
		{
			TypeCode.Byte => Value(((IConvertible)value).ToByte(CultureInfo.InvariantCulture)),
			TypeCode.SByte => Value(((IConvertible)value).ToSByte(CultureInfo.InvariantCulture)),
			TypeCode.Int16 => Value(((IConvertible)value).ToInt16(CultureInfo.InvariantCulture)),
			TypeCode.UInt16 => Value(((IConvertible)value).ToUInt16(CultureInfo.InvariantCulture)),
			TypeCode.Int32 => Value(((IConvertible)value).ToInt32(CultureInfo.InvariantCulture)),
			TypeCode.UInt32 => Value(((IConvertible)value).ToUInt32(CultureInfo.InvariantCulture)),
			TypeCode.Int64 => Value(((IConvertible)value).ToInt64(CultureInfo.InvariantCulture)),
			TypeCode.UInt64 => Value(((IConvertible)value).ToUInt64(CultureInfo.InvariantCulture)),
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
		};
	}
	public static string Value(object? value)
	{
		if (value == null)
			return NullValue;
		Type type = value.GetType();
		if (type.IsEnum)
			type = Enum.GetUnderlyingType(type);

		return Type.GetTypeCode(type) switch
		{
			TypeCode.Empty or TypeCode.DBNull => NullValue,
			TypeCode.Boolean => Value((bool)value),
			TypeCode.Byte => Value((byte)value),
			TypeCode.Char => Value((char)value),
			TypeCode.DateTime => Value((DateTime)value),
			TypeCode.Decimal => Value((decimal)value),
			TypeCode.Double => Value((double)value),
			TypeCode.Int16 => Value((short)value),
			TypeCode.Int32 => Value((int)value),
			TypeCode.Int64 => Value((long)value),
			TypeCode.SByte => Value((sbyte)value),
			TypeCode.Single => Value((float)value),
			TypeCode.String => Value((string)value),
			TypeCode.UInt16 => Value((ushort)value),
			TypeCode.UInt32 => Value((uint)value),
			TypeCode.UInt64 => Value((ulong)value),
			_ => value switch
			{
				Money money => Value(money),
				TimeSpan span => Value(span),
				Guid guid => Value(guid),
				byte[] bytes => Value(bytes),
				IEnum enm => Value(enm.Value),
				_ => throw new ArgumentTypeException(nameof(value), value.GetType())
			}
		};
	}
	#endregion

	#region Mappers

	internal static T? ValueMapper<T>(DbCommand cmd)
	{
		if (AnonymousType<T>.IsBuiltinType)
		{
			var value = cmd.ExecuteScalar();
			if (value == null || Convert.IsDBNull(value))
				return default;
			return AnonymousType<T>.Construct([value]);
		}
		else
		{
			using DbDataReader reader = cmd.ExecuteReader();
			return reader.Read() ? AnonymousType<T>.Construct(reader): default;
		}
	}

	internal static async Task<T?> ValueMapperAsync<T>(DbCommand cmd)
	{
		if (AnonymousType<T>.IsBuiltinType)
		{
			object? value = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
			if (value == null || Convert.IsDBNull(value))
				return default;
			return AnonymousType<T>.Construct([value]);
		}
		else
		{
			#if NET6_0_OR_GREATER
			await 
			#endif
			using DbDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
			return await reader.ReadAsync().ConfigureAwait(false) ? AnonymousType<T>.Construct(reader): default;
		}
	}

	internal static List<T> ListMapper<T>(DbCommand cmd)
	{
		using DbDataReader reader = cmd.ExecuteReader();
		var result = new List<T>();
		var values = new object[reader.FieldCount];
		while (reader.Read())
		{
			reader.GetValues(values);
			result.Add(AnonymousType<T>.Construct(values));
		}
		return result;
	}

	internal static async Task<List<T>> ListMapperAsync<T>(DbCommand cmd)
	{
#if NET6_0_OR_GREATER
		await
#endif
		using DbDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		var result = new List<T>();
		var values = new object[reader.FieldCount];
		while (await reader.ReadAsync().ConfigureAwait(false))
		{
			reader.GetValues(values);
			result.Add(AnonymousType<T>.Construct(values));
		}
		return result;
	}

	internal static bool XmlTextMapper(TextWriter text, DbCommand cmd)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		bool here = false;
		using var reader = cmd.ExecuteReader();
		do
		{
			int width = -1;
			while (reader.Read())
			{
				if (width == -1)
					width = reader.FieldCount;

				for (int i = 0; i < width; ++i)
				{
					if (!reader.IsDBNull(i))
					{
						text.Write(reader.GetString(i));
						here = true;
					}
				}
			}
		} while (reader.NextResult());

		return here;
	}

	internal static async Task<bool> XmlTextMapperAsync(TextWriter text, DbCommand cmd)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		bool here = false;
#if NET6_0_OR_GREATER
		await
#endif
		using DbDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		do
		{
			int width = -1;
			while (await reader.ReadAsync().ConfigureAwait(false))
			{
				if (width == -1)
					width = reader.FieldCount;

				for (int i = 0; i < width; ++i)
				{
					if (!await reader.IsDBNullAsync(i).ConfigureAwait(false))
					{
						await text.WriteAsync(reader.GetString(i)).ConfigureAwait(false);
						here = true;
					}
				}
			}
		} while (await reader.NextResultAsync().ConfigureAwait(false));

		return here;
	}

	internal static List<IXmlReadOnlyNode> XmlMapper(DbCommand cmd)
	{
		var builder = XmlFragBuilder.Create<IXmlReadOnlyNode>();
		using (var reader = cmd.ExecuteReader())
		{
			do
			{
				int width = -1;
				while (reader.Read())
				{
					if (width == -1)
						width = reader.FieldCount;

					for (int i = 0; i < width; ++i)
					{
						if (!reader.IsDBNull(i))
							builder.Xml(reader.GetString(i));
					}
				}
			} while (reader.NextResult());
		}
		return builder.Build();
	}

	internal static async Task<List<IXmlReadOnlyNode>> XmlMapperAsync(DbCommand cmd)
	{
		var builder = XmlFragBuilder.Create<IXmlReadOnlyNode>();
#if NET6_0_OR_GREATER
		await
#endif
		using DbDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		do
		{
			int width = -1;
			while (await reader.ReadAsync().ConfigureAwait(false))
			{
				if (width == -1)
					width = reader.FieldCount;

				for (int i = 0; i < width; ++i)
				{
					if (!await reader.IsDBNullAsync(i).ConfigureAwait(false))
						builder.Xml(reader.GetString(i));
				}
			}
		} while (await reader.NextResultAsync().ConfigureAwait(false));
		return builder.Build();
	}

	private static class AnonymousType<T>
	{
		private static readonly SortedList<int, Func<object?[], object?>> Constructors = CollectConstructors();
		
		public static bool IsBuiltinType { get; } = __systemTypes.ContainsKey(typeof(T));

		public static T Construct(object?[] values)
		{
			if (!Constructors.TryGetValue(values.Length, out var constructor))
				throw new InvalidOperationException(Lexxys.SR.Factory_CannotFindConstructor(typeof(T), values.Length));
			for (int i = 0; i < values.Length; ++i)
			{
				if (Convert.IsDBNull(values[i]))
					values[i] = null;
			}
			return (T)constructor(values)!;
		}

		public static T Construct(IDataRecord reader)
		{
			var values = new object[reader.FieldCount];
			reader.GetValues(values);
			return Construct(values);
		}

		private static SortedList<int, Func<object?[], object?>> CollectConstructors()
		{
			Type type = typeof(T);
			if (__systemTypes.TryGetValue(type, out var f))
			{
				return new SortedList<int, Func<object?[], object?>>
				{
					{ 1, f },
				};
			}
			Type t = Factory.NullableTypeBase(type);
			if (t.IsEnum)
			{
				return new SortedList<int, Func<object?[], object?>>
				{
					{ 1, t == type ?
						o => (T)(object)((int?)o[0] ?? 0):
						o => (T?)(object?)(int?)o[0] }
				};
			}
			ConstructorInfo[] cc = type.GetConstructors();
			var constructors = new SortedList<int, Func<object?[], object?>>();
			for (int i = 0; i < cc.Length; ++i)
			{
				Type[] parameters = Array.ConvertAll(cc[i].GetParameters(), o => o.ParameterType);
				if (!constructors.ContainsKey(parameters.Length))
					constructors[parameters.Length] = Factory.GetConstructor(type, parameters);
			}
			return constructors;
		}
	}

	#region System types constructors

	private static readonly Dictionary<Type, Func<object?[], object?>> __systemTypes = new Dictionary<Type, Func<object?[], object?>>
		{
			{ typeof(bool), o => (bool?)o[0] ?? default },
			{ typeof(byte), o => (byte?)o[0] ?? default },
			{ typeof(sbyte), o => (sbyte?)(byte?)o[0] ?? default },
			{ typeof(char), o => (char?)o[0] ?? default },
			{ typeof(short), o => (short?)o[0] ?? default },
			{ typeof(ushort), o => (ushort?)(short?)o[0] ?? default },
			{ typeof(int), o => (int?)o[0] ?? default },
			{ typeof(uint), o => (uint?)(int?)o[0] ?? default },
			{ typeof(long), o => (long?)o[0] ?? default },
			{ typeof(ulong), o => (ulong?)(long?)o[0] ?? default },
			{ typeof(decimal), o => (decimal?)o[0] ?? default },
			{ typeof(Money), o => (Money)((decimal?)o[0] ?? default) },
			{ typeof(DateTime), o => (DateTime?)o[0] ?? default },
			{ typeof(DateTimeOffset), o => (DateTimeOffset?)o[0] ?? default },
			{ typeof(TimeSpan), o => (TimeSpan?)o[0] ?? default },
			{ typeof(Guid), o => (Guid?)o[0] ?? default },
			{ typeof(RowVersion), o =>
			{
				var v = o[0];
				return v switch
				{
					null => default,
					byte[] b => new RowVersion(b),
					_ => new((ulong)v)
				};
			} },

			{ typeof(string), o => (string?)o[0] },
			{ typeof(byte[]), o => (byte[]?)o[0] },
			{ typeof(object), o => o[0] },

			{ typeof(bool?), o => (bool?)o[0] },
			{ typeof(byte?), o => (byte?)o[0] },
			{ typeof(sbyte?), o => (sbyte?)(byte?)o[0] },
			{ typeof(char?), o => (char?)o[0] },
			{ typeof(short?), o => (short?)o[0] },
			{ typeof(ushort?), o => (ushort?)(short?)o[0] },
			{ typeof(int?), o => (int?)o[0] },
			{ typeof(uint?), o => (uint?)(int?)o[0] },
			{ typeof(long?), o => (long?)o[0] },
			{ typeof(ulong?), o => (ulong?)(long?)o[0] },
			{ typeof(decimal?), o => (decimal?)o[0] },
			{ typeof(Money?), o => (Money?)(decimal?)o[0] },
			{ typeof(DateTime?), o => (DateTime?)o[0] },
			{ typeof(DateTimeOffset?), o => (DateTimeOffset?)o[0] },
			{ typeof(TimeSpan?), o => (TimeSpan?)o[0] },
			{ typeof(Guid?), o => (Guid?)o[0] },
			{ typeof(RowVersion?), o =>
			{
				var v = o[0];
				return v switch
				{
					null => default,
					byte[] b => new RowVersion(b),
					_ => new((ulong)v)
				};
			} },
		};

	#endregion

	#endregion

	#region Disposible objects

	internal class Connecting: IContextHolder
	{
		private readonly MsSqlDataContext _context;
		private readonly int _count;
		private bool _disposed;

		public Connecting(MsSqlDataContext context)
		{
			_context = context;
			_count = context.Context.Connect();
		}

		public IDataContext Context => _context;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				Debug.Assert(_context.Context.ConnectionsCount >= _count);
				_context.Context.Disconnect();
			}
		}
	}

	internal sealed class Transacting: ITransactable
	{
		private readonly MsSqlDataContext _context;
		private readonly bool _autoCommit;
		private bool _disposed;

		public Transacting(MsSqlDataContext context, bool autoCommit, IsolationLevel isolationLevel)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_context.Context.Begin(isolationLevel);
			_autoCommit = autoCommit;
		}

		public IDataContext Context => _context;

		public void Commit() => Close(true, false);

		public void Rollback() => Close(false, false);

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				Close(_autoCommit, true);
			}
		}

		private void Close(bool commit, bool dispose)
		{
			if (commit)
			{
				if (dispose)
					Log.Debug(SR.TransactionDisposedWithCommit());
				_context.Commit();
			}
			else
			{
				if (dispose)
					Log.Debug(SR.TransactionDisposedWithRollback());
				_context.Rollback();
			}
		}
	}

	internal sealed class ContextHolder: IContextHolder
	{
		public ContextHolder(IDataContext context)
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public IDataContext Context { get; }

		public void Dispose()
		{
		}
	}

	internal sealed class TimeoutLocker: IContextHolder
	{
		private readonly MsSqlDataContext _context;
		private readonly TimeSpan _timeout;
		private bool _disposed;

		public TimeoutLocker(MsSqlDataContext context, TimeSpan timeout)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_timeout = _context.Context.DefaultCommandTimeout;
			_context.Context.DefaultCommandTimeout = timeout;
		}

		public IDataContext Context => _context;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_context.Context.DefaultCommandTimeout = _timeout;
			}
		}
	}

	internal sealed class TimingLocker: IContextHolder
	{
		private readonly MsSqlDataContext _context;
		private bool _disposed;

		public TimingLocker(MsSqlDataContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_context.Context.Audit.LockTiming();
		}

		public IDataContext Context => _context;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_context.Context.Audit.UnlockTiming();
			}
		}
	}

	internal sealed class TimeHolder: IContextHolder
	{
		private readonly MsSqlDataContext _context;
		private bool _disposed;

		public TimeHolder(MsSqlDataContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			if (!_context.Context.LockNow(_context.Context.Now))
				_disposed = true;
		}

		public IDataContext Context => _context;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_context.Context.UnlockNow();
			}
		}
	}

	#endregion
}
