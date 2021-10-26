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
		private readonly Uri _location;

		private LocalFileConfigurationSource(Uri location, IReadOnlyCollection<string> parameters)
		{
			_file = new FileInfo(location.LocalPath);
			_location = location;
			_converter = XmlConfigurationProvider.GetSourceConverter(_file.Extension, OptionHandler, parameters);
		}

		#region IConfigurationSource

		public string Name => _file.FullName;

		public Uri Location => _location;

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
			OnChanged(this, new ConfigurationEventArgs());
		}

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			try
			{
				lock (this)
				{
					_content = null;
					FileWatcher.RemoveFileWatcher(_file.FullName, OnFileChanged);
					Changed?.Invoke(sender ?? this, e);
				}
			}
			catch (Exception flaw)
			{
				Config.LogConfigurationError(LogSource, flaw.Add("fileName", _file?.FullName));
			}
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, _file.FullName));
				return null;
			}
			return ConfigurationSource.HandleInclude(LogSource, parameters, _file.DirectoryName, ref _includes, OnChanged);
		}

		public static LocalFileConfigurationSource? Create(Uri location, IReadOnlyCollection<string> parameters)
		{
			if (location == null || !location.IsAbsoluteUri || !location.IsFile)
				return null;
			if (!File.Exists(location.LocalPath))
				return null;
			return new LocalFileConfigurationSource(location, parameters);
		}
	}
}
