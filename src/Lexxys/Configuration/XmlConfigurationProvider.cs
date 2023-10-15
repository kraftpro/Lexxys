// Lexxys Infrastructural library.
// file: XmlLiteConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Configuration;

using Xml;

public class XmlConfigurationProvider: IConfigProvider
{
	private const string ConfigurationRoot = "configuration";
	readonly IXmlConfigurationSource _source;
	private IXmlReadOnlyNode? _node;

	private XmlConfigurationProvider(IXmlConfigurationSource source)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_source.Changed += OnChanged;
	}

	public string Name => _source.Name;

	public Uri Location => _source.Location;

	public int Version => _source.Version;

	public event EventHandler<ConfigurationEventArgs>? Changed;

	public virtual object? GetValue(string key, Type objectType)
	{
		if (string.IsNullOrEmpty(key))
			return null;
		IXmlReadOnlyNode root = GetRootNode();
		if (root.IsEmpty)
			return null;
		var node = XmlNodeSelector.Select(key, root.Elements).FirstOrDefault();
		if (node == null)
			return null;
		return ParseValue(node, objectType);
	}

	public virtual IReadOnlyList<T> GetList<T>(string key)
	{
		if (string.IsNullOrEmpty(key))
			return new List<T>();
		IXmlReadOnlyNode root = GetRootNode();
		if (root.IsEmpty)
			return new List<T>();
		IEnumerable<IXmlReadOnlyNode> nodes = XmlNodeSelector.Select(key, root.Elements);
		return ReadOnly.WrapCopy(nodes
			.Select(o => ParseValue(o, typeof(T)))
			.Where(o => o != null)
			.Select(o => (T)o!))!;
	}

	public static XmlConfigurationProvider? TryCreate(Uri location, IReadOnlyCollection<string>? parameters)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		IXmlConfigurationSource? source = ConfigurationSource.TryCreateXmlConfigurationSource(location, parameters);
		return source == null ? null: new XmlConfigurationProvider(source);
	}

	internal static Func<string, string?, IReadOnlyList<IXmlReadOnlyNode>> GetSourceConverter(string? extension, TextToXmlOptionHandler? optionHandler, IReadOnlyCollection<string>? parameters)
	{
		bool ignoreCase = parameters?.FindIndex(o => String.Equals(XmlTools.OptionIgnoreCase, o, StringComparison.OrdinalIgnoreCase)) >= 0;
		bool forceAttrib = parameters?.FindIndex(o => String.Equals(XmlTools.OptionForceAttributes, o, StringComparison.OrdinalIgnoreCase)) >= 0;

		var sourceType = extension?.TrimStart('.');
		if (String.Equals(sourceType, "INI", StringComparison.OrdinalIgnoreCase))
			return (content, _) => IniToXmlConverter.ConvertLite(content, ignoreCase);
		if (String.Equals(sourceType, "TXT", StringComparison.OrdinalIgnoreCase) || String.Equals(sourceType, "TEXT", StringComparison.OrdinalIgnoreCase))
			return (content, file) => TextToXmlConverter.ConvertLite(content, optionHandler, file, ignoreCase);
		if (String.Equals(sourceType, "JSON", StringComparison.OrdinalIgnoreCase))
			return (content, file) => JsonToXmlConverter.Convert(content, sourceName: file, ignoreCase: ignoreCase, forceAttributes: forceAttrib);
		return (content, _) => XmlFragBuilder.Create<IXmlReadOnlyNode>(ignoreCase).Xml(content).Build();
	}


	private void OnChanged(object? sender, ConfigurationEventArgs e)
	{
		_node = null;
		Changed?.Invoke(sender ?? this, e);
	}

	private IXmlReadOnlyNode GetRootNode()
	{
		if (_node != null)
			return _node;
		_node = XmlFragBuilder.Empty;
		IReadOnlyList<IXmlReadOnlyNode> temp = _source.Content;
		_node = temp is not { Count: >0 } ? XmlFragBuilder.Empty:
			temp.Count == 1 && temp[0].Comparer.Equals(temp[0].Name, ConfigurationRoot) ? temp[0]:
			XmlTools.Wrap(ConfigurationRoot, temp[0].Comparer, null, temp);
		return _node;
	}

	private static object? ParseValue(IXmlReadOnlyNode node, Type type)
	{
		if (node == null) throw new ArgumentNullException(nameof(node));
		if (type == null) throw new ArgumentNullException(nameof(type));
		try
		{
			XmlTools.TryGetValue(node, type, out object? result);
			return result;
		}
		catch
		{
			//e.Add("Source", _source.Name);
			//throw;
			return null;
		}
	}
}
