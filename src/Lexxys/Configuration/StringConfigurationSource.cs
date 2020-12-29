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

namespace Lexxys.Configuration
{

	class StringConfigurationSource: IXmlConfigurationSource
	{
		private const string LogSource = "Lexxys.Configuration.StringConfigurationSource";
		private List<string> _includes;
		private IReadOnlyList<XmlLiteNode> _content;
		private readonly Func<string, string, IReadOnlyList<XmlLiteNode>> _converter;
		private readonly ConfigurationLocator _location;

		private StringConfigurationSource(string sourceType, ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			_location = location ?? throw EX.ArgumentNull("location");
			_converter = XmlLiteConfigurationProvider.GetSourceConverter(sourceType, OptionHandler, parameters);
		}

		#region IConfigurationSource

		public string Name
		{
			get { return _location.Host; }
		}

		public bool Initialized
		{
			get { return true; }
		}

		public string LocalDirectory
		{
			get { return null; }
		}

		public string LocalRoot
		{
			get { return null; }
		}

		public IReadOnlyList<XmlLiteNode> Content
		{
			get
			{
				IReadOnlyList<XmlLiteNode> content = _content;
				if (content == null)
				{
					Interlocked.CompareExchange(ref _content, _converter(_location.Text, null), null);
					content = _content;
				}
				return content;
			}
		}

		public ConfigurationLocator Location
		{
			get { return _location; }
		}

		public event EventHandler<ConfigurationEventArgs> Changed;
		#endregion

		public override bool Equals(object obj)
		{
			return obj is StringConfigurationSource x && x._location.Text == _location.Text;
		}

		public override int GetHashCode()
		{
			return _location.Text.GetHashCode();
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			try
			{
				lock (this)
				{
					_content = null;
					OnChanged(new ConfigurationEventArgs());
				}
			}
			catch (Exception exception)
			{
				exception.Add("fileName", e?.FullPath);
				Config.LogConfigurationError(LogSource, exception);
			}
		}

		private void OnChanged(ConfigurationEventArgs e)
		{
			Changed?.Invoke(this, e);
		}

		private IEnumerable<XmlLiteNode> OptionHandler(string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, null));
				return null;
			}
			return LocalFileConfigurationSource.HandleInclude(LogSource, parameters, null, ref _includes, OnFileChanged);
		}

		public static StringConfigurationSource Create(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				return null;
			if (location.SchemaType != ConfigLocatorSchema.String)
				return null;
			return new StringConfigurationSource(location.SourceType, location, parameters);
		}
	}
}


