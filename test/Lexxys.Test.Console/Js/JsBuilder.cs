﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Lexxys;
using Lexxys.Xml;

namespace Lexxys.Test.Con.Js
{

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

		/// <summary>
		/// Writes a <see cref="String"/> value as a JavaScript escaped string.
		/// </summary>
		/// <param name="value">The <see cref="String"/> value to write.</param>
		protected virtual void Value(string value)
		{
			Text(value == null ? NullValue : Strings.EscapeCsString(value));
		}

		private void Comma()
		{
			switch (_state)
			{
				case State.Value:
					Text(',');
					_state = _elements.Count == 0 || _elements.Peek() != ']' ? State.Object : State.Array;
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
		public JsonBuilder Item(string name)
		{
			if (String.IsNullOrEmpty(name))
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

		private string Escape(object value)
		{
			if (value == null)
				return NullValue;

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
						return Strings.EscapeCsString(value.ToString());
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
							return "\"" + PreferName(cn.ToString(CultureInfo.InvariantCulture)) + "\"";
						else
							return cn.ToString(CultureInfo.InvariantCulture);

					case TypeCode.DateTime:
						return "\"" + XmlTools.Convert((DateTime)value) + "\"";
				}
			}
			if (value is TimeSpan)
				return "\"" + XmlTools.Convert((TimeSpan)value) + "\"";
			if (value is Guid)
				return "\"" + XmlTools.Convert((Guid)value) + "\"";
			if (value is DateTimeOffset)
				return "\"" + XmlTools.Convert((DateTimeOffset)value) + "\"";
			if (value is byte[] bytes)
				return "\"" + Convert.ToBase64String(bytes) + "\"";
			return null;
		}

		/// <summary>
		/// Writes a <see cref="IDictionary"/> value as a JSON object
		/// </summary>
		/// <param name="value">The <see cref="IDictionary"/> value to write</param>
		/// <returns></returns>
		public JsonBuilder Val(IDictionary value)
		{
			if (value == null)
			{
				Text(NullValue);
				return this;
			}
			Obj();
			foreach (DictionaryEntry item in value)
			{
				Item(PreferName(item.Key.ToString())).Val(item.Value);
			}
			return End();
		}

		private string ForceName(string name)
		{
			return ((int)NamingRule & 8) == 0 ? name : PreferName(name);
		}

		private string PreferName(string name)
		{
			if (String.IsNullOrEmpty(name))
				return name;
			switch ((NamingCaseRule)((int)NamingRule & 7))
			{
				case NamingCaseRule.PreferLowerCase:
					return name.ToLowerInvariant();
				case NamingCaseRule.PreferCamelCase:
					return Strings.ToCamelCase(name); // name.Length == 1 || name == name.ToUpperInvariant() ? name.ToLowerInvariant(): Char.ToLowerInvariant(name[0]).ToString() + name.Substring(1);
				case NamingCaseRule.PreferPascalCase:
					return Strings.ToPascalCase(name); // name.Length == 1 ? name.ToUpperInvariant(): Char.ToUpperInvariant(name[0]).ToString() + name.Substring(1);
				case NamingCaseRule.PreferUpperCase:
					return name.ToUpperInvariant();
				default:
					return name;
			}
		}

		/// <summary>
		/// Writes a <see cref="String"/> value.
		/// </summary>
		/// <param name="value">The <see cref="String"/> value to write</param>
		/// <returns></returns>
		public JsonBuilder Val(string value)
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
			Text(value ? "true" : "false");
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
			Text(value.ToString(CultureInfo.InvariantCulture));
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
		public JsonBuilder Val(byte[] value)
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
		public JsonBuilder Val(IEnumerable value)
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
		public JsonBuilder Val(IDumpJson value)
		{
			Comma();
			if (value == null)
				Text(NullValue);
			else
				//value.ToJson(this);
			_state = State.Value;
			return this;
		}

		/// <summary>
		/// Writes an object <paramref name="value"/> using <see cref="IDumpJson"/> implementation if it is present.
		/// </summary>
		/// <param name="value">The <see cref="Object"/> value to write.</param>
		/// <returns></returns>
		public JsonBuilder Val(object value)
		{
			if (!(value is IDumpJson i))
				return ValObj(value);
			if (_state == State.Value)
				Comma();
			Obj();
			//i.ToJsonContent(this);
			End();
			_state = State.Value;
			return this;
		}

		/// <summary>
		/// Writes an object <paramref name="value"/> ignoring <see cref="IDumpJson"/> implementation.
		/// </summary>
		/// <param name="value">The <see cref="Object"/> value to write.</param>
		/// <returns></returns>
		public JsonBuilder ValObj(object value)
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
				Item(PreferName(item.Name)).Val(item.GetValue(value));
			}
			foreach (var item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
			{
				if (item.CanRead &&
					!item.PropertyType.IsGenericTypeDefinition &&
					!item.PropertyType.IsAbstract &&
					item.GetIndexParameters().Length == 0 &&
					!item.PropertyType.IsGenericParameter)
				{
					try
					{
						object v = item.GetValue(value);
						Item(PreferName(item.Name)).Val(v);
					}
					catch
					{
						// ignore all internal exceptions
					}
				}
			}
			return End();
		}

		public JsonBuilder Content(IDumpJson value)
		{
			//value?.ToJsonContent(this);
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
	}

	/// <summary>
	/// Represents <see cref="JsonBuilder"/> that uses <see cref="StringBuilder"/> to write a JSON stream.
	/// </summary>
	[DebuggerDisplay("{" + nameof(_buffer) + "}")]
	public class JsonStringBuilder : JsonBuilder
	{
		private readonly StringBuilder _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonStringBuilder"/> class.
		/// </summary>
		public JsonStringBuilder() : this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonStringBuilder"/> class.
		/// </summary>
		public JsonStringBuilder(StringBuilder buffer)
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
		protected override void Value(string value)
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
		public static explicit operator StringBuilder(JsonStringBuilder json)
		{
			return json._buffer;
		}

		/// <summary>
		/// Creates a new <see cref="JsonStringBuilder"/> from <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static implicit operator JsonStringBuilder(StringBuilder text)
		{
			return new JsonStringBuilder(text);
		}
	}


	/// <summary>
	/// Represents <see cref="JsonBuilder"/> that uses <see cref="TextWriter"/> to write a JSON stream.
	/// </summary>
	public class JsonStreamBuilder : JsonBuilder
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
		protected override void Value(string value)
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
}
