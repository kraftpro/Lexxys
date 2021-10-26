// Lexxys Infrastructural library.
// file: StringConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lexxys.Xml;

#nullable enable

namespace Lexxys.Configuration
{

	class StringConfigurationSource: IXmlConfigurationSource
	{
		private const string LogSource = "Lexxys.Configuration.StringConfigurationSource";
		private List<string>? _includes;
		private IReadOnlyList<XmlLiteNode>? _content;
		private readonly Func<string, string?, IReadOnlyList<XmlLiteNode>> _converter;
		private readonly string _type;
		private readonly string _text;

		private StringConfigurationSource(Uri location, IReadOnlyCollection<string> parameters)
		{
			_type = location.LocalPath.Trim('/', '[', ']');
			Name = location.Host;
			Location = location;
			_text = Uri.UnescapeDataString(location.Query.Substring(1) + location.Fragment);
			_converter = XmlConfigurationProvider.GetSourceConverter(_type, OptionHandler, parameters);
		}

		#region IConfigurationSource

		public string Name { get; }

		public Uri Location { get; }

		public IReadOnlyList<XmlLiteNode> Content
		{
			get
			{
				IReadOnlyList<XmlLiteNode>? content = _content;
				if (content == null)
				{
					Interlocked.CompareExchange(ref _content, _converter(_text, null), null);
					content = _content;
				}
				return content;
			}
		}

		public event EventHandler<ConfigurationEventArgs>? Changed;

		#endregion

		public override bool Equals(object? obj)
		{
			return obj is StringConfigurationSource x && x._type == _type && x._text == _text;
		}

		public override int GetHashCode()
		{
			return HashCode.Join(_type.GetHashCode(), _text.GetHashCode());
		}

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			try
			{
				lock (this)
				{
					_content = null;
					Changed?.Invoke(sender ?? this, e);
				}
			}
			catch (Exception flaw)
			{
				Config.LogConfigurationError(LogSource, flaw);
			}
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, null));
				return null;
			}
			return ConfigurationSource.HandleInclude(LogSource, parameters, null, ref _includes, OnChanged);
		}

		// string://name/txt?configuration_text
		public static StringConfigurationSource? Create(Uri location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				return null;
			if (location.Scheme != "string")
				return null;
			if (location.Query.Length <= 1)
				return null;
			return new StringConfigurationSource(location, parameters);
		}
	}
}
