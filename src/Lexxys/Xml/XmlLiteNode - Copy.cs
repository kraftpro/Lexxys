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
	private readonly IXmlReadOnlyNode[] _elements;
	private readonly KeyValuePair<string, string>[] _attributes;
	private int _hashCode;

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
		_elements = descendants?.ToArray() ?? [];
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

	private static string? ConcatValue(string? value, string node)
	{
		var nd = node.AsSpan();
		int n = TrimRight(nd);
		if (n == 0)
			return value;
		if (value is not { Length: >0 })
			return n >= nd.Length ? node: node.Substring(0, n);

		var vl = value.AsSpan();
		if (!IsCrLf(nd[0]) || !IsCrLf(vl[^1]))
#if NET6_0_OR_GREATER
			return String.Concat(vl, n >= nd.Length ? nd: nd.Slice(0, n));
#else
			return String.Concat(value, n >= nd.Length ? node: node.Substring(0, n));
#endif
		else
#if NET6_0_OR_GREATER
			return String.Concat(vl, n > 1 && (nd[0] ^ nd[1]) == ('\r' ^ '\n') ? nd.Slice(2, n - 2): nd.Slice(1, n - 1));
#else
			return String.Concat(value, n > 1 && (nd[0] ^ nd[1]) == ('\r' ^ '\n') ? node.Substring(2, n - 2): node.Substring(1, n - 1));
#endif

		static bool IsCrLf(char c) => c is '\r' or '\n';

		static int TrimRight(ReadOnlySpan<char> value)
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
	/// Tests if the node is empty/
	/// </summary>
	public bool IsEmpty => Object.ReferenceEquals(this, Empty);

	/// <summary>
	/// String comparer used when finding a node.
	/// </summary>
	public StringComparer Comparer { get; }

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
	/// Gets list of the attributes of the node.
	/// </summary>
	public IReadOnlyList<KeyValuePair<string, string>> Attributes => _attributes;

	/// <summary>
	/// Contains all sub-nodes of this node.
	/// </summary>
	IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => _elements;

	/// <summary>
	/// Returns an XML text representation of this node.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => this.WriteXml().ToString();

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
			left._attributes.AsSpan().SequenceEqual(right._attributes) &&
			left._elements.AsSpan().SequenceEqual(right._elements);
#else
			((IStructuralEquatable)left._attributes).Equals(right._attributes, StructuralComparisons.StructuralEqualityComparer) &&
			((IStructuralEquatable)left._elements).Equals(right._elements, StructuralComparisons.StructuralEqualityComparer);
#endif
	}

	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() => _hashCode == 0 ? (_hashCode = CalcHashCode()): _hashCode;

	private int CalcHashCode()
		=> HashCode.Join(HashCode.Join(HashCode.Join(Name.GetHashCode(), Value.GetHashCode()), _elements), _attributes);

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
