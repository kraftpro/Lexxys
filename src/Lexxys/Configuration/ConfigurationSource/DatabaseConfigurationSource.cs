// Lexxys Infrastructural library.
// file: DatabaseConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Configuration;

using Data;
using Xml;

class DatabaseConfigurationSource: IXmlConfigurationSource
{
	private readonly Uri _location;

	public DatabaseConfigurationSource(Uri location)
	{
		if (location == null)
			throw new ArgumentNullException(nameof(location));
		if (!location.IsAbsoluteUri || location.Scheme != "database" || location.Query.Length < 2)
			throw new ArgumentOutOfRangeException(nameof(location), location, null);

#if NETFRAMEWORK
		var query = System.Net.WebUtility.UrlDecode(location.Query.StartsWith("?", StringComparison.Ordinal) ? location.Query.Substring(1): location.Query);
#else
		var query = System.Web.HttpUtility.UrlDecode(location.Query.StartsWith("?", StringComparison.Ordinal) ? location.Query.Substring(1): location.Query);
#endif
		_location = location;
		Content = Dc.Instance.ReadXml(query);
	}

	#region IXmlConfigurationSource

	public string Name => _location.ToString();

	public Uri Location => _location;

	public int Version => 1;

	public IReadOnlyList<XmlLiteNode> Content { get; }

	public event EventHandler<ConfigurationEventArgs>? Changed
	{
		add { }
		remove { }
	}
	#endregion

	public static DatabaseConfigurationSource? TryCreate(Uri? location, IReadOnlyCollection<string>? _)
	{
		if (location == null || !location.IsAbsoluteUri || location.Scheme != "database")
			return null;
		return new DatabaseConfigurationSource(location);
	}
}
