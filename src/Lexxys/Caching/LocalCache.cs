// Lexxys Infrastructural library.
// file: LocalCache.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Lexxys;

/// <summary>
/// Represents in-local memory cache using a dictionary to store cache items.
/// </summary>
/// <typeparam name="TKey">Type of the cache item key.</typeparam>
/// <typeparam name="TValue">Type of the cache item value.</typeparam>
public class LocalCache<TKey, TValue> where TKey: notnull
{
	private const long MinTimeToLive = 1 * TimeSpan.TicksPerSecond;
	private const long MaxTimeToLive = TimeSpan.TicksPerDay;
	private const long DefaultTimeToLive = 5 * TimeSpan.TicksPerMinute;
	private const int MinCapacity = 31;
	private const int DefaultCapacity = 256;
	private const int MaxCapacity = 1024 * 256;

	private readonly ConcurrentDictionary<TKey, CacheItem> _cache;
	private readonly string? _name;
	private int _capacity;
	private readonly long _timeToLive;
	private readonly long _slidingExpiration;
	private readonly Func<TKey, TValue>? _factory;

	public LocalCache(TimeSpan timeToLive = default, Func<TKey, TValue>? factory = null, IEqualityComparer<TKey>? comparer = null)
		: this(null, 0, 0, timeToLive, default, factory, comparer)
	{
	}

	public LocalCache(TimeSpan timeToLive, TimeSpan slidingExpiration, Func<TKey, TValue>? factory = null, IEqualityComparer<TKey>? comparer = null)
		: this(null, 0, 0, timeToLive, slidingExpiration, factory, comparer)
	{
	}

	public LocalCache(int capacity, int concurrencyLevel, TimeSpan timeToLive, Func<TKey, TValue>? factory, IEqualityComparer<TKey>? comparer = null)
		: this(null, capacity, concurrencyLevel, timeToLive, default, factory, comparer)
	{
	}

	public LocalCache(int capacity, int concurrencyLevel = 0, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, Func<TKey, TValue>? factory = null, IEqualityComparer<TKey>? comparer = null)
		: this(null, capacity, concurrencyLevel, timeToLive, slidingExpiration, factory, comparer)
	{
	}

	public LocalCache(string? name, int capacity, int concurrencyLevel = 0, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, Func<TKey, TValue> ?factory = null, IEqualityComparer<TKey>? comparer = null)
	{
		_name = name;
		if (capacity == 0)
			capacity = DefaultCapacity;
		else if (capacity < MinCapacity)
			capacity = MinCapacity;
		else if (capacity > MaxCapacity)
			capacity = MaxCapacity;
		if (concurrencyLevel == 0)
			concurrencyLevel = 4 * Environment.ProcessorCount;
		_cache = new ConcurrentDictionary<TKey, CacheItem>(concurrencyLevel, Math.Max((MinCapacity + 1) / 2, capacity / 8), comparer ?? EqualityComparer<TKey>.Default);
		_capacity = capacity;
		_factory = factory;
		_timeToLive = timeToLive == default ? DefaultTimeToLive:
			timeToLive.Ticks < MinTimeToLive ? MinTimeToLive:
			timeToLive.Ticks > MaxTimeToLive ? MaxTimeToLive: timeToLive.Ticks;
		_slidingExpiration = slidingExpiration == default ? _timeToLive:
			slidingExpiration.Ticks < MinTimeToLive ? MinTimeToLive:
			slidingExpiration.Ticks > MaxTimeToLive ? MaxTimeToLive: slidingExpiration.Ticks;
	}

	[MaybeNull]
	public TValue this[TKey index]
	{
		get { TryGet(index, out var x); return x; }
		set { Add(index, value); }
	}

	public bool Contains(TKey key)
	{
		return _cache.ContainsKey(key);
	}

	public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue result)
	{
		if (!_cache.TryGetValue(key, out CacheItem? item))
		{
			result = default!;
			return false;
		}
		if (item.IsDirty(_timeToLive, _slidingExpiration))
		{
			// It's possible to remove a freshly added item.
			if (!_cache.TryRemove(key, out var it) || it == item)
			{
				result = default!;
				return false;
			}
			_cache.TryAdd(key, it);
			item = it;
		}
		item.Touch();
		result = item.Value;
		return true;
	}

	public void Add(TKey key, TValue value)
	{
		var item = new CacheItem(value);
		bool created = true;
		_cache.AddOrUpdate(key, item, (_, _) =>
		{
			created = false;
			return item;
		});
		if (created)
			CheckCapacity();
	}

	public void Add(TKey key, Func<TValue> factory)
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));

		var item = new CacheItem(factory);
		bool created = true;
		_cache.AddOrUpdate(key, item, (_, _) =>
		{
			created = false;
			return item;
		});
		if (created)
			CheckCapacity();
	}

	public TValue Get(TKey key, Func<TKey, TValue>? factory = null)
	{
		factory ??= _factory ?? throw new ArgumentNullException(nameof(factory));
		bool created = false;
		CacheItem item = _cache.GetOrAdd(key, o => 
		{
			created = true;
			return new CacheItem(o, factory);
		});
		if (created)
			CheckCapacity();
		else if (item.IsDirty(_timeToLive, _slidingExpiration))
			_cache[key] = item = new CacheItem(key, factory);
		else
			item.Touch();
		return item.Value;
	}

	public bool Remove(TKey key) => _cache.TryRemove(key, out _);

	public void Clear() => _cache.Clear();

	private void CheckCapacity()
	{
		if (_capacity >= _cache.Count)
			return;

		DateTime absoluteTimeToLive = DateTime.UtcNow - TimeSpan.FromTicks(_timeToLive);
		var dirty = _cache.ToList();
		dirty.Sort((p, q) => -CompareItems(p.Value, q.Value));
		int n1 = dirty.Count / 4;
		if (_name != null)
			Lxx.Log?.Trace($"LocalCache '{_name}' capacity ({_capacity}) is low: {n1} valid item(s) have been cleared. (TTL={TimeSpan.FromTicks(_timeToLive)}, EXP={TimeSpan.FromTicks(_slidingExpiration)}).");
		for (int i = 0; i < n1; ++i)
		{
			_cache.TryRemove(dirty[i].Key, out _);
		}

		int CompareItems(CacheItem left, CacheItem right)
		{
			if (left.TouchStamp < absoluteTimeToLive || right.TouchStamp < absoluteTimeToLive)
				return left.TouchStamp.CompareTo(right.TouchStamp);
			return left.CompareTo(right);
		}
	}

	private class CacheItem
	{
		private const int DurationMultiplier = 4;
		private const long EmptyDuration = TimeSpan.TicksPerMillisecond / 10;

		public readonly TValue Value;
		private readonly long _timeStamp;
		private readonly long _duration;
		private int _count;
		private long _touchStamp;

		public CacheItem(TValue value /*, TimeSpan timeToLive*/)
		{
			Value = value;
			_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
			_duration = EmptyDuration;
		}

		public CacheItem(Func<TValue> factory)
		{
			var start = Stopwatch.StartNew();
			Value = factory();
			_duration = Math.Max(EmptyDuration, start.Elapsed.Ticks * DurationMultiplier);
			_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
		}

		public CacheItem(TKey key, Func<TKey, TValue> factory)
		{
			var start = Stopwatch.StartNew();
			Value = factory(key);
			_duration = Math.Max(EmptyDuration, start.Elapsed.Ticks * DurationMultiplier);
			_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
		}

		public void Touch()
		{
			Interlocked.Increment(ref _count);
			_touchStamp = DateTime.UtcNow.Ticks;
		}

		public DateTime TouchStamp => new DateTime(_touchStamp + _duration);

		public bool IsDirty(long timeToLiveTicks, long slidingExpiration)
		{
			long now = DateTime.UtcNow.Ticks;
			return (now - _timeStamp) > timeToLiveTicks || (now - _touchStamp) > slidingExpiration;
		}

		public int CompareTo(CacheItem other) => StampValue.CompareTo(other.StampValue);

		private long StampValue => _touchStamp + _count * _duration;
	}
}


