#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Lexxys.Xml;

/// <summary>
/// A class to build a collection of <see cref="IXmlReadOnlyNode"/> trees.
/// </summary>
[Serializable]
public class XmlLiteBuilder
{
	private List<IXmlReadOnlyNode>? _nodes;
	private XmlDraftNode? _next;
	private readonly StringComparer _comparer;

	/// <summary>
	/// Returns an empty <see cref="IXmlNode"/>.
	/// </summary>
	public static IXmlNode Empty => EmptyNode.Instance;

	/// <summary>
	/// Creates a new instance of <see cref="XmlLiteBuilder"/>.
	/// </summary>
	/// <param name="ignoreCase">If <c>true</c> the <see cref="StringComparer.OrdinalIgnoreCase"/> is used to compare names of elements and attributes.</param>
	public XmlLiteBuilder(bool ignoreCase = false)
	{
		_comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
	}

	/// <summary>
	/// Creates a new instance of <see cref="XmlLiteBuilder"/>.
	/// </summary>
	/// <param name="comparisionMode">A <see cref="StringComparison"/> value that specifies how names of elements and attributes are compared.</param>
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

	/// <summary>
	/// Completes the building process and returns a list of constructed <see cref="IXmlReadOnlyNode"/> trees.
	/// </summary>
	/// <returns></returns>
	public List<IXmlReadOnlyNode> Build()
	{
		Flush();
		List<IXmlReadOnlyNode> result = _nodes ?? new List<IXmlReadOnlyNode>();
		_nodes = null;
		return result;
	}

	/// <summary>
	/// Returns an actual list of constructed <see cref="IXmlReadOnlyNode"/> trees.
	/// </summary>
	/// <returns></returns>
	public IReadOnlyList<IXmlReadOnlyNode> GetNodes()
	{
		Flush();
		return ReadOnly.Wrap(_nodes) ?? ReadOnly.Empty<IXmlReadOnlyNode>();
	}

	/// <summary>
	/// Starts a new XML node with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the node.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteBuilder Begin(string name)
	{
		if (name is not { Length: > 0 }) throw new ArgumentNullException(nameof(name));

		if (_next == null)
			_next = new XmlDraftNode(name, _comparer);
		else
			_next.StartElement(name);
		return this;
	}

	/// <summary>
	/// Ends the current XML node.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public XmlLiteBuilder End()
	{
		if (_next == null) throw new InvalidOperationException();
		_next.EndElement();
		if (_next.IsComplete)
		{
			_nodes ??= new List<IXmlReadOnlyNode>();
			_nodes.Add(_next.Convert());
			_next = null;
		}
		return this;
	}

	/// <summary>
	/// Adds a collection of sub-nodes to the current node.
	/// </summary>
	/// <param name="nodes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public XmlLiteBuilder Elements(IEnumerable<IXmlReadOnlyNode> nodes)
	{
		if (nodes is null) throw new ArgumentNullException(nameof(nodes));
		if (_next == null) throw new InvalidOperationException();

		foreach (var node in nodes)
		{
			_next.Descendant(node);
		}
		return this;
	}

	/// <summary>
	/// Adds a collection of <see cref="IXmlReadOnlyNode"/>s with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="items">Collection of items to be added.</param>
	/// <param name="name">Name of the nodes.</param>
	/// <param name="action">Action to be applied to each item.</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteBuilder Elements<T>(IEnumerable<T> items, string name, Action<XmlLiteBuilder, T> action)
	{
		if (items is null) throw new ArgumentNullException(nameof(items));
		if (name == null) throw new ArgumentNullException(nameof(name));
		if (action is null) throw new ArgumentNullException(nameof(action));

		if (_next == null)
		{
			_nodes ??= new List<IXmlReadOnlyNode>();
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
	/// Writes the specified <paramref name="value"/> and the <paramref name="value"/> properties as an XML element or attribute.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
	public XmlLiteBuilder Value(object? value, bool elements = false)
	{
		if (value != null)
			Item(value, elements);
		return this;
	}

	/// <summary>
	/// Adds an XML node with the specified <paramref name="name"/> and the <paramref name="value"/>.
	/// </summary>
	/// <param name="name">Name of the created XML element</param>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
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
				Begin(name).Value(xmlValue);
			else if (elements)
				Begin(name).Value(xmlValue).End();
			else
				Attrib(name, xmlValue);
			return;
		}

		if (value is not IEnumerable enumerable)
		{
			Begin(name);
			Item(value, elements);
			if (!keepOpen)
				End();
			return;
		}

		if (enumerable is IEnumerable<IXmlReadOnlyNode> liteNodes)
		{
			Begin(name);
			Elements(liteNodes);
			if (!keepOpen)
				End();
			return;
		}

		bool end = false;
		foreach (var item in enumerable)
		{
			if (end)
				End();
			Begin(name);
			Item(item, elements);
			end = true;
		}
		if (!keepOpen && end)
			End();
	}

	private void Element(IXmlReadOnlyNode node) => _next!.Descendant(node);


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

		if (value is IXmlReadOnlyNode node)
		{
			Element(node);
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
		_nodes ??= new List<IXmlReadOnlyNode>();
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

		public void SetValue(string value) => _top.SetValue(value);

		public void StartElement(string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			(_node ??= new Stack<Node>()).Push(_top);
			var d = new Node(name, _comparer);
			_top.AddChildNode(d);
			_top = d;
		}

		public void Descendant(IXmlReadOnlyNode node)
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

		public IXmlReadOnlyNode Convert()
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
			private readonly StringComparer _comparer;
			private OrderedBag<string, string>? _attrib;
			private List<Node>? _nodeDescendants;
			private List<IXmlReadOnlyNode>? _xmlDescendants;
			private string? _value;

			public Node(string name, StringComparer comparer)
			{
				_name = name;
				_comparer = comparer;
			}

			public void SetValue(string value) => _value = value;

			public void SetAttribute(string name, string value) => (_attrib ??= new OrderedBag<string, string>(_comparer))[name] = value;

			public void AddChildNode(Node child)
			{
				if (child is null)
					throw new ArgumentNullException(nameof(child));
				(_nodeDescendants ??= new List<Node>()).Add(child);
			}

			public void AddChildNode(IXmlReadOnlyNode child)
			{
				if (child is null)
					throw new ArgumentNullException(nameof(child));
				if (child.IsEmpty)
					throw new ArgumentOutOfRangeException(nameof(child), child, null);
				(_xmlDescendants ??= new List<IXmlReadOnlyNode>()).Add(child);
			}

			public IXmlReadOnlyNode Convert()
			{
				if (_nodeDescendants == null && _xmlDescendants == null)
					return new XmlLiteNode(_name, _value, _comparer, _attrib, null);
				var descendants = _nodeDescendants == null ? new List<IXmlReadOnlyNode>() : _nodeDescendants.ConvertAll(o => o.Convert());
				if (_xmlDescendants != null)
					descendants.AddRange(_xmlDescendants);
				return new XmlLiteNode(_name, _value, _comparer, _attrib, descendants);
			}
		}
	}

	private class EmptyNode : IXmlNode
	{
		public static readonly IXmlNode Instance = new EmptyNode();

		private EmptyNode() { }

		public string? this[string name] { get => null; set => throw new NotImplementedException(); }
		public string Name { get => String.Empty; set => throw new NotImplementedException(); }
		public string Value { get => String.Empty; set => throw new NotImplementedException(); }

		public IList<KeyValuePair<string, string>> Attributes => Array.Empty<KeyValuePair<string, string>>();
		public IList<IXmlNode> Elements => Array.Empty<IXmlNode>();
		public StringComparer Comparer => StringComparer.Ordinal;
		public bool IsEmpty => true;

		public XmlReader ReadSubtree() => XmlReader.Create(Stream.Null);

		public StringBuilder WriteXml(StringBuilder text, bool innerXml, string prefix) => text;

		public IXmlReadOnlyNode AsReadOnly() => this;

		IReadOnlyList<KeyValuePair<string, string>> IXmlReadOnlyNode.Attributes => Array.Empty<KeyValuePair<string, string>>();

		IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => Array.Empty<IXmlReadOnlyNode>();
	}
}

#endif