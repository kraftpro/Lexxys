// Lexxys Infrastructural library.
// file: DatabaseConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys.Configuration
{
	using Data;
	using Xml;

	class DatabaseConfigurationSource: IXmlConfigurationSource
	{
		private readonly Uri _location;
		private readonly IReadOnlyList<XmlLiteNode> _content;

		public DatabaseConfigurationSource(Uri location)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			if (!location.IsAbsoluteUri || location.Scheme != "database")
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			_location = location;
			_content = Dc.Instance.ReadXml(location.Query);
		}

		#region IXmlConfigurationSource

		public string Name => _location.ToString();

		public Uri Location => _location;

		public IReadOnlyList<XmlLiteNode> Content => _content;

		public event EventHandler<ConfigurationEventArgs>? Changed
		{
			add { }
			remove { }
		}
		#endregion
	}
}
