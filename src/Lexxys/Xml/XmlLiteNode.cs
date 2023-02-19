// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#define USE_XML_DOC
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Collections;
#if USE_XML_DOC
using System.Xml.XPath;
#endif

namespace Lexxys.Xml
{
	/// <summary>
	/// Represents readonly node of XML document
	/// </summary>
	public partial class XmlLiteNode: IXmlReadOnlyNode, IEnumerable<XmlLiteNode>, IEquatable<XmlLiteNode>
	{
		private readonly XmlLiteNode[] _elements;
		private readonly KeyValuePair<string, string>[] _attributes;

		/// <summary>
		/// Empty <see cref="XmlLiteNode"/> node.
		/// </summary>
		public static readonly XmlLiteNode Empty = new XmlLiteNode();

		private XmlLiteNode()
		{
			Name = String.Empty;
			Value = String.Empty;
			_elements = Array.Empty<XmlLiteNode>();
			Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
			_attributes = Array.Empty<KeyValuePair<string, string>>();
			Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
			Comparer = StringComparer.Ordinal;
		}

		public XmlLiteNode(string name, string? value, bool ignoreCase, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<XmlLiteNode>? descendants)
			: this(name, value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal, attributes, descendants)
		{
		}

		public XmlLiteNode(string name, string? value, IEqualityComparer<string?>? comparer, IEnumerable<KeyValuePair<string, string>>? attributes, IEnumerable<XmlLiteNode>? descendants)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			Name = name;
			Value = value ?? String.Empty;
			Comparer = comparer ?? StringComparer.Ordinal;

			if (attributes == null)
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
			}
			else if (attributes is ICollection<KeyValuePair<string, string>> coll)
			{
				if (coll.Count == 0)
				{
					_attributes = Array.Empty<KeyValuePair<string, string>>();
				}
				else
				{
					_attributes = new KeyValuePair<string, string>[coll.Count];
					coll.CopyTo(_attributes, 0);
				}
				Attributes = ReadOnly.ValueWrap(_attributes);
			}
			else
			{
				_attributes = attributes.ToArray();
				Attributes = ReadOnly.ValueWrap(_attributes);
			}

			if (descendants == null)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
			}
			else if (descendants is ICollection<XmlLiteNode> coll)
			{
				if (coll.Count == 0)
				{
					_elements = Array.Empty<XmlLiteNode>();
				}
				else
				{
					_elements = new XmlLiteNode[coll.Count];
					coll.CopyTo(_elements, 0);
				}
				Elements = ReadOnly.ValueWrap(_elements);
			}
			else
			{
				_elements = descendants.ToArray();
				Elements = ReadOnly.ValueWrap(_elements);
			}
		}

		private XmlLiteNode(string name, string? value, IEqualityComparer<string?>? comparer, KeyValuePair<string, string>[]? attributes, XmlLiteNode[]? descendants)
		{
			if (name is not { Length: >0 })
				throw new ArgumentNullException(nameof(name));

			Name = name;
			Value = value ?? String.Empty;
			Comparer = comparer ?? StringComparer.Ordinal;
			_attributes = attributes ?? Array.Empty<KeyValuePair<string, string>>();
			Attributes = ReadOnly.ValueWrap(_attributes);
			_elements = descendants ?? Array.Empty<XmlLiteNode>();
			Elements = ReadOnly.ValueWrap(_elements);
		}

		private XmlLiteNode(XmlReader reader, IEqualityComparer<string?>? comparer)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			Comparer = comparer ?? StringComparer.Ordinal;

			while (reader.NodeType != XmlNodeType.Element)
			{
				if (!reader.Read())
				{
					throw new InvalidOperationException("Cannot find element node");
				}
			}

			Name = reader.Name;
			if (reader.MoveToFirstAttribute())
			{
				_attributes = new KeyValuePair<string, string>[reader.AttributeCount];
				int i = 0;
				do
				{
					_attributes[i++] = new KeyValuePair<string, string>(reader.Name, reader.Value);
				} while (reader.MoveToNextAttribute());
				reader.MoveToElement();
				Attributes = ReadOnly.ValueWrap(_attributes);
			}
			else
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
			}

			if (reader.IsEmptyElement)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
				Value = String.Empty;
				return;
			}

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
						desc[n++] = new XmlLiteNode(reader, comparer);
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
			Value = value ?? String.Empty;
			if (desc == null)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
			}
			else
			{
				_elements = new XmlLiteNode[n];
				Elements = ReadOnly.ValueWrap(_elements);
				Array.Copy(desc, _elements, n);
				ArrayPool<XmlLiteNode>.Shared.Return(desc);
			}
		}

		private static string? ConcatValue(string? value, string node)
		{
			int n = TrimRight(node.AsSpan());
			if (n == 0)
				return value;
			if (value == null || value.Length == 0)
				return n >= node.Length ? node: node.Substring(0, n);

			if (!IsCrLf(node[0]) || !IsCrLf(value[value.Length - 1]))
				return value + (n >= node.Length ? node: node.Substring(0, n));
			else
				return value + (n > 1 && (node[0] ^ node[1]) == ('\r' ^ '\n') ? node.Substring(2, n - 2): node.Substring(1, n - 1));

			static bool IsCrLf(char c) => c == '\r' || c == '\n';
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

			static bool IsSpace(char c) => c == ' ' || c == '\t';
			static bool IsCrLf(char c) => c == '\r' || c == '\n';
		}

#if USE_XML_DOC
		public XmlLiteNode(XPathNavigator node, IEqualityComparer<string?>? comparer = null)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			if (node.NodeType == XPathNodeType.Root)
				node.MoveToFirstChild();
			Name = node.Name;
			comparer ??= StringComparer.Ordinal;
			Comparer = comparer;

			if (!node.IsNode)
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
				if (node.NodeType == XPathNodeType.Element ||
					node.NodeType == XPathNodeType.Attribute ||
					node.NodeType == XPathNodeType.SignificantWhitespace ||
					node.NodeType == XPathNodeType.Text)
					Value = node.Value ?? String.Empty;
				else
					Value = String.Empty;
				return;
			}

			if (node.MoveToFirstAttribute())
			{
				var attr = new List<KeyValuePair<string, string>>();
				do
				{
					attr.Add(new KeyValuePair<string, string>(node.Name, node.Value));
				} while (node.MoveToNextAttribute());
				node.MoveToParent();
				_attributes = attr.ToArray();
				Attributes = ReadOnly.ValueWrap(_attributes);
			}
			else
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
			}
			if (!node.HasChildren)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
				Value = String.Empty;
				return;
			}

			string? value = null;
			XmlLiteNode[]? desc = null;
			int n = 0;
			node.MoveToFirstChild();
			do
			{
				switch (node.NodeType)
				{
					case XPathNodeType.Root:
						break;
					case XPathNodeType.Element:
						if (desc == null)
						{
							desc = new XmlLiteNode[4];
							desc[0] = new XmlLiteNode(node, comparer);
							n = 1;
						}
						else if (n < desc.Length)
						{
							desc[n++] = new XmlLiteNode(node, comparer);
						}
						else
						{
							var temp = desc;
							desc = new XmlLiteNode[n * 4];
							Array.Copy(temp, desc, temp.Length);
							desc[n++] = new XmlLiteNode(node, comparer);
						}
						break;
					case XPathNodeType.Attribute:
						break;
					case XPathNodeType.Namespace:
						break;
					case XPathNodeType.Text:
						value = ConcatValue(value, node.Value);
						break;
					case XPathNodeType.SignificantWhitespace:
						break;
					case XPathNodeType.Whitespace:
						break;
					case XPathNodeType.ProcessingInstruction:
						break;
					case XPathNodeType.Comment:
						break;
					case XPathNodeType.All:
						break;
					default:
						break;
				}
			} while (node.MoveToNext());
			node.MoveToParent();

			Value = value ?? String.Empty;
			if (desc == null)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
			}
			else if (desc.Length == n)
			{
				_elements = desc;
				Elements = ReadOnly.ValueWrap(_elements);
			}
			else
			{
				var temp = new XmlLiteNode[n];
				Array.Copy(desc, temp, n);
				_elements = temp;
				Elements = ReadOnly.ValueWrap(_elements);
			}
		}

		public XmlLiteNode(XmlNode node, IEqualityComparer<string?>? comparer = null)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			comparer ??= StringComparer.Ordinal;
			Comparer = comparer;
			Name = node.Name;

			if (node.NodeType != XmlNodeType.Element)
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
				if (node.NodeType == XmlNodeType.Attribute ||
					node.NodeType == XmlNodeType.SignificantWhitespace ||
					node.NodeType == XmlNodeType.Text)
					Value = node.Value ?? String.Empty;
				else
					Value = String.Empty;
				return;
			}

			Value = node.Value ?? String.Empty;

			if (node.Attributes != null && node.Attributes.Count > 0)
			{
				var attr = new KeyValuePair<string, string>[node.Attributes.Count];
				for (int i = 0; i < attr.Length; ++i)
				{
					XmlAttribute a = node.Attributes[i];
					attr[i] = new KeyValuePair<string, string>(a.Name, a.Value);
				}
				_attributes = attr;
				Attributes = ReadOnly.ValueWrap(_attributes);
			}
			else
			{
				_attributes = Array.Empty<KeyValuePair<string, string>>();
				Attributes = ReadOnly.ListValueWrap<KeyValuePair<string, string>>.Empty;
			}

			if (node.ChildNodes.Count == 0)
			{
				_elements = Array.Empty<XmlLiteNode>();
				Elements = ReadOnly.ListValueWrap<XmlLiteNode>.Empty;
			}
			else
			{
				_elements = new XmlLiteNode[node.ChildNodes.Count];
				for (int i = 0; i < _elements.Length; ++i)
				{
					_elements[i] = new XmlLiteNode(node.ChildNodes[i]!, comparer);
				}
				Elements = ReadOnly.ValueWrap(_elements);
			}
		}
#endif

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
		public ReadOnly.ListValueWrap<KeyValuePair<string, string>> Attributes { get; }

		/// <summary>
		/// Contains all subnodes of this node.
		/// </summary>
		public ReadOnly.ListValueWrap<XmlLiteNode> Elements { get; }

		/// <summary>
		/// Get attribute value
		/// </summary>
		/// <param name="name">Name of attribute</param>
		/// <returns>Value of the attribute or null</returns>
		public string? this[string name]
		{
			get
			{
				for (int i = 0; i < _attributes.Length; ++i)
				{
					if (Comparer.Equals(_attributes[i].Key, name))
						return _attributes[i].Value;
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
			for (int i = 0; i < _attributes.Length; ++i)
			{
				if (Comparer.Equals(_attributes[i].Key, name))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Returns all node elements with the specified <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		public IEnumerable<XmlLiteNode> Where(string? name) => _elements.Where(o => Comparer.Equals(o.Name, name));

		/// <summary>
		/// Returns all node elements with the specified <paramref name="name"/>.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		/// <param name="comparer">Equality comparer to compare nodes names</param>
		public IEnumerable<XmlLiteNode> Where(string? name, IEqualityComparer<string?> comparer) => _elements.Where(o => comparer.Equals(o.Name, name));

		/// <summary>
		/// Filters node node elements based on a <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate">A function to test each element of a node</param>
		public IEnumerable<XmlLiteNode> Where(Func<XmlLiteNode, bool> predicate) => _elements.Where(predicate);

		/// <summary>
		/// Returns the first node element with the specified name or <see cref="Empty"/>.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		/// <returns></returns>
		public XmlLiteNode Element(string? name) => FirstOrDefault(name, Comparer) ?? Empty;

		/// <summary>
		/// Returns the first node element with the specified name or <see cref="Empty"/>.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		/// <param name="comparer">Equality comparer to compare nodes names</param>
		/// <returns></returns>
		public XmlLiteNode Element(string? name, IEqualityComparer<string?> comparer) => FirstOrDefault(name, comparer) ?? Empty;

		/// <summary>
		/// Returns the first node element that satisfies a condition or or <see cref="Empty"/>.
		/// </summary>
		/// <param name="predicate">Name of subnode</param>
		/// <returns></returns>
		public XmlLiteNode Element(Func<XmlLiteNode, bool> predicate) => FirstOrDefault(predicate) ?? Empty;

		/// <summary>
		/// Returns the first node element with the specified name or null.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		/// <returns></returns>
		public XmlLiteNode? FirstOrDefault(string? name) => FirstOrDefault(name, Comparer);

		/// <summary>
		/// Returns the first node element with the specified name or null.
		/// </summary>
		/// <param name="name">Name of subnode</param>
		/// <param name="comparer">Equality comparer to compare nodes names</param>
		/// <returns></returns>
		public XmlLiteNode? FirstOrDefault(string? name, IEqualityComparer<string?> comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException(nameof(comparer));

			for (int i = 0; i < _elements.Length; ++i)
			{
				if (comparer.Equals(_elements[i].Name, name))
					return _elements[i];
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
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			for (int i = 0; i < _elements.Length; i++)
			{
				if (predicate(_elements[i]))
					return _elements[i];
			}
			return null;
		}

		/// <summary>
		/// String comparer used when finding a node.
		/// </summary>
		public IEqualityComparer<string?> Comparer { get; }

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
		/// Eveluates the specified <paramref name="action"/> over this <see cref="XmlLiteNode"/> node.
		/// </summary>
		/// <param name="action"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public void Apply(Action<XmlLiteNode> action)
			=> (action ?? throw new ArgumentNullException(nameof(action))).Invoke(this);

		/// <summary>
		/// Eveluates the specified <paramref name="action"/> over this <see cref="XmlLiteNode"/> node.
		/// </summary>
		/// <param name="action"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public T Apply<T>(Func<XmlLiteNode, T> action)
			=> (action ?? throw new ArgumentNullException(nameof(action))).Invoke(this);

		/// <summary>
		/// Convert all subnodes with the specified <paramref name="converter"/> and returns an array of the converted items.
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

		public override string ToString()
		{
			return WriteXml(new StringBuilder(), false, null).ToString();
		}

		public string ToString(bool format, bool innerXml = false)
		{
			return WriteXml(new StringBuilder(), innerXml, format ? "": null).ToString();
		}

		public StringBuilder WriteXml(StringBuilder text, bool innerXml = false, string? prefix = null)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (IsEmpty)
				return text;
			if (innerXml)
			{
				if (_elements.Length == 0)
					return text;
			}
			else
			{
				text.Append(prefix).Append('<').Append(XmlConvert.EncodeName(Name));
				for (int i = 0; i < _attributes.Length; ++i)
				{
					text.Append(' ')
						.Append(XmlConvert.EncodeName(_attributes[i].Key))
						.Append('=');
					XmlTools.EncodeAttribute(text, _attributes[i].Value);
				}
				if (Value.Length > 0)
				{
					text.Append('>');
					if (prefix != null && (Value.Length > 80 || _elements.Length > 0))
					{
						text.AppendLine().Append(prefix + "  ");
					}
					XmlTools.Encode(text, Value);
				}
				else if (_elements.Length > 0)
				{
					text.Append('>');
				}
				else
				{
					text.Append("/>");
					return text;
				}
			}
			var inner = innerXml ? prefix: prefix == null ? null: prefix + "  ";
			for (int i = 0; i < _elements.Length; ++i)
			{
				if (prefix != null)
					text.AppendLine();
				_elements[i].WriteXml(text, false, inner);
			}
			if (!innerXml)
			{
				if (prefix != null && (Value.Length > 80 || _elements.Length > 0))
					text.AppendLine().Append(prefix);
				text.Append("</").Append(XmlConvert.EncodeName(Name)).Append('>');
			}
			return text;
		}

		public XmlReader ReadSubtree() => XmlReader.Create(new StringReader(ToString()));

		#region IReadOnlyNode

		IReadOnlyList<KeyValuePair<string, string>> IXmlReadOnlyNode.Attributes => Attributes;
		IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => Elements;

		IEnumerable<IXmlReadOnlyNode> IXmlReadOnlyNode.Where(string? name) => Where(name);

		IEnumerable<IXmlReadOnlyNode> IXmlReadOnlyNode.Where(string? name, IEqualityComparer<string?> comparer) => Where(name, comparer);
		IEnumerable<IXmlReadOnlyNode> IXmlReadOnlyNode.Where(Func<IXmlReadOnlyNode, bool> predicate) => Where(predicate);

		IXmlReadOnlyNode IXmlReadOnlyNode.Element(string? name) => Element(name);

		IXmlReadOnlyNode IXmlReadOnlyNode.Element(string? name, IEqualityComparer<string?> comparer) => Element(name, comparer);

		IXmlReadOnlyNode IXmlReadOnlyNode.Element(Func<IXmlReadOnlyNode, bool> predicate) => Element(predicate);

		IXmlReadOnlyNode? IXmlReadOnlyNode.FirstOrDefault(string? name) => FirstOrDefault(name);

		IXmlReadOnlyNode? IXmlReadOnlyNode.FirstOrDefault(string? name, IEqualityComparer<string?> comparer) => FirstOrDefault(name, comparer);

		IXmlReadOnlyNode? IXmlReadOnlyNode.FirstOrDefault(Func<IXmlReadOnlyNode, bool> predicate) => FirstOrDefault(predicate);

		public IEnumerator<XmlLiteNode> GetEnumerator()
		{
			return ((IEnumerable<XmlLiteNode>)_elements).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _elements.GetEnumerator();
		}

		IEnumerator<IXmlReadOnlyNode> IEnumerable<IXmlReadOnlyNode>.GetEnumerator()
		{
			return ((IEnumerable<XmlLiteNode>)_elements).GetEnumerator();
		}

		#endregion

		public static IEnumerable<XmlLiteNode> Select(string path, IEnumerable<XmlLiteNode> nodes) => Selector.Select(path, nodes);

		public static IEnumerable<XmlLiteNode> Select(string path, params XmlLiteNode[] nodes) => Selector.Select(path, nodes);

		public static XmlLiteNode? SelectFirst(string path, IEnumerable<XmlLiteNode> nodes) => Selector.Select(path, nodes).FirstOrDefault();

		public static XmlLiteNode? SelectFirst(string path, params XmlLiteNode[] nodes) => Selector.Select(path, nodes).FirstOrDefault();

		public static XmlLiteNode SelectFirstOrEmpty(string path, IEnumerable<XmlLiteNode> nodes) => Selector.Select(path, nodes).FirstOrDefault() ?? Empty;

		public static XmlLiteNode SelectFirstOrEmpty(string path, params XmlLiteNode[] nodes) => Selector.Select(path, nodes).FirstOrDefault() ?? Empty;

		public static Func<IEnumerable<XmlLiteNode>, IEnumerable<XmlLiteNode>> CompileSelect(string path) => Selector.Compile(path);

		public static bool Equals(XmlLiteNode? left, XmlLiteNode? right)
		{
			return left is null ? right is null: !(right is null) &&
				left.Name == right.Name &&
				left.Value == right.Value &&
				left._attributes.Length == right._attributes.Length &&
				left._elements.Length == right._elements.Length &&
				Lexxys.Comparer.Equals(left._attributes, right._attributes, __attribComparer) &&
				Lexxys.Comparer.Equals(left._elements, right._elements, __nodeComparer);
		}
		private static readonly IEqualityComparer<XmlLiteNode> __nodeComparer =
			Lexxys.Comparer.Create((Func<XmlLiteNode, XmlLiteNode, bool>)Equals);
		private static readonly IEqualityComparer<KeyValuePair<string, string>> __attribComparer =
			Lexxys.Comparer.Create<KeyValuePair<string, string>>((x, y) => x.Key == y.Key && x.Value == y.Value, o => HashCode.Join(o.Key.GetHashCode(), o.Value.GetHashCode()));

		public override int GetHashCode()
			=> HashCode.Join(HashCode.Join(HashCode.Join(Name.GetHashCode(), Value.GetHashCode()), _elements), _attributes);

		public bool Equals(XmlLiteNode? other) => Equals(this, other);

		public override bool Equals(object? obj) => obj is XmlLiteNode that && Equals(this, that);

#if USE_XML_DOC
		public static XmlLiteNode FromXml(XPathNavigator node, IEqualityComparer<string?>? comparer = null)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (!node.IsNode)
				throw new ArgumentException("Specified Argument is not a Node", nameof(node));
			return new XmlLiteNode(node, comparer);
		}

		public static XmlLiteNode FromXml(XmlNode node, IEqualityComparer<string?>? comparer = null)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			if (node.NodeType != XmlNodeType.Element)
				throw new ArgumentException("Specified Argument is not an XmlNodeType.Element", nameof(node));
			return new XmlLiteNode(node, comparer);
		}
#endif

		public static List<XmlLiteNode> FromXmlFragment(XmlReader reader, IEqualityComparer<string?>? comparer = null)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			var result = new List<XmlLiteNode>();
			do
			{
				if (reader.NodeType == XmlNodeType.Element)
					result.Add(new XmlLiteNode(reader, comparer));
			} while (reader.Read());
			return result;
		}

		public static List<XmlLiteNode> FromXmlFragment(XmlReader reader, bool ignoreCase = false) => FromXmlFragment(reader, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

		public static List<XmlLiteNode> FromXmlFragment(string value, IEqualityComparer<string?>? comparer = null)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (value.Length == 0)
				return new List<XmlLiteNode>();
			using var reader = XmlReader.Create(new StringReader(value), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
			return FromXmlFragment(reader, comparer);
		}

		public static List<XmlLiteNode> FromXmlFragment(string value, bool ignoreCase) => FromXmlFragment(value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

		public static XmlLiteNode FromXml(XmlReader reader, IEqualityComparer<string?>? comparer = null)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));
			return reader.ReadState == ReadState.EndOfFile ? Empty: new XmlLiteNode(reader, comparer);
		}

		public static XmlLiteNode FromXml(XmlReader reader, bool ignoreCase = false) => FromXml(reader, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

		public static XmlLiteNode FromXml(string value, IEqualityComparer<string?> comparer)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (value.Length == 0)
				return Empty;
			using var reader = XmlReader.Create(new StringReader(value));
			return new XmlLiteNode(reader, comparer);
		}

		public static XmlLiteNode FromXml(string value, bool ignoreCase = false) => FromXml(value, ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);

		public static XmlLiteNode FromJson(string value, string root, bool ignoreCase = false, bool forceAttributes = false)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (value.Length == 0)
				return Empty;
			return JsonToXmlConverter.Convert(value, root, ignoreCase, forceAttributes);
		}
	}
}
