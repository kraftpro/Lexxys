// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Lexxys.Xml
{
	public interface IXmlReadOnlyNode: IEnumerable<IXmlReadOnlyNode>
	{
		string? this[string name] { get; }

		IEqualityComparer<string?> Comparer { get; }

		bool IsEmpty { get; }
		string Name { get; }
		string Value { get; }

		IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }
		IReadOnlyList<IXmlReadOnlyNode> Elements { get; }

		IEnumerable<IXmlReadOnlyNode> Where(string? name);
		IEnumerable<IXmlReadOnlyNode> Where(string? name, IEqualityComparer<string?> comparer);
		IEnumerable<IXmlReadOnlyNode> Where(Func<IXmlReadOnlyNode, bool> predicate);

		bool HasAttribute(string name);

		IXmlReadOnlyNode Element(string? name);
		IXmlReadOnlyNode Element(string? name, IEqualityComparer<string?> comparer);
		IXmlReadOnlyNode Element(Func<IXmlReadOnlyNode, bool> predicate);

		IXmlReadOnlyNode? FirstOrDefault(string? name);
		IXmlReadOnlyNode? FirstOrDefault(string? name, IEqualityComparer<string?> comparer);
		IXmlReadOnlyNode? FirstOrDefault(Func<IXmlReadOnlyNode, bool> predicate);

		XmlReader ReadSubtree();

		StringBuilder WriteXml(StringBuilder text, bool innerXml, string prefix);
	}

	public interface IXmlNode: IXmlReadOnlyNode
	{
		new string? this[string name] { get;  set; }

		new IList<KeyValuePair<string, string>> Attributes { get; }
		new IList<IXmlNode> Elements { get; }

		new IEnumerable<IXmlNode> Where(string? name);
		new IEnumerable<IXmlNode> Where(string? name, IEqualityComparer<string?> comparer);
		new IEnumerable<IXmlNode> Where(Func<IXmlReadOnlyNode, bool> predicate);

		new IXmlNode Element(string? name);
		new IXmlNode Element(string? name, IEqualityComparer<string?> comparer);
		IXmlNode Element(Func<IXmlNode, bool> predicate);

		new IXmlNode? FirstOrDefault(string? name);
		new IXmlNode? FirstOrDefault(string? name, IEqualityComparer<string?> comparer);
		new IXmlNode? FirstOrDefault(Func<IXmlReadOnlyNode, bool> predicate);
	}
}

