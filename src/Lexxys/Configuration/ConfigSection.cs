// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys.Configuration
{
	internal class ConfigSection: IConfigSection
	{
		private readonly IConfigValue _config;

		private ConfigSection(string path, string? key, IConfigValue config)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (key == null)
			{ 
				Path = path;
				Key = path;
			}
			else
			{
				Path = path + "." + key;
				Key = key;
			}
			_config = config;
		}

		public string Key { get; }
		public string Path {  get; }

		public int Version => DefaultConfig.Version;

		public event EventHandler<ConfigurationEventArgs> Changed
		{
			add => _config.Changed += value;
			remove => _config.Changed -= value;
		}

		public IConfigSection Section(string key)
			=> key.StartsWith("::", StringComparison.Ordinal) ? new ConfigSection(key.Substring(2), null, _config) : new ConfigSection(Path, key, _config);

		public IVersionedValue<T> GetSection<T>(string key, Func<T> defaultValue)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));

			return new ConfigSectionValue<T>(() => GetConfigValue<T>(key, defaultValue), () => Version);
		}

		public IVersionedValue<IReadOnlyList<T>> GetSectionList<T>(string key)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			return new ConfigSectionValue<IReadOnlyList<T>>(() => GetConfigList<T>(key), () => Version);
		}

		private T GetConfigValue<T>(string key, Func<T> defaultValue)
			=> _config.GetValue(key, typeof(T)) is T value ? value : defaultValue(); //, _config.Version);

		private IReadOnlyList<T> GetConfigList<T>(string key)
			=> _config.GetList<T>(key) ?? Array.Empty<T>();

		internal static readonly IConfigSection Instance = new ConfigSection("configuration", null, ConfigValue.Instace);

		private class ConfigValue: IConfigValue
		{
			public static readonly IConfigValue Instace = new ConfigValue();

			public event EventHandler<ConfigurationEventArgs> Changed
			{
				add => DefaultConfig.Changed += value;
				remove => DefaultConfig.Changed -= value;
			}

			public int Version => DefaultConfig.Version;

			public IReadOnlyList<T> GetList<T>(string key)
				=> DefaultConfig.GetList<T>(key);

			public object? GetValue(string key, Type type)
				=> DefaultConfig.GetObjectValue(key, type);
		}
	}
}
