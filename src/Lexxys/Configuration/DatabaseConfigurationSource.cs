// Lexxys Infrastructural library.
// file: DatabaseConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Data;
using Lexxys.Xml;

#nullable enable

namespace Lexxys.Configuration
{
	class DatabaseConfigurationSource: IXmlConfigurationSource
	{
		private readonly ConfigurationLocator _location;
		private readonly IReadOnlyList<XmlLiteNode> _content;

		public DatabaseConfigurationSource(ConfigurationLocator location)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));
			if (location.SchemaType != ConfigLocatorSchema.Database)
				throw EX.ArgumentOutOfRange("location.SchemaType", location.SchemaType);

			_location = location;
			using var dc = new DataContext();
			_content = ReadOnly.Wrap(dc.Map(NodeMapper, location.QueryString), true);
		}

		private static List<XmlLiteNode> NodeMapper(DbCommand cmd)
		{
			var result = new List<XmlLiteNode>();
			using SqlDataReader reader = ((SqlCommand)cmd).ExecuteReader();
			do
			{
				int width = -1;
				while (reader.Read())
				{
					if (width == -1)
						width = reader.FieldCount;

					for (int i = 0; i < width; ++i)
					{
						if (!reader.IsDBNull(i))
						{
							var node = XmlLiteNode.FromXml(reader.GetXmlReader(i));
							if (!node.IsEmpty)
								result.Add(node);
						}
					}
				}
			} while (reader.NextResult());
			return result;
		}

		#region IXmlConfigurationSource

		public string Name => "Dbatabase";

		public IReadOnlyList<XmlLiteNode> Content => _content;

		public ConfigurationLocator Location => _location;

		public event EventHandler<ConfigurationEventArgs>? Changed
		{
			add { }
			remove { }
		}
		#endregion
	}
}
