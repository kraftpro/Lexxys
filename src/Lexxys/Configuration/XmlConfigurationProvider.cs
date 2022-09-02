// Lexxys Infrastructural library.
// file: XmlLiteConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lexxys.Configuration
{
	using Xml;

	public class XmlConfigurationProvider: IConfigProvider
	{
		private const string ConfigurationRoot = "configuration";
		readonly IXmlConfigurationSource _source;
		private XmlLiteNode? _node;

		protected XmlConfigurationProvider(IXmlConfigurationSource source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_source.Changed += OnChanged;
		}

		public string Name => _source.Name;

		public Uri Location => _source.Location;

		public int Version => _source.Version;

		public bool IsEmpty => (GetRootNode() ?? XmlLiteNode.Empty).IsEmpty;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		public virtual object? GetValue(string key, Type objectType)
		{
			if (key == null || key.Length == 0)
				return null;
			if (_source == null)
				return null;
			XmlLiteNode root = GetRootNode();
			if (root == null || root.IsEmpty)
				return null;
			var node = XmlLiteNode.SelectFirst(key, root.Elements);
			if (node == null)
				return null;
			return ParseValue(node, objectType);
		}

		public virtual IReadOnlyList<T> GetList<T>(string key)
		{
			if (key == null || key.Length == 0)
				return new List<T>();
			if (_source == null)
				return new List<T>();
			XmlLiteNode root = GetRootNode();
			if (root == null || root.IsEmpty)
				return new List<T>();
			IEnumerable<XmlLiteNode> nodes = XmlLiteNode.Select(key, root.Elements);
			return ReadOnly.WrapCopy(nodes
				.Select(o => ParseValue(o, typeof(T)))
				.Where(o => o != null)
				.Select(o => (T)o!));
		}

		public static XmlConfigurationProvider? Create(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			IXmlConfigurationSource? source = ConfigurationFactory.TryCreateXmlConfigurationSource(location, parameters);
			return source == null ? null: new XmlConfigurationProvider(source);
		}

		public static Func<string, string?, IReadOnlyList<XmlLiteNode>> GetSourceConverter(string extension, TextToXmlOptionHandler? optionHandler, IReadOnlyCollection<string>? parameters)
		{
			bool ignoreCase = parameters?.FindIndex(o => String.Equals(XmlTools.OptionIgnoreCase, o, StringComparison.OrdinalIgnoreCase)) >= 0;
			bool forceAttib = parameters?.FindIndex(o => String.Equals(XmlTools.OptionForceAttributes, o, StringComparison.OrdinalIgnoreCase)) >= 0;

			var sourceType = extension?.TrimStart('.');
			if (String.Equals(sourceType, "INI", StringComparison.OrdinalIgnoreCase))
				return (content, file) => IniToXmlConverter.ConvertLite(content, ignoreCase);
			if (String.Equals(sourceType, "TXT", StringComparison.OrdinalIgnoreCase) || String.Equals(sourceType, "TEXT", StringComparison.OrdinalIgnoreCase))
				return (content, file) => TextToXmlConverter.ConvertLite(content, optionHandler, file, ignoreCase);
			if (String.Equals(sourceType, "JSON", StringComparison.OrdinalIgnoreCase))
				return (content, file) => JsonToXmlConverter.Convert(content, sourceName: file, ignoreCase: ignoreCase, forceAttributes: forceAttib);
			return (content, file) => XmlLiteNode.FromXmlFragment(content, ignoreCase);
		}


		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			_node = null;
			Changed?.Invoke(sender ?? this, e);
		}

		private XmlLiteNode GetRootNode()
		{
			if (_node == null)
			{
				_node = XmlLiteNode.Empty;
				IReadOnlyList<XmlLiteNode> temp = _source.Content;
				_node = temp == null || temp.Count == 0 ? XmlLiteNode.Empty:
					temp.Count == 1 && temp[0].Comparer.Equals(temp[0].Name, ConfigurationRoot) ? temp[0]:
					new XmlLiteNode(ConfigurationRoot, null, temp[0].Comparer, null, temp);
			}
			return _node;
		}

		private static object? ParseValue(XmlLiteNode node, Type type)
		{
			if (node == null)
				return null;
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			try
			{
				XmlTools.TryGetValue(node, type, out object? result);
				return result;
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch
			{
				//e.Add("Source", _source.Name);
				//throw;
				return null;
			}
			#pragma warning restore CA1031 // Do not catch general exception types
		}
	}
}
