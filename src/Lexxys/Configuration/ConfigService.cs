using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


#nullable enable

namespace Lexxys.Configuration
{
	internal class ConfigService: IConfigService
	{
		public static IConfigService Default = new ConfigService();

		private const string LogSource = "Lexxys.Configuration";

		private volatile IConfigProvider[] _providers;
		private readonly ConcurrentDictionary<string, object?> _cache;
		private static int _version;

		public int Version => _version;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		static ConfigService()
		{
			StaticServices.AddFactory(ConfigServiceFactory.Instance);
		}

		private ConfigService()
			: this(ConfigScanner.ScanConfigFiles(true).ToArray())
		{
		}

		public ConfigService(IConfigProvider[] providers)
		{
			_cache = new ConcurrentDictionary<string, object?>();
			_providers = providers?.Length > 0 ? (IConfigProvider[])providers.Clone(): Array.Empty<IConfigProvider>();
			for (int i = 0; i < _providers.Length; ++i)
			{
				_providers[i].Changed += Provider_Changed;
			}
		}

		public void AddConfigurationProvider(IConfigProvider provider, bool tail = false)
		{
			IConfigProvider[] current;
			IConfigProvider[] updated;
			do
			{
				current = _providers;
				var i = current.FindIndex(o => o.Location == provider.Location);
				if (i >= 0)
					return;
				updated = new IConfigProvider[current.Length + 1];
				if (tail)
				{
					Array.Copy(current, updated, current.Length);
					updated[current.Length] = provider;
				}
				else
				{
					updated[0] = provider;
					Array.Copy(current, 0, updated, 1, current.Length);
				}
			} while (Interlocked.CompareExchange(ref _providers, updated, current) == current);
			provider.Changed += Provider_Changed;
			Provider_Changed(provider, new ConfigurationEventArgs());
		}

		private void Provider_Changed(object? sender, ConfigurationEventArgs e)
		{
			Interlocked.Increment(ref _version);
			_cache.Clear();
			Changed?.Invoke(sender, e);
			if (sender is IConfigProvider provider)
				StaticServices.TryCreate<ILogger>(LogSource)?.Trace(SR.ConfigurationChanged(provider));
		}

		public IReadOnlyList<T> GetList<T>(string key)
		{
			if (key is null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));

			string cacheKey = key.ToUpperInvariant() + "$(" + typeof(T).ToString();
			if (_cache.TryGetValue(cacheKey, out var value))
				return value as IReadOnlyList<T> ?? EmptyArray<T>.Value;

			IReadOnlyList<T>? temp = null;
			List<T>? list = null;
			var providers = _providers;
			for (int i = 0; i < providers.Length; ++i)
			{
				IReadOnlyList<T> x = providers[i].GetList<T>(key);
				if (x != null && x.Count > 0)
				{
					if (temp == null)
					{
						temp = x;
					}
					else
					{
						if (list == null)
						{
							list = new List<T>(temp);
						}
						list.AddRange(x);
					}
				}
			}
			IReadOnlyList<T> result =
				temp == null ? Array.Empty<T>():
				list == null ? temp: ReadOnly.Wrap(list);
			_cache[cacheKey] = result;
			return result;
		}

		public object? GetValue(string key, Type objectType)
		{
			if (objectType is null)
				throw EX.ArgumentNull(nameof(objectType));
			if (key is null || key.Length <= 0)
				throw EX.ArgumentNull(nameof(key));

			string cacheKey = key.ToUpperInvariant() + "$" + objectType.ToString();
			if (_cache.TryGetValue(cacheKey, out object? value))
				return value;

			var providers = _providers;
			for (int i = 0; i < providers.Length; ++i)
			{
				value = providers[i].GetValue(key, objectType);
				if (value != null)
					break;
			}
			_cache[cacheKey] = value;
			return value;
		}

		#region Factory

		private class ConfigServiceFactory: StaticServices.IFactory
		{
			public static ConfigServiceFactory Instance = new ConfigServiceFactory();

			private ConfigServiceFactory()
			{
			}

			public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
			private readonly Type[] _supportedTypes = new Type[] { typeof(IConfigService) };

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type == null)
					throw new ArgumentNullException(nameof(type));
				if (type == typeof(IConfigService))
				{
					var providers = arguments?.Select(o => o as IConfigProvider).Where(o => o != null).ToArray();
					if (providers is null || providers.Length == 0)
						result = Default;
					else
						result = new ConfigService(providers!);
					return true;
				}
				result = null;
				return false;
			}
		}

		#endregion
	}
}
