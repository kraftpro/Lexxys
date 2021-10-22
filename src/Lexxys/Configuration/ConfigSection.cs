// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#nullable enable

namespace Lexxys.Configuration
{
	internal class ConfigSection: IConfigSection
	{
		private readonly string _path;

		private ConfigSection(string? path)
		{
			_path = path?.Trim(Dots) ?? "";
		}

		internal static readonly IConfigSection Instance = new ConfigSection(null);

		#region IConfigSection

		event EventHandler<ConfigurationEventArgs> IConfigSection.Changed
		{
			add => DefaultConfig.Changed += value;
			remove => DefaultConfig.Changed -= value;
		}

		IConfigSection IConfigSection.GetSection(string key)
			=> new ConfigSection(Key(key));

		int IConfigSection.Version => DefaultConfig.Version;

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

		void IConfigSection.DefineCollection<T>(string path, IReadOnlyList<T> value)
			=> Lists<T>.Add(Key(path), value);

		void IConfigSection.DefineValue<T>(string path, T value)
			=> Values<T>.Add(Key(path), value);

		IValue<IReadOnlyList<T>> IConfigSection.GetCollection<T>(string? key)
		{
			var k = Key(key);
			return Lists<T>.TryGet(k, out var value) ? new Out<IReadOnlyList<T>>(() => value!): new ConfigValue<IReadOnlyList<T>>(() => DefaultConfig.GetList<T>(k) ?? Array.Empty<T>(), GetConfigVersion);
		}

		#nullable disable

		IValue<T> IConfigSection.GetValue<T>(string key, Func<T> defaultValue)
		{
			var k = Key(key);
			return Values<T>.TryGet(k, out var value) ? new Out<T>(() => value): new ConfigValue<T>(GetConfigValue(Key(k), defaultValue), GetConfigVersion);
		}

		private static Func<T> GetConfigValue<T>(string key, Func<T> defaultValue)
			=> defaultValue == null ?
				() => DefaultConfig.GetValue(key, typeof(T)) is T value ? value : throw new ConfigurationException(key, typeof(T)) :
				() => DefaultConfig.GetValue(key, typeof(T)) is T value ? value : defaultValue();

		private static int GetConfigVersion() => DefaultConfig.Version;

		#nullable enable

		#endregion

		private string Key(string? key)
		{
			if (key == null)
				return _path;
			if (key.StartsWith("::", StringComparison.Ordinal))
				return key.Trim(Dots);
			var k = key.Trim(Dots);
			if (_pathMap.TryGetValue(k, out var value))
				return value;
			return
				_path.Length == 0 ? k:
				k.Length == 0 ? _path: _path + "." + k;
		}
		private static readonly char[] Dots = new[] { '.', ':', ' ' };

		private static class Values<T>
		{
			private static ConcurrentDictionary<string, T>? _values;

			public static void Add(string key, T value) => (_values ??= new())[key] = value;

#nullable disable
			public static bool TryGet(string key, out T value)
			{
				if (_values == null)
				{
					value = default;
					return false;
				}
				return _values.TryGetValue(key, out value);
			}
#nullable enable
		}

		private static class Lists<T>
		{
			private static ConcurrentDictionary<string, IReadOnlyList<T>>? _lists;

			public static void Add(string key, IReadOnlyList<T> value) => (_lists ??= new())[key] = value;

			public static bool TryGet(string key, out IReadOnlyList<T>? value)
			{
				if (_lists == null)
				{
					value = default;
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
