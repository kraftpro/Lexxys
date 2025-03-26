using System.Collections;
using System.Collections.Concurrent;

namespace Lexxys.Configuration;

internal class ConfigService: IConfigService
{
	private volatile int _version;
	private volatile IConfigSource[] _providers;
	private readonly ConcurrentDictionary<string, object?> _cachedValues = [];
	private readonly ConcurrentDictionary<string, object?> _cachedLists = [];

	public ConfigService()
	{
		_version = 1;
		_providers = [];
	}

	public ConfigService(IEnumerable<IConfigSource> sources) : this()
	{
		if (sources == null) throw new ArgumentNullException(nameof(sources));

		_version = 1;
		_providers = sources.ToArray();
	}

	public int Version => _version;

	public event EventHandler<ConfigurationEventArgs>? Changed;

	public object? GetValue(string key, Type objectType)
	{
		if (objectType is null) throw new ArgumentNullException(nameof(objectType));
		if (key is null || key.Length <= 0) throw new ArgumentNullException(nameof(key));

		string cacheKey = $"{key}::{objectType.FullName}";
		return _cachedValues.GetOrAdd(cacheKey, GetConfigValue);

		object? GetConfigValue(string key)
		{
			var providers = _providers;
			foreach (IConfigSource item in providers)
			{
				var value = item.GetValue(key, objectType);
				if (value != null)
					return value;
			}
			return null;
		}
	}

	public IReadOnlyList<T> GetList<T>(string key)
	{
		if (key is null || key.Length <= 0) throw new ArgumentNullException(nameof(key));

		string cacheKey = $"{key}::{typeof(T).FullName}";
		return (IReadOnlyList<T>)(_cachedValues.GetOrAdd(cacheKey, GetConfigList) ?? Array.Empty<T>());

		IReadOnlyList<T>? GetConfigList(string key)
		{
			IReadOnlyList<T>? temp = null;
			List<T>? list = null;
			var providers = _providers;
			foreach (IConfigSource item in providers)
			{
				var x = item.GetList<T>(key);
				if (x.Count <= 0) continue;
				if (temp == null)
					temp = x;
				else
					(list ??= new List<T>(temp)).AddRange(x);
			}
			return temp == null ? null : list == null ? temp : ReadOnly.Wrap(list)!;
		}
	}

	public int AddConfiguration(IConfigSource source, int position = 0)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));

		int i = AddConfigurationInternal(source, position);
		if (i >= 0)
		{
			OnChanged(source, ConfigurationEventArgs.Default);
		}
		return i;
	}

	public int Count => _providers.Length;

	public IEnumerator<IConfigSource> GetEnumerator() => ((IEnumerable<IConfigSource>)_providers).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _providers.GetEnumerator();

	bool IEquatable<IConfigSource>.Equals(IConfigSource? other) => ReferenceEquals(this, other);

	private int AddConfigurationInternal(IConfigSource provider, int position)
	{
		IConfigSource[] providers;
		IConfigSource[] updated;
		int inserted;
		do
		{
			providers = _providers;
			var i = providers.FindIndex(o => o.Equals(provider));
			if (i >= 0)
				return ~i;
			updated = new IConfigSource[providers.Length + 1];
			if (position >= providers.Length)
			{
				updated = providers.Append(provider);
				inserted = updated.Length;
			}
			else
			{
				updated = providers.Insert(position, provider);
				inserted = position + 1;
			}
		} while (Interlocked.CompareExchange(ref _providers, updated, providers) != providers);
		return inserted;
	}

	private void OnChanged(object? sender, ConfigurationEventArgs e)
	{
		Interlocked.Increment(ref _version);
		Changed?.Invoke(sender, e);
	}
}
