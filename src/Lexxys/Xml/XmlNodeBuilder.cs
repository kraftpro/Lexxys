// Lexxys Infrastructural library.
// file: XmlLiteBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Reflection;

namespace Lexxys.Xml;

public static partial class XmlNodeBuilder
{
	/// <summary>
	/// Returns an empty <see cref="IXmlNode"/>.
	/// </summary>
	public static IXmlNode Empty => EmptyXmlNode.Instance;

	public static IXmlReadOnlyNode EmptyReadOnly => EmptyXmlNode.Instance;

	public static T EmptyNode<T>() => typeof(T).IsAssignableFrom(typeof(IXmlNode)) ? (T)Empty: throw new ArgumentException($"Invalid type {typeof(T).FullName}", nameof(T));

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
	/// <param name="comparer">String comparer to compare elements names and attributes</param>
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
}
