// Lexxys Infrastructural library.
// file: JsonItems.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers.Text;
using System.Collections;
using System.Text;

#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA1307 // Specify StringComparison

namespace Lexxys;

using Xml;

public abstract class JsonItem
{
	protected const string NullValue = "null";
	protected const string TrueValue = "true";
	protected const string FalseValue = "false";
	protected static readonly byte[] NullBytes = new[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
	protected static readonly byte[] TrueBytes = new[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
	protected static readonly byte[] FalseBytes = new[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };


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
		DateTime d => XmlTools.Convert(d),
		DateTimeOffset x => XmlTools.Convert(x),
		TimeSpan t => XmlTools.Convert(t),
		byte[] y => Convert.ToBase64String(y, Base64FormattingOptions.None),
		IConvertible c => c.ToString(null),
		_ => Value.ToString() ?? String.Empty
	};

	protected JsonItem()
	{
		Attributes = _noAttributes;
	}
	private static readonly IReadOnlyList<JsonPair> _noAttributes = Array.Empty<JsonPair>();

	protected JsonItem(IReadOnlyList<JsonPair>? attributes)
	{
		Attributes = attributes ?? _noAttributes;
	}

	public abstract XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false);

	protected XmlLiteNode ToXml(string name, string? value, bool ignoreCase, IEnumerable<XmlLiteNode>? properties)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		return Attributes.Count == 0 ?
			new XmlLiteNode(name, value, ignoreCase, null, properties?.ToList()):
			new XmlLiteNode(name, value, ignoreCase,
			Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value) ?? "")).ToList(),
			properties?.ToList());
	}

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

	//public virtual bool TryWrite(Span<byte> buffer, out int length)
	//{
	//	if (Attributes.Count == 0)
	//	{
	//		length = 0;
	//		return true;
	//	}
	//	int k = 1;
	//	if (buffer.Length > 0)
	//		buffer[0] = (byte)'(';
	//	bool next = false;
	//	foreach (var attrib in Attributes)
	//	{
	//		if (next)
	//		{
	//			if (buffer.Length > ++k)
	//				buffer[k] = (byte)',';
	//		}
	//		else
	//		{
	//			next = true;
	//		}
	//		attrib.TryWrite(buffer.Slice(k), out var len);
	//		k += len;
	//	}

	//	length = ++k;
	//	if (buffer.Length <= k)
	//		return false;
	//	buffer[k] = (byte)')';
	//	return true;
	//}

	public string ToString(bool format, int stringLimit = 0, int arrayLimit = 0)
		=> ToString(new StringBuilder(), format ? "" : null, stringLimit, arrayLimit).ToString();

	public override string ToString() => ToString(new StringBuilder()).ToString();
}

public readonly struct JsonPair: IEquatable<JsonPair>
{
	public string Name { get; }
	public JsonItem Item { get; }

	public JsonPair(string name, JsonItem item)
	{
		if (name is not { Length: >0})
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
	private static readonly byte[] NullBytes = new[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

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
		null => Array.Empty<byte>(),
		byte[] v => v,
		_ => Array.Empty<byte>()
	};

	public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false) => ToXml(name, XmlTools.Convert(Value), ignoreCase, null);

	public override StringBuilder ToString(StringBuilder text, string? indent = null, int stringLimit = 0, int arrayLimit = 0)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		base.ToString(text, indent, stringLimit, arrayLimit);
		return Value switch
		{
			null => text.Append("null"),
			string s => Escape(s),
			bool b => text.Append(b ? "true" : "false"),
			DateTime d => text.Append('"').Append(XmlTools.Convert(d)).Append('"'),
			DateTimeOffset x => text.Append('"').Append(XmlTools.Convert(x)).Append('"'),
			TimeSpan t => text.Append('"').Append(XmlTools.Convert(t)).Append('"'),
			byte[] y => text.Append('"').Append(Convert.ToBase64String(y, Base64FormattingOptions.None)).Append('"'),
			byte or sbyte or
			short or ushort or
			int or uint or
			long or ulong or
			float or double or
			decimal => text.Append(Value),
			_ => Escape(Value.ToString() ?? String.Empty)
		};

		StringBuilder Escape(string s)
		{
			if (stringLimit > 0 && s.Length > stringLimit)
			{
				Strings.EscapeCsString(text, s.AsSpan(0, stringLimit));
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

		base.Write(stream);
		if (Value == null)
		{
			if (Attributes.Count == 0)
				stream.Write(NullBytes);
		}
		else if (Value is string s)
		{
			Strings.EscapeUtf8CsString(stream, s.AsSpan());
		}
		else if (Value is bool b)
		{
			stream.Write(b ? TrueBytes: FalseBytes);
		}
		else if (Value is DateTime d)
		{
			stream.Write(Quote);
			WriteDateTime(d, stream);
			stream.Write(Quote);
		}
		else if (Value is DateTimeOffset x)
		{
			stream.Write(Quote);
			WriteDateTimeOffset(x, stream);
			stream.Write(Quote);
		}
		else if (Value is TimeSpan p)
		{
			stream.Write(Quote);
			WriteTimeSpan(p, stream);
			stream.Write(Quote);
		}
		else if (Value is byte[] y)
		{
			stream.Write(Quote);
			ToBase64(y, stream);
			stream.Write(Quote);
		}
		else
		{
			Strings.EscapeUtf8CsString(stream, (Value.ToString() ?? String.Empty).AsSpan());
		}

		static void ToBase64(byte[] data, Stream stream)
		{
			const int BufferSize = 4096;
			var base64 = new byte[BufferSize];
			var buffer = base64.AsSpan();
			int left = data.Length;
			var bytes = data.AsSpan();
			while (left > 0)
			{
				Base64.EncodeToUtf8(bytes, buffer, out var count, out var written);
				left -= count;
				bytes = bytes.Slice(count);
				stream.Write(base64, 0, written);
			}
		}

		static void WriteTimeSpan(TimeSpan value, Stream stream)
		{
			const int TicksPerSecond = 100000;
			var ticks = value.Ticks >= long.MaxValue / TimeSpan.TicksPerSecond ? long.MaxValue / (TimeSpan.TicksPerSecond * TicksPerSecond): (value.Ticks / (TimeSpan.TicksPerSecond / TicksPerSecond * 10) + 5) / 10;
			
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
			mem[index--] = (byte)sign;
			return Digits(mem, index, value);
		}

		static int Digits(Span<byte> mem, int index, int value)
		{
			do
			{
				mem[index--] = (byte)(value % 10 + '0');
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
				int fract = z % TicksPerSecond;
				if (fract > 0)
				{
					index = Digits(mem, index, fract);
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
	private static readonly byte[] ZeroTime = { (byte)'P', (byte)'T', (byte)'0', (byte)'S' };
}

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
		Properties = ReadOnly.ReWrap(properties)!;
	}

	public JsonMap(IWrappedList<JsonPair> properties)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		Properties = properties;
	}

	public JsonMap(IReadOnlyList<JsonPair> properties, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		Properties = ReadOnly.ReWrap(properties)!;
	}

	public JsonMap(IWrappedList<JsonPair> properties, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (properties is null)
			throw new ArgumentNullException(nameof(properties));
		Properties = properties;
	}

	public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false)
	{
		var attribs = Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value) ?? "")).ToList();
		var properties = new List<XmlLiteNode>();
		if (Properties.Count > 0)
		{
			foreach (var prop in Properties)
			{
				if (prop.IsEmpty)
					continue;
				if (prop.Item is JsonScalar scalar)
				{
					bool attrib = attributes;
					var nm = prop.Name;
					if (nm.StartsWith("@", StringComparison.Ordinal))
					{
						attrib = true;
						if (nm.Length > 1)
							nm = nm.Substring(1);
					}
					if (attrib)
					{
						attribs.Add(new KeyValuePair<string, string>(nm, XmlTools.Convert(scalar.Value) ?? ""));
						continue;
					}
				}
				properties.Add(prop.Item.ToXml(prop.Name, ignoreCase, attributes));
			}
		}
		return new XmlLiteNode(name, null, ignoreCase, attribs, properties);
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

public class JsonArray: JsonItem, IEnumerable<JsonItem>
{
	private const string XmlItemName = "item";
	public IReadOnlyList<JsonItem> Items { get; }
	public override JsonItem? this[int index] => index >= 0 && index < Items.Count ? Items[index]: null;
	public override int Count => Items.Count;
	public override bool IsArray => true;

	public JsonArray(IReadOnlyList<JsonItem> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = ReadOnly.ReWrap(items)!;
	}

	public JsonArray(IWrappedList<JsonItem> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = items;
	}

	public JsonArray(IReadOnlyList<JsonItem> items, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = ReadOnly.ReWrap(items)!;
	}

	public JsonArray(IWrappedList<JsonItem> items, IReadOnlyList<JsonPair>? attributes): base(attributes)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		Items = items;
	}

	public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false)
	{
		return ToXml(name, null, ignoreCase, Items.Select(o => (o ?? JsonScalar.Null).ToXml(XmlItemName, ignoreCase, attributes)));
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
		foreach (var item in Items)
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
			if (item == null)
				JsonScalar.Null.ToString(text, indent2, stringLimit, arrayLimit);
			else
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
		foreach (var item in Items)
		{
			if (next)
				stream.Write((byte)',');
			else
				next = true;
			if (item is null)
				stream.Write(NullBytes);
			else
				item.Write(stream);
		}
		stream.Write((byte)']');
	}

	public IEnumerator<JsonItem> GetEnumerator() => Items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
}


//public ref struct Utf8Writer: IDisposable
//{
//	private readonly bool _ownsStream;
//	private readonly byte[] _buffer;
//	private int _index;
//	public Stream Stream { get; }

//	public Utf8Writer(Stream stream, bool ownsStream = false)
//	{
//		Stream = stream;
//		_ownsStream = ownsStream;
//		_buffer = new byte[4096];
//		_index = 0;
//	}

//	private int Left => _buffer.Length - _index;

//	public void Write(byte value)
//	{
//		if (Left < 1)
//			FlushBuffer();
//		_buffer[_index++] = value;
//	}

//	public unsafe void Write(char value)
//	{
//		const int MaxUtf8Bytes = 3;

//		if (Left < MaxUtf8Bytes)
//			FlushBuffer();
//		if (value <= 127)
//		{
//			_buffer[_index++] = (byte)value;
//		}
//		else
//		{
//			unsafe
//			{
//				char* pc = stackalloc char[1];
//				byte* pb = stackalloc byte[MaxUtf8Bytes];
//				*pc = value;
//				int count = Encoding.UTF8.GetBytes(pc, 1, pb, MaxUtf8Bytes);
//				for (int i = 0; i < count; ++i)
//				{
//					_buffer[_index++] = pb[i];
//				}
//			}
//		}
//	}

//	public void Write(string value)
//	{
//		if (Left < value.Length)
//			FlushBuffer();
//	}

//	public void Write(byte[] value)
//	{
//		int left = value.Length;
//		if (Left < left)
//			FlushBuffer();
//		if (left < _buffer.Length)
//		{
//			Array.Copy(value, 0, _buffer, _index, left);
//			_index += left;
//		}
//		else
//		{
//			Stream.Write(value);
//		}
//	}

//	private void FlushBuffer()
//	{
//		Stream.Write(_buffer, 0, _index);
//		_index = 0;
//	}

//	public void Dispose()
//	{
//		if (_index > 0)
//			FlushBuffer();
//		if (!_ownsStream)
//			Stream.Dispose();
//	}
//}

//public static class EncodingExtensions
//{
//	public static bool TryGetBytes(this Encoding encoding, ReadOnlySpan<char> str, Span<byte> bytes, out int written)
//	{
//		if (encoding.GetByteCount(str) > span.Length)
//		{
//			written = 0;
//			return false;
//		}

//		written = Encoding.UTF8.GetBytes(str, span);
//		return true;
//	}
//}

public static class ZenJson
{
	// JsonScalar

	public static JsonScalar J(string value) => new JsonScalar(value);
	public static JsonScalar J(object value) => new JsonScalar(value);

	// JsonMap

	public static JsonMap J(JsonPair pair) => new JsonMap(new[] { pair });
	public static JsonMap J(params JsonPair[] pair) => new JsonMap(pair);
	public static JsonMap J(List<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IList<JsonPair> pair) => new JsonMap(ReadOnly.Wrap(pair)!);
	public static JsonMap J(IReadOnlyList<JsonPair> pair) => new JsonMap(pair);
	public static JsonMap J(IEnumerable<JsonPair> pair) => new JsonMap(ReadOnly.WrapCopy(pair)!);

	// JsonArray

	public static JsonArray J(JsonItem pair) => new JsonArray(new[] { pair });
	public static JsonArray J(params JsonItem[] pair) => new JsonArray(pair);
	public static JsonArray J(List<JsonItem> pair) => new JsonArray(pair);
	public static JsonArray J(IList<JsonItem> pair) => new JsonArray(ReadOnly.Wrap(pair)!);
	public static JsonArray J(IReadOnlyList<JsonItem> pair) => new JsonArray(pair);
	public static JsonArray J(IEnumerable<JsonItem> pair) => new JsonArray(ReadOnly.WrapCopy(pair)!);

	// JsonPair

	public static JsonPair J(string name, JsonItem value) => new JsonPair(name, value);
	public static JsonPair J(string name, object value) => new JsonPair(name, new JsonScalar(value));

	public static JsonPair J(string name, params JsonPair[] value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, List<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.Wrap(value)!));
	public static JsonPair J(string name, IList<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.Wrap(value)!));
	public static JsonPair J(string name, IReadOnlyList<JsonPair> value) => new JsonPair(name, new JsonMap(value));
	public static JsonPair J(string name, IEnumerable<JsonPair> value) => new JsonPair(name, new JsonMap(ReadOnly.WrapCopy(value)!));

	public static JsonPair J(string name, params JsonItem[] value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, List<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IList<JsonItem> value) => new JsonPair(name, new JsonArray(ReadOnly.Wrap(value)!));
	public static JsonPair J(string name, IReadOnlyList<JsonItem> value) => new JsonPair(name, new JsonArray(value));
	public static JsonPair J(string name, IEnumerable<JsonItem> value) => new JsonPair(name, new JsonArray(ReadOnly.WrapCopy(value)!));
}
