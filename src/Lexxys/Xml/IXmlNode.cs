// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Xml;

namespace Lexxys.Xml;

public interface INameValueEmpty
{
	string Name { get; }
	string Value { get; }
	bool IsEmpty { get; }
}

public interface IXmlReadOnlyNode: INameValueEmpty
{
	StringComparer Comparer { get; }

	string? this[string name] { get; }

	IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

	IReadOnlyList<IXmlReadOnlyNode> Elements { get; }
}

public interface IXmlNode: IXmlReadOnlyNode
{
	new string? this[string name] { get; set; }

	new string Name { get; set; }

	new string Value { get; set; }

	new IList<KeyValuePair<string, string>> Attributes { get; }

	new IList<IXmlNode> Elements { get; }

	IXmlReadOnlyNode AsReadOnly();
}
