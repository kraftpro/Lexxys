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
		private readonly ConfigurationLocator _location;

		private StringConfigurationSource(string sourceType, ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			_location = location ?? throw new ArgumentNullException(nameof(location));
			_converter = XmlConfigurationProvider.GetSourceConverter(sourceType, OptionHandler, parameters);
		}

		#region IConfigurationSource

		public string Name => _location.Host;

		public IReadOnlyList<XmlLiteNode> Content
		{
			get
			{
				IReadOnlyList<XmlLiteNode>? content = _content;
				if (content == null)
				{
					Interlocked.CompareExchange(ref _content, _converter(_location.Text, null), null);
					content = _content;
				}
				return content;
			}
		}

		public ConfigurationLocator Location => _location;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		#endregion

		public override bool Equals(object? obj)
		{
			return obj is StringConfigurationSource x && x._location.Text == _location.Text;
		}

		public override int GetHashCode()
		{
			return _location.Text.GetHashCode();
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

		public static StringConfigurationSource? Create(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				return null;
			if (location.SchemaType != ConfigLocatorSchema.String)
				return null;
			return new StringConfigurationSource(location.SourceType, location, parameters);
		}
	}
}
