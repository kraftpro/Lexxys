// Lexxys Infrastructural library.
// file: StringExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Lexxys
{
	using Xml;

	public static class StringExtensions
	{
#if !NETCOREAPP
		public static bool Contains(this string value, char item)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			return value.IndexOf(item) != -1;
		}

		public static bool Contains(this string value, string item)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			return value.IndexOf(item, StringComparison.CurrentCulture) != -1;
		}

		public static bool Contains(this string value, string item, StringComparison comparisonType)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			return value.IndexOf(item, comparisonType) != -1;
		}
#endif

		public static string Format(this string value, IFormatProvider formatProvider, IDictionary<string, object> data)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			return __formattingElement.Replace(value, m =>
			{
				string key = m.Groups[1].Value;
				if (!data.TryGetValue(key, out var obj))
					return m.Value;
				string fmt;
				if (m.Groups.Count > 1 && m.Groups[2].Value.Length > 1)
					fmt = "{0" + m.Groups[2].Value + "}";
				else
					fmt = "{0}";
				return String.Format(formatProvider, fmt, obj);
			});
		}
		private static readonly Regex __formattingElement = new Regex(@"{\s*([A-Za-z_$][A-Za-z0-9_\.\$-]*)\s*(:[^}]*)?}", RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public static string Format(this string value, IDictionary<string, object> data)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return value.Format(CultureInfo.CurrentCulture, data);
		}

		public static string Left(this string value, int width)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return value.Length <= width ? value: value.Substring(0, width);
		}

		public static string Right(this string value, int width)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return value.Length <= width ? value: value.Substring(value.Length - width, width);
		}

		public static string Slice(this string value, int left, int right)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (left < 0)
				left += value.Length;
			if (right < 0)
				right += value.Length;
			if (left < 0)
				left = 0;
			if (right < 0)
				right = 0;
			return left >= value.Length || left >= right ? "" : right >= value.Length ? value.Substring(left) : value.Substring(left, right - left);
		}

		public static string? TrimToNull(this string? value)
		{
			return value == null || (value = value.Trim()).Length == 0 ? null: value;
		}

		[return: NotNullIfNotNull(nameof(emptyValue))]
		public static unsafe string? TrimSpace(this string? value, string? emptyValue = null, string? space = null)
		{
			if (value == null)
				return emptyValue;
			fixed (char* buffer = value)
			{
				char* v = buffer;
				char* end = v + value.Length;
				while (v != end && (Char.IsControl(*v) || Char.IsWhiteSpace(*v)))
					++v;
				if (v == end)
					return emptyValue;
				if (space == null)
					space = " ";
				var result = new StringBuilder(value.Length);
				result.Append(*v);
				while (++v != end)
				{
					char c = *v;
					if (Char.IsControl(c) || Char.IsWhiteSpace(c))
					{
						do
						{
							if (++v == end)
								return result.ToString();
							c = *v;
						} while (Char.IsControl(c) || Char.IsWhiteSpace(c));
						result.Append(space);
					}
					result.Append(c);
				}
				return result.ToString();
			}
		}

		public static bool IsWhiteSpace(this string? value)
		{
			if (value == null)
				return true;
			for (int i = 0; i < value.Length; i++)
			{
				if (!Char.IsWhiteSpace(value[i]))
					return false;
			}
			return true;
		}

		public static string JoinWith(this string? left, string? right, string? delimiter = null, string? space = null)
		{
			if (left == null)
				left = "";
			if (right == null)
				right = "";
			if (delimiter == null)
			{
				delimiter = " ";
				if (space == null)
					space = "";
			}
			else
			{
				space += delimiter;
			}
			int leftIndex;
			for (leftIndex = left.Length; leftIndex > 0; --leftIndex)
			{
				char c = left[leftIndex - 1];
				if (!(Char.IsControl(c) || Char.IsWhiteSpace(c) || space.IndexOf(c) >= 0))
					break;
			}
			int rightIndex;
			for (rightIndex = 0; rightIndex < right.Length; ++rightIndex)
			{
				char c = right[rightIndex];
				if (!(Char.IsControl(c) || Char.IsWhiteSpace(c) || space.IndexOf(c) >= 0))
					break;
			}
			return leftIndex == 0 ? right.Substring(rightIndex):
				rightIndex == right.Length ? left.Substring(0, leftIndex):
				left.Substring(0, leftIndex) + delimiter + right.Substring(rightIndex);
		}

		#region String as Value

		public static byte AsByte(this string value)
		{
			return XmlTools.GetByte(value);
		}

		public static byte AsByte(this string? value, byte defaultValue)
		{
			return XmlTools.GetByte(value, defaultValue);
		}

		public static byte? AsByte(this string? value, byte? defaultValue)
		{
			return XmlTools.GetByte(value, defaultValue);
		}

		public static sbyte AsSByte(this string value)
		{
			return XmlTools.GetSByte(value);
		}

		public static sbyte AsSByte(this string? value, sbyte defaultValue)
		{
			return XmlTools.GetSByte(value, defaultValue);
		}

		public static sbyte? AsSByte(this string? value, sbyte? defaultValue)
		{
			return XmlTools.GetSByte(value, defaultValue);
		}

		public static Int16 AsInt16(this string value)
		{
			return XmlTools.GetInt16(value);
		}

		public static Int16 AsInt16(this string? value, Int16 defaultValue)
		{
			return XmlTools.GetInt16(value, defaultValue);
		}

		public static Int16? AsInt16(this string? value, Int16? defaultValue)
		{
			return XmlTools.GetInt16(value, defaultValue);
		}

		public static UInt16 AsUInt16(this string value)
		{
			return XmlTools.GetUInt16(value);
		}

		public static UInt16 AsUInt16(this string? value, UInt16 defaultValue)
		{
			return XmlTools.GetUInt16(value, defaultValue);
		}

		public static UInt16? AsUInt16(this string? value, UInt16? defaultValue)
		{
			return XmlTools.GetUInt16(value, defaultValue);
		}

		public static Int32 AsInt32(this string value)
		{
			return XmlTools.GetInt32(value);
		}

		public static Int32 AsInt32(this string? value, Int32 defaultValue)
		{
			return XmlTools.GetInt32(value, defaultValue);
		}

		public static Int32? AsInt32(this string? value, Int32? defaultValue)
		{
			return XmlTools.GetInt32(value, defaultValue);
		}

		public static UInt32 AsUInt32(this string value)
		{
			return XmlTools.GetUInt32(value);
		}

		public static UInt32 AsUInt32(this string? value, UInt32 defaultValue)
		{
			return XmlTools.GetUInt32(value, defaultValue);
		}

		public static UInt32? AsUInt32(this string? value, UInt32? defaultValue)
		{
			return XmlTools.GetUInt32(value, defaultValue);
		}

		public static Int64 AsInt64(this string value)
		{
			return XmlTools.GetInt64(value);
		}

		public static Int64 AsInt64(this string? value, Int64 defaultValue)
		{
			return XmlTools.GetInt64(value, defaultValue);
		}

		public static Int64? AsInt64(this string? value, Int64? defaultValue)
		{
			return XmlTools.GetInt64(value, defaultValue);
		}

		public static UInt64 AsUInt64(this string value)
		{
			return XmlTools.GetUInt64(value);
		}

		public static UInt64 AsUInt64(this string? value, UInt64 defaultValue)
		{
			return XmlTools.GetUInt64(value, defaultValue);
		}

		public static UInt64? AsUInt64(this string? value, UInt64? defaultValue)
		{
			return XmlTools.GetUInt64(value, defaultValue);
		}

		public static Single AsSingle(this string value)
		{
			return XmlTools.GetSingle(value);
		}

		public static Single AsSingle(this string? value, Single defaultValue)
		{
			return XmlTools.GetSingle(value, defaultValue);
		}

		public static Single? AsSingle(this string? value, Single? defaultValue)
		{
			return XmlTools.GetSingle(value, defaultValue);
		}

		public static Double AsDouble(this string value)
		{
			return XmlTools.GetDouble(value);
		}

		public static Double AsDouble(this string? value, Double defaultValue)
		{
			return XmlTools.GetDouble(value, defaultValue);
		}

		public static Double? AsDouble(this string? value, Double? defaultValue)
		{
			return XmlTools.GetDouble(value, defaultValue);
		}

		public static Decimal AsDecimal(this string value)
		{
			return XmlTools.GetDecimal(value);
		}

		public static Decimal AsDecimal(this string? value, decimal defaultValue)
		{
			return XmlTools.GetDecimal(value, defaultValue);
		}

		public static Decimal? AsDecimal(this string? value, decimal? defaultValue)
		{
			return XmlTools.GetDecimal(value, defaultValue);
		}

		public static Char AsChar(this string value)
		{
			return XmlTools.GetChar(value);
		}

		public static Char AsChar(this string? value, Char defaultValue)
		{
			return XmlTools.GetChar(value, defaultValue);
		}

		public static Char? AsChar(this string? value, Char? defaultValue)
		{
			return XmlTools.GetChar(value, defaultValue);
		}

		public static string? AsString(this string value)
		{
			return XmlTools.GetString(value, null);
		}

		[return: NotNullIfNotNull(nameof(defaultValue))]
		public static string? AsString(this string? value, string? defaultValue)
		{
			return XmlTools.GetString(value, defaultValue);
		}

		public static TimeSpan AsTimeSpan(this string value)
		{
			return XmlTools.GetTimeSpan(value);
		}

		public static TimeSpan AsTimeSpan(this string? value, TimeSpan defaultValue)
		{
			return XmlTools.GetTimeSpan(value, defaultValue);
		}

		public static TimeSpan? AsTimeSpan(this string? value, TimeSpan? defaultValue)
		{
			return XmlTools.GetTimeSpan(value, defaultValue);
		}

		public static DateTime AsDateTime(this string value)
		{
			return XmlTools.GetDateTime(value);
		}

		public static DateTime AsDateTime(this string? value, DateTime defaultValue)
		{
			return XmlTools.GetDateTime(value, defaultValue);
		}

		public static DateTime? AsDateTime(this string? value, DateTime? defaultValue)
		{
			return XmlTools.TryGetDateTime(value, out var result) ? result: defaultValue;
		}

		public static DateTimeOffset AsDateTimeOffset(this string value)
		{
			return XmlTools.GetDateTime(value);
		}

		public static DateTimeOffset AsDateTimeOffset(this string? value, DateTimeOffset defaultValue)
		{
			return XmlTools.GetDateTimeOffset(value, defaultValue);
		}

		public static DateTimeOffset? AsDateTimeOffset(this string? value, DateTimeOffset? defaultValue)
		{
			return XmlTools.TryGetDateTimeOffset(value, out var result) ? result : defaultValue;
		}

		public static Guid AsGuid(this string value)
		{
			return XmlTools.GetGuid(value);
		}

		public static Guid AsGuid(this string? value, Guid defaultValue)
		{
			return XmlTools.GetGuid(value, defaultValue);
		}

		public static Guid? AsGuid(this string? value, Guid? defaultValue)
		{
			return XmlTools.GetGuid(value, defaultValue);
		}

		public static T AsEnum<T>(this string value)
			where T: struct
		{
			return XmlTools.GetEnum<T>(value);
		}

		public static T AsEnum<T>(this string? value, T defaultValue)
			where T: struct
		{
			return XmlTools.GetEnum(value, defaultValue);
		}

		public static T? AsEnum<T>(this string? value, T? defaultValue)
			where T: struct
		{
			return XmlTools.GetEnum(value, defaultValue);
		}

		public static Boolean AsBoolean(this string value)
		{
			return XmlTools.GetBoolean(value);
		}

		public static Boolean AsBoolean(this string? value, Boolean defaultValue)
		{
			return XmlTools.GetBoolean(value, defaultValue);
		}

		public static Boolean? AsBoolean(this string? value, Boolean? defaultValue)
		{
			return XmlTools.TryGetBoolean(value, out bool result) ? result : defaultValue;
		}

		public static T AsValue<T>(this string value)
		{
			return XmlTools.GetValue<T>(value);
		}

		public static T AsValue<T>(this string? value, T defaultValue)
		{
			return XmlTools.GetValue(value, defaultValue);
		}

		public static object? AsValue(this string value, Type returnType)
		{
			return XmlTools.GetValue(value, returnType);
		}

		public static object? AsValue(this string? value, Type returnType, object? defaultValue)
		{
			return XmlTools.GetValue(value, returnType, defaultValue);
		}

		#endregion
	}
}
