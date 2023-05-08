// Lexxys Infrastructural library.
// file: IXmlConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Configuration;

using Xml;

public interface IXmlConfigurationSource
{
	string Name { get; }
	Uri Location { get; }
	int Version { get; }

	event EventHandler<ConfigurationEventArgs>? Changed;

	IReadOnlyList<XmlLiteNode> Content { get; }
}


