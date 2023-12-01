// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Xml;

public interface IXmlReadOnlyNode
{
	string Name { get; }

	string Value { get; }

	bool IsEmpty { get; }

	StringComparer Comparer { get; }

	string? this[string name] { get; }

	IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

	IReadOnlyList<IXmlReadOnlyNode> Elements { get; }
}
