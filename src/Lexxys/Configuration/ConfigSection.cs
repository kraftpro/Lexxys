// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#nullable enable

namespace Lexxys.Configuration
{
	public sealed class ConfigSection: IConfigSection
	{
		private readonly string _path;
		private readonly IConfigSource _configSource;

		public ConfigSection(IConfigSource configSource)
		{
			_path = String.Empty;
			_configSource = configSource;
		}

		private ConfigSection(IConfigSource configSource, string path)
		{
			_path = path.Trim(Dots);
			_configSource = configSource;
		}

		#region IConfigSection

		event EventHandler<ConfigurationEventArgs>? IConfigSection.Changed
		{
			add => _configSource.Changed += value;
			remove => _configSource.Changed -= value;
		}

		IConfigSection IConfigSection.GetSection(string? key)
			=> new ConfigSection(_configSource, Key(key));

		int IConfigSection.Version => _configSource.Version;

		void IConfigSection.MapPath(string key, string path)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			key = key.Trim(Dots);
			if (key == path)
				_pathMap.TryRemove(key, out _);
			else
				_pathMap[key] = path;
		}
		private ConcurrentDictionary<string, string> _pathMap = new();

		void IConfigSection.SetCollection<T>(string? path, IReadOnlyList<T> value)
			=> Lists<T>.Add(Key(path), value);

		void IConfigSection.SetValue<T>(string? path, T value)
			=> Values<T>.Add(Key(path), value);

		IValue<IReadOnlyList<T>> IConfigSection.GetCollection<T>(string? key)
		{
			var fullKey = Key(key);
			return new ConfigValue<IReadOnlyList<T>>(GetConfigValue, GetConfigVersion);

			IReadOnlyList<T> GetConfigValue()
				=> Lists<T>.TryGet(fullKey, out var value) ? value: _configSource.GetList<T>(fullKey);
		}

		IValue<T> IConfigSection.GetValue<T>(string? key, Func<T>? defaultValue)
		{
			return new ConfigValue<T>(GetConfigValue(Key(key), defaultValue), GetConfigVersion);

			Func<T> GetConfigValue(string key, Func<T>? defaultValue)
				=> defaultValue == null ?
					() => Values<T>.TryGet(key, out var value) ? value : _configSource.GetValue(key, typeof(T)) is T value2 ? value2 : throw new ConfigurationException(key, typeof(T)) :
					() => Values<T>.TryGet(key, out var value) ? value : _configSource.GetValue(key, typeof(T)) is T value2 ? value2 : defaultValue();
		}

		private int GetConfigVersion() => _configSource.Version;

		#endregion

		private string Key(string? key)
		{
			if (key == null)
				return _path;
			if (key.StartsWith("::", StringComparison.Ordinal))
				return key.Trim(Dots);
			var k = key.Trim(Dots);
			if (k.Length == 0)
				return _path;
			if (_pathMap.TryGetValue(k, out var value))
				return value;
			return
				_path.Length == 0 ? k:
				k.Length == 0 ? _path: _path + "." + k;
		}
		private static readonly char[] Dots = new[] { '.', ':', ' ', '\t', '\r', '\n', '\v', '\f', '\x85', '\xA0' };

		private static class Values<T>
		{
			private static ConcurrentDictionary<string, T>? _values;

			public static void Add(string key, T value) => (_values ??= new())[key] = value;

			public static bool TryGet(string key, [MaybeNullWhen(false)] out T value)
			{
				if (_values == null)
				{
					value = default!;
					return false;
				}
				return _values.TryGetValue(key, out value);
			}
		}

		private static class Lists<T>
		{
			private static ConcurrentDictionary<string, IReadOnlyList<T>>? _lists;

			public static void Add(string key, IReadOnlyList<T> value) => (_lists ??= new())[key] = value;

			public static bool TryGet(string key, [MaybeNullWhen(false)] out IReadOnlyList<T> value)
			{
				if (_lists == null)
				{
					value = default!;
					return false;
				}
				return _lists.TryGetValue(key, out value);
			}
		}

		private class ConfigValue<T>: IValue<T>
		{
			private readonly Func<T> _value;
			private readonly Func<int> _version;
			private volatile VersionValue? _item;

			public ConfigValue(Func<T> value, Func<int> version)
			{
				_value = value ?? throw new ArgumentNullException(nameof(value));
				_version = version ?? throw new ArgumentNullException(nameof(version));
				_item = default;
			}

			public T Value
			{
				get
				{
					for (; ; )
					{
						var current = _item;
						var version = _version();
						if (current?.Version == version)
							return current.Value;
						var value = _value();
						var updated = new VersionValue(version, value);
						Interlocked.CompareExchange(ref _item, updated, current);
					}
				}
			}

			object? IValue.Value => Value;

			class VersionValue
			{
				public T Value { get; }
				public int Version { get; }

				public VersionValue(int version, T value)
				{
					Value = value;
					Version = version;
				}
			}
		}
	}
}
