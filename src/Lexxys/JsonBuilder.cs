// Lexxys Infrastructural library.
// file: JsonBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;

#pragma warning disable CA1305 // Specify IFormatProvider

namespace Lexxys;

using Xml;

/// <summary>
/// Represents a writer that provides a fast, non-cached, forward-only way to generate streams that contain JSON data.
/// </summary>
public abstract class JsonBuilder
{
	protected const string NullValue = "null";

	enum State
	{
		Object,
		Array,
		Item,
		Value
	}

	private readonly Stack<char> _elements;
	private State _state;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonBuilder"/> class.
	/// </summary>
	protected JsonBuilder()
	{
		_elements = new Stack<char>();
	}

	/// <summary>
	/// Naming rules of the JSON items
	/// </summary>
	public NamingCaseRule NamingRule { get; set; } = NamingCaseRule.PreferCamelCase;

	public bool InObject => _state == State.Object || _state == State.Value && (_elements.Count == 0 || _elements.Peek() != ']');

	public bool InArray => !InObject;

	/// <summary>
	/// Writes a <see cref="String"/> value to a stream.
	/// </summary>
	/// <param name="value">The <see cref="String"/> value to write.</param>
	protected abstract void Text(string value);

	/// <summary>
	/// Writes a <see cref="Char"/> value to a stream.
	/// </summary>
	/// <param name="value">The <see cref="Char"/> value to write.</param>
	protected abstract void Text(char value);

	public JsonBuilder WithNamingRule(NamingCaseRule namingRule)
	{
		NamingRule = namingRule;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="String"/> value as a JavaScript escaped string.
	/// </summary>
	/// <param name="value">The <see cref="String"/> value to write.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual void Value(string? value)
	{
		Text(value == null ? NullValue : Strings.EscapeCsString(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Comma()
	{
		switch (_state)
		{
			case State.Value:
				Text(',');
				_state = _elements.Count == 0 || _elements.Peek() != ']' ? State.Object: State.Array;
				break;
			case State.Item:
				break;
			//case State.Object:
			//case State.Array:
			default:
				_state = State.Value;
				break;
		}
	}

	/// <summary>
	/// Writes the start of a JSON object.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Obj()
	{
		_elements.Push('?');
		Comma();
		_state = State.Object;
		return this;
	}

	/// <summary>
	/// Writes the start of an JSON array.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Arr()
	{
		_elements.Push(']');
		Comma();
		Text('[');
		_state = State.Array;
		return this;
	}

	/// <summary>
	/// Writes the end of an array or a object.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder End()
	{
		var c = _elements.Pop();
		if (c == '?')
		{
			if (_state == State.Object)
				Text("{}");
		}
		else
		{
			Text(c);
		}
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a name of an JSON attribute.
	/// </summary>
	/// <param name="name">Name of the attribute</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name)
	{
		if (name is not { Length: > 0 })
			throw new ArgumentNullException(nameof(name));
		if (_state == State.Item || _state == State.Array)
			throw new InvalidOperationException();
		if (_state == State.Object) // && _elements.Peek() == '?')
		{
			_elements.Pop();
			_elements.Push('}');
			Text('{');
		}
		Comma();
		Value(ForceName(name));
		Text(':');
		_state = State.Item;
		return this;
	}

	private string? Escape(object value)
	{
		if (value is IConvertible cn)
		{
			switch (cn.GetTypeCode())
			{
				case TypeCode.Boolean:
					return (bool)value ? "true" : "false";
				case TypeCode.DBNull:
				case TypeCode.Empty:
					return NullValue;
				case TypeCode.Char:
				case TypeCode.String:
					return Strings.EscapeCsString(value.ToString()!);
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Decimal:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					if (value is Enum)
						return "\"" + Strings.ToNamingRule(cn.ToString(CultureInfo.InvariantCulture), NamingRule) + "\"";
					else
						return cn.ToString(CultureInfo.InvariantCulture);

				case TypeCode.DateTime:
					return "\"" + XmlTools.Convert((DateTime)value) + "\"";
			}
		}
		return value switch
		{
			TimeSpan span => "\"" + XmlTools.Convert(span) + "\"",
			Guid guid => "\"" + XmlTools.Convert(guid) + "\"",
			DateTimeOffset offset => "\"" + XmlTools.Convert(offset) + "\"",
			byte[] bytes => "\"" + Convert.ToBase64String(bytes) + "\"",
			_ => null
		};
	}

	/// <summary>
	/// Writes a <see cref="IDictionary"/> value as a JSON object
	/// </summary>
	/// <param name="value">The <see cref="IDictionary"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(IDictionary? value)
	{
		if (value == null)
		{
			Text(NullValue);
			return this;
		}
		Obj();
		foreach (DictionaryEntry item in value)
		{
			Item(Strings.ToNamingRule(item.Key.ToString(), NamingRule)!).Val(item.Value);
		}
		return End();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private string ForceName(string name)
	{
		return (NamingRule & NamingCaseRule.Force) == 0 ? name: Strings.ToNamingRule(name, NamingRule)!;
	}

	/// <summary>
	/// Writes a <see cref="String"/> value.
	/// </summary>
	/// <param name="value">The <see cref="String"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(string? value)
	{
		Comma();
		Value(value);
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Char"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Char"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(char value)
	{
		Comma();
		Value(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="Boolean"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Boolean"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(bool value)
	{
		Comma();
		Text(value ? "true": "false");
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Byte"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Byte"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(byte value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="SByte"/> value.
	/// </summary>
	/// <param name="value">The <see cref="SByte"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(sbyte value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="Int16"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int16"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(short value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="UInt16"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt16"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(ushort value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="Int32"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int32"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(int value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="UInt32"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt32"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(uint value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="Int64"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Int64"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(long value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an <see cref="UInt64"/> value.
	/// </summary>
	/// <param name="value">The <see cref="UInt64"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(ulong value)
	{
		Comma();
		Text(value.ToString());
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Decimal"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Decimal"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(decimal value)
	{
		Comma();
		Text(value.ToString(CultureInfo.InvariantCulture));
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Single"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Single"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(float value)
	{
		Comma();
		Text(value.ToString(CultureInfo.InvariantCulture));
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Double"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Double"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(double value)
	{
		Comma();
		Text(value.ToString(CultureInfo.InvariantCulture));
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="DateTime"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(DateTime value)
	{
		Comma();
		Text('"');
		Text(XmlTools.Convert(value));
		Text('"');
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="DateTimeOffset"/> value.
	/// </summary>
	/// <param name="value">The <see cref="DateTimeOffset"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(DateTimeOffset value)
	{
		Comma();
		Text('"');
		Text(XmlTools.Convert(value));
		Text('"');
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value">The <see cref="TimeSpan"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(TimeSpan value)
	{
		Comma();
		Text('"');
		Text(XmlTools.Convert(value));
		Text('"');
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="Guid"/> value.
	/// </summary>
	/// <param name="value">The <see cref="Guid"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(Guid value)
	{
		Comma();
		Text('"');
		Text(XmlTools.Convert(value));
		Text('"');
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes the value of the bytes array as the base64 string.
	/// </summary>
	/// <param name="value">The <see cref="Guid"/> value to write</param>
	/// <returns></returns>
	public JsonBuilder Val(byte[]? value)
	{
		Comma();
		if (value == null)
		{
			Text(NullValue);
		}
		else
		{
			Text('"');
			Text(Convert.ToBase64String(value));
			Text('"');
		}
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes a <see cref="IEnumerable"/> value as an JSON array.
	/// </summary>
	/// <param name="value">The <see cref="IEnumerable"/> value to write.</param>
	/// <returns></returns>
	public JsonBuilder Val(IEnumerable? value)
	{
		if (value == null)
		{
			Text(NullValue);
			return this;
		}
		if (value is string s)
			return Val(s);
		Arr();
		foreach (var item in value)
		{
			Val(item);
		}
		return End();
	}

	/// <summary>
	/// Writes an object <paramref name="value"/> using <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	public JsonBuilder Val(IDumpJson? value)
	{
		Comma();
		if (value == null)
			Text(NullValue);
		else
			value.ToJson(this);
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an object <paramref name="value"/> using <see cref="IDumpJson"/> implementation if it is present.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	public JsonBuilder Val(object? value)
	{
		if (value is not IDumpJson i)
			return ValObj(value);
		if (_state == State.Value)
			Comma();
		Obj();
		i.ToJsonContent(this);
		End();
		_state = State.Value;
		return this;
	}

	/// <summary>
	/// Writes an object <paramref name="value"/> ignoring <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="value">The <see cref="Object"/> value to write.</param>
	/// <returns></returns>
	public JsonBuilder ValObj(object? value)
	{
		if (value == null)
		{
			Text(NullValue);
			_state = State.Value;
			return this;
		}
		var s = Escape(value);
		if (s != null)
		{
			Comma();
			Text(s);
			_state = State.Value;
			return this;
		}
		if (value is IDictionary dc)
			return Val(dc);
		if (value is IEnumerable en)
			return Val(en);
		Obj();
		Type type = value.GetType();
		foreach (var item in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField))
		{
			Item(Strings.ToNamingRule(item.Name, NamingRule)!).Val(item.GetValue(value));
		}
		foreach (var item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
		{
			if (!item.CanRead ||
				item.PropertyType.IsGenericTypeDefinition ||
				item.PropertyType.IsAbstract ||
				item.GetIndexParameters().Length > 0 ||
				item.PropertyType.IsGenericParameter) 
				continue;
			try
			{
				object? v = item.GetValue(value);
				Item(Strings.ToNamingRule(item.Name, NamingRule)!).Val(v);
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch { }
			#pragma warning restore CA1031 // Do not catch general exception types
		}
		return End();
	}

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, IDictionary? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, string? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, char value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, bool value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, byte value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, sbyte value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, short value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, ushort value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, int value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, uint value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, long value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, ulong value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, decimal value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, float value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, double value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, DateTime value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, DateTimeOffset value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, TimeSpan value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, Guid value) => Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, byte[]? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, IEnumerable? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, IDumpJson? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair using <see cref="IDumpJson"/> implementation if it is present.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Item(string name, object? value) => value == null ? this: Item(name).Val(value);

	/// <summary>
	/// Writes item and value pair ignoring <see cref="IDumpJson"/> implementation.
	/// </summary>
	/// <param name="name">name of the attribute</param>
	/// <param name="value">value of the attribute</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder ItemObj(string name, object? value) => value == null ? this: Item(name).ValObj(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public JsonBuilder Content(IDumpJson? value)
	{
		value?.ToJsonContent(this);
		return this;
	}

	/// <summary>
	/// Closes all end tags.
	/// </summary>
	public virtual void Flush()
	{
		while (_elements.Count > 0)
		{
			End();
		}
		_state = State.Value;
	}

	/// <summary>
	/// Creates a new <see cref="JsonBuilder"/> using the specified <see cref="TextWriter"/>.
	/// </summary>
	/// <param name="writer">The <see cref="TextWriter"/> to write a json.</param>
	/// <returns></returns>
	public static JsonBuilder Create(TextWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		return new JsonStreamBuilder(writer);
	}

	/// <summary>
	/// Creates a new <see cref="JsonBuilder"/> using the specified <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="buffer">The <see cref="StringBuilder"/> to save a json.</param>
	/// <returns></returns>
	public static JsonBuilder Create(StringBuilder? buffer)
	{
		return new JsonStringBuilder(buffer);
	}

	public static string ToJson(object value)
	{
		return new JsonStringBuilder(new StringBuilder()).Val(value).ToString()!;
	}
}

/// <summary>
/// Represents <see cref="JsonBuilder"/> that uses <see cref="StringBuilder"/> to write a JSON stream.
/// </summary>
[DebuggerDisplay("{" + nameof(_buffer) + "}")]
public class JsonStringBuilder: JsonBuilder
{
	private readonly StringBuilder _buffer;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStringBuilder"/> class.
	/// </summary>
	public JsonStringBuilder(): this(null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStringBuilder"/> class.
	/// </summary>
	public JsonStringBuilder(StringBuilder? buffer)
	{
		_buffer = buffer ?? new StringBuilder();
	}

	/// <inheritdoc />
	protected override void Text(string value)
	{
		_buffer.Append(value);
	}

	/// <inheritdoc />
	protected override void Text(char value)
	{
		_buffer.Append(value);
	}

	/// <inheritdoc />
	protected override void Value(string? value)
	{
		if (value == null)
			_buffer.Append(NullValue);
		else
			Strings.EscapeCsString(_buffer, value);
	}

	/// <summary>
	/// Get Json as a string
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		Flush();
		return _buffer.ToString();
	}

	/// <summary>
	/// Returns internal <see cref="StringBuilder"/> buffer of the <see cref="JsonStringBuilder"/>.
	/// </summary>
	/// <param name="json"></param>
	/// <returns></returns>
	public static StringBuilder ToStringBuilder(JsonStringBuilder json)
	{
		if (json is null)
			throw new ArgumentNullException(nameof(json));
		return json._buffer;
	}

	/// <summary>
	/// Creates a new <see cref="JsonStringBuilder"/> from <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static JsonStringBuilder FromStringBuilder(StringBuilder text)
	{
		return new JsonStringBuilder(text);
	}

	/// <summary>
	/// Returns internal <see cref="StringBuilder"/> buffer of the <see cref="JsonStringBuilder"/>.
	/// </summary>
	/// <param name="json"></param>
	/// <returns></returns>
	public static explicit operator StringBuilder(JsonStringBuilder json) => ToStringBuilder(json);

	/// <summary>
	/// Creates a new <see cref="JsonStringBuilder"/> from <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static implicit operator JsonStringBuilder(StringBuilder text) => FromStringBuilder(text);
}


/// <summary>
/// Represents <see cref="JsonBuilder"/> that uses <see cref="TextWriter"/> to write a JSON stream.
/// </summary>
public class JsonStreamBuilder: JsonBuilder
{
	private readonly TextWriter _writer;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonStreamBuilder"/> class.
	/// </summary>
	/// <param name="writer"></param>
	public JsonStreamBuilder(TextWriter writer)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	/// <inheritdoc />
	protected override void Text(string value)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	protected override void Text(char value)
	{
		_writer.Write(value);
	}

	/// <inheritdoc />
	protected override void Value(string? value)
	{
		_writer.Write(value == null ? NullValue : Strings.EscapeCsString(value));
	}

	/// <inheritdoc />
	public override void Flush()
	{
		base.Flush();
		_writer.Flush();
	}
}

/// <summary>
/// Represents <see cref="JsonBuilder"/> that uses <see cref="MemoryStream"/> to write a JSON UTF8 stream.
/// </summary>
public class JsonUtf8Builder: JsonBuilder
{
	private new static readonly byte[] NullValue = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
	private readonly MemoryStream _buffer;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonUtf8Builder"/> class.
	/// </summary>
	public JsonUtf8Builder() : this(null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonUtf8Builder"/> class.
	/// </summary>
	public JsonUtf8Builder(MemoryStream? buffer)
	{
		_buffer = buffer ?? new MemoryStream();
	}

	/// <inheritdoc />
	protected override void Text(string value)
	{
		if (String.IsNullOrEmpty(value))
			return;
		var bytes = Encoding.UTF8.GetBytes(value);
		_buffer.Write(bytes, 0, bytes.Length);
	}

	/// <inheritdoc />
	protected override unsafe void Text(char value)
	{
		if (value < 128)
		{
			_buffer.WriteByte((byte)value);
			return;
		}
		var bytes = stackalloc byte[4];
		var count = Encoding.UTF8.GetBytes(&value, 1, bytes, 4);
		while (--count >= 0)
			_buffer.WriteByte(*bytes++);
	}

	/// <inheritdoc />
	protected override void Value(string? value)
	{
		if (value == null)
			_buffer.Write(NullValue);
		else
			Strings.EscapeUtf8CsString(_buffer, value.AsSpan());
	}

	/// <summary>
	/// Returns internal <see cref="MemoryStream"/> buffer of the <see cref="JsonUtf8Builder"/>.
	/// </summary>
	/// <returns></returns>
	public MemoryStream ToMemoryStream() => _buffer;

	/// <summary>
	/// Creates a new <see cref="JsonUtf8Builder"/> from <see cref="MemoryStream"/>.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static JsonUtf8Builder FromMemoryStream(MemoryStream text) => new JsonUtf8Builder(text);


	/// <summary>
	/// Returns internal <see cref="MemoryStream"/> buffer of the <see cref="JsonUtf8Builder"/>.
	/// </summary>
	/// <param name="json"></param>
	/// <returns></returns>
	public static explicit operator MemoryStream(JsonUtf8Builder json)
	{
		if (json is null)
			throw new ArgumentNullException(nameof(json));
		return json._buffer;
	}

	/// <summary>
	/// Creates a new <see cref="JsonUtf8Builder"/> from <see cref="MemoryStream"/>.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static implicit operator JsonUtf8Builder(MemoryStream text)
	{
		return new JsonUtf8Builder(text);
	}
}
