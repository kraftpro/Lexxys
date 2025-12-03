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

namespace Lexxys;


/// <summary>
/// Represents an abstract item in a JSON structure, providing access to its value, type information, and optional attributes.
/// </summary>
/// <remarks>The JsonItem class serves as the base for all JSON element representations, including objects,
/// arrays, and scalar values. It exposes properties and indexers for querying attributes and values, and supports
/// serialization to text and streams. Derived types implement specific behaviors for different JSON element types. This
/// class is thread-safe for read-only operations, but thread safety for mutation depends on derived
/// implementations.</remarks>
public abstract class JsonItem
{
	protected const string NullValue = "null";
	protected const string TrueValue = "true";
	protected const string FalseValue = "false";
	protected const string NaNValue = "NaN";
	protected internal static readonly byte[] NullBytes = "null"u8.ToArray();
	protected internal static readonly byte[] TrueBytes = "true"u8.ToArray();
	protected internal static readonly byte[] FalseBytes = "false"u8.ToArray();


	public IReadOnlyList<JsonPair> Attributes { get; }
	public virtual object? Value => null;
	public virtual bool IsArray => false;
	public virtual bool IsObject => false;
	public virtual bool IsScalar => false;
	public virtual int Count => 0;
	public string Text => Value switch
	{
		null => NullValue,
		string s => s,
		bool b => b ? TrueValue: FalseValue,
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

		string separator = indent == null ? ",": ", ";
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
		=> ToString(new StringBuilder(), format ? "": null, stringLimit, arrayLimit).ToString();

	public override string ToString() => ToString(new StringBuilder()).ToString();
}

/// <summary>
/// Represents a name-value pair in a JSON object, where the name is a string and the value is a JSON item.
/// </summary>
/// <remarks>A JsonPair is typically used to model a property within a JSON object, associating a property name
/// with its corresponding value. The struct is immutable and supports equality comparison. The name must be a non-empty
/// string, and the value cannot be null. Use the IsEmpty property to determine if the pair is empty, which occurs when
/// the name is an empty string.</remarks>
public readonly struct JsonPair: IEquatable<JsonPair>
{
	public string Name { get; }
	public JsonItem Item { get; }

	public JsonPair(string name, JsonItem item)
	{
		if (name is not { Length: >0 }) throw new ArgumentNullException(nameof(name));
		Name = name;
		Item = item ?? throw new ArgumentNullException(nameof(item));
	}

	public void Deconstruct(out string name, out JsonItem item)
	{
		name = Name;
		item = Item;
	}

	public bool IsEmpty => Name.Length == 0;

	public StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));
		if (IsEmpty) return text;

		Strings.EscapeCsString(text, Name);
		text.Append(indent == null ? ":": ": ");
		if (Item is null)
			text.Append("null");
		else
			Item.ToString(text, indent, stringLimit, arrayLimit);
		return text;
	}

	public void Write(Stream stream)
	{
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		if (IsEmpty) return;

		stream.Write(Encoding.UTF8.GetBytes(Strings.EscapeCsString(Name)));
		stream.Write((byte)':');
		if (Item is null)
			stream.Write(JsonItem.NullBytes);
		else
			Item.Write(stream);
	}

	public string ToString(bool format, bool pair = false) =>
		IsEmpty ? String.Empty:
			format ?
				pair ?
					ToString(new StringBuilder(), "").ToString():
					ToString(new StringBuilder().Append("{\n  "), "  ").Append("\n}").ToString():
				pair ?
					ToString(new StringBuilder()).ToString():
					ToString(new StringBuilder().Append('{')).Append('}').ToString();

	public override string ToString() => ToString(false);

	public bool Equals(JsonPair other) => Name == other.Name && Item == other.Item;

	public override bool Equals([NotNullWhen(true)] object? obj) => obj is JsonPair other && Equals(other);

	public override int GetHashCode() => HashCode.Join(Name?.GetHashCode() ?? 0, Item.GetHashCode());

	public static bool operator == (JsonPair left, JsonPair right) => left.Equals(right);

	public static bool operator != (JsonPair left, JsonPair right) => !left.Equals(right);

	public static implicit operator JsonPair((string name, JsonItem item) value) => new JsonPair(value.name, value.item);
}


/// <summary>
/// Represents a scalar JSON value, such as a string, number, boolean, or null.
/// </summary>
/// <remarks>Use the static fields to access common scalar values, such as <see cref="Null"/>, <see cref="True"/>,
/// <see cref="False"/>, and <see cref="NaN"/>. The <see cref="JsonScalar"/> type provides conversion properties for
/// retrieving the value as various .NET types, including <see langword="string"/>, <see langword="bool"/>, <see
/// langword="DateTime"/>, <see langword="DateTimeOffset"/>, <see langword="double"/>, <see langword="decimal"/>, <see
/// langword="int"/>, <see langword="long"/>, and <see langword="byte[]"/>. This type is immutable and
/// thread-safe.</remarks>
public sealed class JsonScalar: JsonItem
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
			bool bl => text.Append(bl ? "true": "false"),
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
#if NET
			float f => Single.IsFinite(f) ? text.Append(f): text.Append(NaNValue),
			double d => Double.IsFinite(d) ? text.Append(d): text.Append(NaNValue),
#else
			float f => Single.IsNaN(f) || Single.IsInfinity(f) ? text.Append(f): text.Append(NaNValue),
			double d => Double.IsNaN(d) || Double.IsInfinity(d) ? text.Append(d): text.Append(NaNValue),
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
				stream.Write(digits[DigitsUInt(digits, Size, b)..]);
				break;
			case sbyte sb:
				stream.Write(digits[DigitsInt(digits, Size, sb)..]);
				break;
			case short h:
				stream.Write(digits[DigitsInt(digits, Size, h)..]);
				break;
			case ushort uh:
				stream.Write(digits[DigitsUInt(digits, Size, uh)..]);
				break;
			case int i:
				stream.Write(digits[DigitsInt(digits, Size, i)..]);
				break;
			case uint ui:
				stream.Write(digits[DigitsUInt(digits, Size, ui)..]);
				break;
			case long l:
				stream.Write(digits[DigitsLong(digits, Size, l)..]);
				break;
			case ulong ul:
				stream.Write(digits[DigitsULong(digits, Size, ul)..]);
				break;
			case float f:
#if NET
				if (Single.IsFinite(f) && f.TryFormat(digits, out var nf))
					stream.Write(digits[..nf]);
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
#if NET
				if (Double.IsFinite(d) && d.TryFormat(digits, out var nd))
					stream.Write(digits[..nd]);
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
#if NET
				m.TryFormat(digits, out var nm);
				stream.Write(digits[..nm]);
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

			var len = Base64.GetMaxEncodedToUtf8Length(data.Length);
			byte[]? array = len > Tools.MaxStackAllocSize ? ArrayPool<byte>.Shared.Rent(BufferSize): null;
			Span<byte> buffer = array is null ? stackalloc byte[BufferSize] : array.AsSpan();
			int left = data.Length;
			var bytes = data.AsSpan();
			while (left > 0)
			{
				Base64.EncodeToUtf8(bytes, buffer, out int count, out int written);
				left -= count;
				bytes = bytes[count..];
				stream.Write(buffer[..written]);
			}
			if (array != null)
				ArrayPool<byte>.Shared.Return(array);
		}

		static void WriteTimeSpan(TimeSpan value, Stream stream)
		{
			const int TicksPerSecond = 100000;
			long ticks = value.Ticks >= Int64.MaxValue / TimeSpan.TicksPerSecond ? Int64.MaxValue / (TimeSpan.TicksPerSecond * TicksPerSecond): (value.Ticks / (TimeSpan.TicksPerSecond / TicksPerSecond * 10) + 5) / 10;
			
			if (ticks == 0)
			{
				stream.Write("PT0S"u8);
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
			stream.Write(mem[index..]);
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
			stream.Write(mem[index..]);
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
				mem[--index] = offset < 0 ? (byte)'-': (byte)'+';
			}
			else
			{
				mem[--index] = (byte)'Z';
			}
			index = PutDateTime(mem, index, value.DateTime);
			stream.Write(mem[index..]);
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
}


/// <summary>
/// Represents a read-only JSON object composed of named property pairs, providing access to properties by name or
/// index.
/// </summary>
/// <remarks>
/// The JsonMap class models a JSON object as a collection of JsonPair instances, each representing a
/// property name and value. Properties can be accessed by their string name or by their zero-based index. The class
/// implements <see cref="IReadOnlyList{T}">IReadOnlyList&lt;JsonPair&gt;</see> to support enumeration and indexed access.
/// Instances are immutable after construction. This type is typically used when parsing or manipulating JSON data
/// structures that represent objects. Thread safety is guaranteed for read operations if the provided property list
/// is not modified externally.
/// </remarks>
public sealed class JsonMap: JsonItem, IReadOnlyList<JsonPair>
{
	private readonly IReadOnlyList<JsonPair> _map;

	public JsonItem? this[string name] => _map.FirstOrDefault(o => o.Name == name).Item;
	public JsonPair this[int index] => _map[index];
	public override int Count => _map.Count;
	public override bool IsObject => true;


	public JsonMap(IReadOnlyList<JsonPair> properties)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		_map = properties;
	}

	public JsonMap(IReadOnlyList<JsonPair> properties, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		_map = properties;
	}

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, stringLimit, arrayLimit);
		text.Append('{');
		if (_map.Count > 0)
		{
			string? indent2 = indent == null ? null: indent + "  ";
			string comma = "";
			foreach (var item in _map)
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
		if (_map.Count > 0)
		{
			bool next = false;
			foreach (var item in _map)
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

	public IEnumerator<JsonPair> GetEnumerator() => _map.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();
}


/// <summary>
/// Represents a JSON array, providing read-only access to its elements as a collection of JSON items.
/// </summary>
/// <remarks>
/// Use this class to work with JSON arrays in a structured and type-safe manner. The array elements are
/// accessible by index and the collection implements read-only list semantics. This type is immutable after
/// construction. Thread safety is guaranteed for read operations. The class also supports serialization to JSON text
/// and writing to streams.
/// </remarks>
public sealed class JsonArray: JsonItem, IReadOnlyList<JsonItem>
{
	private readonly IReadOnlyList<JsonItem> _items;

	public JsonItem this[int index] => index >= 0 && index < _items.Count ? _items[index]: throw new ArgumentOutOfRangeException(nameof(index), index, null);
	public override int Count => _items.Count;
	public override bool IsArray => true;

	public JsonArray(IReadOnlyList<JsonItem> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		_items = ReadOnly.ReWrap(items);
	}

	public JsonArray(IReadOnlyList<JsonItem> items, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		_items = ReadOnly.ReWrap(items);
	}

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, stringLimit, arrayLimit);
		text.Append('[');
		string? indent2 = indent == null ? null: indent + "  ";
		string comma = "";
		int i = 0;
		foreach (JsonItem item in _items)
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
		foreach (JsonItem item in _items)
		{
			if (next)
				stream.Write((byte)',');
			else
				next = true;
			item.Write(stream);
		}
		stream.Write((byte)']');
	}

	public IEnumerator<JsonItem> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

/// <summary>
/// Provides static factory methods for creating JSON elements, including scalars, arrays, maps, and key-value pairs, in
/// a concise and type-safe manner.
/// </summary>
/// <remarks>
/// The methods in this class offer multiple overloads to simplify the construction of JSON structures from
/// various .NET types and collections. This enables fluent and readable creation of complex JSON objects without
/// manual instantiation of individual JSON element types. All methods are thread-safe and do not modify input collections.
/// </remarks>
public static class ZenJson
{
	// JsonScalar

	public static JsonScalar J(string value) => new JsonScalar(value);
	public static JsonScalar J(object value) => new JsonScalar(value);

	// JsonMap

	public static JsonMap J(JsonPair pair) => new JsonMap([pair]);
	public static JsonMap J(params JsonPair[] pair) => new JsonMap(pair);
	public static JsonMap J(List<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IList<JsonPair> pair) => new JsonMap(pair.ToIReadOnlyList());
	public static JsonMap J(IReadOnlyList<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IEnumerable<JsonPair> pair) => new JsonMap(pair.ToIReadOnlyList());

	// JsonArray

	public static JsonArray J(JsonItem item) => new JsonArray([item]);
	public static JsonArray J(params JsonItem[] items) => new JsonArray(items);
	public static JsonArray J(List<JsonItem> items) => new JsonArray(items);
	public static JsonArray J(IList<JsonItem> items) => new JsonArray(items.ToIReadOnlyList());
	public static JsonArray J(IReadOnlyList<JsonItem> items) => new JsonArray(items);
	public static JsonArray J(IEnumerable<JsonItem> items) => new JsonArray(items.ToIReadOnlyList());
	public static JsonArray J(JsonArray item) => new JsonArray([item]);
	public static JsonArray J(JsonMap item) => new JsonArray([item]);
	// JsonPair

	public static JsonPair J(string name, object value) => new JsonPair(name, new JsonScalar(value));

	public static JsonPair J(string name, params JsonPair[] value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, List<JsonPair> value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, IList<JsonPair> value) => new JsonPair(name, new JsonMap(value.ToIReadOnlyList()));
	public static JsonPair J(string name, IReadOnlyList<JsonPair> value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, IEnumerable<JsonPair> value) => new JsonPair(name, new JsonMap(value.ToIReadOnlyList()));

	public static JsonPair J(string name, JsonItem value) => new JsonPair(name, value);
	public static JsonPair J(string name, JsonMap value) => new JsonPair(name, value);
	public static JsonPair J(string name, JsonArray value) => new JsonPair(name, value);
	public static JsonPair J(string name, params JsonItem[] value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, List<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IList<JsonItem> value) => new JsonPair(name, new JsonArray(value.ToIReadOnlyList()));
	public static JsonPair J(string name, IReadOnlyList<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IEnumerable<JsonItem> value) => new JsonPair(name, new JsonArray(value.ToIReadOnlyList()));
}
