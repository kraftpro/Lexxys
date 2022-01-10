// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Data
{

	public static class Dc
	{
		public const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
		public const string ConfigSection = "database.connection";

		public static ILogging Log => __log ??= new Logger("Dc");
		private static ILogging? __log;

		public static ILogging Timing => __logTrace ??= new Logger("Dc-Timing");
		private static ILogging? __logTrace;

		private static IValue<ConnectionStringInfo> __connectionInfo = Config.Current.GetValue<ConnectionStringInfo>(ConfigSection);
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
			public IDataContext CreateContext(ConnectionStringInfo connectionInfo) => new DataContext(connectionInfo);
		}

		public static IDataContext Instance => __instance ??= StaticDataFactory.CreateContext(__connectionInfo.Value);


		#region Tools

		private const int MaxNStrLen = 4000;
		private const int MaxStrLen = 8000;

		#region Parameters

		public static DbParameter Parameter(string name, object value, DbType type, int size)
		{
			return new SqlParameter(name.StartsWith("@") ? name : "@" + name, value ?? DBNull.Value) { DbType = type, Size = size };
		}

		public static DbParameter Parameter(string name, object value, DbType type)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? DBNull.Value) { DbType = type };
		}

		public static DbParameter Parameter(string name, object value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? DBNull.Value);
		}

		public static DbParameter Parameter(string name, bool value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Bit };
		}

		public static DbParameter Parameter(string name, bool? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Bit };
		}

		public static DbParameter Parameter(string name, byte value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.TinyInt };
		}

		public static DbParameter Parameter(string name, byte? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.TinyInt };
		}

		public static DbParameter Parameter(string name, short value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.SmallInt };
		}

		public static DbParameter Parameter(string name, short? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.SmallInt };
		}

		public static DbParameter Parameter(string name, int value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Int };
		}

		public static DbParameter Parameter(string name, int? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Int };
		}

		public static DbParameter Parameter(string name, long value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.BigInt };
		}

		public static DbParameter Parameter(string name, long? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.BigInt };
		}

		public static DbParameter Parameter(string name, decimal value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Decimal };
		}

		public static DbParameter Parameter(string name, decimal? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Decimal };
		}

		public static DbParameter Parameter(string name, Money value)
		{
			return new SqlParameter(name.StartsWith("@") ? name : "@" + name, value.Amount) { SqlDbType = SqlDbType.Money };
		}

		public static DbParameter Parameter(string name, Money? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name : "@" + name, value?.Amount ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Money };
		}

		public static DbParameter Parameter(string name, float value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Real };
		}

		public static DbParameter Parameter(string name, float? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Real };
		}

		public static DbParameter Parameter(string name, double value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Float };
		}

		public static DbParameter Parameter(string name, double? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Float };
		}

		public static DbParameter Parameter(string name, DateTime value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.DateTime2 };
		}

		public static DbParameter Parameter(string name, DateTime? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.DateTime2 };
		}

		public static DbParameter Parameter(string name, TimeSpan value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.Time };
		}

		public static DbParameter Parameter(string name, TimeSpan? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Time };
		}

		public static DbParameter Parameter(string name, Guid value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value) { SqlDbType = SqlDbType.UniqueIdentifier };
		}

		public static DbParameter Parameter(string name, Guid? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.UniqueIdentifier };
		}

		public static DbParameter Parameter(string name, RowVersion value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value.GetBits()) { SqlDbType = SqlDbType.Timestamp };
		}

		public static DbParameter Parameter(string name, RowVersion? value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value == null ? (object)DBNull.Value: value.Value.GetBits()) { SqlDbType = SqlDbType.Timestamp };
		}

		public static DbParameter Parameter(string name, string value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.NVarChar };
		}

		public static DbParameter Parameter(string name, byte[] value)
		{
			return new SqlParameter(name.StartsWith("@") ? name: "@" + name, value ?? (object)DBNull.Value) { SqlDbType = SqlDbType.VarBinary };
		}

		#endregion

		public static string NullValue => "null";

		public static string IsNullValue => " is null";

		public static string IsNotNullValue => " is not null";

		public static string EmptySqlFilter => "(-1.18E-38)";

		public static string Equal(string value)
		{
			return value == null ? IsNullValue: "=" + Value(value);
		}
		public static string Equal(DateTime value)
		{
			return "=" + Value(value);
		}
		public static string Equal(DateTime? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(Guid value)
		{
			return "=" + Value(value);
		}
		public static string Equal(Guid? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(bool value)
		{
			return "=" + Value(value);
		}
		public static string Equal(bool? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(int value)
		{
			return "=" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Equal(int? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(long value)
		{
			return "=" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Equal(long? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(double value)
		{
			return "=" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Equal(double? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(decimal value)
		{
			return "=" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Equal(decimal? value)
		{
			return value == null ? IsNullValue: Equal(value.GetValueOrDefault());
		}
		public static string Equal(byte[]? value)
		{
			return value == null ? IsNullValue: "=0x" + new String(Strings.ToHexCharArray(value));
		}

		public static string NotEqual(string? value)
		{
			return value == null ? IsNotNullValue: "<>" + Value(value);
		}
		public static string NotEqual(DateTime value)
		{
			return "<>" + Value(value);
		}
		public static string NotEqual(DateTime? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(Guid value)
		{
			return "<>" + Value(value);
		}
		public static string NotEqual(Guid? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(bool value)
		{
			return "<>" + Value(value);
		}
		public static string NotEqual(bool? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(int value)
		{
			return "<>" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string NotEqual(int? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(long value)
		{
			return "<>" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string NotEqual(long? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(double value)
		{
			return "<>" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string NotEqual(double? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(decimal value)
		{
			return "<>" + value.ToString(CultureInfo.InvariantCulture);
		}
		public static string NotEqual(decimal? value)
		{
			return value == null ? IsNotNullValue: NotEqual(value.GetValueOrDefault());
		}
		public static string NotEqual(byte[]? value)
		{
			return value == null ? IsNotNullValue: "<>0x" + new String(Strings.ToHexCharArray(value));
		}

		public static string IdFilter(IEnumerable<int>? ids)
		{
			if (ids == null)
				return EmptySqlFilter;
			string result = string.Join(",", ids.Where(o => o > 0));
			return result.Length == 0 ? EmptySqlFilter: "(" + result + ")";
		}

		public static string Id(int value)
		{
			return value > 0 ? value.ToString(CultureInfo.InvariantCulture): "0";
		}
		public static string Id(int? value)
		{
			return value.GetValueOrDefault() > 0 ? value.GetValueOrDefault().ToString(CultureInfo.InvariantCulture): "0";
		}
		public static string Id(string? value)
		{
			return Int32.TryParse(value, out int id) && id > 0 ? id.ToString(CultureInfo.InvariantCulture): "0";
		}
		public static string IdOrNull(int? value)
		{
			return value.GetValueOrDefault() > 0 ? value.GetValueOrDefault().ToString(CultureInfo.InvariantCulture): NullValue;
		}
		public static string IdOrNull(string? value)
		{
			return Int32.TryParse(value, out int id) && id > 0 ? id.ToString(CultureInfo.InvariantCulture): NullValue;
		}
		public static string DateValue(DateTime? value)
		{
			return value == null ? NullValue: DateValue(value.GetValueOrDefault());
		}
		public static string DateValue(DateTime value)
		{
			return value.ToString(@"\'yyyyMMdd\'", CultureInfo.InvariantCulture);
		}
		public static string TimeValue(DateTime value)
		{
			return value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
		}
		public static string Name(string? value)
		{
			if (value == null)
				return String.Empty;
			Match m = __objectPartsRex.Match(value.Trim());
			return String.Join("", m.Groups[1].Captures.Cast<Capture>().Select(o => NamePart(o.Value) + ".")) + NamePart(m.Groups[2].Value);
		}
		private static readonly Regex __objectPartsRex = new Regex(@"\A(?:(?<a>\[(?:[^\]]|]])*\]|[^\.\[]*)\.){0,3}(?<b>.*)?\z", RegexOptions.IgnoreCase);
		[return: NotNullIfNotNull("name")]
		private static string NamePart(string? name)
		{
			return name == null ? String.Empty: name.Length == 0 || (name[0] == '[' && name[name.Length - 1] == ']') ? name: "[" + name.Replace("]", "]]") + "]";
		}
		public static string TextValue(string? value)
		{
			return value == null ? NullValue: "'" + value.Replace("'", "''") + "'";
		}
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
				return "n'" + value.Replace("'", "''") + "'";
			}
			if (value.Length > MaxStrLen)
				value = value.Substring(0, MaxStrLen);
			return "'" + value.Replace("'", "''") + "'";
		}
		public static string Value(DateTime? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(DateTime value)
		{
			return value.ToString(value.TimeOfDay == default ? @"\'yyyyMMdd\'": @"\'yyyyMMdd HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
		}
		public static string Value(TimeSpan? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(TimeSpan value)
		{
			return value.ToString(@"\'HH:mm:ss.fff\'", CultureInfo.InvariantCulture);
		}
		public static string Value(Guid value)
		{
			return "cast ('" + value.ToString("D") + "' as uniqueidentifier)";
		}
		public static string Value(bool value)
		{
			return value ? "1": "0";
		}
		public static string Value(int? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(long? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(long value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(ulong value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(double? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(double value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(decimal? value)
		{
			return value == null ? NullValue: Value(value.GetValueOrDefault());
		}
		public static string Value(decimal value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(Money? value)
		{
			return value == null ? NullValue : Value(value.GetValueOrDefault());
		}
		public static string Value(Money value)
		{
			return value.Amount.ToString(CultureInfo.InvariantCulture);
		}
		public static string Value(byte[]? value)
		{
			return Strings.ToHexString(value, "0x") ?? NullValue;
		}
		public static string Value(IEnum? value)
		{
			return value == null ? NullValue: Value(value.Value);
		}
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

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return NullValue;
				case TypeCode.Boolean:
					return Value((bool)value);
				case TypeCode.Byte:
					return Value((byte)value);
				case TypeCode.Char:
					return Value((char)value);
				case TypeCode.DateTime:
					return Value((DateTime)value);
				case TypeCode.Decimal:
					return Value((decimal)value);
				case TypeCode.Double:
					return Value((double)value);
				case TypeCode.Int16:
					return Value((short)value);
				case TypeCode.Int32:
					return Value((int)value);
				case TypeCode.Int64:
					return Value((long)value);
				case TypeCode.SByte:
					return Value((sbyte)value);
				case TypeCode.Single:
					return Value((float)value);
				case TypeCode.String:
					return Value((string)value);
				case TypeCode.UInt16:
					return Value((ushort)value);
				case TypeCode.UInt32:
					return Value((uint)value);
				case TypeCode.UInt64:
					return Value((ulong)value);

				//case TypeCode.Object:
				default:
					if (value is Money money)
						return Value(money);
					if (value is TimeSpan span)
						return Value(span);
					if (value is Guid guid)
						return Value(guid);
					if (value is byte[] bytes)
						return Value(bytes);
					if (value is IEnum enm)
						return Value(enm.Value);

					throw new ArgumentTypeException(nameof(value), type);
			}
		}
		#endregion

		#region Mappers

		#nullable disable

		internal static T ValueMapper<T>(DbCommand cmd)
		{
			if (AnonymousType<T>.IsBuiltinType)
			{
				var value = cmd.ExecuteScalar();
				if (value == null || value == DBNull.Value)
					return default;
				return AnonymousType<T>.Construct(new[] { value });
			}
			else
			{
				using DbDataReader reader = cmd.ExecuteReader();
				return reader.Read() ? AnonymousType<T>.Construct(reader) : default;
			}
		}

		internal static async Task<T> ValueMapperAsync<T>(DbCommand cmd)
		{
			if (AnonymousType<T>.IsBuiltinType)
			{
				var value = await cmd.ExecuteScalarAsync();
				if (value == null || value == DBNull.Value)
					return default;
				return AnonymousType<T>.Construct(new[] { value });
			}
			else
			{
				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
				return await reader.ReadAsync() ? AnonymousType<T>.Construct(reader) : default;
			}
		}

		internal static List<T> ListMapper<T>(DbCommand cmd)
		{
			using DbDataReader reader = cmd.ExecuteReader();
			var result = new List<T>();
			while (reader.Read())
			{
				result.Add(AnonymousType<T>.Construct(reader));
			}
			return result;
		}

		internal static async Task<List<T>> ListMapperAsync<T>(DbCommand cmd)
		{
			using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
			var result = new List<T>();
			while (await reader.ReadAsync().ConfigureAwait(false))
			{
				result.Add(AnonymousType<T>.Construct(reader));
			}
			return result;
		}

		#nullable enable

		internal static bool XmlTextMapper(TextWriter text, DbCommand cmd)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			bool here = false;
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
							{
								var rdr = reader.GetTextReader(i);
								text.Write(reader.GetString(i));
								here = true;
							}
						}
					}
				} while (reader.NextResult());
			}
			return here;
		}

		internal static async Task<bool> XmlTextMapperAsync(TextWriter text, DbCommand cmd)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			bool here = false;
			using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
			{
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
								var rdr = reader.GetTextReader(i);
								text.Write(reader.GetString(i));
								here = true;
							}
						}
					}
				} while (await reader.NextResultAsync().ConfigureAwait(false));
			}
			return here;
		}

		internal static List<Xml.XmlLiteNode> XmlMapper(DbCommand cmd)
		{
			var result = new List<Xml.XmlLiteNode>();
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
								result.AddRange(Xml.XmlLiteNode.FromXmlFragment(reader.GetString(i)));
						}
					}
				} while (reader.NextResult());
			}
			return result;
		}

		internal static async Task<List<Xml.XmlLiteNode>> XmlMapperAsync(DbCommand cmd)
		{
			var result = new List<Xml.XmlLiteNode>();
			using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
			{
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
								result.AddRange(Xml.XmlLiteNode.FromXmlFragment(reader.GetString(i)));
						}
					}
				} while (await reader.NextResultAsync().ConfigureAwait(false));
			}
			return result;
		}

		#nullable disable

		private static class AnonymousType<T>
		{
			private static readonly SortedList<int, Func<object[], object>> Constructors;
			public static bool IsBuiltinType { get; }

			static AnonymousType()
			{
				Type type = typeof(T);
				if (__systemTypes.TryGetValue(type, out var f))
				{
					Constructors = new SortedList<int, Func<object[], object>>
					{
						{ 1, f },
					};
					IsBuiltinType = true;
					return;
				}
				Type t = Factory.NullableTypeBase(type);
				if (t.IsEnum)
				{
					Constructors = new SortedList<int, Func<object[], object>>
					{
						{ 1, t == type ?
							(Func<object[], object>)(o => (T)(object)((int?)o[0] ?? 0)):
							(Func<object[], object>)(o => (T)(object)(int?)o[0]) }
					};
					return;
				}
				ConstructorInfo[] cc = type.GetConstructors();
				var constructors = new SortedList<int, Func<object[], object>>();
				for (int i = 0; i < cc.Length; ++i)
				{
					Type[] parameters = Array.ConvertAll(cc[i].GetParameters(), o => o.ParameterType);
					if (!constructors.ContainsKey(parameters.Length))
						constructors[parameters.Length] = Factory.TryGetConstructor(type, true, parameters);
				}
				Constructors = constructors;
			}

			public static T Construct(object[] values)
			{
				if (!Constructors.TryGetValue(values.Length, out Func<object[], object> constructor))
					throw EX.InvalidOperation(SR.Factory_CannotFindConstructor(typeof(T), values.Length));
				for (int i = 0; i < values.Length; ++i)
				{
					if (values[i] == DBNull.Value)
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
		}

		#region System types constructors

		private static readonly Dictionary<Type, Func<object[], object>> __systemTypes = new Dictionary<Type, Func<object[], object>>
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
				{ typeof(Ternary), o => new Ternary((bool?)o[0]) },
				{ typeof(RowVersion), o =>
				{
					var v = o[0];
					return v == null ? default: v is byte[] b ? new RowVersion(b): new RowVersion((long)v);
				} },

				{ typeof(string), o => (string)o[0] },
				{ typeof(byte[]), o => (byte[])o[0] },
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
				{ typeof(Ternary?), o => (Ternary?)new Ternary((bool?)o[0]) },
				{ typeof(RowVersion?), o =>
				{
					var v = o[0];
					return v == null ? default: v is byte[] b ? (RowVersion?)new RowVersion(b): (RowVersion?)new RowVersion((long)v);
				} },
			};

		#endregion

		#nullable enable

		#endregion

		#region Disposible objects

		internal class Connecting: IContextHolder
		{
			private DataContext _context;
			private readonly int _count;
			private bool _disposed;

			public Connecting(DataContext context)
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
					Debug.Assert(_context.Context.ConnectionsCount == _count);
					_context.Context.Disconnect();
				}
			}
		}

		internal sealed class Transacting: ITransactable
		{
			private readonly DataContext _context;
			private readonly int _count;
			private readonly bool _autoCommit;
			private bool _disposed;

			public Transacting(DataContext context, bool autoCommit, IsolationLevel isolationLevel)
			{
				_context = context ?? throw new ArgumentNullException(nameof(context));
				_count = _context.Context.Begin(isolationLevel);
				_autoCommit = autoCommit;
			}

			public IDataContext Context => _context;

			public void Commit()
			{
				Close(true, false);
			}

			public void Rollback()
			{
				Close(false, false);
			}

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
			private readonly DataContext _context;
			private readonly TimeSpan _timeout;
			private bool _disposed;

			public TimeoutLocker(DataContext context, TimeSpan timeout)
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
			private readonly DataContext _context;
			private bool _disposed;

			public TimingLocker(DataContext context)
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
			private readonly DataContext _context;
			private bool _disposed;

			public TimeHolder(DataContext context)
			{
				_context = context ?? throw new ArgumentNullException(nameof(context));
				if (!_context.Context.LockNow(_context.Context.Time))
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
}
