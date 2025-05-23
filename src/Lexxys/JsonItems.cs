// Lexxys Infrastructural library.
// file: JsonItems.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Xml;

using static System.Net.Mime.MediaTypeNames;

namespace Lexxys;


[Serializable]
public abstract class JsonItem
{
	protected const string NullValue = "null";
	protected const string TrueValue = "true";
	protected const string FalseValue = "false";
	protected const string NaNValue = "NaN";
	protected static readonly byte[] NullBytes = [(byte)'n', (byte)'u', (byte)'l', (byte)'l'];
	protected static readonly byte[] TrueBytes = [(byte)'t', (byte)'r', (byte)'u', (byte)'e'];
	protected static readonly byte[] FalseBytes = [(byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e'];


	public IReadOnlyList<JsonPair> Attributes { get; }
	public virtual object? Value => null;
	public virtual JsonItem? this[string item] => null;
	public virtual JsonItem? this[int index] => null;
	public virtual bool IsArray => false;
	public virtual bool IsObject => false;
	public virtual bool IsScalar => false;
	public virtual int Count => 0;
	public string Text => Value switch
	{
		null => NullValue,
		string s => s,
		bool b => b ? TrueValue : FalseValue,
		DateTime d => XmlConvert.ToString(d, XmlDateTimeSerializationMode.RoundtripKind),
		DateTimeOffset x => XmlConvert.ToString(x),
		TimeSpan t => XmlConvert.ToString(t),
		byte[] y => Convert.ToBase64String(y, Base64FormattingOptions.None),
		IConvertible c => c.ToString(null),
		_ => Value.ToString() ?? String.Empty
	};

	protected JsonItem() => Attributes = [];

	protected JsonItem(IReadOnlyList<JsonPair>? attributes) => Attributes = attributes ?? [];

	public virtual StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (Attributes.Count == 0)
			return text;

		string separator = indent == null ? "," : ", ";
		string comma = "(";
		foreach (var attrib in Attributes)
		{
			text.Append(comma);
			attrib.ToString(text, indent, stringLimit, arrayLimit);
			comma = separator;
		}
		text.Append(')');
		return text;
	}

	public virtual void Write(Stream stream)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (Attributes.Count == 0)
			return;

		stream.Write((byte)'(');
		bool next = false;
		foreach (var attribute in Attributes)
		{
			if (next)
				stream.Write((byte)',');
			else
				next = true;
			attribute.Write(stream);
		}
		stream.Write((byte)')');
	}

	public string ToString(bool format, int stringLimit = 0, int arrayLimit = 0)
		=> ToString(new StringBuilder(), format ? "" : null, stringLimit, arrayLimit).ToString();

	public override string ToString() => ToString(new StringBuilder()).ToString();
}

[Serializable]
public readonly struct JsonPair: IEquatable<JsonPair>
{
	public string Name { get; }
	public JsonItem Item { get; }

	public JsonPair(string name, JsonItem item)
	{
		if (name is not { Length: >0 })
			throw new ArgumentNullException(nameof(name));
		if (item is null)
			throw new ArgumentNullException(nameof(item));

		Name = name;
		Item = item;
	}

	public void Deconstruct(out string name, out JsonItem item)
	{
		name = Name;
		item = Item;
	}

	public bool IsEmpty => Name.Length == 0;

	public StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		if (IsEmpty)
			return text;
		Strings.EscapeCsString(text, Name);
		text.Append(indent == null ? ":" : ": ");
		if (Item is null)
			text.Append("null");
		else
			Item.ToString(text, indent, stringLimit, arrayLimit);
		return text;
	}

	public void Write(Stream stream)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (IsEmpty)
			return;
		stream.Write(Encoding.UTF8.GetBytes(Strings.EscapeCsString(Name)));
		stream.Write((byte)':');
		if (Item is null)
			stream.Write(NullBytes);
		else
			Item.Write(stream);
	}
	private static readonly byte[] NullBytes = [(byte)'n', (byte)'u', (byte)'l', (byte)'l'];

	public string ToString(bool format, bool pair = false)
	{
		if (IsEmpty)
			return "";
		if (format)
			return pair ? ToString(new StringBuilder(), "").ToString() : ToString(new StringBuilder().Append("{\n  "), "  ").Append("\n}").ToString();
		else
			return pair ? ToString(new StringBuilder()).ToString() : ToString(new StringBuilder().Append('{')).Append('}').ToString();
	}

	public override string ToString()
	{
		return ToString(false);
	}

	public bool Equals(JsonPair other) => Name == other.Name && Item == other.Item;

	public override bool Equals([NotNullWhen(true)] object? obj) => obj is JsonPair other && Equals(other);

	public override int GetHashCode() => HashCode.Join(Name?.GetHashCode() ?? 0, Item.GetHashCode());

	public static bool operator == (JsonPair left, JsonPair right) => left.Equals(right);

	public static bool operator != (JsonPair left, JsonPair right) => !left.Equals(right);
}


[Serializable]
public class JsonScalar: JsonItem
{
	public static readonly JsonScalar Null = new JsonScalar(null);
	public static readonly JsonScalar True = new JsonScalar(true);
	public static readonly JsonScalar False = new JsonScalar(false);
	public static readonly JsonScalar NaN = new JsonScalar(double.NaN);

	public override object? Value { get; }
	public override bool IsScalar => true;

	public JsonScalar(object? value)
	{
		Value = value;
	}

	public JsonScalar(object? value, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		Value = value;
	}

	public string StringValue => Text;

	public bool BooleanValue => Value switch
	{
		bool v => v,
		IConvertible ic => ic.ToBoolean(null),
		_ => false
	};

	public DateTime DateTimeValue => Value switch
	{
		null => default,
		DateTime v => v,
		DateTimeOffset o => o.DateTime,
		IConvertible i => i.ToDateTime(null),
		_ => default
	};

	public DateTimeOffset DateTimeOffsetValue => Value switch
	{
		null => default,
		DateTimeOffset o => o,
		DateTime v => v,
		IConvertible i => i.ToDateTime(null),
		_ => default
	};

	public double DoubleValue => Value switch
	{
		null => default,
		double v => v,
		IConvertible i => i.ToDouble(null),
		_ => default
	};

	public decimal DecimalValue => Value switch
	{
		null => default,
		decimal v => v,
		IConvertible i => i.ToDecimal(null),
		_ => default
	};

	public int IntValue => Value switch
	{
		null => default,
		int v => v,
		IConvertible i => i.ToInt32(null),
		_ => default
	};

	public long LongValue => Value switch
	{
		null => default,
		long v => v,
		IConvertible i => i.ToInt64(null),
		_ => default
	};

	public byte[] BytesValue => Value switch
	{
		null => [],
		byte[] v => v,
		_ => []
	};

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int maxValueLength = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, maxValueLength, arrayLimit);
		return Value switch
		{
			null => text.Append("null"),
			string s => Escape(text, s, maxValueLength),
			bool bl => text.Append(bl ? "true" : "false"),
			DateTime dt => text.Append('"').Append(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind)).Append('"'),
			DateTimeOffset dtx => text.Append('"').Append(XmlConvert.ToString(dtx)).Append('"'),
			TimeSpan tm => text.Append('"').Append(XmlConvert.ToString(tm)).Append('"'),
			byte[] ba => text.Append('"').Append(Convert.ToBase64String(ba, Base64FormattingOptions.None)).Append('"'),
			byte b => text.Append(b),
			sbyte sb => text.Append(sb),
			short h => text.Append(h),
			ushort uh => text.Append(uh),
			int i => text.Append(i),
			uint ui => text.Append(ui),
			long l => text.Append(l),
			ulong ul => text.Append(ul),
#if NET6_0_OR_GREATER
			float f => Single.IsFinite(f) ? text.Append(f): text.Append(NaNValue),
			double d => Double.IsFinite(d) ? text.Append(d): text.Append(NaNValue),
#else
			float f => Single.IsNaN(f) || Single.IsInfinity(f) ? text.Append(f) : text.Append(NaNValue),
			double d => Double.IsNaN(d) || Double.IsInfinity(d) ? text.Append(d) : text.Append(NaNValue),
#endif
			decimal m => text.Append(m),
			_ => Escape(text, Value.ToString() ?? String.Empty, maxValueLength)
		};

		static StringBuilder Escape(StringBuilder text, string s, int maxValueLength)
		{
			if (maxValueLength > 0 && s.Length > maxValueLength - 3)
			{
				Strings.EscapeCsString(text, s.AsSpan(0, Math.Min(maxValueLength, 0)));
				--text.Length;
				text.Append("...\"");
			}
			else
			{
				Strings.EscapeCsString(text, s);
			}
			return text;
		}
	}

	public override void Write(Stream stream)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		const int Size = 50;
		Span<byte> digits = stackalloc byte[Size];
		base.Write(stream);
		switch (Value)
		{
			case null:
				stream.Write(NullBytes);
				break;
			case string s:
				Strings.EscapeUtf8CsString(stream, s.AsSpan());
				break;
			case bool bl:
				stream.Write(bl ? TrueBytes: FalseBytes);
				break;
			case DateTime dt:
				stream.Write(Quote);
				WriteDateTime(dt, stream);
				stream.Write(Quote);
				break;
			case DateTimeOffset dtx:
				stream.Write(Quote);
				WriteDateTimeOffset(dtx, stream);
				stream.Write(Quote);
				break;
			case TimeSpan tm:
				stream.Write(Quote);
				WriteTimeSpan(tm, stream);
				stream.Write(Quote);
				break;
			case byte[] ba:
				stream.Write(Quote);
				ToBase64(ba, stream);
				stream.Write(Quote);
				break;
			case byte b:
				stream.Write(digits.Slice(DigitsUInt(digits, Size, b)));
				break;
			case sbyte sb:
				stream.Write(digits.Slice(DigitsInt(digits, Size, sb)));
				break;
			case short h:
				stream.Write(digits.Slice(DigitsInt(digits, Size, h)));
				break;
			case ushort uh:
				stream.Write(digits.Slice(DigitsUInt(digits, Size, uh)));
				break;
			case int i:
				stream.Write(digits.Slice(DigitsInt(digits, Size, i)));
				break;
			case uint ui:
				stream.Write(digits.Slice(DigitsUInt(digits, Size, ui)));
				break;
			case long l:
				stream.Write(digits.Slice(DigitsLong(digits, Size, l)));
				break;
			case ulong ul:
				stream.Write(digits.Slice(DigitsULong(digits, Size, ul)));
				break;
			case float f:
#if NET8_0_OR_GREATER
				if (Single.IsFinite(f) && f.TryFormat(digits, out var nf))
					stream.Write(digits.Slice(0, nf));
				else
					stream.Write(NullBytes);
#else
				if (Single.IsNaN(f) || Single.IsInfinity(f))
					stream.Write(NullBytes);
				else
					stream.Write(Encoding.UTF8.GetBytes(f.ToString(CultureInfo.InvariantCulture)));
#endif
				break;
			case double d:
#if NET8_0_OR_GREATER
				if (Double.IsFinite(d) && d.TryFormat(digits, out var nd))
					stream.Write(digits.Slice(0, nd));
				else
					stream.Write(NullBytes);
#else
				if (Double.IsNaN(d) || Double.IsInfinity(d))
					stream.Write(NullBytes);
				else
					stream.Write(Encoding.UTF8.GetBytes(d.ToString(CultureInfo.InvariantCulture)));
#endif
				break;
			case decimal m:
#if NET8_0_OR_GREATER
				m.TryFormat(digits, out var nm);
				stream.Write(digits.Slice(0, nm));
#else
				stream.Write(Encoding.UTF8.GetBytes(m.ToString(CultureInfo.InvariantCulture)));
#endif
				break;

			default:
				Strings.EscapeUtf8CsString(stream, (Value.ToString() ?? String.Empty).AsSpan());
				break;
		}

		static void ToBase64(byte[] data, Stream stream)
		{
			const int BufferSize = 4096;
			var base64 = ArrayPool<byte>.Shared.Rent(BufferSize);
			var buffer = base64.AsSpan();
			int left = data.Length;
			var bytes = data.AsSpan();
			while (left > 0)
			{
				Base64.EncodeToUtf8(bytes, buffer, out int count, out int written);
				left -= count;
				bytes = bytes.Slice(count);
				stream.Write(base64, 0, written);
			}
			ArrayPool<byte>.Shared.Return(base64);
		}

		static void WriteTimeSpan(TimeSpan value, Stream stream)
		{
			const int TicksPerSecond = 100000;
			long ticks = value.Ticks >= Int64.MaxValue / TimeSpan.TicksPerSecond ? Int64.MaxValue / (TimeSpan.TicksPerSecond * TicksPerSecond): (value.Ticks / (TimeSpan.TicksPerSecond / TicksPerSecond * 10) + 5) / 10;
			
			if (ticks == 0)
			{
				stream.Write(ZeroTime);
				return;
			}
			Span<byte> mem = stackalloc byte[50];
			int index = 50;

			int z = (int)(ticks % (60 * TicksPerSecond));
			ticks /= 60 * TicksPerSecond;
			if (z > 0)
			{
				mem[--index] = (byte)'S';
				var c = z % TicksPerSecond;
				if (c > 0)
				{
					index = Digits(mem, index, c);
					mem[--index] = (byte)'.';
				}
				index = Digits(mem, index, z / TicksPerSecond);
			}
			z = (int)(ticks % 60);
			ticks /= 60;
			if (z > 0)
				index = TimePart(mem, index, z, 'M');

			z = (int)(ticks % 24);
			ticks /= 24;
			if (z > 0)
				index = TimePart(mem, index, z, 'H');
			if (ticks > 0)
				index = TimePart(mem, index, (int)ticks, 'D');
			mem[--index] = (byte)'P';
			mem[--index] = (byte)'T';
			stream.Write(mem.Slice(index));
		}

		static int TimePart(Span<byte> mem, int index, int value, char sign)
		{
			if (value <= 0)
				return index;
			mem[--index] = (byte)sign;
			return Digits(mem, index, value);
		}

		static int Digits(Span<byte> mem, int index, int value)
		{
			do
			{
				mem[--index] = (byte)(value % 10 + '0');
				value /= 10;
			} while (value > 0);
			return index;
		}

		static int DigitsInt(Span<byte> mem, int index, int value)
		{
			if (value >= 0)
				return DigitsUInt(mem, index, (uint)value);
			index = DigitsUInt(mem, index, (uint)(-value));
			mem[--index] = (byte)'-';
			return index;
		}

		static int DigitsUInt(Span<byte> mem, int index, uint value)
		{
			do
			{
				mem[--index] = (byte)(value % 10 + '0');
				value /= 10;
			} while (value > 0);
			return index;
		}

		static int DigitsLong(Span<byte> mem, int index, long value)
		{
			if (value >= 0)
				return DigitsULong(mem, index, (ulong)value);
			index = DigitsULong(mem, index, (ulong)(-value));
			mem[--index] = (byte)'-';
			return index;
		}

		static int DigitsULong(Span<byte> mem, int index, ulong value)
		{
			do
			{
				mem[--index] = (byte)(value % 10 + '0');
				value /= 10;
			} while (value > 0);
			return index;
		}

		static void WriteDateTime(DateTime value, Stream stream)
		{
			Span<byte> mem = stackalloc byte[50];
			int index = PutDateTime(mem, 50, value);
			stream.Write(mem.Slice(index));
		}

		static int PutDateTime(Span<byte> mem, int index, DateTime value)
		{
			const int TicksPerSecond = 100000;

			long time = (value.TimeOfDay.Ticks / (TimeSpan.TicksPerSecond * TicksPerSecond * 10) + 5) / 10;
			if (time > 0)
			{
				int z = (int)(time % (60 * TicksPerSecond));
				int fraction = z % TicksPerSecond;
				if (fraction > 0)
				{
					index = Digits(mem, index, fraction);
					mem[--index] = (byte)'.';
				}
				index = Digits2(mem, index, z / TicksPerSecond);
				int minutes = (int)(time / 60 * TicksPerSecond);
				mem[--index] = Colon;
				index = Digits2(mem, index, minutes % 60);
				mem[--index] = Colon;
				index = Digits2(mem, index, minutes / 60);
				mem[--index] = (byte)'T';
			}
			index = Digits2(mem, index, value.Day);
			mem[--index] = Colon;
			index = Digits2(mem, index, value.Month);
			mem[--index] = Colon;
			index = Digits4(mem, index, value.Year);
			return index;
		}

		static void WriteDateTimeOffset(DateTimeOffset value, Stream stream)
		{
			Span<byte> mem = stackalloc byte[50];
			int index = 50;

			int offset = (int)(value.Offset.Ticks / TimeSpan.TicksPerMinute);
			if (offset != 0)
			{
				if (offset % 60 > 0)
				{
					index = Digits2(mem, index, offset % 60);
					mem[--index] = Colon;
				}
				index = Digits2(mem, index, offset / 60);
				mem[--index] = offset < 0 ? (byte)'-' : (byte)'+';
			}
			else
			{
				mem[--index] = (byte)'Z';
			}
			index = PutDateTime(mem, index, value.DateTime);
			stream.Write(mem.Slice(index));
		}

		static int Digits4(Span<byte> mem, int index, int value)
		{
			mem[--index] = (byte)(value % 10 + '0');
			value /= 10;
			mem[--index] = (byte)(value % 10 + '0');
			value /= 10;
			mem[--index] = (byte)(value % 10 + '0');
			value /= 10;
			mem[--index] = (byte)(value % 10 + '0');
			return index;
		}

		static int Digits2(Span<byte> mem, int index, int value)
		{
			mem[--index] = (byte)(value % 10 + '0');
			value /= 10;
			mem[--index] = (byte)(value % 10 + '0');
			return index;
		}
	}
	private const byte Colon = (byte)':';
	private const byte Quote = (byte)'"';
	private static readonly byte[] ZeroTime = [(byte)'P', (byte)'T', (byte)'0', (byte)'S'];
}

[Serializable]
public class JsonMap: JsonItem, IEnumerable<JsonPair>
{
	public IReadOnlyList<JsonPair> Properties { get; }

	public override JsonItem? this[string name] => Properties.FirstOrDefault(o => o.Name == name).Item;
	public override JsonItem? this[int index] => index >= 0 && index < Properties.Count ? Properties[index].Item: null;
	public override int Count => Properties.Count;
	public override bool IsObject => true;

	public JsonMap(IReadOnlyList<JsonPair> properties)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		Properties = ReadOnly.ReWrap(properties);
	}

	public JsonMap(IWrappedList<JsonPair> properties)
	{
		Properties = properties ?? throw new ArgumentNullException(nameof(properties));
	}

	public JsonMap(IReadOnlyList<JsonPair> properties, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		Properties = ReadOnly.ReWrap(properties);
	}

	public JsonMap(IWrappedList<JsonPair> properties, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		Properties = properties ?? throw new ArgumentNullException(nameof(properties));
	}

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, stringLimit, arrayLimit);
		text.Append('{');
		if (Properties.Count > 0)
		{
			string? indent2 = indent == null ? null : indent + "  ";
			string comma = "";
			foreach (var item in Properties)
			{
				if (item.IsEmpty)
					continue;
				if (indent2 == null)
					text.Append(comma);
				else
					text.AppendLine(comma).Append(indent2);
				item.ToString(text, indent2, stringLimit, arrayLimit);
				comma = ",";
			}
			if (comma.Length > 0 && indent != null)
				text.AppendLine().Append(indent);
		}
		text.Append('}');
		return text;
	}

	public override void Write(Stream stream)
	{
		base.Write(stream);
		stream.Write((byte)'{');
		if (Properties.Count > 0)
		{
			bool next = false;
			foreach (var item in Properties)
			{
				if (item.IsEmpty)
					continue;
				if (next)
					stream.Write((byte)',');
				else
					next = true;
				item.Write(stream);
			}
		}
		stream.Write((byte)'}');
	}

	public IEnumerator<JsonPair> GetEnumerator() => Properties.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => Properties.GetEnumerator();
}

[Serializable]
public class JsonArray: JsonItem, IEnumerable<JsonItem>
{
	public IReadOnlyList<JsonItem> Items { get; }
	public override JsonItem? this[int index] => index >= 0 && index < Items.Count ? Items[index]: null;
	public override int Count => Items.Count;
	public override bool IsArray => true;

	public JsonArray(IReadOnlyList<JsonItem> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = ReadOnly.ReWrap(items);
	}

	public JsonArray(IWrappedList<JsonItem> items)
	{
		Items = items ?? throw new ArgumentNullException(nameof(items));
	}

	public JsonArray(IReadOnlyList<JsonItem> items, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = ReadOnly.ReWrap(items);
	}

	public JsonArray(IWrappedList<JsonItem> items, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		Items = items ?? throw new ArgumentNullException(nameof(items));
	}

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, stringLimit, arrayLimit);
		text.Append('[');
		string? indent2 = indent == null ? null : indent + "  ";
		string comma = "";
		int i = 0;
		foreach (JsonItem item in Items)
		{
			if (indent == null)
				text.Append(comma);
			else
				text.AppendLine(comma).Append(indent2);
			if (arrayLimit > 0 && ++i >= arrayLimit)
			{
				text.Append("...");
				break;
			}
			item.ToString(text, indent2, stringLimit, arrayLimit);
			comma = ",";
		}
		if (comma.Length > 0 && indent != null)
			text.AppendLine().Append(indent);
		text.Append(']');
		return text;
	}

	public override void Write(Stream stream)
	{
		base.Write(stream);
		stream.Write((byte)'[');
		bool next = false;
		foreach (JsonItem item in Items)
		{
			if (next)
				stream.Write((byte)',');
			else
				next = true;
			item.Write(stream);
		}
		stream.Write((byte)']');
	}

	public IEnumerator<JsonItem> GetEnumerator() => Items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
}


public static class ZenJson
{
	// JsonScalar

	public static JsonScalar J(string value) => new JsonScalar(value);
	public static JsonScalar J(object value) => new JsonScalar(value);

	// JsonMap

	public static JsonMap J(JsonPair pair) => new JsonMap([pair]);
	public static JsonMap J(params JsonPair[] pair) => new JsonMap(pair);
	public static JsonMap J(List<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IList<JsonPair> pair) => new JsonMap(ReadOnly.Wrap(pair));
	public static JsonMap J(IReadOnlyList<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IEnumerable<JsonPair> pair) => new JsonMap(ReadOnly.WrapCopy(pair));

	// JsonArray

	public static JsonArray J(JsonItem pair) => new JsonArray([pair]);
	public static JsonArray J(params JsonItem[] pair) => new JsonArray(pair);
	public static JsonArray J(List<JsonItem> pair) => new JsonArray(pair);
	public static JsonArray J(IList<JsonItem> pair) => new JsonArray(ReadOnly.Wrap(pair));
	public static JsonArray J(IReadOnlyList<JsonItem> pair) => new JsonArray(pair);
	public static JsonArray J(IEnumerable<JsonItem> pair) => new JsonArray(ReadOnly.WrapCopy(pair));

	// JsonPair

	public static JsonPair J(string name, JsonItem value) => new JsonPair(name, value);
	public static JsonPair J(string name, object value) => new JsonPair(name, new JsonScalar(value));

	public static JsonPair J(string name, params JsonPair[] value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, List<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.Wrap(value)));
	public static JsonPair J(string name, IList<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.Wrap(value)));
	public static JsonPair J(string name, IReadOnlyList<JsonPair> value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, IEnumerable<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.WrapCopy(value)));

	public static JsonPair J(string name, params JsonItem[] value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, List<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IList<JsonItem> value) => new JsonPair(name, new JsonArray(ReadOnly.Wrap(value)));
	public static JsonPair J(string name, IReadOnlyList<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IEnumerable<JsonItem> value) => new JsonPair(name, new JsonArray(ReadOnly.WrapCopy(value)));
}
