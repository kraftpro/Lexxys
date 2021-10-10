// Lexxys Infrastructural library.
// file: LocalFileConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Lexxys;
using Lexxys.Xml;

#nullable enable

namespace Lexxys.Configuration
{

	class LocalFileConfigurationSource: IXmlConfigurationSource
	{
		private const string LogSource = "Lexxys.Configuration.LocalFileConfigurationSource";
		private readonly FileInfo _file;
		private readonly Func<string, string, IReadOnlyList<XmlLiteNode>> _converter;
		private List<string>? _includes;
		private IReadOnlyList<XmlLiteNode>? _content;
		private readonly ConfigurationLocator _location;

		private LocalFileConfigurationSource(FileInfo file, string sourceType, ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			if (!file.Exists)
				throw new ArgumentOutOfRangeException(nameof(file), file, null);
			_file = file;
			_converter = ConfigurationProvider.GetSourceConverter(sourceType, OptionHandler, parameters);
			_location = location;
		}

		#region IConfigurationSource

		public string Name => _file.FullName;

		public IReadOnlyList<XmlLiteNode> Content
		{
			get
			{
				if (_content == null)
				{
					lock (this)
					{
						if (_content == null)
						{
							_content = _converter(File.ReadAllText(_file.FullName), _file.FullName);
							FileWatcher.AddFileWatcher(_file.FullName, OnFileChanged);
						}
					}
				}
				return _content;
			}
		}

		public ConfigurationLocator Location => _location;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		#endregion

		public FileInfo FileInfo => _file;

		public override bool Equals(object? obj)
		{
			if (obj is not LocalFileConfigurationSource x)
				return false;
			if (_file == null)
				return x._file == null;
			return _file.FullName == x._file.FullName;
		}

		public override int GetHashCode()
		{
			return _file == null ? 0: _file.FullName.GetHashCode();
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			try
			{
				if (_content == null)
					return;
				lock (this)
				{
					FileWatcher.RemoveFileWatcher(_file.FullName, OnFileChanged);
					_content = null;
					OnChanged(new ConfigurationEventArgs());
				}
			}
			catch (Exception exception)
			{
				exception.Add("fileName", _file == null ? "(null)": _file.FullName);
				Config.LogConfigurationError(LogSource, exception);
			}
		}

		private void OnChanged(ConfigurationEventArgs e)
		{
			Changed?.Invoke(this, e);
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, _file.FullName));
				return null;
			}
			return HandleInclude(LogSource, parameters, _file.DirectoryName, ref _includes, OnFileChanged);
		}

		public static IEnumerable<XmlLiteNode>? HandleInclude(string logSource, IReadOnlyCollection<string> parameters, string? directory, ref List<string>? includes, FileSystemEventHandler eventHandler)
		{
			var include = parameters?.FirstOrDefault();
			if (String.IsNullOrEmpty(include))
			{
				Config.LogConfigurationEvent(logSource, SR.OptionIncludeFileNotFound(null, directory));
				return null;
			}
			var cl = new ConfigurationLocator(include).Locate(String.IsNullOrEmpty(directory) ? null: new[] { directory }, null);
			if (!cl.IsLocated)
			{
				Config.LogConfigurationEvent(logSource, SR.OptionIncludeFileNotFound(include, directory));
				return null;
			}
			var xs = ConfigurationFactory.FindXmlSource(cl, parameters);
			if (xs == null)
				return null;

			Config.RegisterSource(xs);

			if (includes == null)
				includes = new List<string>();
			if (!includes.Contains(cl.Path))
			{
				includes.Add(cl.Path);
				FileWatcher.AddFileWatcher(cl.Path, eventHandler);
			}
			Config.LogConfigurationEvent(logSource, SR.ConfigurationFileIncluded(cl.Path));
			return xs.Content;
		}

		public static LocalFileConfigurationSource? Create(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				return null;
			if (location.SchemaType != ConfigLocatorSchema.File && location.SchemaType != ConfigLocatorSchema.Undefined)
				return null;
			if (!File.Exists(location.Path))
				return null;
			var file = new FileInfo(location.Path);
			if (!file.Exists)
				return null;
			return new LocalFileConfigurationSource(file, location.SourceType, location, parameters);
		}
	}
}
