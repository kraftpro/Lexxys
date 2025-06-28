// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys.Xml;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Data;

public static class Dc
{
	public static readonly SqlPart NullValue = (SqlPart)"null";

	public static readonly SqlPart IsNullValue = (SqlPart)" is null";

	public static readonly SqlPart IsNotNullValue = (SqlPart)" is not null";

	public static readonly SqlPart EmptySqlFilter = (SqlPart)"(null)";

	public static readonly SqlPart EqualSign = (SqlPart)"=";
	public static readonly SqlPart NotEqualSign = (SqlPart)"<>";

	public static readonly SqlPart TrueValue = (SqlPart)"1";
	public static readonly SqlPart FalseValue = (SqlPart)"0";
	public static readonly SqlPart ZeroValue = (SqlPart)"0";

	public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
	public const string ConfigSection = "database.connection";

	public static ILogger Log => __log ??= Statics.TryGetLogger("Dc") ?? NullLogger.Instance;
	private static ILogger? __log;

	public static ILogger Timing => __logTrace ??= Statics.TryGetLogger("Dc-Timing") ?? NullLogger.Instance;
	private static ILogger? __logTrace;

	static Lazy<IDataContextFactory> _dataFactory = new(() => StaticDataFactory);

	private static IDataContextFactory? __staticDataFactory;
	[ThreadStatic]
	private static IDataContext? __instance;

	public static IDataContextFactory StaticDataFactory
	{
		get => __staticDataFactory ??= new SimpleDataFactory();
		set => __staticDataFactory = value ?? throw new ArgumentNullException(nameof(value));
	}

	private class SimpleDataFactory: IDataContextFactory
	{
		public IDataContext CreateContext(ConnectionStringInfo connectionInfo) => new SqlServerDataContext(connectionInfo);
	}

	public static IDataContext Instance => __instance ??= StaticDataFactory.CreateContext(Config.Current.GetValue<ConnectionStringInfo>(ConfigSection).Value);

	#region Tools

	#region Parameters

	public static DataParameter Parameter(string name, DbType type, ParameterDirection direction) => new DataParameter(name, null, type) { Direction = direction };

	public static DataParameter Parameter(string name, object? value, DbType type, int size) => new DataParameter(name, value, type, size);

	public static DataParameter Parameter(string name, object? value, DbType type) => new DataParameter(name, value, type);

	public static DataParameter Parameter(string name, object? value) => new DataParameter(name, value);

	public static DataParameter Parameter(string name, IEnum? value) => new DataParameter(name, value?.Value, DbType.Int32);

	public static DataParameter Parameter<T>(string name, T? value) where T: struct, Enum
	{
		var (dbValue, dbType) = value == null ? (null, DbType.Int32): UnderlyingValue(value);
		return new DataParameter(name, dbValue, dbType);
	}

	public static DataParameter Parameter<T>(string name, T value) where T: struct, Enum
	{
		var (dbValue, dbType) = UnderlyingValue(value);
		return new DataParameter(name, dbValue, dbType);
	}

	private static (object, DbType) UnderlyingValue(Enum value) => (value.GetTypeCode()) switch
	{
		TypeCode.Byte => (((IConvertible)value).ToByte(CultureInfo.InvariantCulture), DbType.Byte),
		TypeCode.SByte => (((IConvertible)value).ToSByte(CultureInfo.InvariantCulture), DbType.SByte),
		TypeCode.Int16 => (((IConvertible)value).ToInt16(CultureInfo.InvariantCulture), DbType.Int16),
		TypeCode.UInt16 => (((IConvertible)value).ToUInt16(CultureInfo.InvariantCulture), DbType.UInt16),
		TypeCode.Int32 => (((IConvertible)value).ToInt32(CultureInfo.InvariantCulture), DbType.Int32),
		TypeCode.UInt32 => (((IConvertible)value).ToUInt32(CultureInfo.InvariantCulture), DbType.UInt32),
		TypeCode.Int64 => (((IConvertible)value).ToInt64(CultureInfo.InvariantCulture), DbType.Int64),
		TypeCode.UInt64 => (((IConvertible)value).ToUInt64(CultureInfo.InvariantCulture), DbType.UInt64),
		_ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
	};

	public static DataParameter Parameter(string name, bool value) => new DataParameter(name, value, DbType.Boolean);

	public static DataParameter Parameter(string name, bool? value) => new DataParameter(name, value, DbType.Boolean);

	public static DataParameter Parameter(string name, byte value) => new DataParameter(name, value, DbType.Byte);

	public static DataParameter Parameter(string name, byte? value) => new DataParameter(name, value, DbType.Byte);

	public static DataParameter Parameter(string name, short value) => new DataParameter(name, value, DbType.Int16);

	public static DataParameter Parameter(string name, short? value) => new DataParameter(name, value, DbType.Int16);

	public static DataParameter Parameter(string name, int value) => new DataParameter(name, value, DbType.Int32);

	public static DataParameter Parameter(string name, int? value) => new DataParameter(name, value, DbType.Int32);

	public static DataParameter Parameter(string name, long value) => new DataParameter(name, value, DbType.Int64);

	public static DataParameter Parameter(string name, long? value) => new DataParameter(name, value, DbType.Int64);

	public static DataParameter Parameter(string name, decimal value) => new DataParameter(name, value, DbType.Decimal);

	public static DataParameter Parameter(string name, decimal? value) => new DataParameter(name, value, DbType.Decimal);

	public static DataParameter Parameter(string name, Money value) => new DataParameter(name, value.Amount, DbType.Currency);

	public static DataParameter Parameter(string name, Money? value) => new DataParameter(name, value?.Amount, DbType.Currency);

	public static DataParameter Parameter(string name, float value) => new DataParameter(name, value, DbType.Single);

	public static DataParameter Parameter(string name, float? value) => new DataParameter(name, value, DbType.Single);

	public static DataParameter Parameter(string name, double value) => new DataParameter(name, value, DbType.Double);

	public static DataParameter Parameter(string name, double? value) => new DataParameter(name, value, DbType.Double);

	public static DataParameter Parameter(string name, DateTime value) => new DataParameter(name, value, DbType.DateTime2);

	public static DataParameter Parameter(string name, DateTime? value) => new DataParameter(name, value, DbType.DateTime2);

	public static DataParameter Parameter(string name, TimeSpan value) => new DataParameter(name, value, DbType.Time);

	public static DataParameter Parameter(string name, TimeSpan? value) => new DataParameter(name, value, DbType.Time);

	public static DataParameter Parameter(string name, Guid value) => new DataParameter(name, value, DbType.Guid);

	public static DataParameter Parameter(string name, Guid? value) => new DataParameter(name, value, DbType.Guid);

	public static DataParameter Parameter(string name, RowVersion value) => new DataParameter(name, value.ToByteArray(), DbType.Binary);

	public static DataParameter Parameter(string name, RowVersion? value) => new DataParameter(name, value?.ToByteArray(), DbType.Binary);

	public static DataParameter Parameter(string name, string? value) => new DataParameter(name, value, DbType.String);

	public static DataParameter Parameter(string name, byte[]? value) => new DataParameter(name, value, DbType.Binary);

	#endregion

	#region Equal

	public static SqlPart Equal(string? value) => value == null ? IsNullValue: EqualSign + Value(value);
	public static SqlPart Equal(DateTime value) => EqualSign + Value(value);
	public static SqlPart Equal(DateTime? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(Guid value) => EqualSign + Value(value);
	public static SqlPart Equal(Guid? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(bool value) => EqualSign + Value(value);
	public static SqlPart Equal(bool? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(int value) => EqualSign + (SqlPart)value.ToString();
	public static SqlPart Equal(int? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(long value) => EqualSign + (SqlPart)value.ToString();
	public static SqlPart Equal(long? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(double value) => EqualSign + (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart Equal(double? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(decimal value) => EqualSign + (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart Equal(decimal? value) => value == null ? IsNullValue: Equal(value.GetValueOrDefault());
	public static SqlPart Equal(byte[]? value) => value == null ? IsNullValue: (SqlPart)Strings.ToHexString(value, "=0x");

	#endregion

	#region NotEqual

	public static SqlPart NotEqual(string? value) => value == null ? IsNotNullValue: NotEqualSign + Value(value);
	public static SqlPart NotEqual(DateTime value) => NotEqualSign + Value(value);
	public static SqlPart NotEqual(DateTime? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(Guid value) => NotEqualSign + Value(value);
	public static SqlPart NotEqual(Guid? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(bool value) => NotEqualSign + Value(value);
	public static SqlPart NotEqual(bool? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(int value) => NotEqualSign + (SqlPart)value.ToString();
	public static SqlPart NotEqual(int? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(long value) => NotEqualSign + (SqlPart)value.ToString();
	public static SqlPart NotEqual(long? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(double value) => NotEqualSign + (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart NotEqual(double? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(decimal value) => NotEqualSign + (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart NotEqual(decimal? value) => value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
	public static SqlPart NotEqual(byte[]? value) => value == null ? IsNotNullValue: (SqlPart)Strings.ToHexString(value, "<>0x");

	#endregion

	#region Value

	public static SqlPart Value(string? value)
	{
		if (value == null)
			return NullValue;
		var ss = value.AsSpan();
		bool apos = false;
		for (int i = 0; i < ss.Length; ++i)
		{
			if (ss[i] > 127)
				return (SqlPart)("N'" + value.Replace("'", "''") + "'");
			if (ss[i] == '\'')
				apos = true;
		}

		if (apos)
			return (SqlPart)("'" + value.Replace("'", "''") + "'");
#if NET
		return (SqlPart)String.Concat("'", ss, ";");
#else
		return (SqlPart)("'" + ss.ToString() + "'");
#endif
	}
	public static SqlPart Value(string? value, bool unicode) => value == null ? NullValue: (SqlPart)((unicode ? "N'": "'") + value.Replace("'", "''") + "'");
	public static SqlPart Value(DateTime? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(DateTime value) => (SqlPart)value.ToString(value.TimeOfDay == default ? @"\'yyyyMMdd\'": @"\'yyyyMMdd HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static SqlPart Value(DateTimeOffset? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(DateTimeOffset value) => (SqlPart)value.ToString(value.TimeOfDay == default ? @"\'yyyyMMdd\'": @"\'yyyyMMdd HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static SqlPart Value(TimeSpan? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(TimeSpan value) => (SqlPart)value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
	public static SqlPart Value(Guid value) => (SqlPart)("cast ('" + value.ToString("D") + "' as uniqueidentifier)");
	public static SqlPart Value(bool value) => value ? TrueValue: FalseValue;
	public static SqlPart Value(int? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(int value) => (SqlPart)value.ToString();
	public static SqlPart Value(long? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(long value) => (SqlPart)value.ToString();
	public static SqlPart Value(ulong? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(ulong value) => (SqlPart)value.ToString();
	public static SqlPart Value(double? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(double value) => (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart Value(decimal? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(decimal value) => (SqlPart)value.ToString(CultureInfo.InvariantCulture);
	public static SqlPart Value(Money? value) => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value(Money value) => (SqlPart)value.Amount.ToString(CultureInfo.InvariantCulture);
	public static SqlPart Value(byte[]? value) => value == null ? NullValue: (SqlPart)Strings.ToHexString(value, "0x".AsSpan());
	public static SqlPart Value(RowVersion value) => (SqlPart)value.ToString();
	public static SqlPart Value(RowVersion? value) => value == null ? NullValue: (SqlPart)value.GetValueOrDefault().ToString();
	public static SqlPart Value(IEnum? value) => value == null ? NullValue: Value(value.Value);
	public static SqlPart Value<T>(T? value) where T: struct, Enum => value == null ? NullValue: Value(value.GetValueOrDefault());
	public static SqlPart Value<T>(T value) where T: struct, Enum => (value.GetTypeCode()) switch
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
	public static SqlPart Value(object? value)
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

	public static SqlPart Name(string? value)
	{
		if (value == null)
			return SqlPart.Empty;
		Match m = __objectPartsRex.Match(value.Trim());
		return (SqlPart)(String.Join("", m.Groups[1].Captures.Cast<Capture>().Select(o => NamePart(o.Value) + ".")) + NamePart(m.Groups[2].Value));

		static string NamePart(string name)
			=> name.Length == 0 || (name[0] == '[' && name[^1] == ']') ? name: "[" + name.Replace("]", "]]") + "]";

	}
	private static readonly Regex __objectPartsRex = new Regex(@"\A(?:(?<a>\[(?:[^\]]|]])*\]|[^\.\[]*)\.){0,3}(?<b>.*)?\z", RegexOptions.IgnoreCase);

	public static SqlPart IdFilter(IEnumerable<int>? ids)
	{
		if (ids is null)
			return EmptySqlFilter;
#if NET
		var text = new StringBuilder().Append('(').AppendJoin(',', ids).Append(')');
		return text.Length == 2 ? EmptySqlFilter: (SqlPart)text.ToString();
#else
		string result = string.Join(",", ids);
		return result.Length == 0 ? EmptySqlFilter: (SqlPart)("(" + result + ")");
#endif
	}

	public static SqlPart IdFilter(IEnumerable<long>? ids)
	{
		if (ids is null)
			return EmptySqlFilter;
#if NET
		var text = new StringBuilder().Append('(').AppendJoin(',', ids).Append(')');
		return text.Length == 2 ? EmptySqlFilter: (SqlPart)text.ToString();
#else
		string result = string.Join(",", ids);
		return result.Length == 0 ? EmptySqlFilter: (SqlPart)("(" + result + ")");
#endif
	}

	public static SqlPart IdFilter(IEnumerable<Guid>? ids)
	{
		if (ids is null)
			return EmptySqlFilter;

		var text = new StringBuilder();
		var comma = '(';
		foreach (var id in ids)
		{
			text.Append(comma);
			text.Append("{guid'").Append(id.ToString("D")).Append("'}");
			comma = ',';
		}
		return text.Length == 0 ? EmptySqlFilter: (SqlPart)text.Append(')').ToString();
	}

	public static SqlPart IdFilter(IEnumerable<string>? ids)
	{
		if (ids is null)
			return EmptySqlFilter;

		var text = new StringBuilder();
		var comma = '(';
		foreach (var id in ids)
		{
			if (id == null) continue;
			text.Append(comma);
			text.Append("N'").Append(id.Replace("'", "''")).Append('\'');
			comma = ',';
		}
		return text.Length == 0 ? EmptySqlFilter: (SqlPart)text.Append(')').ToString();
	}

	public static SqlPart Id(int value) => value > 0 ? (SqlPart)value.ToString(): ZeroValue;
	public static SqlPart Id(int? value) => value.GetValueOrDefault() > 0 ? (SqlPart)value.GetValueOrDefault().ToString(): ZeroValue;
	public static SqlPart Id(string? value) => Int32.TryParse(value, out int id) && id > 0 ? (SqlPart)id.ToString(): ZeroValue;
	public static SqlPart IdOrNull(int? value) => value.GetValueOrDefault() > 0 ? (SqlPart)value.GetValueOrDefault().ToString(): NullValue;
	public static SqlPart IdOrNull(string? value) => Int32.TryParse(value, out int id) && id > 0 ? (SqlPart)id.ToString(): NullValue;
	public static SqlPart DateValue(DateTime? value) => value == null ? NullValue: DateValue(value.GetValueOrDefault());
	public static SqlPart DateValue(DateTime value) => (SqlPart)value.ToString(@"\'yyyyMMdd\'", CultureInfo.InvariantCulture);
	public static SqlPart TimeValue(DateTime value) => (SqlPart)value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);

	public static string EscapeLike(string? value) => value == null ? String.Empty: value.Replace("[", "[[]").Replace("_", "[_]").Replace("%", "[%]");

	public static SqlPart Sql(string? value) => new SqlPart(value);

#if NET
	public static SqlPart Sql(ref SqlInterpolatedHandler value) => new SqlPart(value.ToStringAndClear());
#endif

	#endregion

	#region Mappers

	internal static T? ValueMapper<T>(DbCommand cmd) => ValueMapper<T?>(cmd, default);

	internal static Task<T?> ValueMapperAsync<T>(DbCommand cmd) => ValueMapperAsync<T?>(cmd, default);

	internal static T ValueMapper<T>(DbCommand cmd, T defaultValue)
	{
		if (AnonymousType<T>.IsBuiltInType)
		{
			var value = cmd.ExecuteScalar();
			if (value == null || Convert.IsDBNull(value))
				return defaultValue;
			return AnonymousType<T>.Construct([value]);
		}
		else
		{
			using DbDataReader reader = cmd.ExecuteReader();
			return reader.Read() ? AnonymousType<T>.Construct(reader): defaultValue;
		}
	}

	internal static async Task<T> ValueMapperAsync<T>(DbCommand cmd, T defaultValue)
	{
		if (AnonymousType<T>.IsBuiltInType)
		{
			object? value = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
			if (value == null || Convert.IsDBNull(value))
				return defaultValue;
			return AnonymousType<T>.Construct([value]);
		}
		else
		{
			#if NET6_0_OR_GREATER
			await 
			#endif
			using DbDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
			return await reader.ReadAsync().ConfigureAwait(false) ? AnonymousType<T>.Construct(reader): defaultValue;
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
		if (text == null) throw new ArgumentNullException(nameof(text));
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
		if (text == null) throw new ArgumentNullException(nameof(text));
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
		var builder = XmlNodeBuilder.Create<IXmlReadOnlyNode>();
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
		var builder = XmlNodeBuilder.Create<IXmlReadOnlyNode>();
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
		
		public static bool IsBuiltInType { get; } = __systemTypes.ContainsKey(typeof(T));

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
				return new SortedList<int, Func<object?[], object?>>{ { 1, f } };
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
		private readonly SqlServerDataContext _context;
		private readonly int _count;
		private bool _disposed;

		public Connecting(SqlServerDataContext context)
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
		private readonly SqlServerDataContext _context;
		private readonly bool _autoCommit;
		private bool _disposed;

		public Transacting(SqlServerDataContext context, bool autoCommit, IsolationLevel isolationLevel)
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
		private readonly SqlServerDataContext _context;
		private readonly TimeSpan _timeout;
		private bool _disposed;

		public TimeoutLocker(SqlServerDataContext context, TimeSpan timeout)
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
		private readonly SqlServerDataContext _context;
		private bool _disposed;

		public TimingLocker(SqlServerDataContext context)
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

	#endregion
}
