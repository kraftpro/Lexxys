// Lexxys Infrastructural library.
// file: XmlTools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

#pragma warning disable 3002, 3001	// CLS-compliant

namespace Lexxys.Xml
{
	public static class XmlTools
	{
		private static Logger Log => __logger ??= new Logger("XmlTools");
		private static Logger __logger;

		public const string OptionIgnoreCase = "opt:ignoreCase";
		public const string OptionForceAttributes = "opt:forceAttributes";

		private const int MaxStackAllocSize = 4096;

		#region Convert Primitives

		public static string Convert(bool value)
		{
			return value ? "true": "false";
		}

		public static string Convert(Guid value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(Guid? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(DateTime value, XmlDateTimeSerializationMode mode = XmlDateTimeSerializationMode.Unspecified)
		{
			string result = XmlConvert.ToString(value, mode);
			return result.EndsWith("T00:00:00", StringComparison.Ordinal) ? result.Substring(0, result.Length - 9): result;
		}

		public static string Convert(DateTime? value, XmlDateTimeSerializationMode mode = XmlDateTimeSerializationMode.Unspecified)
		{
			return value.HasValue ? Convert(value.GetValueOrDefault(), mode): "";
		}

		public static string Convert(DateTime value, string format)
		{
			return XmlConvert.ToString(value, format);
		}

		public static string Convert(DateTime? value, string format)
		{
			return value.HasValue ? Convert(value.GetValueOrDefault(), format): "";
		}

		public static string Convert(DateTimeOffset value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(DateTimeOffset? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(DateTimeOffset value, string format)
		{
			return XmlConvert.ToString(value, format);
		}

		public static string Convert(DateTimeOffset? value, string format)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault(), format): "";
		}

		public static string Convert(TimeSpan value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(TimeSpan? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(sbyte value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(sbyte? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(byte value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(byte? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(short value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(short? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(ushort value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(ushort? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(int value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(int? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(uint value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(uint? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(long value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(long? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(ulong value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(ulong? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(decimal value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(decimal? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(float value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(float? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(double value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(double? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()): "";
		}

		public static string Convert(object value, bool encodeQuote = false)
		{
			if (value == null)
				return "";

			if (value is IConvertible cn)
			{
				switch (cn.GetTypeCode())
				{
					case TypeCode.Boolean:
						return Convert((bool)value);
					case TypeCode.Byte:
						return XmlConvert.ToString((byte)value);
					case TypeCode.SByte:
						return XmlConvert.ToString((sbyte)value);
					case TypeCode.Char:
						return XmlConvert.ToString((char)value);
					case TypeCode.Decimal:
						return XmlConvert.ToString((decimal)value);
					case TypeCode.Single:
						return XmlConvert.ToString((float)value);
					case TypeCode.Double:
						return XmlConvert.ToString((double)value);
					case TypeCode.Int16:
						return XmlConvert.ToString((short)value);
					case TypeCode.UInt16:
						return XmlConvert.ToString((ushort)value);
					case TypeCode.Int32:
						return XmlConvert.ToString((int)value);
					case TypeCode.UInt32:
						return XmlConvert.ToString((uint)value);
					case TypeCode.Int64:
						return XmlConvert.ToString((long)value);
					case TypeCode.UInt64:
						return XmlConvert.ToString((ulong)value);
					case TypeCode.DBNull:
					case TypeCode.Empty:
						return "";
					case TypeCode.DateTime:
						return Convert((DateTime)value);
					case TypeCode.String:
						return Encode((string)value, encodeQuote);
				}
			}
			return value switch
			{
				TimeSpan ts => Convert(ts),
				Guid guid => Convert(guid),
				DateTimeOffset dto => Convert(dto),
				byte[] ba => System.Convert.ToBase64String(ba),
				char[] ca => Encode(ca),
				_ => null,
			};
		}

		public static string ConvertCollection(IEnumerable collection, bool braces = false)
		{
			return ConvertCollection(new StringBuilder(), collection, braces).ToString();
		}

		public static StringBuilder ConvertCollection(StringBuilder text, IEnumerable collection, bool braces = false)
		{
			if (collection == null)
				return text;
			if (braces)
				text.Append("(");
			string pad = "";
			foreach (var item in collection)
			{
				text.Append(pad);
				string s = Convert(item);
				if (s == null)
					ConvertCollection(text, item as IEnumerable, true);
				else
					text.Append(s);
				pad = ",";
			}
			if (braces)
				text.Append(")");
			return text;
		}

		public static unsafe StringBuilder Encode(StringBuilder text, string value, bool encodeQuote = false)
		{
			if (value == null)
				return text;

			fixed (char* source = value)
			{
				return Encode(text, source, value.Length, encodeQuote) ?? text.Append(value);
			}
		}

		public static unsafe StringBuilder Encode(StringBuilder text, string value, int start, int length, bool encodeQuote = false)
		{
			if (value == null || length <= 0 || value.Length <= start || start < 0)
				return text;
			if (length > value.Length - start)
				length = value.Length - start;
			fixed (char* source = value)
			{
				return Encode(text, source + start, length, encodeQuote) ?? text.Append(value, start, length);
			}
		}

		private static unsafe StringBuilder Encode(StringBuilder text, char* source, int length, bool encodeQuote = false)
		{
			(int k, int first) = EncodeSize(source, length, encodeQuote);
			if (k == length)
				return null;

			if (k <= MaxStackAllocSize / sizeof(char))
			{
				char* target = stackalloc char[k];
				return text.Append(target, EncodeMemory(source, target, first, length, encodeQuote));
			}
			fixed (char* target = new char[k])
			{
				return text.Append(target, EncodeMemory(source, target, first, length, encodeQuote));
			}
		}


		public static unsafe string Encode(string value, bool encodeQuote = false)
		{
			if (value == null)
				return "";

			fixed (char* source = value)
			{
				return Encode(source, value.Length, encodeQuote) ?? value;
			}
		}

		public static unsafe string Encode(string value, int start, int length, bool encodeQuote = false)
		{
			if (value == null || length <= 0 || value.Length <= start || start < 0)
				return "";
			if (length > value.Length - start)
				length = value.Length - start;
			fixed (char* source = value)
			{
				return Encode(source + start, length, encodeQuote) ?? value.Substring(start, length);
			}
		}

		public static unsafe string Encode(char[] value, bool encodeQuote = false)
		{
			if (value == null)
				return "";

			fixed (char* source = value)
			{
				return Encode(source, value.Length, encodeQuote) ?? new String(value);
			}
		}

		public static unsafe string Encode(char[] value, int start, int length, bool encodeQuote = false)
		{
			if (value == null || length <= 0 || value.Length <= start || start < 0)
				return "";
			if (length > value.Length - start)
				length = value.Length - start;

			fixed (char* source = value)
			{
				return Encode(source + start, Math.Min(length, value.Length - start), encodeQuote) ?? new String(value, start, length);
			}
		}

		private static unsafe string Encode(char* source, int length, bool encodeQuote = false)
		{
			(int k, int first) = EncodeSize(source, length, encodeQuote);
			if (k == length)
				return null;
			if (k <= MaxStackAllocSize / sizeof(char))
			{
				char* target = stackalloc char[k];
				return new String(target, 0, EncodeMemory(source, target, first, length, encodeQuote));
			}
			fixed (char* target = new char[k])
			{
				return new String(target, 0, EncodeMemory(source, target, first, length, encodeQuote));
			}
		}

		private static unsafe (int Length, int First) EncodeSize(char* source, int length, bool encodeQuote)
		{
			char* s = source;
			int k = 0;
			int first = -1;
			for (int i = 0; i < length; ++i)
			{
				switch (*s)
				{
					case '<':
					case '>':
						k += 3;
						if (first == -1)
							first = i;
						break;

					case '&':
						k += 4;
						if (first == -1)
							first = i;
						break;

					case '"':
						if (encodeQuote)
						{
							k += 5;
							if (first == -1)
								first = i;
						}
						break;
				}
				++s;
			}
			return (k + length, first);
		}

		private static unsafe int EncodeMemory(char* source, char* target, int skip, int count, bool encodeQuote)
		{
			char* s = source + skip;
			char* t = target + skip;
			char* e = source + count;
			Memcpy((byte*)source, (byte*)target, skip * sizeof(char));
			while (s != e)
			{
				char c = *s++;
				switch (c)
				{
					case '<':
						*t++ = '&';
						*t++ = 'l';
						*t++ = 't';
						*t++ = ';';
						break;
					case '>':
						*t++ = '&';
						*t++ = 'g';
						*t++ = 't';
						*t++ = ';';
						break;
					case '&':
						*t++ = '&';
						*t++ = 'a';
						*t++ = 'm';
						*t++ = 'p';
						*t++ = ';';
						break;
					case '"':
						if (encodeQuote)
						{
							*t++ = '&';
							*t++ = 'q';
							*t++ = 'u';
							*t++ = 'o';
							*t++ = 't';
							*t++ = ';';
						}
						else
						{
							*t++ = c;
						}
						break;

					default:
						*t++ = c;
						break;
				}
			}
			return (int)(t - target);
		}

		public static unsafe StringBuilder Decode(StringBuilder text, string value)
		{
			int i = value.IndexOf('&');
			if (i < 0)
				return text.Append(value);
			fixed (char* source = value)
			{
				int n = value.Length;
				if (n <= MaxStackAllocSize / sizeof(char))
				{
					char* target = stackalloc char[n];
					return text.Append(target, DecodeMemory(source, target, i, n));
				}
				fixed (char* target = new char[n])
				{
					return text.Append(target, DecodeMemory(source, target, i, n));
				}
			}
		}

		public static unsafe string Decode(string value)
		{
			int i = value.IndexOf('&');
			if (i < 0)
				return value;
			fixed (char* source = value)
			{
				int n = value.Length;
				if (n <= MaxStackAllocSize/sizeof(char))
				{
					char* target = stackalloc char[n];
					return new String(target, 0, DecodeMemory(source, target, i, n));
				}
				fixed (char* target = new char[n])
				{
					return new String(target, 0, DecodeMemory(source, target, i, n));
				}
			}
		}

		private static unsafe int DecodeMemory(char* source, char* target, int start, int count)
		{
			Memcpy((byte*)source, (byte*)target, start * sizeof(char));
			char* s = source + start;
			char* t = target + start;
			int left = count - start;
			while (left > 0)
			{
				if (s[0] != '&')
				{
					*t++ = *s++;
					--left;
					continue;
				}

				switch (s[1])
				{
					case 'l':
						if (left >= 4 && s[2] == 't' && s[3] == ';')
						{
							s += 4;
							left -= 4;
							*t++ = '<';
							continue;
						}
						break;
					case 'g':
						if (left >= 4 && s[2] == 't' && s[3] == ';')
						{
							s += 4;
							left -= 4;
							*t++ = '>';
							continue;
						}
						break;
					case 'a':
						if (left >= 5 && s[2] == 'm' && s[3] == 'p' && s[4] == ';')
						{
							s += 5;
							left -= 5;
							*t++ = '&';
							continue;
						}
						if (left >= 6 && s[2] == 'p' && s[3] == 'o' && s[4] == 's' && s[5] == ';')
						{
							s += 6;
							left -= 6;
							*t++ = '\'';
							continue;
						}
						break;
					case 'q':
						if (left >= 6 && s[2] == 'u' && s[3] == 'o' && s[4] == 't' && s[5] == ';')
						{
							s += 6;
							left -= 6;
							*t++ = '"';
							continue;
						}
						break;
				}
				*t++ = *s++;
				--left;
			}
			return (int)(t - target);
		}

		private static unsafe void Memcpy(byte* source, byte* target, int count)
		{
			if (count >= 16)
			{
				do
				{
					*(int*)target = *(int*)source;
					*(int*)(target + 4) = *(int*)(source + 4);
					*(int*)(target + 8) = *(int*)(source + 8);
					*(int*)(target + 12) = *(int*)(source + 12);
					target += 16;
					source += 16;
				}
				while ((count -= 16) >= 16);
			}
			if (count <= 0)
				return;
			if ((count & 8) != 0)
			{
				*(int*)target = *(int*)source;
				*(int*)(target + 4) = *(int*)(source + 4);
				target += 8;
				source += 8;
			}
			if ((count & 4) != 0)
			{
				*(int*)target = *(int*)source;
				target += 4;
				source += 4;
			}
			if ((count & 2) != 0)
			{
				*(short*)target = *(short*)source;
				target += 2;
				source += 2;
			}
			if ((count & 1) != 0)
				*target = *source;
		}

		#endregion

		#region Parse Primitives

		public static byte GetByte(string value)
		{
			return Byte.TryParse(value, out byte result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static byte GetByte(string value, byte defaultValue)
		{
			return Byte.TryParse(value, out byte result) ? result: defaultValue;
		}

		public static byte? GetByte(string value, byte? defaultValue)
		{
			return Byte.TryParse(value, out byte result) ? result: defaultValue;
		}

		public static sbyte GetSByte(string value)
		{
			return SByte.TryParse(value, out sbyte result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static sbyte GetSByte(string value, sbyte defaultValue)
		{
			return SByte.TryParse(value, out sbyte result) ? result: defaultValue;
		}

		public static sbyte? GetSByte(string value, sbyte? defaultValue)
		{
			return SByte.TryParse(value, out sbyte result) ? result : defaultValue;
		}

		public static short GetInt16(string value)
		{
			return Int16.TryParse(value, out short result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static short GetInt16(string value, short defaultValue)
		{
			return Int16.TryParse(value, out short result) ? result: defaultValue;
		}

		public static short? GetInt16(string value, short? defaultValue)
		{
			return Int16.TryParse(value, out short result) ? result: defaultValue;
		}

		public static ushort GetUInt16(string value)
		{
			return UInt16.TryParse(value, out ushort result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static ushort GetUInt16(string value, ushort defaultValue)
		{
			return UInt16.TryParse(value, out ushort result) ? result: defaultValue;
		}

		public static ushort? GetUInt16(string value, ushort? defaultValue)
		{
			return UInt16.TryParse(value, out ushort result) ? result : defaultValue;
		}

		public static int GetInt32(string value)
		{
			return Int32.TryParse(value, out int result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static int GetInt32(string value, int defaultValue)
		{
			return Int32.TryParse(value, out int result) ? result: defaultValue;
		}

		public static int? GetInt32(string value, int? defaultValue)
		{
			return Int32.TryParse(value, out int result) ? result: defaultValue;
		}

		public static int GetInt32(string value, int defaultValue, int minValue, int maxValue)
		{
			return !Int32.TryParse(value, out int result) ? defaultValue: result < minValue ? minValue: result > maxValue ? maxValue: result;
		}

		public static uint GetUInt32(string value)
		{
			return UInt32.TryParse(value, out uint result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static uint GetUInt32(string value, uint defaultValue)
		{
			return UInt32.TryParse(value, out uint result) ? result: defaultValue;
		}

		public static uint? GetUInt32(string value, uint? defaultValue)
		{
			return UInt32.TryParse(value, out uint result) ? result: defaultValue;
		}

		public static long GetInt64(string value)
		{
			return Int64.TryParse(value, out long result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static long GetInt64(string value, long defaultValue)
		{
			return Int64.TryParse(value, out long result) ? result: defaultValue;
		}

		public static long? GetInt64(string value, long? defaultValue)
		{
			return Int64.TryParse(value, out long result) ? result: defaultValue;
		}

		public static ulong GetUInt64(string value)
		{
			return UInt64.TryParse(value, out ulong result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static ulong GetUInt64(string value, ulong defaultValue)
		{
			return UInt64.TryParse(value, out ulong result) ? result: defaultValue;
		}

		public static ulong? GetUInt64(string value, ulong? defaultValue)
		{
			return UInt64.TryParse(value, out ulong result) ? result: defaultValue;
		}

		public static float GetSingle(string value)
		{
			return Single.TryParse(value, out float result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static float GetSingle(string value, float defaultValue)
		{
			return Single.TryParse(value, out float result) ? result: defaultValue;
		}

		public static float? GetSingle(string value, float? defaultValue)
		{
			return Single.TryParse(value, out float result) ? result: defaultValue;
		}

		public static double GetDouble(string value)
		{
			return Double.TryParse(value, out double result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static double GetDouble(string value, double defaultValue)
		{
			return Double.TryParse(value, out double result) ? result: defaultValue;
		}

		public static double? GetDouble(string value, double? defaultValue)
		{
			return Double.TryParse(value, out double result) ? result: defaultValue;
		}

		public static decimal GetDecimal(string value)
		{
			return Decimal.TryParse(value, out decimal result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static decimal GetDecimal(string value, decimal defaultValue)
		{
			return Decimal.TryParse(value, out decimal result) ? result: defaultValue;
		}

		public static decimal? GetDecimal(string value, decimal? defaultValue)
		{
			return Decimal.TryParse(value, out decimal result) ? result : defaultValue;
		}

		public static char GetChar(string value)
		{
			return TryGetChar(value, out char result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static char GetChar(string value, char defaultValue)
		{
			return TryGetChar(value, out char result) ? result: defaultValue;
		}

		public static char? GetChar(string value, char? defaultValue)
		{
			return TryGetChar(value, out char result) ? result : defaultValue;
		}

		public static bool TryGetChar(string value, out char result)
		{
			if (value == null)
			{
				result = '\0';
				return false;
			}
			if (value.Length != 1)
			{
				value = value.Trim();
				if (value.Length != 1)
				{
					result = ' ';
					return false;
				}
			}
			result = value[0];
			return true;
		}

		public static TimeSpan GetTimeSpan(string value)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static TimeSpan GetTimeSpan(string value, TimeSpan defaultValue)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result: defaultValue;
		}

		public static TimeSpan? GetTimeSpan(string value, TimeSpan? defaultValue)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result: defaultValue;
		}

		public static TimeSpan GetTimeSpan(string value, TimeSpan defaultValue, TimeSpan minValue, TimeSpan maxValue)
		{
			return !TryGetTimeSpan(value, out TimeSpan result) ? defaultValue: result < minValue ? minValue: result > maxValue ? maxValue: result;
		}

		/// <summary>
		/// Converts string representation of time span into <see cref="System.TimeSpan"/>.
		///	  syntax: [P] [days 'D'] [T] [hours 'H'][minutes 'M'][seconds 'S'][milliseconds 'MS']
		///     [[[days] hours:]minutes:]seconds [AM/PM]
		/// </summary>
		/// <param name="value">A string to convert.</param>
		/// <param name="result">result of conversion.</param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryGetTimeSpan(string value, out TimeSpan result)
		{
			result = new TimeSpan();
			if (value == null || value.Length == 0)
				return false;

			var text = new Tokenizer.CharStream(value, false, 1);
			var last = new NumberScale();
			var temp = new TimeSpan();

			bool MatchNumber(char ch, int pos) => last.Append(ch, pos);
			static bool Space(char c) => c <= ' ';

			if (text[0] == 'P' || text[0] == 'p')
				text.Forward(1, Space);
			else
				text.Forward(Space);
			text.Match(MatchNumber);
			if (!last.HasValue)
			{
				if (last.Size != 0)
					return false;
				if (text[0] != 'T' && text[0] != 't')
					return false;
			}
			text.Forward(last.Size, Space);

			if (text.Eof)
			{
				result = last.Time(TimeSpan.TicksPerSecond);
				return true;
			}
			char c0 = text[0];
			if (c0 == ':' || (c0 >= '0' && c0 <= '9'))
				goto ShortFormat;

			if (c0 == 'D' || c0 == 'd')
			{
				temp = last.Time(TimeSpan.TicksPerDay);
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				text.Forward(last.Size, Space);
				c0 = text[0];
				if (!last.HasValue)
				{
					if (last.Size != 0)
						return false;
					if (c0 != 'T' && c0 != 't')
						return false;
				}
			}

			if (c0 == 'T' || c0 == 't')
			{
				if (last.HasValue)
					return false;
				text.Forward(1, Space);
				if (text.Eof)
					return false;
				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if (c0 == 'H' || c0 == 'h')
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerHour));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if ((c0 == 'M' || c0 == 'm') && !(text[1] == 'S' || text[1] == 's'))
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerMinute));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if (c0 == 'S' || c0 == 's')
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerSecond));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if ((c0 == 'M' || c0 == 'm') && (text[1] == 'S' || text[1] == 's'))
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerMillisecond));
				text.Forward(2, Space);
			}
			else
			{
				return false;
			}

			if (!text.Eof)
				return false;

			result = temp;
			return true;


		ShortFormat:

			TimeSpan days;
			NumberScale part1;
			bool hoursRequired = false;

			// [[days] hours:]minutes:seconds
			if (text[0] == ':')
			{
				// [hours:]minutes:seconds
				days = new TimeSpan();
				part1 = last;
			}
			else // text[0] >= '0' && text[0] <= '9'
			{
				// days hours:minutes:seconds

				hoursRequired = true;
				days = last.Time(TimeSpan.TicksPerDay);

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				if (text[0] != ':')
					return false;
				part1 = last;

				// {days} {hours} :minutes:seconds
			}

			// {days} {hours} :minutes:seconds
			// {minutes} :seconds
			text.Forward(1, Space);
			last.Reset();
			text.Match(MatchNumber);
			if (!last.HasValue)
				return false;
			text.Forward(last.Size, Space);

			if (text.Eof)
			{
				if (hoursRequired)
					return false;
				if (part1.Point)
					// days.hours:minutes
					result = new TimeSpan(part1.Left * TimeSpan.TicksPerDay)
						.Add(new TimeSpan(part1.Right * TimeSpan.TicksPerHour))
						.Add(last.Time(TimeSpan.TicksPerMinute));
				else
					// minutes:seconds
					result = part1.Time(TimeSpan.TicksPerMinute)
						.Add(last.Time(TimeSpan.TicksPerSecond));
				return true;
			}
			if (text[0] != ':')
				return false;

			NumberScale part2 = last;
			// :seconds
			text.Forward(1, Space);
			last.Reset();
			text.Match(MatchNumber);
			if (!last.HasValue)
				return false;
			text.Forward(last.Size, Space);
			bool pm = false;
			if (!text.Eof)
			{
				if (text.Length != 2)
					return false;
				string ap = text.Substring(0, 2);
				if (String.Equals(ap, "PM", StringComparison.OrdinalIgnoreCase))
					pm = true;
				else if (!String.Equals(ap, "AM", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (!hoursRequired && part1.Point) // days.hours:minutes:seconds
			{
				days = new TimeSpan(part1.Left * TimeSpan.TicksPerDay);
				part1 = new NumberScale(part1.Right, 0, part1.Size - part1.Scale - 1, 0);
			}

			result = days.Add(part1.Time(TimeSpan.TicksPerHour)).Add(part2.Time(TimeSpan.TicksPerMinute)).Add(last.Time(TimeSpan.TicksPerSecond));
			if (pm)
			{
				if (result.Hours == 12)
					result -= TimeSpan.FromHours(12);
				else if (result.Hours < 12)
					result += TimeSpan.FromHours(12);
			}
			return true;
		}

		[DebuggerDisplay("Number = {_left},{_right}; Scale={_scale}; Point={_point}")]
		private struct NumberScale
		{
			private int _width;
			private long _left;
			private long _right;
			private int _scale;
			private bool _point;
			private static readonly long[] ScaleTable;
			private static readonly long[] OverflowTable;
			private const int ScaleLength = 19;

			static NumberScale()
			{
				ScaleTable = new long[ScaleLength];
				OverflowTable = new long[ScaleLength];
				long x = 1;
				for (int i = 0; i < ScaleTable.Length; ++i)
				{
					ScaleTable[i] = x;
					OverflowTable[i] = Int64.MaxValue / x;
					x *= 10;
				}
			}

			public NumberScale(long left, long right, int width, int scale)
			{
				_left = left;
				_right = right;
				_width = width;
				_scale = scale;
				_point = scale > 0;
			}

			public void Reset()
			{
				_width = 0;
				_left = 0;
				_right = 0;
				_scale = 0;
				_point = false;
			}

			public long Left => _left;
			public long Right => _right;
			public int Scale => _scale;
			public bool Point => _point;
			public int Size => _width;
			public decimal Value => _left + (decimal)_right / ScaleTable[_scale];

			public bool Append(char value, int position)
			{
				if (value < '0' || value > '9')
				{
					if (value != '.' || _point)
						return false;
					_point = true;
					++_width;
					return true;
				}
				if (_point)
				{
					if (_scale >= ScaleLength - 1)
					{
						_scale = ~_scale;
						return false;
					}
					++_scale;
					_right = _right * 10 + (value - '0');
				}
				else
				{
					if (position >= ScaleLength && _left > (Int64.MaxValue - (value - '0'))/10)
					{
						_scale = ~_scale;
						return false;
					}
					_left = _left * 10 + (value - '0');
				}
				++_width;
				return true;
			}

			[Pure]
			public bool HasValue => _scale >= 0 && (_width > 1 || _width == 1 && !_point);

			[Pure]
			public TimeSpan Time(long ticksPerItem)
			{
				return ticksPerItem < OverflowTable[_scale] ?
					new TimeSpan(_left * ticksPerItem + _right * ticksPerItem / ScaleTable[_scale]):
					new TimeSpan(_left * ticksPerItem + (long)((decimal)_right * ticksPerItem / ScaleTable[_scale] + 0.5m));
			}
		}

		public static DateTime GetDateTime(string value)
		{
			return TryGetDateTime(value, out DateTime result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static DateTime GetDateTime(string value, DateTime defaultValue)
		{
			return TryGetDateTime(value, out DateTime result) ? result: defaultValue;
		}

		public static DateTime GetDateTime(string value, DateTime defaultValue, DateTime minValue, DateTime maxValue)
		{
			return !TryGetDateTime(value, out DateTime result) ? defaultValue : result < minValue ? minValue: result > maxValue ? maxValue: result;
		}

		public static bool TryGetDateTime(string value, out DateTime result)
		{
			if (!TryGetDateTimeOffset(value, out DateTimeOffset dto, out bool zone))
			{
				result = new DateTime();
				return false;
			}
			result = !zone ? dto.DateTime: dto.Offset == TimeSpan.Zero ? dto.UtcDateTime: dto.LocalDateTime;
			return true;
		}

		public static DateTimeOffset GetDateTimeOffset(string value)
		{
			return TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static DateTimeOffset GetDateTimeOffset(string value, DateTimeOffset defaultValue)
		{
			return TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? result: defaultValue;
		}

		public static DateTimeOffset GetDateTimeOffset(string value, DateTimeOffset defaultValue, DateTimeOffset minValue, DateTimeOffset maxValue)
		{
			return !TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? defaultValue: result < minValue ? minValue: result > maxValue ? maxValue: result;
		}

		public static bool TryGetDateTimeOffset(string value, out DateTimeOffset result)
		{
			return TryGetDateTimeOffset(value, out result, out _);
		}

		private static int MatchTwo(Tokenizer.CharStream text)
		{
			char a = text[0];
			char b = text[1];
			if (a < '0' || a > '9' || b < '0' || b > '9')
				return -1;
			text.Forward(2);
			return (a - '0') * 10 + (b - '0');
		}

		/// <summary>
		/// Converts string representation of date and time into <see cref="System.DateTimeOffset"/>.
		///   syntax: [yyyy[-]mm[-]dd] [T] [hh[:]mm[:]ss[.ccc]] [zone]
		/// </summary>
		/// <param name="value">A string to convert.</param>
		/// <param name="result">result of conversion.</param>
		///	<param name="timeZone">Time zone indicator</param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryGetDateTimeOffset(string value, out DateTimeOffset result, out bool timeZone)
		{
			result = new DateTimeOffset();
			timeZone = false;
			if (value == null || value.Length == 0)
				return false;

			static bool Space(char c) => c <= ' ';

			var text = new Tokenizer.CharStream(value, false, 1);
			int year = 1;
			int month = 1;
			int day = 1;

			text.Forward(Space);

			if (text[0] == 'T' || text[0] == 't')
			{
				text.Forward(1, Space);
				goto SetHour;
			}
			if (text[2] == ':')
				goto SetHour;

			int x = MatchTwo(text);
			if (x < 0)
				return false;
			year = MatchTwo(text);
			if (year < 0)
				return false;
			year += x * 100;
			if (year < 1)
				return false;
			bool delimiter = text[0] == '-';
			if (delimiter)
				text.Forward(1);
			month = MatchTwo(text);
			if (month < 1 || month > 12)
				return false;
			if (delimiter)
				if (text[0] == '-')
					text.Forward(1);
				else
					return false;
			day = MatchTwo(text);
			if (day < 1 || (day > 28 && day > DateTime.DaysInMonth(year, month)))
				return false;

			text.Forward(Space);
			if (text[0] == 'T' || text[0] == 't')
				text.Forward(1, Space);

			if (text.Eof)
			{
				result = new DateTime(year, month, day);
				return true;
			}

		SetHour:
			int hour = MatchTwo(text);
			if (hour < 0 || hour > 23)
				return false;
			delimiter = text[0] == ':';
			if (delimiter)
				text.Forward(1);
			int minute = MatchTwo(text);
			if (minute < 0 || minute > 59)
				return false;
			if (delimiter)
				if (text[0] == ':')
					text.Forward(1);
				else
					return false;
			int second = MatchTwo(text);
			if (second < 0 || second > 59)
				return false;

			long ticks = 0;
			if (text[0] != '.')
			{
				text.Forward(Space);
			}
			else
			{
				int k = 0;
				char b;
				long w = TimeSpan.TicksPerSecond;
				while ((b = text[++k]) >= '0' && b <= '9')
				{
					w /= 10;
					ticks += w * (b - '0');
				}
				text.Forward(k, Space);
			}

			TimeSpan offset;
			if (text[0] == 'Z')
			{
				text.Forward(1, Space);
				offset = TimeSpan.Zero;
				timeZone = true;
			}
			else if (text[0] == 'G' && text[1] == 'M' && text[2] == 'T')
			{
				text.Forward(3, Space);
				offset = TimeSpan.Zero;
				timeZone = true;
			}
			else if (text[0] == '+' || text[0] == '-')
			{
				bool minus = text[0] == '-';
				text.Forward(1, Space);
				char b = text[0];
				if (b < '0' || b > '9')
					return false;
				int h = b - '0';
				if ((b = text[1]) >= '0' && b <= '9')
				{
					h = h * 10 + (b - '0');
					text.Forward(2);
				}
				else
				{
					text.Forward(1);
				}
				int m = 0;
				if (text[0] == ':')
				{
					text.Forward(1);
					m = MatchTwo(text);
					if (m < 0 || m > 59)
						return false;
				}
				text.Forward(Space);
				offset = minus ? new TimeSpan(-h, -m, 0): new TimeSpan(h, m, 0);
				timeZone = true;
			}
			else
			{
				offset = DateTimeOffset.Now.Offset;
			}

			bool pm = false;
			if (!text.Eof)
			{
				if (hour > 12 || text.Length != 2)
					return false;
				string ap = text.Substring(0, 2);
				if (string.Equals(ap, "PM", StringComparison.OrdinalIgnoreCase))
					pm = true;
				else if (!string.Equals(ap, "AM", StringComparison.OrdinalIgnoreCase))
					return false;
				if (hour > 12 || pm && hour == 12)
					return false;
				if (pm)
					hour += 12;
			}


			result = new DateTimeOffset(year, month, day, hour, minute, second, offset);
			if (ticks > 0)
				result += TimeSpan.FromTicks(ticks);
			return true;
		}

		public static Guid GetGuid(string value)
		{
			return Guid.TryParse(value, out Guid result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static Guid GetGuid(string value, Guid defaultValue)
		{
			return Guid.TryParse(value, out Guid result) ? result: defaultValue;
		}

		public static Guid? GetGuid(string value, Guid? defaultValue)
		{
			return Guid.TryParse(value, out Guid result) ? result : defaultValue;
		}

		public static bool TryGetGuid(string value, out Guid result)
		{
			return Guid.TryParse(value, out result);
		}

		public static Type GetType(string value)
		{
			return TryGetType(value, out Type result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static Type GetType(string value, Type defaultValue)
		{
			return TryGetType(value, out Type result) ? result: defaultValue;
		}

		public static bool TryGetType(string value, out Type result)
		{
			if (String.IsNullOrWhiteSpace(value))
			{
				result = null;
				return false;
			}
			result = Factory.GetType(value);
			return result != null;
		}

		public static bool GetBoolean(string value, bool defaultValue)
		{
			return TryGetBoolean(value, out bool result) ? result: defaultValue;
		}

		public static bool GetBoolean(string value)
		{
			return TryGetBoolean(value, out bool result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static bool TryGetBoolean(string value, out bool result)
		{
			if (value != null && value.Length > 0)
			{
				value = value.Trim().ToUpperInvariant();
				if (value == "TRUE" || value == "ON" || value == "YES" || value == "1" || value == "GRANT")
				{
					result = true;
					return true;
				}
				if (value == "FALSE" || value == "OFF" || value == "NO" || value == "0" || value == "DENY")
				{
					result = false;
					return true;
				}
			}
			result = false;
			return false;
		}

		public static Ternary GetTernary(string value)
		{
			return TryGetTernary(value, out Ternary result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static Ternary GetTernary(string value, Ternary defaultValue)
		{
			return TryGetTernary(value, out Ternary result) ? result: defaultValue;
		}

		public static bool TryGetTernary(string value, out Ternary result)
		{
			if (value == null)
			{
				result = Ternary.Unknown;
				return false;
			}
			value = value.Trim().ToUpperInvariant();
			if (value == "TRUE" || value == "ON" || value == "YES" || value == "1" || value == "GRANT")
			{
				result = Ternary.True;
				return true;
			}
			if (value == "FALSE" || value == "OFF" || value == "NO" || value == "0" || value == "DENY")
			{
				result = Ternary.False;
				return true;
			}
			if (value == "UNKNOWN" || value == "SOME" || value == "ANY" || value == "ALL" || value == "2" || value == "BOTH" || value == "DEFAULT")
			{
				result = Ternary.Unknown;
				return true;
			}
			result = Ternary.Unknown;
			return false;
		}

		public static int GetIndex(string value, params string[] variants)
		{
			if (variants == null)
				throw new ArgumentNullException(nameof(variants));

			if (value != null && (value = value.Trim()).Length == 0)
				value = null;

			for (int i = 0; i < variants.Length; ++i)
			{
				if (String.Equals(variants[i], value, StringComparison.OrdinalIgnoreCase))
					return i;
			}
			return -1;
		}

		public static T GetEnum<T>(string value)
			where T: struct
		{
			if (TryGetEnum(value, out T result))
				return result;
			throw new FormatException(SR.FormatException(value));
		}

		public static T GetEnum<T>(string value, T defaultValue)
			where T: struct
		{
			return TryGetEnum(value, out T result) ? result: defaultValue;
		}

		public static T? GetEnum<T>(string value, T? defaultValue)
			where T: struct
		{
			return TryGetEnum(value, out T result) ? result: defaultValue;
		}

		private static bool IsEnum(Type type)
		{
			return type.IsEnum;
		}

		public static bool TryGetEnum<T>(string value, out T result)
			where T: struct
		{
			if (value == null || value.Length == 0 || !IsEnum(typeof(T)))
			{
				result = default;
				return false;
			}
			if (Enum.TryParse(value.Trim(), true, out result))
				return true;

			if (Int64.TryParse(value, out long x))
			{
				object y = Enum.ToObject(typeof(T), x);
				foreach (var item in Enum.GetValues(typeof(T)))
				{
					if (Object.Equals(y, item))
					{
						result = (T)y;
						return true;
					}
				}
			}
			return false;
		}

		public static object GetEnum(string value, Type enumType)
		{
			return TryGetEnum(value, enumType, out object result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static object GetEnum(string value, Type enumType, object defaultValue)
		{
			return TryGetEnum(value, enumType, out object result) ? result: defaultValue;
		}

		public static bool TryGetEnum(string value, Type enumType, out object result)
		{
			if (!IsEnum(enumType))
			{
				result = null;
				return false;
			}
			result = Enum.ToObject(enumType, 0);
			if (value == null || (value = value.Trim()).Length == 0)
				return false;

			string[] names = Enum.GetNames(enumType);
			for (int i = 0; i < names.Length; ++i)
			{
				if (String.Equals(names[i], value, StringComparison.OrdinalIgnoreCase))
				{
					result = Enum.ToObject(enumType, Enum.GetValues(enumType).GetValue(i));
					return true;
				}
			}

			if (!Int64.TryParse(value, out long x))
				return false;

			object y = Enum.ToObject(enumType, x);
			foreach (var item in Enum.GetValues(enumType))
			{
				if (Object.Equals(y, item))
				{
					result = y;
					return true;
				}
			}
			return false;
		}

		public static string GetString(string value, string defaultValue)
		{
			if (value == null)
				return defaultValue;
			value = value.Trim();
			return value.Length == 0 ? defaultValue: value;
		}

		public static string EncodeAttribute(string value)
		{
			if (value == null || value.Length == 0)
				return "\"\"";

			return
				value.IndexOf('"') < 0 ? "\"" + Encode(value, false) + "\"":
				value.IndexOf('\'') < 0 ? "'" + Encode(value, false) + "'" :
				"\"" + Encode(value, true) + "\"";
		}

		public static StringBuilder EncodeAttribute(StringBuilder text, string value)
		{
			if (value == null || value.Length == 0)
				return text.Append("\"\"");

			if (value.IndexOf('"') < 0)
			{
				text.Append('"');
				Encode(text, value, false);
				return text.Append('"');
			}
			if (value.IndexOf('\'') < 0)
			{
				text.Append('\'');
				Encode(text, value, false);
				return text.Append('\'');
			}
			text.Append('"');
			Encode(text, value, true);
			return text.Append('"');
		}

		#endregion

		#region Parse Value

		public static object GetValue(string value, Type type, object defaultValue)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return TryGetValue(value, type, out object result) ? result: defaultValue;
		}

		public static object GetValue(string value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return TryGetValue(value, type, out object result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static T GetValue<T>(string value, T defaultValue)
		{
			return TryGetValue(value, out T result) ? result: defaultValue;
		}

		public static T GetValue<T>(string value)
		{
			return TryGetValue(value, out T result) ? result: throw new FormatException(SR.FormatException(value));
		}

		public static bool TryGetValue<T>(string value, out T result)
		{
			bool success = TryGetValue(value, typeof(T), out object temp);
			result = (T)temp;
			return success;
		}

		public static object GetValueOrDefault(string value, Type returnType)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));
			return !TryGetValue(value, returnType, out object result) ? Factory.DefaultValue(returnType): result;
		}

		public static bool TryGetValue(string value, Type returnType, out object result)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));

			if (value == null || __missingConverters.ContainsKey(returnType))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser parser))
				return parser(value, out result);

			if (String.IsNullOrWhiteSpace(value))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (returnType.IsEnum || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>) && returnType.GetGenericArguments()[0].IsEnum)
			{
				bool nullable = returnType.IsGenericType;
				Type type1 = nullable ? returnType.GetGenericArguments()[0]: returnType;
				Type type2 = nullable ? returnType: typeof(Nullable<>).MakeGenericType(returnType);
				__stringConditionalConstructor.TryAdd(type1, (string x, out object y) => TryGetEnum(x, type1, out y));
				__stringConditionalConstructor.TryAdd(type2, (string x, out object y) =>
				{
					if (String.IsNullOrWhiteSpace(x))
					{
						y = null;
						return true;
					}
					return TryGetEnum(x, type1, out y);
				});

				if (String.IsNullOrWhiteSpace(value))
				{
					result = null;
					return nullable;
				}
				return TryGetEnum(value, type1, out result);
			}

			TypeConverter converter = GetTypeConverter(returnType);
			if (converter != null)
				return TryConverter(value, returnType, converter, out result);

			parser = GetExplicitConverter(returnType);
			if (parser != null)
			{
				__stringConditionalConstructor.TryAdd(returnType, parser);
				return parser(value, out result);
			}

			__missingConverters.TryAdd(returnType, true);
			result = Factory.DefaultValue(returnType);
			return false;
		}
		private static readonly ConcurrentDictionary<Type, bool> __missingConverters = new ConcurrentDictionary<Type, bool>();

		private static bool TryConverter(string value, Type returnType, TypeConverter converter, out object result)
		{
			try
			{
				result = converter.ConvertFromInvariantString(value);
				return true;
			}
			catch (NotSupportedException)
			{
			}
			catch (ArgumentException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		private static TypeConverter GetTypeConverter(Type targetType)
		{
			if (__typeConverterTypeMap.TryGetValue(targetType, out Type converterType))
				return converterType == null ? null: Factory.Construct(converterType) as TypeConverter;

			converterType = GetConverterType(targetType);
			TypeConverter converter = null;
			if (converterType != null)
			{
				converter = Factory.Construct(converterType) as TypeConverter;
				if (converter == null || !converter.CanConvertFrom(typeof(string)))
				{
					converterType = null;
					converter = null;
				}
			}
			__typeConverterTypeMap.TryAdd(targetType, converterType);
			return converter;
		}
		private static readonly ConcurrentDictionary<Type, Type> __typeConverterTypeMap = new ConcurrentDictionary<Type, Type>();

		private static Type GetConverterType(Type type)
		{
			while (type != null)
			{
				CustomAttributeTypedArgument argument = CustomAttributeData.GetCustomAttributes(type)
					.Where(o => o.Constructor.ReflectedType == typeof(TypeConverterAttribute) && o.ConstructorArguments.Count == 1)
					.Select(o => o.ConstructorArguments[0]).FirstOrDefault();

				if (argument != default)
				{
					var qualifiedType = argument.Value as Type;
					if (qualifiedType == null && argument.Value is string qualifiedTypeName)
						qualifiedType = Factory.GetType(qualifiedTypeName);
					return Factory.IsPublicType(qualifiedType) ? qualifiedType: null;
				}
				type = type.BaseType;
			}
			return null;
		}

		private static ValueParser GetExplicitConverter(Type type)
		{
			MethodInfo parser = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), type.MakeByRefType() }, null);
			if (parser != null)
				return Tgv(parser, type, Factory.IsNullableType(type));

			//		result = null;
			//		try
			//		{
			//
			//	3:		xtype tmp1;
			//			if (Convert(value, out tmp1))
			//			{
			//	3.1:		result = (object)operator(tmp1);
			//	3.2:		result = (object)new operator(tmp1);
			//				return true;
			//			}
			//		...
			//	3:		xtype tmp2;
			//			if (Convert(value, out tmp2))
			//			{
			//	3.1:		result = (object)operator(tmp2);
			//	3.2:		result = (object)new operator(tmp2);
			//				return true;
			//			}
			//		...
			//			string:
			//	1:			result = (object)operator(value);
			//	2:			result = (object)new operator(value);
			//				return true;
			//
			//			return false;
			//		}
			//		catch (Exception ex)
			//		{
			//			if (EX.IsCriticalException(ex))
			//				throw;
			//			return false;
			//		}


			ParameterExpression value = Expression.Parameter(typeof(string), "value");
			ParameterExpression result = Expression.Parameter(typeof(object).MakeByRefType(), "result");

			var operators = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Select(o =>
				{
					if (!type.IsAssignableFrom(o.ReturnType))
						return null;
					if (!(o.IsSpecialName && (o.Name == "op_Explicit" || o.Name == "op_Implicit")))
						return null;
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer)
						return null;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return null;
					return new { Method = o, Parameter = pp[0], Parser = ps.Method };
				})
				.Where(o=> o != null).ToList();

			if (operators.Count > 1)
				operators = __stringTypedParsers.Select(o => operators.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
				.Select(o =>
				{
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer ||
						operators.Any(x => x.Parameter.ParameterType == pp[0].ParameterType))
						return null;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return null;

					return new { Constructor = o, Parameter = pp[0], Parser = ps.Method };
				})
				.Where(o=> o != null).ToList();

			if (constructors.Count > 1)
				constructors = __stringTypedParsers.Select(o => constructors.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			LabelTarget end = Expression.Label(typeof(bool));
			var convert = new List<Expression>();
			var variable = new List<ParameterExpression>();
			foreach (var item in operators)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result, 
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.Call(item.Method, tmp), typeof(object)):
									Expression.Call(item.Method, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			foreach (var item in constructors)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)new operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result,
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.New(item.Constructor, tmp), typeof(object)):
									Expression.New(item.Constructor, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			var stringOperator = operators.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
			if (stringOperator != null)
			{
				Expression last = Expression.Assign(result,
					type.IsValueType ? (Expression)
						Expression.TypeAs(Expression.Call(stringOperator.Method, value), typeof(object)):
						Expression.Call(stringOperator.Method, value));
				convert.Add(last);
				convert.Add(Expression.Goto(end, Expression.Constant(true)));
			}
			else
			{
				var stringConstructor = constructors.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
				if (stringConstructor != null)
				{
					Expression last = Expression.Assign(result,
						type.IsValueType ? (Expression)
							Expression.TypeAs(Expression.New(stringConstructor.Constructor, value), typeof(object)):
							Expression.New(stringConstructor.Constructor, value));
					convert.Add(last);
					convert.Add(Expression.Goto(end, Expression.Constant(true)));
				}
			}

			if (convert.Count == 0)
				return null;

			convert.Add(Expression.Label(end, Expression.Constant(false)));

			ParameterExpression ex = Expression.Variable(typeof(Exception), "ex");

			Expression body = Expression.Block(
				Expression.Assign(result, Expression.Constant(null)),
				Expression.TryCatch(
					Expression.Block(variable, convert),
					Expression.Catch(ex, Expression.Block(
						Expression.IfThen(
							Expression.Call(((Func<Exception, bool>)ExceptionExtensions.IsCriticalException).Method, ex),
							Expression.Throw(ex)),
						Expression.Constant(false))
						)
					));
			return Expression.Lambda<ValueParser>(body, value, result).Compile();
		}

		public static T GetValue<T>(XmlLiteNode node)
		{
			return TryGetValue(node, out T result) ? result: throw new FormatException(SR.FormatException(node.ToString().Left(1024), typeof(T)));
		}

		public static T GetValue<T>(XmlLiteNode node, T defaultValue)
		{
			return TryGetValue(node, out T result) ? result: defaultValue;
		}

		public static bool TryGetValue<T>(XmlLiteNode node, out T result)
		{
			if (TryGetValue(node, typeof(T), out object temp))
			{
				result = (T)temp;
				return true;
			}
			result = default;
			return false;
		}

		public static object GetValue(XmlLiteNode node, Type returnType)
		{
			if (TryGetValue(node, returnType, out object result))
				return result;
			throw new FormatException(SR.FormatException(node.ToString().Left(1024), returnType));
		}

		public static bool TryGetValue(XmlLiteNode node, Type returnType, out object result)
		{
			if (node == null || returnType == null)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			try
			{
				if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser stringParser))
					return stringParser(node.Value, out result);

				if (__nodeConditionalConstructor.TryGetValue(returnType, out TryGetNodeValue nodeParser))
				{
					if (nodeParser == null)
					{
						result = Factory.DefaultValue(returnType);
						return false;
					}
					return nodeParser(node, returnType, out result);
				}
				if (returnType.IsGenericType && __nodeGenericConstructor.TryGetValue(returnType.GetGenericTypeDefinition(), out nodeParser))
				{
					if (nodeParser == null)
					{
						result = Factory.DefaultValue(returnType);
						return false;
					}
					__nodeConditionalConstructor[returnType] = nodeParser;
					return nodeParser(node, returnType, out result);
				}
				if (returnType.IsEnum)
				{
					__stringConditionalConstructor.TryAdd(returnType, (string x, out object y) => TryGetEnum(x, returnType, out y));
					return TryGetEnum(node.Value, returnType, out result);
				}

				nodeParser = TryReflection;
				if (TestFromXmlLite(node, returnType, out result))
					nodeParser = TryFromXmlLite;
				else if (TestFromXmlReader(node, returnType, out result))
					nodeParser = TryFromXmlReader;
				else if (TestSerializer(node, returnType, out result))
					nodeParser = TrySerializer;
				else
					TryReflection(node, returnType, out result);

				__nodeConditionalConstructor.TryAdd(returnType, nodeParser);
				return result != null;
			}
			catch (Exception e)
			{
				throw new FormatException(SR.CannotParseValue(node.ToString().Left(1024), returnType), e);
			}
		}

		private static bool TryFromXmlLite(XmlLiteNode node, Type returnType, out object value)
		{
			value = __fromXmlLiteNodeParsers[returnType](node);
			return value != null;
		}

		private static bool TryFromXmlReader(XmlLiteNode node, Type returnType, out object value)
		{
			using (XmlReader reader = node.ReadSubtree())
			{
				value = __fromXmlReaderParsers[returnType](reader);
			}
			return value != null;
		}

		private static bool TrySerializer(XmlLiteNode node, Type returnType, out object result)
		{
			try
			{
				var xs = new XmlSerializer(returnType, new XmlRootAttribute(node.Name));
				using XmlReader reader = node.ReadSubtree();
				if (xs.CanDeserialize(reader))
				{
					result = xs.Deserialize(reader);
					return true;
				}
			}
			catch (InvalidOperationException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		#endregion

		#region TryGetValue Implementation

		private static bool TestFromXmlLite(XmlLiteNode node, Type returnType, out object value)
		{
			value = null;
			if (!__fromXmlLiteNodeParsers.TryGetValue(returnType, out Func<XmlLiteNode, object> parser))
			{
				Type baseType = Factory.NullableTypeBase(returnType);
				parser = __fromXmlLiteNodeParsers.GetOrAdd(baseType, type => GetFromXmlConstructor<XmlLiteNode>(type) ?? GetFromXmlStaticConstructor<XmlLiteNode>(type));
				if (baseType.IsValueType)
					__fromXmlLiteNodeParsers.TryAdd(typeof(Nullable<>).MakeGenericType(baseType), parser);
			}
			if (parser == null)
				return false;

			value = parser(node);
			return true;
		}
		private static readonly ConcurrentDictionary<Type, Func<XmlLiteNode, object>> __fromXmlLiteNodeParsers = new ConcurrentDictionary<Type, Func<XmlLiteNode, object>>();

		private static bool TestFromXmlReader(XmlLiteNode node, Type returnType, out object value)
		{
			value = null;
			if (!__fromXmlReaderParsers.TryGetValue(returnType, out Func<XmlReader, object> parser))
			{
				Type baseType = Factory.NullableTypeBase(returnType);
				parser = __fromXmlReaderParsers.GetOrAdd(baseType, type => GetFromXmlConstructor<XmlReader>(type) ?? GetFromXmlStaticConstructor<XmlReader>(type));
				if (baseType.IsValueType)
					__fromXmlReaderParsers.TryAdd(typeof(Nullable<>).MakeGenericType(baseType), parser);
			}
			if (parser == null)
				return false;

			using (XmlReader reader = node.ReadSubtree())
			{
				value = parser(reader);
			}
			return true;
		}
		private static readonly ConcurrentDictionary<Type, Func<XmlReader, object>> __fromXmlReaderParsers = new ConcurrentDictionary<Type, Func<XmlReader, object>>();

		private static Func<T, object> GetFromXmlConstructor<T>(Type type)
		{
			Func<T, object> result = null;
			ConstructorInfo ci = type.GetConstructor(new[] { typeof(T) });
			if (ci != null)
			{
				try
				{
					ParameterExpression a = Expression.Parameter(typeof(object), "arg");
					Expression e = Expression.Lambda<Func<T, object>>(Expression.New(ci, a), a);
					if (type.IsValueType)
						e = Expression.TypeAs(e, typeof(object));
					result = Expression.Lambda<Func<T, object>>(e).Compile();
				}
				catch (ArgumentException)
				{
				}
			}
			return result;
		}

		private static Func<T, object> GetFromXmlStaticConstructor<T>(Type type)
		{
			try
			{
				IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

				foreach (MethodInfo method in methods)
				{
					if (type == Factory.NullableTypeBase(method.ReturnType))
					{
						if (method.Name == "Create" || method.Name == "FromXml")
						{
							ParameterInfo[] parameters = method.GetParameters();
							if (parameters.Length == 1)
							{
								if (parameters[0].ParameterType == typeof(T))
								{
									ParameterExpression arg = Expression.Parameter(typeof(T), "arg");
									Expression e = Expression.Call(method, arg);
									if (type.IsValueType)
										e = Expression.TypeAs(e, typeof(object));
									return Expression.Lambda<Func<T, object>>(e, arg).Compile();
								}
							}
						}
					}
				}
			}
			catch (ArgumentException)
			{
			}
			return null;
		}

		private static bool TestSerializer(XmlLiteNode node, Type returnType, out object result)
		{
			result = null;
			if (!returnType.IsPublic)
				return false;
			if (returnType.GetCustomAttributes(typeof(SerializableAttribute), true).Length == 0)
				return false;
			if (returnType.GetInterface("IDictionary") != null)
				return false;
			try
			{
				var xs = new XmlSerializer(returnType, new XmlRootAttribute(node.Name));
				using XmlReader reader = node.ReadSubtree();
				if (!xs.CanDeserialize(reader))
					return false;
				result = xs.Deserialize(reader);
				return true;
			}
			catch (InvalidOperationException)
			{
			}
			return false;
		}

		#region Try Reflection

		private static bool TryReflection(XmlLiteNode node, Type returnType, out object result)
		{
			result = Factory.DefaultValue(returnType);
			if (node == null)
				return false;

			if (node.Elements.Count == 0 && node.Attributes.Count == 0)
				return TryGetValue(node.Value, returnType, out result);

			if (TryCollection(node, node.Elements, returnType, ref result))
				return true;

			var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in node.Attributes)
			{
				args[item.Key] = item.Value;
			}
			var skipped = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in node.Elements)
			{
				if (skipped.Contains(item.Name))
					continue;
				if (args.ContainsKey(item.Name))
				{
					skipped.Add(item.Name);
					args.Remove(item.Name);
				}
				else
				{
					args.Add(item.Name, item);
				}
			}

			var (missings, obj) = TryConstruct(returnType, args);
			if (obj == null)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}
			result = obj;

			if (missings.Count > 0 || skipped.Count > 0)
			{
				if (skipped.Count > 0)
				{
					foreach (var item in missings)
					{
						skipped.Add(item);
					}
					missings = skipped;
				}
				
				foreach (FieldInfo item in returnType.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					GetFieldValue(node, item.Name, item.FieldType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}
				foreach (PropertyInfo item in returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!item.CanWrite || !item.CanWrite || !item.SetMethod.IsPublic || item.GetIndexParameters().Length != 0)
						continue;

					GetFieldValue(node, item.Name, item.PropertyType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}

				static void GetFieldValue(XmlLiteNode node, string name, Type itemType, IReadOnlyCollection<string> missings, Func<object> getter, Action<object> setter)
				{
					if (missings.FirstOrDefault(o => String.Equals(o, name, StringComparison.OrdinalIgnoreCase)) != null)
					{
						var element = node.Element(name, StringComparer.OrdinalIgnoreCase);
						if (element.IsEmpty)
						{
							var attrib = node.Attributes.FirstOrDefault(o => String.Equals(o.Key, name, StringComparison.OrdinalIgnoreCase));
							if (attrib.Value != null && TryGetValue(attrib.Value, itemType, out var value))
								setter(value);
						}
						else
						{
							object value = getter();
							if (TryCollection(element, element.Elements, itemType, ref value) || TryGetValue(element, itemType, out value))
								setter(value);
						}
					}
					else
					{
						var singular = Lingua.Singular(name);
						if (name != singular && missings.FirstOrDefault(o => String.Equals(o, singular, StringComparison.OrdinalIgnoreCase)) != null)
						{
							object value = getter();
							if (TryCollection(XmlLiteNode.Empty, node.Elements.Where(o => String.Equals(o.Name, singular, StringComparison.OrdinalIgnoreCase)), itemType, ref value))
								setter(value);
						}
					}
				}
			}

			return true;
		}

		private class TypesKey: IEquatable<TypesKey>
		{
			private readonly Type[] _key;
			private readonly int _hashCode;

			public TypesKey(Type type, Type[] types)
			{
				_key = new Type[types.Length + 1];
				_key[0] = type;
				Array.Copy(types, 0, _key, 1, types.Length);
				_hashCode = HashCode.Join(153173, _key);
			}

			public override int GetHashCode()
			{
				return _hashCode;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as TypesKey);
			}

			public bool Equals(TypesKey other)
			{
				if (other is null || _hashCode != other._hashCode)
					return false;
				var a = _key;
				var b = other._key;
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
				{
					if (a[i] != b[i])
						return false;
				}
				return true;
			}
		}

		private static (IReadOnlyCollection<string> Missings, object Value) TryConstruct(Type type, Dictionary<string, object> arguments)
		{
			if (arguments == null || arguments.Count == 0)
				return (Array.Empty<string>(), Factory.TryConstruct(type, false));

			var key = new ConstructorKey(type, arguments);
			if (!__attributedConstructors.TryGetValue(key, out var constructor))
				constructor = __attributedConstructors.GetOrAdd(key, AttributedConstructor.Create(type, arguments));
			return (constructor?.Missings ?? (IReadOnlyCollection<string>)arguments.Keys, constructor?.Invoke(arguments));
		}
		private static readonly ConcurrentDictionary<ConstructorKey, AttributedConstructor> __attributedConstructors = new ConcurrentDictionary<ConstructorKey, AttributedConstructor>();

		private class ConstructorKey: IEquatable<ConstructorKey>
		{
			private readonly Type _type;
			private readonly string _key;
			private readonly int _hashCode;

			public ConstructorKey(Type type, Dictionary<string, object> arguments)
			{
				_type = type;
				if (arguments != null && arguments.Count > 0)
				{
					var ss = new StringBuilder(arguments.Count * 12);
					foreach (var key in arguments.Keys)
					{
						ss.Append(':').Append(key);
					}
					_key = ss.ToString().ToUpperInvariant();
				}
				_hashCode = HashCode.Join(_type?.GetHashCode() ?? 0, _key?.GetHashCode() ?? 0);
			}

			public override int GetHashCode() => _hashCode;

			public override bool Equals(object obj) => Equals(obj as ConstructorKey);

			public bool Equals(ConstructorKey other) => other != null && _type == other._type && _key == other._key;
		}

		private class AttributedConstructor
		{
			private readonly Func<object[], object> _constructor;
			private readonly (string Name, Type Type)[] _parameters;
			private readonly Type _type;

			private AttributedConstructor(Type type, Func<object[], object> constructor, (string Name, Type Info)[] parameters, IReadOnlyList<string> missings)
			{
				_type = type ?? throw new ArgumentNullException(nameof(type));
				_constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
				_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
				Missings = missings ?? Array.Empty<string>();
			}

			public IReadOnlyList<string> Missings { get; }

			public object Invoke(Dictionary<string, object> arguments)
			{
				if (_parameters.Length > 0)
				{
					if (arguments == null)
						throw new ArgumentNullException(nameof(arguments));
					if (arguments.Count < _parameters.Length)
						throw new ArgumentOutOfRangeException(nameof(arguments.Count), arguments.Count, null).Add("expected value", _parameters.Length);
				}

				var values = new object[_parameters.Length];
				for (int i = 0; i < _parameters.Length; ++i)
				{
					if (!arguments.TryGetValue(_parameters[i].Name, out var value))
						return null;
					if (value is string s)
					{
						if (!TryGetValue(s, _parameters[i].Type, out object v))
						{
							Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot parse parameter {_parameters[i].Name} of {_parameters[i].Type.Name} used to construct {_type.Name}.");
							return null;
						}
						values[i] = v;
					}
					else if (value is XmlLiteNode x)
					{
						if (!TryGetValue(x, _parameters[i].Type, out object v))
						{
							Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {_parameters[i].Name} to type {_parameters[i].Type.Name} used to construct {_type.Name}.");
							return null;
						}
						values[i] = v;
					}
					else if (value is null)
					{
						values[i] = Factory.DefaultValue(_parameters[i].Type);
					}
					else if (_parameters[i].Type.IsAssignableFrom(value.GetType()))
					{
						values[i] = value;
					}
					else
					{
						Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {_parameters[i].Name} to type {_parameters[i].Type.Name} from {value.GetType().Name} used to construct {_type.Name}.");
						return null;
					}
				}
				return _constructor.Invoke(values);
			}

			public static AttributedConstructor Create(Type type, Dictionary<string, object> arguments)
			{
				if (type == null)
					throw new ArgumentNullException(nameof(type));
				if (arguments == null)
					throw new ArgumentNullException(nameof(arguments));

				ConstructorInfo[] constructors = type.GetConstructors();
				var parametersSet = new ParameterInfo[constructors.Length][];
				for (int i = 0; i < constructors.Length; ++i)
				{
					parametersSet[i] = constructors[i].GetParameters();
				}

				Array.Sort(parametersSet, constructors, Comparer.Create<ParameterInfo[]>((a, b) => b.Length.CompareTo(a.Length)));

				ParameterInfo[] parameters = null;
				ConstructorInfo constructor = null;
				BitArray index = null;
				int weight = 0;
				for (int i = 0; i < constructors.Length; ++i)
				{
					var prm = parametersSet[i];
					if (prm.Length < weight)
						break;
					var idx = new BitArray(prm.Length);
					int w = 0;
					for (int j = 0; j < prm.Length; ++j)
					{
						idx[j] = arguments.ContainsKey(prm[j].Name);
						if (idx[j])
							++w;
						else if (!prm[j].IsOptional)
							goto skip;
					}
					if (index == null || w >= weight)
					{
						index = idx;
						weight = w;
						constructor = constructors[i];
						parameters = prm;
					}
				skip:;
				}

				if (constructor == null)
				{
					if (!type.IsValueType)
					{
						Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Create)}: Cannot find a counstructor for {type.Name} and arguments: {String.Join(", ", arguments.Keys)}");
						return null;
					}
					parameters = Array.Empty<ParameterInfo>();
				}

				ParameterExpression arg = Expression.Parameter(typeof(object[]));
				Expression ctor;
				var parms = new List<(string Name, Type Type)>();
				if (weight == 0 && type.IsValueType)
				{
					ctor = Expression.TypeAs(Expression.Default(type), typeof(object));
				}
				else if (parameters.Length == 0)
				{
					ctor = Expression.New(type);
				}
				else
				{
					var args = new Expression[index.Length];
					for (int i = 0; i < index.Length; ++i)
					{
						if (!index[i])
						{
							args[i] = Expression.Convert(Expression.Constant(DefaultParameterValue(parameters[i])), parameters[i].ParameterType);
						}
						else
						{
							args[i] = Expression.Convert(Expression.ArrayAccess(arg, Expression.Constant(parms.Count)), parameters[i].ParameterType);
							parms.Add((parameters[i].Name, parameters[i].ParameterType));
						}
					}
					if (type.IsValueType)
						ctor = Expression.TypeAs(Expression.New(constructor, args), typeof(object));
					else
						ctor = Expression.New(constructor, args);
				}
				var missings = new List<string>(arguments.Keys.Except(parameters.Select(o => o.Name), StringComparer.OrdinalIgnoreCase));
				return new AttributedConstructor(type, Expression.Lambda<Func<object[], object>>(ctor, arg).Compile(), parms.ToArray(), missings);
			}
		}

		private static object DefaultParameterValue(ParameterInfo parameter)
		{
			object value = null;
			try { value = parameter.DefaultValue; } catch {}
			return value ?? Factory.DefaultValue(parameter.ParameterType);
		}

		private static bool TryCollection(XmlLiteNode node, IEnumerable<XmlLiteNode> items, Type returnType, ref object result)
		{
			if (returnType.IsInterface)
			{
				if (returnType.IsGenericType)
				{
					Type[] aat = returnType.GetGenericArguments();
					if (aat.Length == 1)
					{
						if (returnType.IsAssignableFrom(typeof(IList<>).MakeGenericType(aat)) ||
							returnType.IsAssignableFrom(typeof(IReadOnlyList<>).MakeGenericType(aat)))
						{
							Type t = typeof(List<>).MakeGenericType(aat);
							bool r = TryParseCollection(node, items, t, t, aat[0], ref result);
							result = WrapCollection(result, aat, typeof(IList<>));
							return r;
						}
						if (returnType.IsAssignableFrom(typeof(ISet<>).MakeGenericType(aat)) ||
							returnType.IsAssignableFrom(typeof(IReadOnlySet<>).MakeGenericType(aat)))
						{
							Type t = typeof(HashSet<>).MakeGenericType(aat);
							bool r = TryParseCollection(node, items, t, t, aat[0], ref result);
							result = WrapCollection(result, aat, typeof(ISet<>));
							return r;
						}
					}
					else if (aat.Length == 2)
					{
						if (returnType.IsAssignableFrom(typeof(IDictionary<,>).MakeGenericType(aat)) ||
							returnType.IsAssignableFrom(typeof(IReadOnlyDictionary<,>).MakeGenericType(aat)))
						{
							Type vt = typeof(Dictionary<,>).MakeGenericType(aat);
							Type it = typeof(KeyValuePair<,>).MakeGenericType(aat);
							Type ct = typeof(ICollection<>).MakeGenericType(it);
							bool r = TryParseCollection(node, items, vt, ct, typeof(KeyValuePair<,>).MakeGenericType(aat), ref result);
							result = WrapCollection(result, aat, typeof(IDictionary<,>));
							return r;
						}
					}
				}
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (returnType.IsArray)
			{
				Type itemType = returnType.GetElementType();
				returnType = typeof(List<>).MakeGenericType(itemType);
				bool r = TryParseCollection(node, items, returnType, returnType, itemType, ref result);
				if (result != null)
					result = returnType.GetMethod("ToArray")?.Invoke(result, null);
				return r;
			}

			Type[] ii = returnType.GetInterfaces();
			Type ic = ii.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
			return ic != null && TryParseCollection(node, items, returnType, ic, ic.GetGenericArguments()[0], ref result);
		}

		private static object WrapCollection(object value, Type[] parametersType, Type collectionType)
		{
			if (value == null)
				return null;
			Func<object, object> f = __genericReadonlyWrappers.GetOrAdd(new TypesKey(collectionType, parametersType),
				key =>
				{
					MethodInfo m = Factory.GetGenericMethod(typeof(ReadOnly), "Wrap", new[] { collectionType });
					if (m == null)
						return null;
					Type type = collectionType.MakeGenericType(parametersType);
					m = m.MakeGenericMethod(parametersType);
					ParameterExpression arg = Expression.Parameter(typeof(object));
					return Expression.Lambda<Func<object, object>>(
						Expression.Call(m, Expression.Convert(arg, type)),
						arg).Compile();
				});
			return f == null ? value: f(value);
		}
		private static readonly ConcurrentDictionary<TypesKey, Func<object, object>> __genericReadonlyWrappers = new ConcurrentDictionary<TypesKey, Func<object, object>>();

		private static bool TryParseCollection(XmlLiteNode node, IEnumerable<XmlLiteNode> items, Type returnType, Type collectionType, Type itemType, ref object result)
		{
			MethodInfo add = collectionType.GetMethod("Add", new[] { itemType });
			if (add == null)
				return false;

			PropertyInfo readOnly = collectionType.GetProperty("IsReadOnly", typeof(bool));
			if (readOnly != null && !readOnly.CanRead)
				readOnly = null;
			if (result == null || !returnType.IsInstanceOfType(result) ||
				(readOnly != null && Object.Equals(Factory.Invoke(result, readOnly.GetGetMethod()), true)))
			{
				var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				foreach (var item in node.Attributes)
				{
					args[item.Key] = item.Value;
				}
				result = TryConstruct(returnType, args).Value;
				if (result == null || (readOnly != null && Object.Equals(Factory.Invoke(result, readOnly.GetGetMethod()), true)))
					return false;
			}

			foreach (var item in items)
			{
				if (TryGetValue(item, itemType, out object itemValue))
					Factory.Invoke(result, add, itemValue);
			}
			return true;
		}

		#endregion

		#region Predefined Parsers

		/// <summary>
		/// Parse Key/Value pair
		/// </summary>
		/// <param name="node">XmlLiteNode to get value from</param>
		/// <param name="returnType">Concrete type of KeyValue pair structure</param>
		/// <param name="result">Parsed Value</param>
		/// <returns>True if success</returns>
		/// <remarks>
		/// supported nodes structure:
		///		node
		///			:key	value_of_key
		///			:value	value_of_value
		///		
		///		key	value
		///			
		///		key
		///			value
		///	
		///		node		value_of_value
		///			:key	value_of_key
		///
		///		node
		///			:key	value_of_key
		///			value
		///
		///		node
		///			key
		///			value
		/// </remarks>
		private static bool TryKeyValuePair(XmlLiteNode node, Type returnType, out object result)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));
			if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
				throw new ArgumentOutOfRangeException(nameof(returnType), returnType, null);

			result = null;
	
			Type[] typeArgs = returnType.GetGenericArguments();
			Type keyType = typeArgs[0];
			Type valueType = typeArgs[1];
			object parsedKey;
			object parsedValue;

			if (node.Attributes.Count == 0)
			{
				XmlLiteNode nodeKey = node.FirstOrDefault("key");
				XmlLiteNode nodeValue = node.FirstOrDefault("value");
				// <item><key>Key</key><value>Value</value></item>
				if (node.Elements.Count == 2 && nodeKey != null && nodeValue != null)
				{
					if (!TryGetValue(nodeKey, keyType, out parsedKey))
						return false;
					if (!TryGetValue(nodeValue, valueType, out parsedValue))
						return false;
				}
				// <Key ...>
				else
				{
					if (!TryGetValue(node.Name, keyType, out parsedKey))
						return false;
					// <Key value="Value" />
					if (node.Elements.Count == 1 && nodeValue != null)
					{
						if (!TryGetValue(nodeValue, valueType, out parsedValue))
							return false;
					}
					// <Key>Value</Key>
					else
					{
						if (!TryGetValue(node, valueType, out parsedValue))
							return false;
					}
				}

			}
			// <item key="Key" value="Value" />
			// <Key ... />
			else if (node.Attributes.Count == 2 && node.Elements.Count == 0)
			{
				if (!TryGetValue(node["key"] ?? node.Name, keyType, out parsedKey))
					return false;
				if (node["value"] == null)
				{
					if (!TryGetValue(node, valueType, out parsedValue))
						return false;
				}
				else
				{
					if (!TryGetValue(node["value"], valueType, out parsedValue))
						return false;
				}
			}
			// <Key ...> ... </Key>
			// <item key="Key" ...> ... </item>
			else
			{
				if (!TryGetValue(node["key"] ?? node.Name, keyType, out parsedKey))
					return false;

				XmlLiteNode nodeValue;
				// <... ><value>Value</value></...>
				if (node.Elements.Count == 1 && (nodeValue = node.FirstOrDefault("value")) != null)
				{
					if (!TryGetValue(nodeValue, valueType, out parsedValue))
						return false;
				}
				// <... >Value</...>
				else
				{
					if (!TryGetValue(node, valueType, out parsedValue))
						return false;
				}
			}
			ConstructorInfo constructor = returnType.GetConstructor(typeArgs);
			if (constructor == null)
				throw EX.InvalidOperation(SR.CannotFindConstructor(returnType, typeArgs));
			result = Factory.Invoke(constructor, parsedKey, parsedValue);
			return true;
		}

		private static bool TryNullable(XmlLiteNode node, Type returnType, out object result)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));
			if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Nullable<>))
				throw new ArgumentOutOfRangeException(nameof(returnType), returnType, null);

			if (node.Attributes.Count == 0 && node.Elements.Count == 0 && node.Value.Length == 0)
			{
				result = null;
				return true;
			}
			if (!TryGetValue(node, returnType.GetGenericArguments()[0], out var value))
			{
				result = null;
				return true;
			}
			if (value == null)
			{
				result = DefaultValue(returnType);
				return true;
			}
			result = Activator.CreateInstance(returnType, value);
			return true;
		}

		private static object DefaultValue(Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type): null;
		}

		private delegate bool TryGetNodeValue(XmlLiteNode node, Type returnType, out object result);

		private static bool TryCopy(XmlLiteNode node, Type returnType, out object result)
		{
			result = node;
			return true;
		}

		private static ValueParser Tgv(MethodInfo getter, Type type, bool nullable = false)
		{
			//	T tmp = default(T);
			//	1: bool res = getter(argValue, out tmp)
			//	2: bool res = String.IsNullOrWhiteSpace(argValue) || getter(argValue, out tmp)
			//	if (res)
			//		argResult = tmp;
			//	else
			//		argResult = null;
			//	return res;
			ParameterExpression argValue = Expression.Parameter(typeof(string), "value");
			ParameterExpression argResult = Expression.Parameter(typeof(object).MakeByRefType(), "result");
			ParameterExpression tmp = Expression.Variable(type, "tmp");
			ParameterExpression res = Expression.Variable(typeof(bool), "res");
			Expression condition = Expression.Call(getter, argValue, tmp);
			if (nullable)
				condition = Expression.OrElse(Expression.Call(((Func<string, bool>)String.IsNullOrWhiteSpace).Method, argValue), condition);
			BlockExpression body = Expression.Block(typeof(bool),
				new[] { tmp, res },
				Expression.Assign(tmp, Expression.Default(type)),
				Expression.Assign(res, condition),
				//Expression.IfThenElse(res,
					Expression.Assign(argResult, Expression.TypeAs(tmp, typeof(object))),
				//	Expression.Assign(argResult, Expression.Constant(null))
				//	),
				res
				);

			return Expression.Lambda<ValueParser>(body, argValue, argResult).Compile();
		}
		private static bool TryParseString(string value, out string result)
		{
			result = value;
			return true;
		}
		private delegate bool ConcreteValueParser<T>(string value, out T result);
		private delegate bool ValueParser(string value, out object result);
		private struct ParserPair
		{
			public readonly Type Type;
			public readonly MethodInfo Method;

			private ParserPair(Type type, MethodInfo method)
			{
				Type = type;
				Method = method;
			}

			public static ParserPair New<T>(ConcreteValueParser<T> parser)
			{
				return new ParserPair(typeof(T), parser.Method);
			}
		}
		private static readonly ConcurrentDictionary<Type, TryGetNodeValue> __nodeConditionalConstructor = new ConcurrentDictionary<Type, TryGetNodeValue>(
			new[]
			{
				new KeyValuePair<Type, TryGetNodeValue>(typeof(XmlLiteNode), TryCopy)
			});

		private static readonly Dictionary<Type, TryGetNodeValue> __nodeGenericConstructor =
			new Dictionary<Type, TryGetNodeValue>
			{
				{ typeof(KeyValuePair<,>), TryKeyValuePair },
				{ typeof(Nullable<>), TryNullable }
			};
		private static readonly ParserPair[] __stringTypedParsers =
		{
			ParserPair.New<bool>(TryGetBoolean),
			ParserPair.New<byte>(Byte.TryParse),
			ParserPair.New<sbyte>(SByte.TryParse),
			ParserPair.New<short>(Int16.TryParse),
			ParserPair.New<ushort>(UInt16.TryParse),
			ParserPair.New<int>(Int32.TryParse),
			ParserPair.New<uint>(UInt32.TryParse),
			ParserPair.New<long>(Int64.TryParse),
			ParserPair.New<ulong>(UInt64.TryParse),
			ParserPair.New<float>(Single.TryParse),
			ParserPair.New<double>(Double.TryParse),
			ParserPair.New<decimal>(Decimal.TryParse),
			ParserPair.New<char>(TryGetChar),
			ParserPair.New<DateTime>(TryGetDateTime),
			ParserPair.New<TimeSpan>(TryGetTimeSpan),
			ParserPair.New<Ternary>(TryGetTernary),
			ParserPair.New<Guid>(TryGetGuid),
			ParserPair.New<Type>(TryGetType),
			ParserPair.New<string>(TryParseString),
		};
		private static readonly ConcurrentDictionary<Type, ValueParser> __stringConditionalConstructor = new ConcurrentDictionary<Type, ValueParser>
			(
				__stringTypedParsers.Select(o => new KeyValuePair<Type, ValueParser>(o.Type, Tgv(o.Method, o.Type)))
				.Union(
				__stringTypedParsers.Where(o => !o.Type.IsClass).Select(o => new KeyValuePair<Type, ValueParser>(typeof(Nullable<>).MakeGenericType(o.Type), Tgv(o.Method, o.Type, true)))
				)
			);

		#endregion

		#endregion
	}
}

