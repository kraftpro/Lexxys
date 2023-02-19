// Lexxys Infrastructural library.
// file: LocalFileConfigurationSource.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexxys.Configuration
{
	using Xml;

	internal class LocalFileConfigurationSource: IXmlConfigurationSource
	{
		private const string LogSource = "Lexxys.Configuration.LocalFileConfigurationSource";
		private readonly FileInfo _file;
		private readonly Func<string, string, IReadOnlyList<XmlLiteNode>> _converter;
		private List<string>? _includes;
		private IReadOnlyList<XmlLiteNode>? _content;
		private readonly Uri _location;
		private int _version;

		private LocalFileConfigurationSource(Uri location, IReadOnlyCollection<string>? parameters)
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
					lock (_location)
					{
						if (_content == null)
						{
							_content = _converter(File.ReadAllText(_file.FullName), _file.FullName);
							FileWatcher.AddFileWatcher(_file.FullName, OnFileChanged);
							++_version;
						}
					}
				}
				return _content;
			}
		}

		public event EventHandler<ConfigurationEventArgs>? Changed;

		public int Version => _version;

		#endregion

		public FileInfo FileInfo => _file;

		public override bool Equals(object? obj)
		{
			return obj is LocalFileConfigurationSource x && _file.FullName == x._file.FullName;
		}

		public override int GetHashCode()
		{
			return _file.FullName.GetHashCode();
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			OnChanged(this, new ConfigurationEventArgs());
		}

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			try
			{
				lock (_location)
				{
					_content = null;
					FileWatcher.RemoveFileWatcher(_file.FullName, OnFileChanged);
					Changed?.Invoke(sender ?? this, e);
				}
			}
			#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception flaw)
			{
				Config.LogConfigurationError(LogSource, flaw.Add("fileName", _file.FullName));
			}
			#pragma warning restore CA1031 // Do not catch general exception types
		}

		private IEnumerable<XmlLiteNode>? OptionHandler(TextToXmlConverter converter, string option, IReadOnlyCollection<string>? parameters)
		{
			if (option != "include")
			{
				Config.LogConfigurationEvent(LogSource, SR.UnknownOption(option, _file.FullName));
				return null;
			}
			return ConfigurationSource.HandleInclude(LogSource, parameters, _file.DirectoryName, ref _includes, OnChanged);
		}

		public static LocalFileConfigurationSource? TryCreate(Uri? location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null || !location.IsAbsoluteUri || !location.IsFile)
				return null;
			return !File.Exists(location.LocalPath) ? null: new LocalFileConfigurationSource(location, parameters);
		}
	}
}
