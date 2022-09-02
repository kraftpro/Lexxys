using System;
using System.Collections;
using System.Text;
using System.Xml;

#nullable enable

#pragma warning disable CA1307 // Specify StringComparison for clarity

namespace Lexxys.Xml
{
    public static partial class XmlTools
    {
		public static string Convert(bool value)
		{
			return value ? "true" : "false";
		}

		public static string Convert(Guid value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(Guid? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(DateTime value, XmlDateTimeSerializationMode mode = XmlDateTimeSerializationMode.Unspecified)
		{
			string result = XmlConvert.ToString(value, mode);
			return result.EndsWith("T00:00:00", StringComparison.Ordinal) ? result.Substring(0, result.Length - 9) : result;
		}

		public static string Convert(DateTime? value, XmlDateTimeSerializationMode mode = XmlDateTimeSerializationMode.Unspecified)
		{
			return value.HasValue ? Convert(value.GetValueOrDefault(), mode) : "";
		}

		public static string Convert(DateTime value, string format)
		{
			return XmlConvert.ToString(value, format);
		}

		public static string Convert(DateTime? value, string format)
		{
			return value.HasValue ? Convert(value.GetValueOrDefault(), format) : "";
		}

		public static string Convert(DateTimeOffset value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(DateTimeOffset? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string Convert(DateTimeOffset value, string format)
		{
			return XmlConvert.ToString(value, format);
		}

		public static string Convert(DateTimeOffset? value, string format)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault(), format) : "";
		}

		public static string Convert(TimeSpan value)
		{
			return XmlConvert.ToString(value);
		}

		public static string Convert(TimeSpan? value)
		{
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
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
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
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
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
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
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
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
			return value.HasValue ? XmlConvert.ToString(value.GetValueOrDefault()) : "";
		}

		public static string? Convert(object? value, bool encodeQuote = false)
		{
			if (value == null)
				return null;

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

		public static string ConvertCollection(IEnumerable? collection, bool braces = false)
		{
			return ConvertCollection(new StringBuilder(), collection, braces).ToString();
		}

		public static StringBuilder ConvertCollection(StringBuilder text, IEnumerable? collection, bool braces = false)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (collection == null)
				return text;
			if (braces)
				text.Append('(');
			string pad = "";
			foreach (var item in collection)
			{
				text.Append(pad);
				string? s = Convert(item);
				if (s != null)
					text.Append(s);
				else if (item is IEnumerable ienum)
					ConvertCollection(text, ienum, true);
				pad = ",";
			}
			if (braces)
				text.Append(')');
			return text;
		}

		public static unsafe StringBuilder Encode(StringBuilder text, string? value, bool encodeQuote = false)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (value == null)
				return text;

			fixed (char* source = value)
			{
				return Encode(text, source, value.Length, encodeQuote) ?? text.Append(value);
			}
		}

		public static unsafe StringBuilder Encode(StringBuilder text, string? value, int start, int length, bool encodeQuote = false)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (value == null || length <= 0 || value.Length <= start || start < 0)
				return text;
			if (length > value.Length - start)
				length = value.Length - start;
			fixed (char* source = value)
			{
				return Encode(text, source + start, length, encodeQuote) ?? text.Append(value, start, length);
			}
		}

		private static unsafe StringBuilder? Encode(StringBuilder text, char* source, int length, bool encodeQuote = false)
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


		public static unsafe string Encode(string? value, bool encodeQuote = false)
		{
			if (value == null)
				return "";

			fixed (char* source = value)
			{
				return Encode(source, value.Length, encodeQuote) ?? value;
			}
		}

		public static unsafe string Encode(string? value, int start, int length, bool encodeQuote = false)
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

		public static unsafe string Encode(char[]? value, bool encodeQuote = false)
		{
			if (value == null)
				return "";

			fixed (char* source = value)
			{
				return Encode(source, value.Length, encodeQuote) ?? new String(value);
			}
		}

		public static unsafe string Encode(char[]? value, int start, int length, bool encodeQuote = false)
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

		public static string EncodeAttribute(string? value)
		{
			if (value == null || value.Length == 0)
				return "\"\"";

			return
				value.IndexOf('"') < 0 ? "\"" + Encode(value, false) + "\"" :
				value.IndexOf('\'') < 0 ? "'" + Encode(value, false) + "'" :
				"\"" + Encode(value, true) + "\"";
		}

		public static StringBuilder EncodeAttribute(StringBuilder text, string? value)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

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

		private static unsafe string? Encode(char* source, int length, bool encodeQuote = false)
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
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (value == null)
				throw new ArgumentNullException(nameof(value));

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
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			int i = value.IndexOf('&');
			if (i < 0)
				return value;
			fixed (char* source = value)
			{
				int n = value.Length;
				if (n <= MaxStackAllocSize / sizeof(char))
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
	}
}
