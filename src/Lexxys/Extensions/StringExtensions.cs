// Lexxys Infrastructural library.
// file: StringExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Lexxys;

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

	public static string Format(this string value, IReadOnlyDictionary<string, object?>? data) => Format(value, null, data);

	public static string Format(this string value, IFormatProvider? formatProvider, IReadOnlyDictionary<string, object?>? data)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (data == null || data.Count == 0)
			return value;

		var text = new StringBuilder(value.Length + data.Count * 12);
		var template = value.AsSpan();

		while (template.Length > 0)
		{
			int k = template.IndexOf('{');
			if (k < 0)
			{
				text.Append(template);
				break;
			}
			text.Append(template.Slice(0, k));
			template = template.Slice(k + 1);
			if (template.Length > 0 && template[0] == '{')
			{
				text.Append('{');
				template = template.Slice(1);
				continue;
			}
			k = template.IndexOf('}');
			if (k < 0)
			{
				text.Append('{').Append(template);
				break;
			}
			var part = template.Slice(0, k);
			template = template.Slice(k + 1);
			k = part.IndexOf(':');
			string name;
			string format;
			if (k > 0)
			{
				format = part.Slice(k + 1).ToString();
				name = part.Slice(0, k).ToString();
			}
			else
			{
				format = String.Empty;
				name = part.ToString();
			}
			if (data.TryGetValue(name, out var v))
			{
				if (format.Length > 0 && v is IFormattable formattable)
					text.Append(formattable.ToString(format, formatProvider));
				else
					text.Append(v?.ToString() ?? String.Empty);
			}
			else
			{
				text.Append('{').Append(part).Append('}');
			}
		}
		return text.ToString();
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
			space ??= " ";
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
		left ??= "";
		right ??= "";
		if (delimiter == null)
		{
			delimiter = " ";
			space ??= "";
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
		return Strings.GetByte(value);
	}

	public static byte AsByte(this string? value, byte defaultValue)
	{
		return Strings.GetByte(value, defaultValue);
	}

	public static byte? AsByte(this string? value, byte? defaultValue)
	{
		return Strings.GetByte(value, defaultValue);
	}

	public static sbyte AsSByte(this string value)
	{
		return Strings.GetSByte(value);
	}

	public static sbyte AsSByte(this string? value, sbyte defaultValue)
	{
		return Strings.GetSByte(value, defaultValue);
	}

	public static sbyte? AsSByte(this string? value, sbyte? defaultValue)
	{
		return Strings.GetSByte(value, defaultValue);
	}

	public static Int16 AsInt16(this string value)
	{
		return Strings.GetInt16(value);
	}

	public static Int16 AsInt16(this string? value, Int16 defaultValue)
	{
		return Strings.GetInt16(value, defaultValue);
	}

	public static Int16? AsInt16(this string? value, Int16? defaultValue)
	{
		return Strings.GetInt16(value, defaultValue);
	}

	public static UInt16 AsUInt16(this string value)
	{
		return Strings.GetUInt16(value);
	}

	public static UInt16 AsUInt16(this string? value, UInt16 defaultValue)
	{
		return Strings.GetUInt16(value, defaultValue);
	}

	public static UInt16? AsUInt16(this string? value, UInt16? defaultValue)
	{
		return Strings.GetUInt16(value, defaultValue);
	}

	public static Int32 AsInt32(this string value)
	{
		return Strings.GetInt32(value);
	}

	public static Int32 AsInt32(this string? value, Int32 defaultValue)
	{
		return Strings.GetInt32(value, defaultValue);
	}

	public static Int32? AsInt32(this string? value, Int32? defaultValue)
	{
		return Strings.GetInt32(value, defaultValue);
	}

	public static UInt32 AsUInt32(this string value)
	{
		return Strings.GetUInt32(value);
	}

	public static UInt32 AsUInt32(this string? value, UInt32 defaultValue)
	{
		return Strings.GetUInt32(value, defaultValue);
	}

	public static UInt32? AsUInt32(this string? value, UInt32? defaultValue)
	{
		return Strings.GetUInt32(value, defaultValue);
	}

	public static Int64 AsInt64(this string value)
	{
		return Strings.GetInt64(value);
	}

	public static Int64 AsInt64(this string? value, Int64 defaultValue)
	{
		return Strings.GetInt64(value, defaultValue);
	}

	public static Int64? AsInt64(this string? value, Int64? defaultValue)
	{
		return Strings.GetInt64(value, defaultValue);
	}

	public static UInt64 AsUInt64(this string value)
	{
		return Strings.GetUInt64(value);
	}

	public static UInt64 AsUInt64(this string? value, UInt64 defaultValue)
	{
		return Strings.GetUInt64(value, defaultValue);
	}

	public static UInt64? AsUInt64(this string? value, UInt64? defaultValue)
	{
		return Strings.GetUInt64(value, defaultValue);
	}

	public static Single AsSingle(this string value)
	{
		return Strings.GetSingle(value);
	}

	public static Single AsSingle(this string? value, Single defaultValue)
	{
		return Strings.GetSingle(value, defaultValue);
	}

	public static Single? AsSingle(this string? value, Single? defaultValue)
	{
		return Strings.GetSingle(value, defaultValue);
	}

	public static Double AsDouble(this string value)
	{
		return Strings.GetDouble(value);
	}

	public static Double AsDouble(this string? value, Double defaultValue)
	{
		return Strings.GetDouble(value, defaultValue);
	}

	public static Double? AsDouble(this string? value, Double? defaultValue)
	{
		return Strings.GetDouble(value, defaultValue);
	}

	public static Decimal AsDecimal(this string value)
	{
		return Strings.GetDecimal(value);
	}

	public static Decimal AsDecimal(this string? value, decimal defaultValue)
	{
		return Strings.GetDecimal(value, defaultValue);
	}

	public static Decimal? AsDecimal(this string? value, decimal? defaultValue)
	{
		return Strings.GetDecimal(value, defaultValue);
	}

	public static Char AsChar(this string value)
	{
		return Strings.GetChar(value);
	}

	public static Char AsChar(this string? value, Char defaultValue)
	{
		return Strings.GetChar(value, defaultValue);
	}

	public static Char? AsChar(this string? value, Char? defaultValue)
	{
		return Strings.GetChar(value, defaultValue);
	}

	public static string? AsString(this string value)
	{
		return Strings.GetString(value, null);
	}

	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static string? AsString(this string? value, string? defaultValue)
	{
		return Strings.GetString(value, defaultValue);
	}

	public static TimeSpan AsTimeSpan(this string value)
	{
		return Strings.GetTimeSpan(value);
	}

	public static TimeSpan AsTimeSpan(this string? value, TimeSpan defaultValue)
	{
		return Strings.GetTimeSpan(value, defaultValue);
	}

	public static TimeSpan? AsTimeSpan(this string? value, TimeSpan? defaultValue)
	{
		return Strings.GetTimeSpan(value, defaultValue);
	}

	public static DateTime AsDateTime(this string value)
	{
		return Strings.GetDateTime(value);
	}

	public static DateTime AsDateTime(this string? value, DateTime defaultValue)
	{
		return Strings.GetDateTime(value, defaultValue);
	}

	public static DateTime? AsDateTime(this string? value, DateTime? defaultValue)
	{
		return Strings.TryGetDateTime(value, out var result) ? result: defaultValue;
	}

	public static DateTimeOffset AsDateTimeOffset(this string value)
	{
		return Strings.GetDateTime(value);
	}

	public static DateTimeOffset AsDateTimeOffset(this string? value, DateTimeOffset defaultValue)
	{
		return Strings.GetDateTimeOffset(value, defaultValue);
	}

	public static DateTimeOffset? AsDateTimeOffset(this string? value, DateTimeOffset? defaultValue)
	{
		return Strings.TryGetDateTimeOffset(value, out var result) ? result : defaultValue;
	}

	public static Guid AsGuid(this string value)
	{
		return Strings.GetGuid(value);
	}

	public static Guid AsGuid(this string? value, Guid defaultValue)
	{
		return Strings.GetGuid(value, defaultValue);
	}

	public static Guid? AsGuid(this string? value, Guid? defaultValue)
	{
		return Strings.GetGuid(value, defaultValue);
	}

	public static T AsEnum<T>(this string value)
		where T: struct
	{
		return Strings.GetEnum<T>(value);
	}

	public static T AsEnum<T>(this string? value, T defaultValue)
		where T: struct
	{
		return Strings.GetEnum(value, defaultValue);
	}

	public static T? AsEnum<T>(this string? value, T? defaultValue)
		where T: struct
	{
		return Strings.GetEnum(value, defaultValue);
	}

	public static Boolean AsBoolean(this string value)
	{
		return Strings.GetBoolean(value);
	}

	public static Boolean AsBoolean(this string? value, Boolean defaultValue)
	{
		return Strings.GetBoolean(value, defaultValue);
	}

	public static Boolean? AsBoolean(this string? value, Boolean? defaultValue)
	{
		return Strings.TryGetBoolean(value, out bool result) ? result : defaultValue;
	}

	public static T AsValue<T>(this string value)
	{
		return Strings.GetValue<T>(value);
	}

	public static T AsValue<T>(this string? value, T defaultValue)
	{
		return Strings.GetValue(value, defaultValue);
	}

	public static object? AsValue(this string value, Type returnType)
	{
		return Strings.GetValue(value, returnType);
	}

	public static object? AsValue(this string? value, Type returnType, object? defaultValue)
	{
		return Strings.GetValue(value, returnType, defaultValue);
	}

#if NET7_0_OR_GREATER

	public static T Parse<T>(this string? value, T defaultValue) where T: IParsable<T>
	{
		return T.TryParse(value, null, out var result) ? result: defaultValue;
	}

	public static T Parse<T>(this string value) where T: IParsable<T>
	{
		return T.Parse(value, null);
	}

#endif


	#endregion
}
