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

using Lexxys.Xml;

namespace Lexxys.Configuration
{
	[DebuggerDisplay("{_source.Location,nq}")]
	public class XmlLiteConfigurationProvider: IConfigurationProvider
	{
		private const string ConfigurationRoot = "configuration";
		readonly IXmlConfigurationSource _source;
		private XmlLiteNode _node;

		protected XmlLiteConfigurationProvider(IXmlConfigurationSource source)
		{
			_source = source ?? throw EX.ArgumentNull("resource");
			_source.Changed += OnChanged;
		}

		public string Name
		{
			get { return _source.Name; }
		}

		public bool Initialized
		{
			get { return _source.Initialized; }
		}

		public bool IsEmpty
		{
			get { return (GetRootNode() ?? XmlLiteNode.Empty).IsEmpty; }
		}

		public virtual object GetValue(string reference, Type returnType)
		{
			if (reference == null || reference.Length == 0)
				return null;
			if (_source == null || !_source.Initialized)
				return null;
			XmlLiteNode root = GetRootNode();
			if (root == null || root.IsEmpty)
				return null;
			var node = XmlLiteNode.SelectFirst(reference, root.Elements);
			if (node == null)
				return null;
			return ParseValue(node, returnType);
		}

		public virtual List<T> GetList<T>(string reference)
		{
			if (reference == null || reference.Length == 0)
				return null;
			if (_source == null || !_source.Initialized)
				return null;
			XmlLiteNode root = GetRootNode();
			if (root == null || root.IsEmpty)
				return null;
			IEnumerable<XmlLiteNode> nodes = XmlLiteNode.Select(reference, root.Elements);
			return nodes
				.Select(o => ParseValue(o, typeof(T)))
				.Where(o => o != null)
				.Select(o => (T)o).ToList();
		}

		public event EventHandler<ConfigurationEventArgs> Changed;

		public static XmlLiteConfigurationProvider Create(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			IXmlConfigurationSource source = ConfigurationFactory.FindXmlSource(location, parameters);
			return source == null ? null: new XmlLiteConfigurationProvider(source);
		}

		public static Func<string, string, IReadOnlyList<XmlLiteNode>> GetSourceConverter(string sourceType, TextToXmlOptionHandler optionHandler, IReadOnlyCollection<string> parameters)
		{
			bool ignoreCase = parameters?.FindIndex(o => String.Equals(XmlTools.OptionIgnoreCase, o, StringComparison.OrdinalIgnoreCase)) >= 0;
			bool forceAttib = parameters?.FindIndex(o => String.Equals(XmlTools.OptionForceAttributes, o, StringComparison.OrdinalIgnoreCase)) >= 0;

			if (String.Equals(sourceType, "INI", StringComparison.OrdinalIgnoreCase))
				return (content, file) => IniToXmlConverter.ConvertLite(content, ignoreCase);
			if (String.Equals(sourceType, "TXT", StringComparison.OrdinalIgnoreCase) || String.Equals(sourceType, "TEXT", StringComparison.OrdinalIgnoreCase))
				return (content, file) => TextToXmlConverter.ConvertLite(content, optionHandler, file, ignoreCase);
			if (String.Equals(sourceType, "JSON", StringComparison.OrdinalIgnoreCase))
				return (content, file) => JsonToXmlConverter.Convert(content, sourceName: file, ignoreCase: ignoreCase, forceAttributes: forceAttib);
			return (content, file) => XmlLiteNode.FromXmlFragment(content, ignoreCase);
		}


		private void OnChanged(object sender, ConfigurationEventArgs e)
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

		private object ParseValue(XmlLiteNode node, Type type)
		{
			if (node == null)
				return null;
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			try
			{
				XmlTools.TryGetValue(node, type, out object result);
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
}


