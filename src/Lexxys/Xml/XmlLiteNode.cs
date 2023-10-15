// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#define USE_XML_DOC
using System.Buffers;
using System.Xml;
using System.Collections;
#if USE_XML_DOC
using System.Xml.XPath;
#endif

namespace Lexxys.Xml;

/// <summary>
/// Represents readonly node of XML document
/// </summary>
[Serializable]
public class XmlLiteNode: IXmlReadOnlyNode, IEquatable<XmlLiteNode>
{
	private readonly XmlLiteNode[] _elements;
	private readonly KeyValuePair<string, string>[] _attributes;
	private readonly int _hashCode;

	/// <summary>
	/// Empty <see cref="XmlLiteNode"/> node.
	/// </summary>
	public static readonly XmlLiteNode Empty = new XmlLiteNode();

	private XmlLiteNode()
	{
		Name = String.Empty;
		Value = String.Empty;
		Comparer = StringComparer.Ordinal;
		_elements = [];
		_attributes = [];
		_hashCode = CalcHashCode();
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="name">Name of the node</param>
	/// <param name="value">Value of the node</param>
	/// <param name="ignoreCase">Ignore case when comparing node names</param>
	/// <param name="attributes">Collection of attributes</param>
	/// <param name="descendants">Collection of sub-nodes</param>
	public XmlLiteNode(string name, string? value, bool ignoreCase, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<XmlLiteNode>? descendants)
		: this(name, value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal, attributes, descendants)
	{
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="name">Name of the node</param>
	/// <param name="value">Value of the node</param>
	/// <param name="comparer">Equality comparer to compare names of the nodes and attributes</param>
	/// <param name="attributes">Collection of attributes</param>
	/// <param name="descendants">Collection of sub-nodes</param>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteNode(string name, string? value, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<XmlLiteNode>? descendants)
	{
		if (name is not { Length: >0 }) throw new ArgumentNullException(nameof(name));
		Name = name;
		Value = value ?? String.Empty;
		Comparer = comparer ?? StringComparer.Ordinal;
		_attributes = attributes?.ToArray() ?? [];
		_elements = descendants?.ToArray() ?? [];
		_hashCode = CalcHashCode();
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="name">Name of the node</param>
	/// <param name="value">Value of the node</param>
	/// <param name="ignoreCase">Ignore case when comparing node names</param>
	/// <param name="attributes">Collection of attributes</param>
	/// <param name="descendants">Collection of sub-nodes</param>
	public XmlLiteNode(string name, string? value, bool ignoreCase, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<IXmlReadOnlyNode>? descendants)
		: this(name, value, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, attributes, descendants)
	{
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="name">Name of the node</param>
	/// <param name="value">Value of the node</param>
	/// <param name="comparer">Equality comparer to compare names of the nodes and attributes</param>
	/// <param name="attributes">Collection of attributes</param>
	/// <param name="descendants">Collection of sub-nodes</param>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteNode(string name, string? value, StringComparer? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<IXmlReadOnlyNode>? descendants)
	{
		if (name is not { Length: >0}) throw new ArgumentNullException(nameof(name));
		Name = name;
		Value = value ?? String.Empty;
		Comparer = comparer ?? StringComparer.Ordinal;
		_attributes = attributes?.ToArray() ?? [];
		_elements = descendants?.ToArray().ConvertAll(FromIXmlReadOnlyNode) ?? [];
		_hashCode = CalcHashCode();
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="name">Name of the node</param>
	/// <param name="value">Value of the node</param>
	/// <param name="comparer">Equality comparer to compare names of the nodes and attributes</param>
	/// <param name="attributes">Collection of attributes</param>
	/// <param name="descendants">Collection of sub-nodes</param>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteNode(string name, string? value, StringComparer? comparer, KeyValuePair<string, string>[]? attributes, XmlLiteNode[]? descendants)
	{
		if (name is not { Length: >0 }) throw new ArgumentNullException(nameof(name));

		Name = name;
		Value = value ?? String.Empty;
		Comparer = comparer ?? StringComparer.Ordinal;
		_attributes = attributes ?? [];
		_elements = descendants ?? [];
	}

	/// <summary>
	/// Initializes new instance of <see cref="XmlLiteNode"/> from <see cref="IXmlReadOnlyNode"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlLiteNode(IXmlReadOnlyNode node)
	{
		if (node is null) throw new ArgumentNullException(nameof(node));

		Name = node.Name;
		Value = node.Value;
		Comparer = node.Comparer;
		_attributes = node.Attributes.ToArray();
		_elements = node.Elements.ConvertAll(FromIXmlReadOnlyNode).ToArray();
		_hashCode = CalcHashCode();
	}

	/// <summary>
	/// Converts <see cref="IXmlReadOnlyNode"/> to <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static XmlLiteNode FromIXmlReadOnlyNode(IXmlReadOnlyNode node)
	{
		if (node is null) throw new ArgumentNullException(nameof(node));
		return node switch
		{
			XmlLiteNode xmlLiteNode => xmlLiteNode,
			_ => new XmlLiteNode(node)
		};
	}

	private int CalcHashCode()
		=> HashCode.Join(HashCode.Join(HashCode.Join(Name.GetHashCode(), Value.GetHashCode()), _elements), _attributes);
	
	private static string? ConcatValue(string? value, string node)
	{
		int n = TrimRight(node.AsSpan());
		if (n == 0)
			return value;
		if (value is not { Length: >0 })
			return n >= node.Length ? node: node.Substring(0, n);

		if (!IsCrLf(node[0]) || !IsCrLf(value[value.Length - 1]))
			return value + (n >= node.Length ? node: node.Substring(0, n));
		else
			return value + (n > 1 && (node[0] ^ node[1]) == ('\r' ^ '\n') ? node.Substring(2, n - 2): node.Substring(1, n - 1));

		static bool IsCrLf(char c) => c is '\r' or '\n';
	}

	private static int TrimRight(ReadOnlySpan<char> value)
	{
		int i = value.Length;
		do
		{
			if (--i < 0)
				return 0;
		} while (IsSpace(value[i]));

		if (IsCrLf(value[i]) && --i > 0)
			if ((value[i] ^ value[i - 1]) == ('\r' ^ '\n'))
				--i;
		return i + 1;

		static bool IsSpace(char c) => c is ' ' or '\t';
		static bool IsCrLf(char c) => c is '\r' or '\n';
	}

	/// <summary>
	/// Gets the name of the node
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the value of the node
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// Gets list of the attributes of the node.
	/// </summary>
	public IReadOnlyList<KeyValuePair<string, string>> Attributes => _attributes;

	/// <summary>
	/// Contains all sub-nodes of this node.
	/// </summary>
	public IReadOnlyList<XmlLiteNode> Elements => _elements;

	IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => _elements;

	/// <summary>
	/// Get attribute value
	/// </summary>
	/// <param name="name">Name of attribute</param>
	/// <returns>Value of the attribute or null</returns>
	public string? this[string name]
	{
		get
		{
			foreach (KeyValuePair<string, string> a in _attributes)
			{
				if (Comparer.Equals(a.Key, name))
					return a.Value;
			}
			return null;
		}
	}

	/// <summary>
	/// Tests if the node is empty/
	/// </summary>
	public bool IsEmpty => Object.ReferenceEquals(this, Empty);

	/// <summary>
	/// Check if the attribute exists
	/// </summary>
	/// <param name="name">Name of the attribute</param>
	/// <returns></returns>
	public bool HasAttribute(string name)
	{
		foreach (var a in _attributes)
		{
			if (Comparer.Equals(a.Key, name))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Returns all node elements with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	public IEnumerable<XmlLiteNode> Where(string? name) => _elements.Where(o => Comparer.Equals(o.Name, name));

	/// <summary>
	/// Returns all node elements with the specified <paramref name="name"/>.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	/// <param name="comparer">Equality comparer to compare nodes names</param>
	public IEnumerable<XmlLiteNode> Where(string? name, StringComparer comparer) => _elements.Where(o => comparer.Equals(o.Name, name));

	/// <summary>
	/// Filters node node elements based on a <paramref name="predicate"/>.
	/// </summary>
	/// <param name="predicate">A function to test each element of a node</param>
	public IEnumerable<XmlLiteNode> Where(Func<XmlLiteNode, bool> predicate) => _elements.Where(predicate);

	/// <summary>
	/// Returns the first node element with the specified name or <see cref="Empty"/>.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	/// <returns></returns>
	public XmlLiteNode Element(string? name) => FirstOrDefault(name, Comparer) ?? Empty;

	/// <summary>
	/// Returns the first node element with the specified name or <see cref="Empty"/>.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	/// <param name="comparer">Equality comparer to compare nodes names</param>
	/// <returns></returns>
	public XmlLiteNode Element(string? name, StringComparer comparer) => FirstOrDefault(name, comparer) ?? Empty;

	/// <summary>
	/// Returns the first node element that satisfies a condition or or <see cref="Empty"/>.
	/// </summary>
	/// <param name="predicate">Name of sub-node</param>
	/// <returns></returns>
	public XmlLiteNode Element(Func<XmlLiteNode, bool> predicate) => FirstOrDefault(predicate) ?? Empty;

	/// <summary>
	/// Returns the first node element with the specified name or null.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	/// <returns></returns>
	public XmlLiteNode? FirstOrDefault(string? name) => FirstOrDefault(name, Comparer);

	/// <summary>
	/// Returns the first node element with the specified name or null.
	/// </summary>
	/// <param name="name">Name of sub-node</param>
	/// <param name="comparer">Equality comparer to compare nodes names</param>
	/// <returns></returns>
	public XmlLiteNode? FirstOrDefault(string? name, StringComparer comparer)
	{
		if (comparer == null) throw new ArgumentNullException(nameof(comparer));

		foreach (var item in _elements)
		{
			if (comparer.Equals(item.Name, name))
				return item;
		}
		return null;
	}

	/// <summary>
	/// Returns the first node element that satisfies a condition or null.
	/// </summary>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns></returns>
	public XmlLiteNode? FirstOrDefault(Func<XmlLiteNode, bool> predicate)
	{
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		foreach (var item in _elements)
		{
			if (predicate(item))
				return item;
		}
		return null;
	}

	/// <summary>
	/// String comparer used when finding a node.
	/// </summary>
	public StringComparer Comparer { get; }

	/// <summary>
	/// Convert this node to the specified value type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <exception cref="FormatException"></exception>
	/// <returns></returns>
	public T AsValue<T>() => XmlTools.GetValue<T>(this);

	/// <summary>
	/// Tries to convert this node to the specified value type.  Returns <paramref name="defaultValue"/> when the conversion fault.
	/// </summary>
	/// <param name="defaultValue">Default value</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T AsValue<T>(T defaultValue) => XmlTools.GetValue(this, defaultValue);

	/// <summary>
	/// Convert this node to the specified value type.
	/// </summary>
	/// <param name="returnType"></param>
	/// <exception cref="FormatException"></exception>
	/// <returns></returns>
	public object? AsObject(Type returnType) => XmlTools.GetValue(this, returnType);

	/// <summary>
	/// Convert all sub-nodes with the specified <paramref name="converter"/> and returns an array of the converted items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="converter"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public T[] ConvertElements<T>(Func<XmlLiteNode, T> converter)
	{
		if (converter == null)
			throw new ArgumentNullException(nameof(converter));

		var result = new T[_elements.Length];
		for (int i = 0; i < _elements.Length; ++i)
		{
			result[i] = converter(_elements[i]);
		}
		return result;
	}

	/// <summary>
	/// Returns an XML text representation of this node.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => this.WriteXml().ToString();

	/// <summary>
	/// Returns an XML text representation of this node.
	/// </summary>
	/// <param name="format">True to format the output using indentation, otherwise false</param>
	/// <param name="innerXml">True to return only inner XML of the node, otherwise false</param>
	/// <returns></returns>
	public string ToString(bool format, bool innerXml = false) => this.WriteXml(null, innerXml, format).ToString();

	/// <summary>
	/// Compares two <see cref="XmlLiteNode"/> objects for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool Equals(XmlLiteNode? left, XmlLiteNode? right)
	{
		return left is null ? right is null: !(right is null) &&
			left.Name == right.Name &&
			left.Value == right.Value &&
#if NET6_0_OR_GREATER
			((ReadOnlySpan<KeyValuePair<string, string>>)left._attributes).SequenceEqual((ReadOnlySpan<KeyValuePair<string, string>>)right._attributes) &&
			((ReadOnlySpan<XmlLiteNode>)left._elements).SequenceEqual((ReadOnlySpan<XmlLiteNode>)right._elements);
#else
			((IStructuralEquatable)left._attributes).Equals(right._attributes, StructuralComparisons.StructuralEqualityComparer) &&
			((IStructuralEquatable)left._elements).Equals(right._elements, StructuralComparisons.StructuralEqualityComparer);
#endif
	}

	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() => _hashCode;

	/// <summary>
	/// Determines whether the specified <see cref="XmlLiteNode"/> is equal to the current <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(XmlLiteNode? other) => Equals(this, other);

	/// <summary>
	/// Determines whether the specified <see cref="object"/> is equal to the current <see cref="XmlLiteNode"/>.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object? obj) => obj is XmlLiteNode that && Equals(this, that);

	/// <summary>
	/// Creates a list of <see cref="IXmlReadOnlyNode"/> from <see cref="XmlReader"/>.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static List<IXmlReadOnlyNode> FromXmlFragment(XmlReader reader, StringComparer? comparer = null)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		var result = new List<IXmlReadOnlyNode>();
		do
		{
			if (reader.NodeType == XmlNodeType.Element)
				result.Add(FromXmlReader(reader, comparer));
		} while (reader.Read());
		return result;
	}

	/// <summary>
	/// Creates a list of <see cref="IXmlReadOnlyNode"/> from <see cref="XmlReader"/>.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static List<IXmlReadOnlyNode> FromXmlFragment(XmlReader reader, bool ignoreCase = false) => FromXmlFragment(reader, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates a list of <see cref="IXmlReadOnlyNode"/> from the specified XML fragment string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static List<IXmlReadOnlyNode> FromXmlFragment(string value, StringComparer? comparer = null)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length == 0)
			return new List<IXmlReadOnlyNode>();
		using var reader = XmlReader.Create(new StringReader(value), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
		return FromXmlFragment(reader, comparer);
	}

	/// <summary>
	/// Creates a list of <see cref="IXmlReadOnlyNode"/> from the specified XML fragment string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static List<IXmlReadOnlyNode> FromXmlFragment(string value, bool ignoreCase) => FromXmlFragment(value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XmlReader"/>.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromXml(XmlReader reader, StringComparer? comparer = null) => FromXmlReader(reader, comparer);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XmlReader"/>.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static IXmlReadOnlyNode FromXml(XmlReader reader, bool ignoreCase = false) => FromXmlReader(reader, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from the specified XML string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromXml(string value, StringComparer? comparer)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length == 0)
			return Empty;
		using var reader = XmlReader.Create(new StringReader(value));
		return FromXmlReader(reader, comparer);
	}

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from the specified XML string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static IXmlReadOnlyNode FromXml(string value, bool ignoreCase = false) => FromXml(value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from the specified JSON string.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="root"></param>
	/// <param name="ignoreCase"></param>
	/// <param name="forceAttributes"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromJson(string value, string root, bool ignoreCase = false, bool forceAttributes = false)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length == 0)
			return Empty;
		return JsonToXmlConverter.Convert(value, root, ignoreCase, forceAttributes);
	}

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XmlReader"/>.
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	public static IXmlReadOnlyNode FromXmlReader(XmlReader reader, StringComparer? comparer = null) => FromXmlReaderInternal(reader, comparer);

	private static XmlLiteNode FromXmlReaderInternal(XmlReader reader, StringComparer? comparer)
	{
		if (reader is null) throw new ArgumentNullException(nameof(reader));
		if (reader.ReadState == ReadState.EndOfFile) return Empty;

		while (reader.NodeType != XmlNodeType.Element)
		{
			if (!reader.Read()) return Empty;
		}

		string name = reader.Name;
		KeyValuePair<string, string>[]? attributes = null;
		if (reader.MoveToFirstAttribute())
		{
			attributes = new KeyValuePair<string, string>[reader.AttributeCount];
			int i = 0;
			do
			{
				attributes[i++] = new KeyValuePair<string, string>(reader.Name, reader.Value);
			} while (reader.MoveToNextAttribute());
			reader.MoveToElement();
		}

		if (reader.IsEmptyElement)
			return new XmlLiteNode(name, null, comparer, attributes, null);

		string? value = null;
		XmlLiteNode[]? desc = null;
		int n = 0;
		while (reader.Read())
		{
			switch (reader.NodeType)
			{
				case XmlNodeType.Element:
					if (desc == null)
					{
						desc = ArrayPool<XmlLiteNode>.Shared.Rent(16);
					}
					else if (n >= desc.Length)
					{
						var temp = desc;
						desc = ArrayPool<XmlLiteNode>.Shared.Rent(n * 4);
						Array.Copy(temp, desc, temp.Length);
						ArrayPool<XmlLiteNode>.Shared.Return(temp);
					}
					desc[n++] = FromXmlReaderInternal(reader, comparer);
					break;

				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					value = ConcatValue(value, reader.Value);
					break;

				case XmlNodeType.EndElement:
					goto done;
			}
		}

		done:
		if (desc == null)
			return new XmlLiteNode(name, value, comparer, attributes, null);

		var elements = new XmlLiteNode[n];
		Array.Copy(desc, elements, n);
		ArrayPool<XmlLiteNode>.Shared.Return(desc);
		
		return new XmlLiteNode(name, value, comparer, attributes, elements);
	}

#if USE_XML_DOC

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XPathNavigator"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromXml(XPathNavigator node, StringComparer? comparer = null) => FromXPathNavigator(node, comparer);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XPathNavigator"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static IXmlReadOnlyNode FromXml(XPathNavigator node, bool ignoreCase = false) => FromXPathNavigator(node, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates <see cref="XmlLiteNode"/> from <see cref="XPathNavigator"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static XmlLiteNode FromXPathNavigator(XPathNavigator node, StringComparer? comparer = null)
	{
		if (node == null) throw new ArgumentNullException(nameof(node));

		if (node.NodeType == XPathNodeType.Root)
			node.MoveToFirstChild();

		static string NodeValue(XPathNavigator node) => node.NodeType is XPathNodeType.Element or XPathNodeType.Attribute or XPathNodeType.SignificantWhitespace or XPathNodeType.Text ? node.Value: String.Empty;
		
		if (!node.IsNode)
			return new XmlLiteNode(node.Name, NodeValue(node), comparer, null, null);

		KeyValuePair<string, string>[]? attributes = null;
		if (node.MoveToFirstAttribute())
		{
			var attr = new List<KeyValuePair<string, string>>();
			do
			{
				attr.Add(new KeyValuePair<string, string>(node.Name, node.Value));
			} while (node.MoveToNextAttribute());
			node.MoveToParent();
			attributes = attr.ToArray();
		}
		if (!node.HasChildren)
			return new XmlLiteNode(node.Name, NodeValue(node), comparer, attributes, null);

		string? value = null;
		XmlLiteNode[]? desc = null;
		int n = 0;
		node.MoveToFirstChild();
		do
		{
			switch (node.NodeType)
			{
				case XPathNodeType.Element:
					if (desc == null)
					{
						desc = ArrayPool<XmlLiteNode>.Shared.Rent(16);
					}
					else if (n >= desc.Length)
					{
						var temp = desc;
						desc = ArrayPool<XmlLiteNode>.Shared.Rent(n * 4);
						Array.Copy(temp, desc, temp.Length);
						ArrayPool<XmlLiteNode>.Shared.Return(temp);
					}
					desc[n++] = FromXPathNavigator(node, comparer);
					break;
				case XPathNodeType.Text:
					value = ConcatValue(value, node.Value);
					break;
				case XPathNodeType.Root:
				case XPathNodeType.Attribute:
				case XPathNodeType.Namespace:
				case XPathNodeType.SignificantWhitespace:
				case XPathNodeType.Whitespace:
				case XPathNodeType.ProcessingInstruction:
				case XPathNodeType.Comment:
				case XPathNodeType.All:
				default:
					break;
			}
		} while (node.MoveToNext());
		node.MoveToParent();

		if (desc is null)
			return new XmlLiteNode(node.Name, value, comparer, attributes, null);

		XmlLiteNode[] elements = new XmlLiteNode[n];
		Array.Copy(desc, elements, n);
		ArrayPool<XmlLiteNode>.Shared.Return(desc);
		return new XmlLiteNode(node.Name, value, comparer, attributes, elements);
	}
    
	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XmlNode"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IXmlReadOnlyNode FromXml(XmlNode node, StringComparer? comparer = null) => FromXmlNode(node, comparer);

	/// <summary>
	/// Creates a new <see cref="IXmlReadOnlyNode"/> from <see cref="XmlNode"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="ignoreCase"></param>
	/// <returns></returns>
	public static IXmlReadOnlyNode FromXml(XmlNode node, bool ignoreCase = false) => FromXmlNode(node, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

	/// <summary>
	/// Creates <see cref="XmlLiteNode"/> from <see cref="XmlNode"/>.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static XmlLiteNode FromXmlNode(XmlNode node, StringComparer? comparer = null)
	{
		if (node == null)
			throw new ArgumentNullException(nameof(node));

		static string? ElementValue(XmlNode node) => node.NodeType is XmlNodeType.Attribute or XmlNodeType.SignificantWhitespace or XmlNodeType.Text ? node.Value: String.Empty;
		
		if (node.NodeType != XmlNodeType.Element)
			return new XmlLiteNode(node.Name, ElementValue(node), comparer, null, null);

		KeyValuePair<string, string>[]? attributes = null;
		if (node.Attributes is { Count: > 0 })
		{
			attributes = new KeyValuePair<string, string>[node.Attributes.Count];
			for (int i = 0; i < attributes.Length; ++i)
			{
				XmlAttribute a = node.Attributes[i];
				attributes[i] = new KeyValuePair<string, string>(a.Name, a.Value);
			}
		}

		if (node.ChildNodes.Count == 0)
			return new XmlLiteNode(node.Name, node.Value, comparer, attributes, null);

		var elements = new XmlLiteNode[node.ChildNodes.Count];
		for (int i = 0; i < elements.Length; ++i)
		{
			elements[i] = FromXmlNode(node.ChildNodes[i]!, comparer);
		}
		return new XmlLiteNode(node.Name, node.Value, comparer, attributes, elements);
	}
	
#endif

}
