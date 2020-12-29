// Lexxys Infrastructural library.
// file: XmlBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Lexxys
{
	using Xml;

	/// <summary>
	/// Represents a writer that provides a fast, non-cached, forward-only way to generate
	/// streams or files that contain XML data.
	/// </summary>
	public abstract class XmlBuilder
	{
		private enum State
		{
			Value,
			Element,
			Attribute
		}

		private readonly Stack<string> _elements;
		private readonly HashSet<object> _visited;
		private State _state;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBuilder"/> class.
		/// </summary>
		protected XmlBuilder()
		{
			_elements = new Stack<string>();
			_visited = new HashSet<object>();
		}

		/// <summary>
		/// Writes a <see cref="String"/> value to a stream.
		/// </summary>
		/// <param name="value">The <see cref="String"/> value to write.</param>
		/// <returns></returns>
		protected abstract XmlBuilder Text(string value);
		/// <summary>
		/// Writes a <see cref="Char"/> value to a stream.
		/// </summary>
		/// <param name="value">The <see cref="Char"/> value to write.</param>
		/// <returns></returns>
		protected abstract XmlBuilder Text(char value);
		/// <summary>
		/// Encodes a <see cref="String"/> value as XML attribute value and writes the encoded value to a stream.
		/// </summary>
		/// <param name="value">The <see cref="String"/> value to write.</param>
		/// <returns></returns>
		protected abstract XmlBuilder EncodeAttribute(string value);
		/// <summary>
		/// Encodes a <see cref="String"/> value as XML value and writes the encoded value to a stream.
		/// </summary>
		/// <param name="value">The <see cref="String"/> value to write.</param>
		/// <returns></returns>
		protected abstract XmlBuilder EncodeValue(string value);

		/// <summary>
		/// Naming rules of the XML elements and attributes (default <see cref="NamingCaseRule.None"/>).
		/// </summary>
		public NamingCaseRule NamingRule { get; set; }
		/// <summary>
		/// Indicates that XML elements are better than XML attributes while writing <see cref="System.Object"/> value (default <code>false</code>).
		/// </summary>
		public bool PreferElements { get; set; }

		/// <summary>
		/// Indicates that the <see cref="XmlBuilder"/> is writing start element tag.
		/// </summary>
		public bool InElement => _state != State.Value;
		/// <summary>
		/// Indicates that the <see cref="XmlBuilder"/> is writing attribute.
		/// </summary>
		public bool InAttribute => _state == State.Attribute;

		private string ForceName(string name)
		{
			if (((int)NamingRule & 8) != 0)
				name = Rename(name);
			return XmlConvert.EncodeName(name);
		}

		private string PreferName(string name)
		{
			return XmlConvert.EncodeName(Rename(name));
		}

		/// <summary>
		/// Renames the <paramref name="value"/> according to the <see cref="NamingRule"/>.
		/// </summary>
		/// <param name="value">The name to rename</param>
		/// <returns>Renamed value</returns>
		public string Rename(string value)
		{
			if (String.IsNullOrEmpty(value))
				return value;
			return ((NamingCaseRule)((int)NamingRule & 7)) switch
			{
				NamingCaseRule.PreferLowerCase => value.ToLowerInvariant(),
				NamingCaseRule.PreferCamelCase => Strings.ToCamelCase(value),
				NamingCaseRule.PreferPascalCase => Strings.ToPascalCase(value),
				NamingCaseRule.PreferUpperCase => value.ToUpperInvariant(),
				_ => value,
			};
		}

		private static string ReferenceValue(object value)
		{
			Contract.Requires(value != null);
			return "^[" + value.ToString() + "]";
		}

		/// <summary>
		/// Writes a start tag with specified <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the element</param>
		/// <returns><see cref="XmlBuilder"/></returns>
		public XmlBuilder Element(string name)
		{
			if (name == null || name.Length == 0)
				throw new ArgumentNullException(nameof(name));
			if (_state == State.Attribute)
				Text("\"\">");
			else if (_state == State.Element)
				Text('>');
			var s = ForceName(name);
			Text('<').Text(s);
			_elements.Push(s);
			_state = State.Element;
			return this;
		}

		/// <summary>
		/// Writes end tag of current element.
		/// </summary>
		/// <returns><see cref="XmlBuilder"/></returns>
		public XmlBuilder End()
		{
			var s = _elements.Pop();
			if (_state == State.Attribute)
				Text("\"\"/>");
			if (_state == State.Element)
				Text("/>");
			else
				Text("</").Text(s).Text('>');
			_state = State.Value;
			return this;
		}

		/// <summary>
		/// Writes the start of attribute with the specified <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Attrib(string name)
		{
			if (name == null || name.Length <= 0)
				throw new ArgumentNullException(nameof(name));
			if (_state == State.Value)
				throw new InvalidOperationException("Trying to add attribute without element.");
			if (_state == State.Attribute)
				Text("\"\"");
			else
				_state = State.Attribute;
			return Text(' ').Text(ForceName(name)).Text('=');
		}

		#region Item

		private XmlBuilder AppendItem(string name, string value)
		{
			if (name == null || name.Length == 0)
				throw new ArgumentNullException(nameof(name));
			var s = ForceName(name);
			if (_state == State.Value)
				return String.IsNullOrEmpty(value) ? Text('<').Text(s).Text("/>"): Text('<').Text(s).Text('>').Text(value).Text("</").Text(s).Text('>');
			if (_state == State.Attribute)
			{
				Text("\"\"");
				_state = State.Element;
			}
			return String.IsNullOrEmpty(value) ? this: Text(' ').Text(s).Text('=').EncodeAttribute(value);
		}

		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, string value)
		{
			return AppendItem(name, value);
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, bool value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, sbyte value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, byte value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, short value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, ushort value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, int value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, uint value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, long value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, ulong value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, float value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, double value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, decimal value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, Guid value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, TimeSpan value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime value, XmlDateTimeSerializationMode dateTimeOption)
		{
			return AppendItem(name, XmlTools.Convert(value, dateTimeOption));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime value, string format)
		{
			return AppendItem(name, XmlTools.Convert(value, format));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTimeOffset value)
		{
			return AppendItem(name, XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTimeOffset value, string format)
		{
			return AppendItem(name, XmlTools.Convert(value, format));
		}

		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, bool? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, sbyte? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, byte? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, short? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, ushort? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, int? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, uint? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, long? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, ulong? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, float? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, double? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, decimal? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, Guid? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, TimeSpan? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime? value)
		{
			return value == null ? AppendItem(name, null): Item(name, value.GetValueOrDefault());
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime? value, XmlDateTimeSerializationMode dateTimeOption)
		{
			return value == null ? AppendItem(name, null): Item(name, value.GetValueOrDefault(), dateTimeOption);
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTime? value, string format)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault(), format));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTimeOffset? value)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, DateTimeOffset? value, string format)
		{
			return AppendItem(name, value == null ? null: XmlTools.Convert(value.GetValueOrDefault(), format));
		}

		/// <summary>
		/// Writes an attribute or single element with specified <paramref name="name"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="name">The name of the attribute.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns></returns>
		public XmlBuilder Item(string name, object value)
		{
			if (value == null)
				return AppendItem(name, null);
			if (value is IDumpXml id)
				return Element(name).Value(id).End();
			string s = XmlTools.Convert(value);
			if (s != null)
				return AppendItem(name, s);
			return Element(name).Object(value).End();
		}

		#endregion

		#region Value

		/// <summary>
		/// Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">Encoded element value to append to the buffer</param>
		/// <returns><see cref="XmlBuilder"/></returns>
		private XmlBuilder AppendValue(string value)
		{
			if (_state == State.Attribute)
			{
				_state = State.Element;
				return value == null ? Text("\"\""): EncodeAttributeValue(value);
			}
			if (_state == State.Element)
			{
				_state = State.Value;
				Text('>');
			}
			return Text(value);
		}

		private XmlBuilder EncodeAttributeValue(string value)
		{
			if (value.IndexOf('"') < 0)
				return Text('"').Text(value).Text('"');
			if (value.IndexOf('\'') < 0)
				return Text('\'').Text(value).Text('\'');
			return Text('"').Text(value.Replace("\"", "&quot;")).Text('"');
		}

		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(string value)
		{
			if (_state == State.Attribute)
			{
				_state = State.Element;
				return EncodeAttribute(value);
			}
			if (_state == State.Element)
			{
				_state = State.Value;
				Text('>');
			}
			return EncodeValue(value);
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(bool value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(sbyte value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(byte value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(short value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(ushort value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(int value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(uint value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(long value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(ulong value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(float value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(double value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(decimal value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(Guid value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(TimeSpan value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="dateTimeOption"></param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime value, XmlDateTimeSerializationMode dateTimeOption)
		{
			return AppendValue(XmlTools.Convert(value, dateTimeOption));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime value, string format)
		{
			return AppendValue(XmlTools.Convert(value, format));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTimeOffset value)
		{
			return AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTimeOffset value, string format)
		{
			return AppendValue(XmlTools.Convert(value, format));
		}

		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(bool? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(sbyte? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(byte? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(short? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(ushort? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(int? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(uint? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(long? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(ulong? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(float? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(double? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(decimal? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(Guid? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(TimeSpan? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime? value)
		{
			return value == null ? this : Value(value.GetValueOrDefault());
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="dateTimeOption"></param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime? value, XmlDateTimeSerializationMode dateTimeOption)
		{
			return value == null ? this : Value(value.GetValueOrDefault(), dateTimeOption);
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTime? value, string format)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault(), format));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTimeOffset? value)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault()));
		}
		/// <summary>
		///  Writes the value of the current XML element or attribute.
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <param name="format">The format to which <paramref name="value"/> is converted</param>
		/// <returns></returns>
		public XmlBuilder Value(DateTimeOffset? value, string format)
		{
			return value == null ? this : AppendValue(XmlTools.Convert(value.GetValueOrDefault(), format));
		}

		#endregion

		/// <summary>
		/// Writes object value
		/// </summary>
		/// <param name="value">The value to write</param>
		/// <returns></returns>
		public XmlBuilder Value(IDumpXml value)
		{
			return value == null ? this: value.ToXmlContent(this);
		}

		/// <summary>
		/// Writes collection of values
		/// </summary>
		/// <param name="items">The collection to write</param>
		/// <param name="itemName">Optional name of the element item.</param>
		/// <returns></returns>
		public XmlBuilder Value(IEnumerable<IDumpXml> items, string itemName = null)
		{
			foreach (var item in items)
			{
				Element(itemName ?? item.XmlElementName).Value(item).End();
			}
			return this;
		}

		/// <summary>
		/// Writes object value.
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <returns><see cref="XmlBuilder"/></returns>
		public XmlBuilder Value(object value)
		{
			return value == null ? this : Value(value, PreferElements);
		}

		/// <summary>
		/// Writes object value.
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <param name="elements">Use XML elments instead of attributies for properties of <paramref name="value"/></param>
		/// <returns><see cref="XmlBuilder"/></returns>
		public XmlBuilder Value(object value, bool elements)
		{
			return value is IDumpXml dump ? dump.ToXmlContent(this) : Object(value, elements);
		}

		/// <summary>
		/// Writes object value ignoring <see cref="IDumpXml"/> implementation.
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <param name="elements">Use XML elments instead of attributies for properties of <paramref name="value"/></param>
		/// <returns><see cref="XmlBuilder"/></returns>
		public XmlBuilder Object(object value, bool elements = false)
		{
			if (value == null)
				return this;

			string xmlValue = XmlTools.Convert(value);
			if (xmlValue != null)
				return AppendValue(xmlValue);

			if (_visited.Contains(value))
				return Value(ReferenceValue(value));
			_visited.Add(value);

			if (value is IEnumerable enumerable)
			{
				string itemName = Rename("Item");
				foreach (var item in enumerable)
				{
					if (item is IDumpXml dumpXml)
						Element(dumpXml.XmlElementName ?? itemName).Value(dumpXml).End();
					else
						Element(itemName).Value(item, elements).End();
				}
			}
			else
			{
				AppendProperties(value, elements);
			}
			return this;
		}

		/// <summary>
		/// Writes XML element and value.
		/// </summary>
		/// <param name="name">Name of the XML element</param>
		/// <param name="value">Value of the XML element</param>
		/// <returns></returns>
		public XmlBuilder Element(string name, object value)
		{
			return Element(name).Value(value, PreferElements).End();
		}

		/// <summary>
		/// Writes XML element and value.
		/// </summary>
		/// <param name="name">Name of the XML element</param>
		/// <param name="value">Value of the XML element</param>
		/// <param name="elements">Use XML elments instead of attributies for properties of <paramref name="value"/></param>
		/// <returns></returns>
		public XmlBuilder Element(string name, object value, bool elements)
		{
			return Element(name).Value(value, elements).End();
		}

		/// <summary>
		/// Writes XML element and value.
		/// </summary>
		/// <param name="name">Name of the XML element</param>
		/// <param name="value">Value of the XML element</param>
		/// <returns></returns>
		public XmlBuilder Element(string name, IDumpXml value)
		{
			return Element(name).Value(value).End();
		}

		/// <summary>
		/// Writes XML element and collection of sub-elements.
		/// </summary>
		/// <param name="name">Name of the XML element</param>
		/// <param name="value">Value of the XML element</param>
		/// <param name="itemName">Optional name of the collection item</param>
		/// <returns></returns>
		public XmlBuilder Element(string name, IEnumerable<IDumpXml> value, string itemName = null)
		{
			return Element(name).Value(value, itemName).End();
		}

		private void AppendElement(string name, object value, bool elements)
		{
			if (value == null)
				return;

			string xmlValue = XmlTools.Convert(value);
			if (xmlValue != null)
			{
				if (elements || _state == State.Value)
					Element(name).AppendValue(xmlValue).End();
				else
					AppendItem(name, xmlValue);
				return;
			}

			if (_visited.Contains(value))
			{
				Element(name).AppendValue(ReferenceValue(value)).End();
				return;
			}
			_visited.Add(value);

			if (!(value is IEnumerable enumerable))
			{
				Element(name);
				AppendProperties(value, elements);
				End();
			}
			else
			{
				AppendValue("");
				foreach (var item in enumerable)
				{
					AppendElement(name, item, elements);
				}
			}
		}

		private void AppendProperties(object value, bool elements)
		{
			if (value == null)
				return;

			Type type = value.GetType();
			var items = new List<KeyValuePair<string, object>>();
			foreach (var item in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField))
			{
				try
				{
					object v = item.GetValue(value);
					items.Add(KeyValue.Create(item.Name, v));
				}
				catch
				{
					// ignore all internal exceptions
				}
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
						items.Add(KeyValue.Create(item.Name, v));
					}
					catch
					{
						// ignore all internal exceptions
					}
				}
			}
			if (elements || _state == State.Value)
			{
				for (int i = 0; i < items.Count; ++i)
				{
					var item = items[i];
					if (item.Key == null)
						continue;
					AppendElement(PreferName(item.Key), item.Value, elements);
				}
			}
			else
			{
				var rest = new List<KeyValuePair<string, object>>();
				for (int i = 0; i < items.Count; ++i)
				{
					var item = items[i];
					string xmlValue = XmlTools.Convert(item.Value);
					if (xmlValue == null)
					{
						rest.Add(item);
					}
					else
					{
						AppendItem(PreferName(item.Key), xmlValue);
						items[i] = new KeyValuePair<string, object>();
					}
				}
				foreach (var item in rest)
				{
					if (item.Key != null)
						AppendElement(PreferName(item.Key), item.Value, false);
				}
			}
		}

		/// <summary>
		/// Write all ends of elements.
		/// </summary>
		public virtual void Flush()
		{
			while (_elements.Count > 0)
				End();
		}
	}

	/// <summary>
	/// Represents <see cref="XmlBuilder"/> that uses <see cref="StringBuilder"/> to write an XML stream.
	/// </summary>
	[DebuggerDisplay("{" + nameof(_buffer) + "}")]
	public class XmlStringBuilder: XmlBuilder
	{
		private readonly StringBuilder _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStringBuilder"/> class.
		/// </summary>
		public XmlStringBuilder(): this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStringBuilder"/> class.
		/// </summary>
		public XmlStringBuilder(StringBuilder buffer)
		{
			_buffer = buffer ?? new StringBuilder();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStringBuilder"/> class.
		/// </summary>
		public XmlStringBuilder(NamingCaseRule namingRule, bool preferElements = false): this(null, namingRule, preferElements)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStringBuilder"/> class.
		/// </summary>
		public XmlStringBuilder(StringBuilder buffer, NamingCaseRule namingRule, bool preferElements = false) : this(null)
		{
			_buffer = buffer ?? new StringBuilder();
			NamingRule = namingRule;
			PreferElements = preferElements;
		}

		public StringBuilder Buffer => _buffer;

		/// <inheritdoc />
		protected override XmlBuilder Text(char value)
		{
			_buffer.Append(value);
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder Text(string value)
		{
			_buffer.Append(value);
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder EncodeAttribute(string value)
		{
			XmlTools.EncodeAttribute(_buffer, value);
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder EncodeValue(string value)
		{
			XmlTools.Encode(_buffer, value);
			return this;
		}

		/// <summary>
		/// Get XML as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			Flush();
			return _buffer.ToString();
		}

		/// <summary>
		/// Returns internal <see cref="StringBuilder"/> buffer of the <see cref="XmlStringBuilder"/>.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static explicit operator StringBuilder(XmlStringBuilder xml)
		{
			return xml._buffer;
		}
		/// <summary>
		/// Creates a new <see cref="XmlStringBuilder"/> from <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static implicit operator XmlStringBuilder(StringBuilder text)
		{
			return new XmlStringBuilder(text);
		}
	}

	/// <summary>
	/// Represents <see cref="XmlBuilder"/> that uses <see cref="TextWriter"/> to write an XML stream.
	/// </summary>
	public class XmlStreamBuilder: XmlBuilder
	{
		private readonly TextWriter _writer;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStreamBuilder"/> class.
		/// </summary>
		public XmlStreamBuilder(TextWriter writer)
		{
			_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlStreamBuilder"/> class.
		/// </summary>
		public XmlStreamBuilder(TextWriter writer, NamingCaseRule namingRule, bool preferElements = false) : this(null)
		{
			_writer = writer ?? throw new ArgumentNullException(nameof(writer));
			NamingRule = namingRule;
			PreferElements = preferElements;
		}

		public TextWriter Writer => _writer;

		/// <inheritdoc />
		protected override XmlBuilder Text(string value)
		{
			_writer.Write(value);
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder Text(char value)
		{
			_writer.Write(value);
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder EncodeAttribute(string value)
		{
			_writer.Write(XmlTools.EncodeAttribute(value));
			return this;
		}

		/// <inheritdoc />
		protected override XmlBuilder EncodeValue(string value)
		{
			_writer.Write(XmlTools.Encode(value));
			return this;
		}

		/// <inheritdoc />
		public override void Flush()
		{
			base.Flush();
			_writer.Flush();
		}
	}
}


