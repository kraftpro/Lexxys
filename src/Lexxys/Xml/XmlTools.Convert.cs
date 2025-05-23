using System.Buffers;
using System.Globalization;
using System.Text;

namespace Lexxys.Xml;

public static partial class XmlTools
{
	public static string Convert(bool value) => value ? "true" : "false";

	public static string Convert(Guid value) => value.ToString();

	public static string Convert(Guid? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	/// <summary>
	/// Convert DateTime to string in ISO 8601 format.
	/// </summary>
	/// <param name="value">The DateTime value to convert.</param>
	/// <param name="omitTimeZone">If true, the time zone will be omitted in the result.</param>
	/// <returns></returns>
	public static string Convert(DateTime value, bool omitTimeZone = false)
	{
		string format;
		if (value.Ticks % TimeSpan.TicksPerSecond != 0)
			format = omitTimeZone || value.Kind == DateTimeKind.Unspecified ? @"yyyy-MM-ddThh:mm:ss.fffff": value.Kind == DateTimeKind.Utc ? @"yyyy-MM-ddThh:mm:ss.fffff\Z" : @"yyyy-MM-ddThh:mm:ss.fffffzzz";
		else if (value.Ticks % TimeSpan.TicksPerDay != 0)
			format = omitTimeZone || value.Kind == DateTimeKind.Unspecified ? @"yyyy-MM-ddThh:mm:ss": value.Kind == DateTimeKind.Utc ? @"yyyy-MM-ddThh:mm:ss\Z" : @"yyyy-MM-ddThh:mm:sszzz";
		else
			format = omitTimeZone || value.Kind == DateTimeKind.Unspecified ? @"yyyy-MM-dd": value.Kind == DateTimeKind.Utc ? @"yyyy-MM-dd\Z" : @"yyyy-MM-ddzzz";
		return value.ToString(format, CultureInfo.InvariantCulture);
	}

	public static string Convert(DateTime? value, bool omitTimeZone = false) => value.HasValue ? Convert(value.GetValueOrDefault(), omitTimeZone): "";

	public static string Convert(DateTimeOffset value)
	{
		string format =
			value.Ticks % TimeSpan.TicksPerSecond != 0 ? @"yyyy-MM-ddThh:mm:ss.fffffzzz":
			value.Ticks % TimeSpan.TicksPerDay != 0 ? @"yyyy-MM-ddThh:mm:sszzz": @"yyyy-MM-ddzzz";
		return value.ToString(format, CultureInfo.InvariantCulture);
	}

	public static string Convert(DateTimeOffset? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(TimeSpan value)
	{
		StringBuilder text = new StringBuilder(12);
		long ticks = value.Ticks;
		if (ticks < 0)
		{
			text.Append('-');
			ticks = -ticks;
		}
		text.Append('P');
		if (ticks >= TimeSpan.TicksPerDay)
		{
			text.Append(ticks / TimeSpan.TicksPerDay).Append('D');
			ticks %= TimeSpan.TicksPerDay;
			if (ticks == 0)
				return text.ToString();
		}
		text.Append('T');
		if (ticks >= TimeSpan.TicksPerHour)
		{
			text.Append(ticks / TimeSpan.TicksPerHour).Append('H');
			ticks %= TimeSpan.TicksPerHour;
		}
		if (ticks >= TimeSpan.TicksPerMinute)
		{
			text.Append(ticks / TimeSpan.TicksPerMinute).Append('M');
			ticks %= TimeSpan.TicksPerMinute;
		}
		if (ticks > 0)
		{
			text.Append(ticks / TimeSpan.TicksPerSecond);
			ticks %= TimeSpan.TicksPerSecond;
			if (ticks > 0)
				text.Append('.').Append(ticks.ToString("d7").TrimEnd('0'));
			text.Append('S');
		}
		return text.Length < 4 ? "P0D": text.ToString();
	}

#if NET6_0_OR_GREATER

	public static string Convert(DateOnly value) => value.ToString("o");

	public static string Convert(DateOnly? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(TimeOnly value) => value.Ticks % TimeSpan.TicksPerSecond == 0 ? value.ToString("o").Substring(0, 8): value.ToString("o").TrimEnd('0');

	public static string Convert(TimeOnly? value) => value.HasValue ? Convert(value.GetValueOrDefault()) : "";

#endif

	public static string Convert(TimeSpan? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(sbyte value) => value.ToString();

	public static string Convert(sbyte? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(byte value) => value.ToString();

	public static string Convert(byte? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(short value) => value.ToString();

	public static string Convert(short? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(ushort value) => value.ToString();

	public static string Convert(ushort? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(int value) => value.ToString();

	public static string Convert(int? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(uint value) => value.ToString();

	public static string Convert(uint? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(long value) => value.ToString();

	public static string Convert(long? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(ulong value) => value.ToString();

	public static string Convert(ulong? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(decimal value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Convert(decimal? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(float value) =>
		Single.IsPositiveInfinity(value) ? "INF":
		Single.IsNegativeInfinity(value) ? "-INF":
		value.ToString("R", NumberFormatInfo.InvariantInfo);

	public static string Convert(float? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

	public static string Convert(double value) =>
		Double.IsPositiveInfinity(value) ? "INF":
		Double.IsNegativeInfinity(value) ? "-INF":
		value.ToString("R", NumberFormatInfo.InvariantInfo);

	public static string Convert(double? value) => value.HasValue ? Convert(value.GetValueOrDefault()): "";

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
					return Convert((byte)value);
				case TypeCode.SByte:
					return Convert((sbyte)value);
				case TypeCode.Char:
					return Convert((char)value);
				case TypeCode.Decimal:
					return Convert((decimal)value);
				case TypeCode.Single:
					return Convert((float)value);
				case TypeCode.Double:
					return Convert((double)value);
				case TypeCode.Int16:
					return Convert((short)value);
				case TypeCode.UInt16:
					return Convert((ushort)value);
				case TypeCode.Int32:
					return Convert((int)value);
				case TypeCode.UInt32:
					return Convert((uint)value);
				case TypeCode.Int64:
					return Convert((long)value);
				case TypeCode.UInt64:
					return Convert((ulong)value);
				case TypeCode.DBNull:
				case TypeCode.Empty:
					return "";
				case TypeCode.DateTime:
					return Convert((DateTime)value);
				case TypeCode.String:
					return Encode(((string)value).AsSpan(), encodeQuote);
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

	//public static string ConvertCollection(IEnumerable? collection, bool braces = false)
	//{
	//	return ConvertCollection(new StringBuilder(), collection, braces).ToString();
	//}

	//public static StringBuilder ConvertCollection(StringBuilder text, IEnumerable? collection, bool braces = false)
	//{
	//	if (text == null)
	//		throw new ArgumentNullException(nameof(text));
	//	if (collection == null)
	//		return text;
	//	if (braces)
	//		text.Append('(');
	//	string pad = "";
	//	foreach (var item in collection)
	//	{
	//		text.Append(pad);
	//		string? s = Convert(item);
	//		if (s != null)
	//			text.Append(s);
	//		else if (item is IEnumerable ienum)
	//			ConvertCollection(text, ienum, true);
	//		pad = ",";
	//	}
	//	if (braces)
	//		text.Append(')');
	//	return text;
	//}

	public static StringBuilder Encode(StringBuilder text, ReadOnlySpan<char> value, bool encodeQuote = false)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));

		if (value.Length == 0) return text;
		int size = ProjectedSize(value);
		var local = size > Tools.MaxStackAllocSizeChar ? new LocalStringBuilder(size): new LocalStringBuilder(stackalloc char[size]);
		Encode(value, encodeQuote, ref local);
		text.Append(local.AsSpan());
		local.Dispose();
		return text;
	}

	public static string Encode(string value, bool encodeQuote = false) => Encode(value.AsSpan(), encodeQuote);

	public static string Encode(ReadOnlySpan<char> value, bool encodeQuote = false)
	{
		if (value.Length == 0) return String.Empty;
		int size = ProjectedSize(value);
		var local = size > Tools.MaxStackAllocSizeChar ? new LocalStringBuilder(size): new LocalStringBuilder(stackalloc char[size]);
		Encode(value, encodeQuote, ref local);
		var result = local.AsSpan().ToString();
		local.Dispose();
		return result;
	}

	public static string EncodeAttribute(string value) => EncodeAttribute(value.AsSpan());

	public static string EncodeAttribute(ReadOnlySpan<char> value)
	{
		if (value.Length == 0) return "\"\"";
		int size = ProjectedSize(value) + 2;
		var local = size > Tools.MaxStackAllocSizeChar ? new LocalStringBuilder(size): new LocalStringBuilder(stackalloc char[size]);
		local.Append('"');
		Encode(value, true, ref local);
		local.Append('"');
		var result = local.AsSpan().ToString();
		local.Dispose();
		return result;
	}

	private static int ProjectedSize(ReadOnlySpan<char> value) => value.Length + 86 < Tools.MaxStackAllocSizeChar ? Tools.MaxStackAllocSizeChar: value.Length + 256;

	public static StringBuilder EncodeAttribute(StringBuilder text, ReadOnlySpan<char> value)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));
		text.Append('"');
		Encode(text, value, true);
		return text.Append('"');
	}

	private static void Encode(ReadOnlySpan<char> value, bool encodeQuote, ref LocalStringBuilder text)
	{
		int i;
		ReadOnlySpan<char> special = encodeQuote ? "<>&\"'".AsSpan(): "<>&".AsSpan();
		while ((i = value.IndexOfAny(special)) >= 0)
		{
			text.Append(value.Slice(0, i));
			text.Append(value[i] switch
				{
					'<' => "&lt;".AsSpan(),
					'>' => "&gt;".AsSpan(),
					'"' => "&quot;".AsSpan(),
					'\'' => "&#39;".AsSpan(),
					'&' => "&amp;".AsSpan(),
					_ => ReadOnlySpan<char>.Empty,
				});
			value = value.Slice(i + 1);
		}
		text.Append(value);
	}

	public static unsafe StringBuilder Decode(StringBuilder text, ReadOnlySpan<char> value)
	{
		if (text == null) throw new ArgumentNullException(nameof(text));

		int i = value.IndexOf('&');
		if (i < 0)
			return text.Append(value);
		int vlen = value.Length;
		Span<char> buffer = vlen > Tools.MaxStackAllocSizeChar ? new char[vlen] : stackalloc char[vlen];
		var n = Decode(value, buffer);
		return text.Append(buffer.Slice(0, n));
	}

	public static unsafe string Decode(ReadOnlySpan<char> value)
	{
		int i = value.IndexOf('&');
		if (i < 0)
			return value.ToString();
		int vlen = value.Length;
		Span<char> buffer = vlen > Tools.MaxStackAllocSizeChar ? new char[vlen] : stackalloc char[vlen];
		var n = Decode(value, buffer);
		return buffer.Slice(0, n).ToString();
	}

	private static int Decode(ReadOnlySpan<char> source, Span<char> target)
	{
		int len = 0;
		int i;
		while ((i = source.IndexOf('&')) >= 0)
		{
			source.Slice(0, i).CopyTo(target);
			target = target.Slice(i);
			source = source.Slice(i);
			len += i;
			if (source.Length < 4) goto exit;
			switch (source[1])
			{
				case 'l':
					if (source[2] != 't' || source[3] != ';')
						goto default;
					target[0] = '<';
					target = target.Slice(1);
					source = source.Slice(4);
					++len;
					break;

				case 'g':
					if (source[2] != 't' || source[3] != ';')
						goto default;
					target[0] = '>';
					target = target.Slice(1);
					source = source.Slice(4);
					++len;
					break;

				case 'q':
					if (source.Length < 6) goto default;
					if (source[2] != 'u' || source[3] != 'o' || source[4] != 't' || source[5] != ';')
						goto default;
					target[0] = '"';
					target = target.Slice(1);
					source = source.Slice(6);
					++len;
					break;

				case 'a':
					if (source[2] == 'm')
					{
						if (source.Length < 5) goto exit;
						if (source[3] != 'p' || source[4] != ';')
							goto default;
						target[0] = '&';
						source = source.Slice(5);
					}
					else
					{
						if (source.Length < 6) goto default;
						if (source[2] != 'p' || source[3] != 'o' || source[4] != 's' || source[5] != ';')
							goto default;
						target[0] = '\'';
						source = source.Slice(6);
					}
					target = target.Slice(1);
					++len;
					break;

				case '#':
					int k = 2;
					int d = 0;
					while (source.Length > k && source[k] is >= '0' and <= '9')
					{
						d = d * 10 + (source[k] - '0');
						++k;
					}
					if (k == 2 || source.Length == k || source[k] != ';' || d > 0xFFFF)
						goto default;
					target[0] = (char)d;
					target = target.Slice(1);
					source = source.Slice(k + 1);
					++len;
					break;

				default:
					target[0] = '&';
					target = target.Slice(1);
					source = source.Slice(1);
					++len;
					break;
			}
		}
		exit:
		source.CopyTo(target);
		return len + source.Length;
	}

	private ref struct LocalStringBuilder
	{
		private char[]? _shared;
		private Span<char> _buffer;
		private int _length;

		public LocalStringBuilder(Span<char> buffer)
		{
			_buffer = buffer;
		}

		public LocalStringBuilder(int capacity)
		{
			_shared = ArrayPool<char>.Shared.Rent(capacity);
			_buffer = _shared;
		}

		public Span<char> AsSpan() => _buffer.Slice(0, _length);

		public void Append(ReadOnlySpan<char> value)
		{
			if (value.Length == 0) return;
			int len = _length;
			int vlen = value.Length;
			if (len + vlen > _buffer.Length)
				Grow(vlen);
			if (vlen == 1)
				_buffer[len] = value[0];
			else
				value.CopyTo(_buffer.Slice(len));
			_length = len + vlen;
		}

		public void Append(char value)
		{
			int len = _length;
			if (len == _buffer.Length)
				Grow(1);
			_buffer[len] = value;
			_length = len + 1;
		}

		private void Grow(int extra)
		{
			int size = Math.Max(extra, _buffer.Length * 2);
			var temp = ArrayPool<char>.Shared.Rent(size);
			_buffer.Slice(0, _length).CopyTo(temp);
			if (_shared != null)
				ArrayPool<char>.Shared.Return(_shared);
			_buffer = temp;
			_shared = temp;
		}

		public void Dispose()
		{
			var temp = _shared;
			this = default;
			if (temp != null)
				ArrayPool<char>.Shared.Return(temp);
		}
	}
}
