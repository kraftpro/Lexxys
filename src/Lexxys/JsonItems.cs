// Lexxys Infrastructural library.
// file: JsonItems.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Lexxys
{
	using Xml;

	public abstract class JsonItem
	{
		public static readonly JsonItem Empty = new EmptyItem();

		public IReadOnlyList<JsonPair> Attributes { get; }
		public virtual object Value => null;
		public virtual string Text => null;
		public virtual JsonItem this[string item] => Empty;
		public virtual JsonItem this[int index] => Empty;
		public virtual bool IsArray => false;
		public virtual bool IsObject => false;
		public virtual bool IsScalar => false;
		public virtual bool IsEmpty => false;
		public virtual int Count => 0;

		protected JsonItem(IReadOnlyList<JsonPair> attribues)
		{
			Attributes = attribues ?? Array.Empty<JsonPair>();
		}

		public abstract XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false);

		protected XmlLiteNode ToXml(string name, string value, bool ignoreCase, IEnumerable<XmlLiteNode> properties)
		{
			return new XmlLiteNode(name, value, ignoreCase,
				Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value))).ToList(),
				properties?.ToList());
		}

		public virtual StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
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
			if (comma != "(")
				text.Append(')');
			return text;
		}

		public virtual void Write(Stream stream)
		{
			if (Attributes.Count == 0)
				return;
			stream.Write((byte)'(');
			bool next = false;
			foreach (var attrib in Attributes)
			{
				if (next)
					stream.Write((byte)',');
				else
					next = true;
				attrib.Write(stream);
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
		{
			return ToString(new StringBuilder(), format ? "": null, stringLimit, arrayLimit).ToString();
		}

		public override string ToString()
		{
			return ToString(new StringBuilder()).ToString();
		}

		private class EmptyItem: JsonItem
		{
			public EmptyItem(): base(null)
			{
			}

			public override bool IsEmpty => true;

			public override StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
			{
				return text;
			}

			public override void Write(Stream stream)
			{
			}

			public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false)
			{
				return XmlLiteNode.Empty;
			}
		}
	}

	public readonly struct JsonPair: IEquatable<JsonPair>
	{
		public static readonly JsonPair Empty = new JsonPair(null, JsonItem.Empty);

		public string Name { get; }
		public JsonItem Item { get; }

		public JsonPair(string name, JsonItem item)
		{
			Name = name;
			Item = item;
		}

		public void Deconstruct(out string name, out JsonItem item)
		{
			name = Name;
			item = Item;
		}

		public bool IsEmpty => Name == null || (Item != null && Item.IsEmpty);

		public StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
		{
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
				stream.Write(Null);
			else
				Item.Write(stream);
		}
		private static readonly byte[] Null = new[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };

		//public bool TryWrite(Span<byte> buffer, out int length)
		//{
		//	var s = Strings.EscapeCsString(Name).AsSpan();
		//	Encoding.UTF8.


		//	stream.Write(Encoding.UTF8.GetBytes(Strings.EscapeCsString(Name)));
		//	stream.Write((byte)':');
		//	if (Item is null)
		//		stream.Write(Null);
		//	else
		//		Item.Write(stream);
		//}

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

		public override bool Equals([NotNullWhen(true)] object obj) => obj is JsonPair pair && Equals(pair);

		public override int GetHashCode() => HashCode.Join(Name?.GetHashCode() ?? 0, Item.GetHashCode());

		public static bool operator == (JsonPair left, JsonPair right) => left.Equals(right);

		public static bool operator != (JsonPair left, JsonPair right) => !left.Equals(right);
	}


	public class JsonScalar: JsonItem
	{
		public override object Value { get; }
		public override string Text { get; }
		public override bool IsScalar => true;

		public JsonScalar(object value, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Value = value;
			Text = null;
		}

		public JsonScalar(object value, string text, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Value = value;
			Text = text;
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
			null => default,
			byte[] v => v,
			_ => default
		};

		public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false) => ToXml(name, XmlTools.Convert(Value), ignoreCase, null);

		public override StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			base.ToString(text, indent, stringLimit, arrayLimit);
			if (Value == null)
			{
				if (Attributes.Count == 0)
					text.Append("null");
			}
			else if (Value is string s)
			{
				if (stringLimit > 0 && s.Length > stringLimit)
				{
					Strings.EscapeCsString(text, s.Substring(0, stringLimit));
					--text.Length;
					text.Append("...\"");
				}
				else
				{
					Strings.EscapeCsString(text, s);
				}
			}
			else if (Text != null)
			{
				text.Append(Text);
			}
			else if (Value is bool b)
			{
				text.Append(b ? "true" : "false");
			}
			else if (Value is DateTime d)
			{
				text.Append('"').Append(XmlTools.Convert(d)).Append('"');
			}
			else if (Value is DateTimeOffset x)
			{
				text.Append('"').Append(XmlTools.Convert(x)).Append('"');
			}
			else if (Value is byte[] y)
			{
				text.Append('"').Append(Convert.ToBase64String(y, Base64FormattingOptions.None)).Append('"');
			}
			else
			{
				text.Append(Value.ToString());
			}
			return text;
		}

		public override void Write(Stream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			base.Write(stream);
			if (Value == null)
			{
				if (Attributes.Count == 0)
					stream.Write(Null);
			}
			else if (Value is string s)
			{
				stream.Write(Encoding.UTF8.GetBytes(Strings.EscapeCsString(s)));
			}
			else if (Text != null)
			{
				stream.Write(Encoding.UTF8.GetBytes(Text));
			}
			else if (Value is bool b)
			{
				stream.Write(b ? True: False);
			}
			else if (Value is DateTime d)
			{
				stream.Write(Quot);
				stream.Write(Encoding.UTF8.GetBytes(XmlTools.Convert(d)));
				stream.Write(Quot);
			}
			else if (Value is DateTimeOffset x)
			{
				stream.Write(Quot);
				stream.Write(Encoding.UTF8.GetBytes(XmlTools.Convert(x)));
				stream.Write(Quot);
			}
			else if (Value is byte[] y)
			{
				stream.Write(Quot);
				ToBase64(y, stream);
				stream.Write(Quot);
			}
			else
			{
				stream.Write(Encoding.UTF8.GetBytes(Value.ToString()));
			}

			static unsafe void ToBase64(byte[] data, Stream stream)
			{
				const int BufferSize = 4096;
				var base64 = new byte[BufferSize];
				var buffer = base64.AsSpan();
				int left = data.Length;
				var bytes = data.AsSpan();
				while (left > 0)
				{
					Base64.EncodeToUtf8(bytes, buffer, out var count, out var writen);
					left -= count;
					bytes = bytes.Slice(count);
					stream.Write(base64, 0, writen);
				}
			}
		}
		private static readonly byte[] Null = new[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
		private static readonly byte[] True = new[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
		private static readonly byte[] False = new[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
		private const byte Quot = (byte)'"';
	}

	public class JsonMap: JsonItem, IEnumerable<JsonPair>
	{
		public IReadOnlyList<JsonPair> Properties { get; }

		public override JsonItem this[string name] => Properties.FirstOrDefault(o => o.Name == name).Item ?? base[name];
		public override JsonItem this[int index] => index < 0 || index >= Properties.Count ? base[index] : Properties[index].Item;
		public override int Count => Properties.Count;
		public override bool IsObject => true;

		public JsonMap(IReadOnlyList<JsonPair> properties, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Properties = ReadOnly.ReWrap(properties);
		}

		public JsonMap(IWrappedList<JsonPair> properties, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Properties = properties;
		}

		public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false)
		{
			var attribs = Attributes.Select(o => new KeyValuePair<string, string>(o.Name, XmlTools.Convert(o.Item.Value))).ToList();
			var properties = new List<XmlLiteNode>();
			if (Properties != null)
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
							attribs.Add(new KeyValuePair<string, string>(nm, XmlTools.Convert(scalar.Value)));
							continue;
						}
					}
					properties.Add(prop.Item.ToXml(prop.Name, ignoreCase, attributes));
				}
			}
			return new XmlLiteNode(name, null, ignoreCase, attribs, properties);
		}

		public override StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			base.ToString(text, indent, stringLimit, arrayLimit);
			text.Append('{');
			if (Properties != null)
			{
				string indent2 = indent == null ? null : indent + "  ";
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
			if (Properties != null)
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
		public override JsonItem this[int index] => index < 0 || index >= Items.Count ? base[index] : Items[index];
		public override int Count => Items.Count;
		public override bool IsArray => true;

		public JsonArray(IReadOnlyList<JsonItem> items, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Items = ReadOnly.ReWrap(items);
		}

		public JsonArray(IWrappedList<JsonItem> items, IReadOnlyList<JsonPair> attribues = null) : base(attribues)
		{
			Items = items;
		}

		public override XmlLiteNode ToXml(string name, bool ignoreCase = false, bool attributes = false)
		{
			return ToXml(name, null, ignoreCase, Items?.Where(o => o != null && !o.IsEmpty).Select(o => o.ToXml(XmlItemName, ignoreCase, attributes)));
		}

		public override StringBuilder ToString(StringBuilder text, string indent = null, int stringLimit = 0, int arrayLimit = 0)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			base.ToString(text, indent, stringLimit, arrayLimit);
			text.Append('[');
			if (Items != null)
			{
				string indent2 = indent == null ? null : indent + "  ";
				string comma = "";
				int i = 0;
				foreach (var item in Items)
				{
					if (item == null || item.IsEmpty)
						continue;
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
			}
			text.Append(']');
			return text;
		}

		public override void Write(Stream stream)
		{
			base.Write(stream);
			stream.Write((byte)'[');
			if (Items != null)
			{
				bool next = false;
				foreach (var item in Items)
				{
					if (item == null || item.IsEmpty)
						continue;
					if (next)
						stream.Write((byte)',');
					else
						next = true;
					item.Write(stream);
				}
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
		public static JsonMap J(IList<JsonPair> pair) => new JsonMap(ReadOnly.Wrap(pair));
		public static JsonMap J(IReadOnlyList<JsonPair> pair) => new JsonMap(pair);
		public static JsonMap J(IEnumerable<JsonPair> pair) => new JsonMap(ReadOnly.WrapCopy(pair));

		// JsonArray

		public static JsonArray J(JsonItem pair) => new JsonArray(new[] { pair });
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
}
