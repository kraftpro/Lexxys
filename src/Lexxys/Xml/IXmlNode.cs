// Lexxys Infrastructural library.
// file: XmlLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Xml;

public interface IXmlNode: IXmlReadOnlyNode
{
	new string Name { get; set; }

	new string Value { get; set; }

	new string? this[string name] { get; set; }

	new IList<KeyValuePair<string, string>> Attributes { get; }

	new IList<IXmlNode> Elements { get; }

	IXmlReadOnlyNode AsReadOnly();
}
