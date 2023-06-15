// Lexxys Infrastructural library.
// file: XmlLiteBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Reflection;
using System.Xml;

namespace Lexxys.Xml;

public class XmlLiteBuilder
{
	private List<XmlLiteNode>? _nodes;
	private XmlDraftNode? _next;
	private readonly StringComparer _comparer;

	public XmlLiteBuilder(bool ignoreCase = false)
	{
		_comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal;
	}

	public XmlLiteBuilder(StringComparison comparisionMode)
	{
		_comparer = comparisionMode switch
		{
			StringComparison.CurrentCulture => StringComparer.CurrentCulture,
			StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
			StringComparison.Ordinal => StringComparer.Ordinal,
			StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
			StringComparison.InvariantCulture => StringComparer.InvariantCulture,
			StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
			_ => StringComparer.Ordinal,
		};
	}

	public List<XmlLiteNode> Complete()
	{
		Flush();
		List<XmlLiteNode> result = _nodes ?? new List<XmlLiteNode>();
		_nodes = null;
		return result;
	}

	public IReadOnlyList<XmlLiteNode> GetNodes()
	{
		Flush();
		return ReadOnly.Wrap(_nodes) ?? ReadOnly.Empty<XmlLiteNode>();
	}

	public XmlLiteNode GetFirstNode()
	{
		Flush();
		return _nodes == null || _nodes.Count == 0 ? XmlLiteNode.Empty: _nodes[0];
	}

	public XmlLiteBuilder Element(string name)
	{
		if (name is not { Length: > 0 })
			throw new ArgumentNullException(nameof(name));

		if (_next == null)
			_next = new XmlDraftNode(name, _comparer);
		else
			_next.StartElement(name);
		return this;
	}

	public XmlLiteBuilder End()
	{
		if (_next == null)
			throw new InvalidOperationException();
		_next.EndElement();
		if (_next.IsComplete)
		{
			_nodes ??= new List<XmlLiteNode>();
			_nodes.Add(_next.Convert());
			_next = null;
		}
		return this;
	}

	public XmlLiteBuilder Descendant(XmlLiteNode node)
	{
		if (_next == null)
			throw new InvalidOperationException();

		_next.Descendant(node);
		return this;
	}

	public XmlLiteBuilder Descendant(params XmlLiteNode[] nodes)
	{
		if (nodes is null)
			throw new ArgumentNullException(nameof(nodes));
		if (_next == null)
			throw new InvalidOperationException();

		foreach (var node in nodes)
		{
			_next.Descendant(node);
		}
		return this;
	}

	public XmlLiteBuilder Descendant(IEnumerable<XmlLiteNode> nodes)
	{
		if (nodes is null)
			throw new ArgumentNullException(nameof(nodes));
		if (_next == null)
			throw new InvalidOperationException();

		foreach (var node in nodes)
		{
			_next.Descendant(node);
		}
		return this;
	}

	public XmlLiteBuilder Elements<T>(IEnumerable<T> items, string name, Action<XmlLiteBuilder, T> action)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));
		if (name == null)
			throw new ArgumentNullException(nameof(name));
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		if (_next == null)
		{
			_nodes ??= new List<XmlLiteNode>();
			foreach (var item in items)
			{
				_next = new XmlDraftNode(name, _comparer);
				action(this, item);
				_nodes.Add(_next.Convert());
			}
			_next = null;
		}
		else
		{
			foreach (var item in items)
			{
				_next.StartElement(name);
				action(this, item);
				_next.EndElement();
			}
		}


		return this;
	}

	#region Attrib

	/// <summary>
	/// Writes an attribute <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, string? value)
	{
		if (value != null)
		{
			if (_next == null)
				throw new InvalidOperationException();
			_next.Attribute(name, value);
		}
		return this;
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, bool value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, sbyte value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, byte value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, short value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, ushort value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, int value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, uint value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, long value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, ulong value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, float value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, double value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, decimal value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, Guid value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, TimeSpan value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime value, XmlDateTimeSerializationMode dateTimeOption)
	{
		return Attrib(name, XmlTools.Convert(value, dateTimeOption));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime value, string format)
	{
		return Attrib(name, XmlTools.Convert(value, format));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTimeOffset value)
	{
		return Attrib(name, XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTimeOffset value, string format)
	{
		return Attrib(name, XmlTools.Convert(value, format));
	}

	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, bool? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, sbyte? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, byte? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, short? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, ushort? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, int? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, uint? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, long? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, ulong? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, float? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, double? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, decimal? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, Guid? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, TimeSpan? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime? value)
	{
		return value == null ? Attrib(name, (string?)null) : Attrib(name, value.GetValueOrDefault());
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="dateTimeOption">How to treat the <paramref name="value"/>.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime? value, XmlDateTimeSerializationMode dateTimeOption)
	{
		return value == null ? Attrib(name, (string?)null) : Attrib(name, value.GetValueOrDefault(), dateTimeOption);
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTime? value, string format)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault(), format));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTimeOffset? value)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes an attribute or element with <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted.</param>
	/// <returns></returns>
	public XmlLiteBuilder Attrib(string name, DateTimeOffset? value, string format)
	{
		return Attrib(name, value == null ? null : XmlTools.Convert(value.GetValueOrDefault(), format));
	}

	#endregion

	#region Value

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(string value)
	{
		if (_next == null)
			throw new InvalidOperationException();
		_next.SetValue(value);
		return this;
	}

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(bool value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(sbyte value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(byte value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(short value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(ushort value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(int value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(uint value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(long value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(ulong value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(float value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(double value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(decimal value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(Guid value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(TimeSpan value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="dateTimeOption"></param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime value, XmlDateTimeSerializationMode dateTimeOption)
	{
		return Value(XmlTools.Convert(value, dateTimeOption));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime value, string format)
	{
		return Value(XmlTools.Convert(value, format));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTimeOffset value)
	{
		return Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTimeOffset value, string format)
	{
		return Value(XmlTools.Convert(value, format));
	}

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(bool? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(sbyte? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(byte? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(short? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(ushort? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(int? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(uint? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(long? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(ulong? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(float? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(double? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(decimal? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(Guid? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(TimeSpan? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime? value)
	{
		return value == null ? this : Value(value.GetValueOrDefault());
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="dateTimeOption"></param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime? value, XmlDateTimeSerializationMode dateTimeOption)
	{
		return value == null ? this : Value(value.GetValueOrDefault(), dateTimeOption);
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTime? value, string format)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault(), format));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTimeOffset? value)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault()));
	}
	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <param name="format">The format to which <paramref name="value"/> is converted</param>
	/// <returns></returns>
	public XmlLiteBuilder Value(DateTimeOffset? value, string format)
	{
		return value == null ? this : Value(XmlTools.Convert(value.GetValueOrDefault(), format));
	}

	#endregion

	public XmlLiteBuilder Value(object? value, bool elements = false)
	{
		if (value != null)
			Item(value, elements);
		return this;
	}

	public XmlLiteBuilder Element(string name, object? value, bool elements = false)
	{
		if (name == null)
			throw new ArgumentNullException(nameof(name));

		Item(name, value, elements, true);
		return this;
	}

	private void Item(string name, object? value, bool elements, bool keepOpen)
	{
		string? xmlValue = value as string ?? XmlTools.Convert(value);
		if (xmlValue != null)
		{
			if (keepOpen)
				Element(name).Value(xmlValue);
			else if (elements)
				Element(name).Value(xmlValue).End();
			else
				Attrib(name, xmlValue);
			return;
		}

		if (value is not IEnumerable enumerable)
		{
			Element(name);
			Item(value, elements);
			if (!keepOpen)
				End();
			return;
		}

		if (enumerable is IEnumerable<XmlLiteNode> liteNodes)
		{
			Element(name);
			Descendant(liteNodes);
			if (!keepOpen)
				End();
			return;
		}

		bool end = false;
		foreach (var item in enumerable)
		{
			if (end)
				End();
			Element(name);
			Item(item, elements);
			end = true;
		}
		if (!keepOpen && end)
			End();
	}

	private void Item(object? value, bool elements)
	{
		if (value is null)
			return;

		string? xmlValue = value as string ?? XmlTools.Convert(value);
		if (xmlValue != null)
		{
			Value(xmlValue);
			return;
		}

		if (value is XmlLiteNode node)
		{
			Descendant(node);
			return;
		}
		Type type = value.GetType();
		foreach (var item in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField))
		{
			try
			{
				object? v = item.GetValue(value);
				Item(item.Name, v, elements, false);
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch { /* ignore */ }
			#pragma warning restore CA1031 // Do not catch general exception types
		}
		foreach (var item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
		{
			if (!item.CanRead ||
				item.PropertyType.IsGenericTypeDefinition ||
				item.PropertyType.IsAbstract ||
				item.GetIndexParameters().Length != 0 ||
				item.PropertyType.IsGenericParameter)
				continue;
			try
			{
				object? v = item.GetValue(value);
				Item(item.Name, v, elements, false);
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch { /* ignore */ }
			#pragma warning restore CA1031 // Do not catch general exception types
		}
	}

	private void Flush()
	{
		if (_next == null)
			return;
		_nodes ??= new List<XmlLiteNode>();
		_nodes.Add(_next.Convert());
		_next = null;
	}

	private class XmlDraftNode
	{
		private const string EmptyAttributeName = "@";
		private Stack<Node>? _node;
		private Node _top;
		private readonly StringComparer _comparer;

		public XmlDraftNode(string name, StringComparer comparer)
		{
			_comparer = comparer;
			_top = new Node(name, _comparer);
		}

		public void SetValue(string value)
		{
			_top.SetValue(value);
		}

		public void StartElement(string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			(_node ??= new Stack<Node>()).Push(_top);
			var d = new Node(name, _comparer);
			_top.AddChildNode(d);
			_top = d;
		}

		public void Descendant(XmlLiteNode node)
		{
			if (node is { IsEmpty: false })
				_top.AddChildNode(node);
		}

		public void EndElement()
		{
			if (_node != null && _node.Count != 0)
				_top = _node.Pop();
			else
				IsComplete = true;
		}

		public bool IsComplete { get; private set; }

		public XmlLiteNode Convert()
		{
			if (_node is { Count: > 0 })
			{
				while (_node.Count > 1)
					_node.Pop();
				_top = _node.Pop();
			}
			return _top.Convert();
		}

		public void Attribute(string name, string? value)
		{
			if (value == null)
				return;
			if (_top == null)
				throw new InvalidOperationException();
			_top.SetAttribute(name.TrimToNull() ?? EmptyAttributeName, value);
		}

		private class Node
		{
			private readonly string _name;
			private readonly IEqualityComparer<string?> _comparer;
			private OrderedBag<string, string>? _attrib;
			private List<Node>? _nodeDescendants;
			private List<XmlLiteNode>? _xmlDescendants;
			private string? _value;

			public Node(string name, StringComparer comparer)
			{
				_name = name;
				_comparer = comparer;
			}

			public void SetValue(string value)
			{
				_value = value;
			}

			public void SetAttribute(string name, string value)
			{
				_attrib ??= new OrderedBag<string, string>(_comparer);
				_attrib[name] = value;
			}

			public void AddChildNode(Node child)
			{
				if (child is null)
					throw new ArgumentNullException(nameof(child));
				(_nodeDescendants ??= new List<Node>()).Add(child);
			}

			public void AddChildNode(XmlLiteNode child)
			{
				if (child is null)
					throw new ArgumentNullException(nameof(child));
				if (child.IsEmpty)
					throw new ArgumentOutOfRangeException(nameof(child), child, null);
				(_xmlDescendants ??= new List<XmlLiteNode>()).Add(child);
			}

			public XmlLiteNode Convert()
			{
				if (_nodeDescendants == null && _xmlDescendants == null)
					return new XmlLiteNode(_name, _value, _comparer, _attrib, null);
				var descendants = _nodeDescendants == null ? new List<XmlLiteNode>(): _nodeDescendants.ConvertAll(o => o.Convert());
				if (_xmlDescendants != null)
					descendants.AddRange(_xmlDescendants);
				return new XmlLiteNode(_name, _value, _comparer, _attrib, descendants);
			}
		}
	}
}
