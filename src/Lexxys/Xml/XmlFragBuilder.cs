// Lexxys Infrastructural library.
// file: XmlLiteBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Lexxys.Xml;

public static class XmlFragBuilder
{
	/// <summary>
	/// Returns an empty <see cref="IXmlNode"/>.
	/// </summary>
	public static IXmlNode Empty => EmptyXmlNode.Instance;

	public static IXmlReadOnlyNode EmptyReadOnly => EmptyXmlNode.Instance;

	public static T EmptyNode<T>() => typeof(T).IsAssignableFrom(typeof(IXmlNode)) ? (T)(object)Empty: throw new ArgumentException($"Invalid type {typeof(T).FullName}", nameof(T));

	/// <summary>
	/// Creates a new <see cref="IXmlNodeBuilder{TNode}"/>.
	/// </summary>
	/// <typeparam name="T">Type of XML node</typeparam>
	/// <param name="ignoreCase">If <c>true</c> then element names and attributes are compared ignoring case.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static IXmlNodeBuilder<T> Create<T>(bool ignoreCase) => Create<T>(ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates a new <see cref="IXmlNodeBuilder{TNode}"/>.
	/// </summary>
	/// <typeparam name="T">Type of XML node</typeparam>
	/// <param name="comparer">String comparer to compare elements names and attributies</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static IXmlNodeBuilder<T> Create<T>(StringComparer? comparer = null)
	{
		return
			typeof(T) == typeof(IXmlReadOnlyNode) ? (IXmlNodeBuilder<T>)new XmNodeBuilder<IXmlReadOnlyNode>(CreateNodeRo, comparer):
			typeof(T) == typeof(IXmlNode) ? (IXmlNodeBuilder<T>)new XmNodeBuilder<IXmlNode>(CreateNodeRw, comparer) :
			throw new ArgumentException($"Invalid type {typeof(T).FullName}", nameof(T));
		
		static IXmlReadOnlyNode CreateNodeRo(string name, string? value, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<IXmlReadOnlyNode>? descendants)
			=> new XmlLiteNode(name, value, comparer, attributes, descendants);

		static IXmlNode CreateNodeRw(string name, string? value, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<IXmlNode>? descendants)
			=> new XmlUpdatableNode(name, value, comparer, attributes?.ToList(), descendants?.ToList());
	}

	#region EmptyXmlNode

	private class EmptyXmlNode: IXmlNode
	{
		public static readonly IXmlNode Instance = new EmptyXmlNode();

		private EmptyXmlNode() { }

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

	#endregion
}

[Serializable]
internal class XmNodeBuilder<T>: IXmlNodeBuilder<T> where T: INameValueEmpty
{
	public delegate T NodeConstructor(string name, string? value, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<T>? descendants);

	private List<T>? _nodes;
	private NodeStack? _next;
	private readonly NodeConstructor _constructor;
	private readonly StringComparer _comparer;

	/// <summary>
	/// Creates a new instance of <see cref="XmNodeBuilder{T}" />.
	/// </summary>
	/// <param name="constructor">Node constructor to create a new XML node</param>
	/// <param name="comparer">A <see cref="IEqualityComparer{T}"/> implementation to use when comparing names of elements and attributes.</param>
	public XmNodeBuilder(NodeConstructor constructor, StringComparer? comparer)
	{
		_constructor = constructor;
		_comparer = comparer ?? StringComparer.Ordinal;
	}

	/// <summary>
	/// Writes an attribute <paramref name="name"/> and <paramref name="value"/>.
	/// </summary>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="value">The value of the attribute.</param>
	/// <returns></returns>
	public IXmlNodeBuilder<T> Attrib(string name, string? value)
	{
		if (_next == null) throw new InvalidOperationException();
		if (value != null)
			_next.Attribute(name, value);
		return this;
	}

	/// <summary>
	/// Writes value of an XML element or attribute.
	/// </summary>
	/// <param name="value">The value to write.</param>
	/// <returns></returns>
	public IXmlNodeBuilder<T> Value(string? value)
	{
		if (_next == null) throw new InvalidOperationException();
		if (value != null)
			_next.SetValue(value);
		return this;
	}

	/// <summary>
	/// Writes the specified <paramref name="value"/> and the <paramref name="value"/> properties as an XML element or attribute.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
	public IXmlNodeBuilder<T> Value(object? value, bool elements = false)
	{
		if (value != null)
			Item(value, elements);
		return this;
	}

	/// <summary>
	/// Starts a new XML node with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of the node.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public IXmlNodeBuilder<T> Begin(string name)
	{
		if (name is not { Length: > 0 }) throw new ArgumentNullException(nameof(name));

		if (_next == null)
			_next = new NodeStack(name, _comparer);
		else
			_next.StartElement(name);
		return this;
	}

	/// <summary>
	/// Ends the current XML node.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public IXmlNodeBuilder<T> End()
	{
		if (_next == null) throw new InvalidOperationException();

		_next.EndElement();
		if (_next.IsComplete)
			Flush();
		return this;
	}

	/// <summary>
	/// Adds an XML node with the specified <paramref name="name"/> and the <paramref name="value"/>.
	/// </summary>
	/// <param name="name">Name of the created XML element</param>
	/// <param name="value"></param>
	/// <param name="elements">Indicates whether to write properties of the <paramref name="value"/> as an XML elements or attributes.</param>
	public IXmlNodeBuilder<T> Element(string name, object? value, bool elements = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		Item(name, value, elements, true);
		return this;
	}

	/// <summary>
	/// Adds a collection of sub-nodes to the current node.
	/// </summary>
	/// <param name="nodes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="InvalidOperationException"></exception>
	public IXmlNodeBuilder<T> Elements(IEnumerable<T> nodes)
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
	/// Adds a collection <typeparamref name="T2"/>s with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="items">Collection of items to be added.</param>
	/// <param name="name">Name of the nodes.</param>
	/// <param name="action">Action to be applied to each item.</param>
	/// <typeparam name="T2"></typeparam>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public IXmlNodeBuilder<T> Elements<T2>(IEnumerable<T2> items, string name, Action<IXmlNodeBuilder<T>, T2> action)
	{
		if (items is null) throw new ArgumentNullException(nameof(items));
		if (name == null) throw new ArgumentNullException(nameof(name));
		if (action is null) throw new ArgumentNullException(nameof(action));

		if (_next == null)
		{
			_nodes ??= new List<T>();
			foreach (var item in items)
			{
				_next = new NodeStack(name, _comparer);
				action(this, item);
				_nodes.Add(_next.Convert(_constructor));
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
	/// Returns an actual list of constructed <typeparamref name="T"/> trees.
	/// </summary>
	/// <returns></returns>
	public IReadOnlyList<T> GetNodes()
	{
		Flush();
		return ReadOnly.Wrap(_nodes) ?? ReadOnly.Empty<T>();
	}

	/// <summary>
	/// Completes the building process and returns a list of constructed <typeparamref name="T"/> trees.
	/// </summary>
	/// <returns></returns>
	public List<T> Build()
	{
		Flush();
		List<T> result = _nodes ?? new List<T>();
		_nodes = null;
		return result;
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

		if (enumerable is IEnumerable<T> liteNodes)
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

	private void Element(T node) => _next!.Descendant(node);

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

		if (value is T node)
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
			catch { /* ignore */ }
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
			catch { /* ignore */ }
		}
	}

	private void Flush()
	{
		if (_next == null)
			return;
		(_nodes ??= new List<T>()).Add(_next.Convert(_constructor));
		_next = null;
	}

	private class NodeStack
	{
		private const string EmptyAttributeName = "@";
		private Stack<Node>? _rest;
		private Node _current;
		private readonly StringComparer _comparer;

		public NodeStack(string name, StringComparer comparer)
		{
			_comparer = comparer;
			_current = new Node(name, _comparer);
		}

		public void SetValue(string? value) => _current.SetValue(value);

		public void StartElement(string name)
		{
			if (name is null) throw new ArgumentNullException(nameof(name));

			(_rest ??= new Stack<Node>()).Push(_current);
			var d = new Node(name, _comparer);
			_current.AddChildNode(d);
			_current = d;
		}

		public void Descendant(T node)
		{
			if (node is { IsEmpty: false })
				_current.AddChildNode(node);
		}

		public void EndElement()
		{
			if (_rest != null && _rest.Count != 0)
				_current = _rest.Pop();
			else
				IsComplete = true;
		}

		public bool IsComplete { get; private set; }

		public T Convert(NodeConstructor constructor)
		{
			if (_rest is { Count: > 0 })
			{
				while (_rest.Count > 1)
					_rest.Pop();
				_current = _rest.Pop();
			}
			return _current.Convert(constructor);
		}

		public void Attribute(string name, string? value)
		{
			if (_current == null) throw new InvalidOperationException();
			if (value == null) return;
			_current.SetAttribute(name.TrimToNull() ?? EmptyAttributeName, value);
		}

		private class Node
		{
			private readonly string _name;
			private readonly StringComparer _comparer;
			private List<KeyValuePair<string, string>>? _attrib;
			private List<Node>? _nodeDescendants;
			private List<T>? _xmlDescendants;
			private string? _value;

			public Node(string name, StringComparer comparer)
			{
				_name = name;
				_comparer = comparer;
			}

			public void SetValue(string? value) => _value = value;

			public void SetAttribute(string name, string value) 
			{
				int i;
				if (_attrib == null)
					_attrib = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(name, value) };
				else if ((i = _attrib.FindIndex(o => _comparer.Equals(o.Key, name))) < 0)
					_attrib.Add(new KeyValuePair<string, string>(name, value));
				else
					_attrib[i] = new KeyValuePair<string, string>(name, value);
			}

			public void AddChildNode(Node child)
			{
				if (child is null) throw new ArgumentNullException(nameof(child));
				(_nodeDescendants ??= new List<Node>()).Add(child);
			}

			public void AddChildNode(T child)
			{
				if (child is null) throw new ArgumentNullException(nameof(child));
				if (child.IsEmpty) throw new ArgumentOutOfRangeException(nameof(child), child, null);
				(_xmlDescendants ??= new List<T>()).Add(child);
			}

			public T Convert(NodeConstructor constructor)
			{
				if (_nodeDescendants == null && _xmlDescendants == null)
					return constructor(_name, _value, _comparer, _attrib, null);
				var descendants = _nodeDescendants == null ? new List<T>(): _nodeDescendants.ConvertAll(o => o.Convert(constructor));
				if (_xmlDescendants != null)
					descendants.AddRange(_xmlDescendants);
				return constructor(_name, _value, _comparer, _attrib, descendants);
			}
		}
	}
}