// Lexxys Infrastructural library.
// file: IXmlConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lexxys.Xml;

namespace Lexxys.Configuration
{
	public interface ITrackedConfiguration
	{
		event EventHandler<ConfigurationEventArgs> Changed;
	}

	public interface IXmlConfigurationSource: ITrackedConfiguration
	{
		string Name { get; }
		bool Initialized { get; }
		string LocalDirectory { get; }
		string LocalRoot { get; }
		IReadOnlyList<XmlLiteNode> Content { get; }
		ConfigurationLocator Location { get; }
	}
}


