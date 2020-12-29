// Lexxys Infrastructural library.
// file: LocalCache.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public class LocalCache<TKey, TValue>
	{
		private const long MinTimeToLive = 1 * TimeSpan.TicksPerSecond;
		private const long MaxTimeToLive = TimeSpan.TicksPerDay;
		private const long DefaultTimeToLive = 5 * TimeSpan.TicksPerMinute;
		private const int MinCapacity = 31;
		private const int DefaultCapacity = 128;
		private const int MaxCapacity = 1024 * 128;

		private readonly ConcurrentDictionary<TKey, CacheItem> _cache;
		private readonly string _name;
		private int _capacity;
		private int _growFactor;
		private readonly long _timeToLive;
		private readonly long _slidingExpiration;
		private readonly Func<TKey, TValue> _factory;

		public LocalCache(TimeSpan timeToLive = default, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null)
			: this(null, 0, 0, timeToLive, default, factory, comparer)
		{
		}

		public LocalCache(TimeSpan timeToLive, TimeSpan slidingExpiration, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null)
			: this(null, 0, 0, timeToLive, slidingExpiration, factory, comparer)
		{
		}

		public LocalCache(int capacity, int concurrencyLevel, TimeSpan timeToLive, Func<TKey, TValue> factory, IEqualityComparer<TKey> comparer = null)
			: this(null, capacity, concurrencyLevel, timeToLive, default, factory, comparer)
		{
		}

		public LocalCache(int capacity, int concurrencyLevel = 0, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null)
			: this(null, capacity, concurrencyLevel, timeToLive, slidingExpiration, factory, comparer)
		{
		}

		public LocalCache(string name, int capacity, int concurrencyLevel = 0, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null, int growFactor = 0)
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
			_growFactor = growFactor;
			_factory = factory;
			_timeToLive = timeToLive == default ? DefaultTimeToLive:
				timeToLive.Ticks < MinTimeToLive ? MinTimeToLive:
				timeToLive.Ticks > MaxTimeToLive ? MaxTimeToLive: timeToLive.Ticks;
			_slidingExpiration = slidingExpiration == default ? _timeToLive:
				slidingExpiration.Ticks < MinTimeToLive ? MinTimeToLive:
				slidingExpiration.Ticks > MaxTimeToLive ? MaxTimeToLive: slidingExpiration.Ticks;
		}

		public TValue this[TKey index]
		{
			get { TryGet(index, out TValue x); return x; }
			set { Add(index, value); }
		}

		public bool Contains(TKey key)
		{
			return _cache.ContainsKey(key);
		}

		public bool TryGet(TKey key, out TValue result)
		{
			if (!_cache.TryGetValue(key, out CacheItem item))
			{
				result = default;
				return false;
			}
			if (item.IsDirty(_timeToLive, _slidingExpiration))
			{
				_cache.TryRemove(key, out _);
				result = default;
				return false;
			}
			item.Touch();
			result = item.Value;
			return true;
		}

		public void Add(TKey key, TValue value)
		{
			var item = new CacheItem(value);
			item.Touch();
			CheckCapacity();
			_cache[key] = item;
		}

		public void Add(TKey key, Func<TValue> factory)
		{
			var item = new CacheItem(factory);
			item.Touch();
			CheckCapacity();
			_cache[key] = item;
		}

		public TValue Get(TKey key, Func<TKey, TValue> factory = null)
		{
			if (factory == null)
				factory = _factory ?? throw new ArgumentNullException(nameof(factory));
			CacheItem item = _cache.GetOrAdd(key, o => new CacheItem(o, factory));
			if (item.IsDirty(_timeToLive, _slidingExpiration))
				item = _cache[key] = new CacheItem(key, factory);
			item.Touch();
			CheckCapacity();
			return item.Value;
		}

		public bool Remove(TKey key)
		{
			return _cache.TryRemove(key, out var _);
		}

		public void Clear()
		{
			_cache.Clear();
		}

		private void CheckCapacity()
		{
			if (_capacity >= _cache.Count)
				return;
			DateTime bound = DateTime.UtcNow - TimeSpan.FromTicks(_timeToLive);
			var dirty = _cache.ToList();
			int n0 = dirty.Count;
			int n1 = n0 * 3 / 4;
			for (int i = dirty.Count - 1; i >= 0; --i)
			{
				_cache.TryRemove(dirty[i].Key, out _);
			}
			if (dirty.Count <= n1)
				return;
			if (_growFactor > 0)
			{
				if (_name != null)
					Lxx.Log.Trace($"LocalCache '{_name}' capacity doubled ({_capacity} -> {_capacity * 2}).");
				--_growFactor;
				_capacity += _capacity;
				return;
			}

			dirty.Sort((p, q) => CompareItems(q.Value, p.Value, bound));
			if (_name != null)
				Lxx.Log.Trace($"LocalCache '{_name}' capacity ({_capacity}) is low: {dirty.Count - n1} valid item(s) have been cleared. (TTL={TimeSpan.FromTicks(_timeToLive)}, EXP={TimeSpan.FromTicks(_slidingExpiration)}).");
			for (int i = dirty.Count - 1; i >= n1; --i)
			{
				_cache.TryRemove(dirty[i].Key, out _);
			}
		}

		private static int CompareItems(CacheItem left, CacheItem right, DateTime bound)
		{
			if (left.TouchStamp < bound || right.TouchStamp < bound)
				return left.TouchStamp.CompareTo(right.TouchStamp);
			return left.CompareTo(right);
		}

		private class CacheItem: IComparable<CacheItem>
		{
			private const int DurationMultiplier = 4;

			public readonly TValue Value;
			private int _count;
			private long _touchStamp;
			private readonly long _timeStamp;
			private readonly long _duration;

			public CacheItem(TValue value /*, TimeSpan timeToLive*/)
			{
				Value = value;
				_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
			}

			public CacheItem(Func<TValue> factory)
			{
				long start = WatchTimer.Start();
				Value = factory();
				_duration = WatchTimer.ToTimeSpan(WatchTimer.Stop(start) * DurationMultiplier).Ticks;
				_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
			}

			public CacheItem(TKey key, Func<TKey, TValue> factory)
			{
				long start = WatchTimer.Start();
				Value = factory(key);
				_duration = WatchTimer.ToTimeSpan(WatchTimer.Stop(start) * DurationMultiplier).Ticks;
				_timeStamp = _touchStamp = DateTime.UtcNow.Ticks;
			}

			public void Touch()
			{
				++_count;
				_touchStamp = DateTime.UtcNow.Ticks;
			}

			public DateTime TouchStamp => new DateTime(_touchStamp + _duration);

			public bool IsDirty(long timeToLiveTicks, long slidingExpiration)
			{
				long now = DateTime.UtcNow.Ticks;
				return (now - _timeStamp) > timeToLiveTicks || (now - _touchStamp) > slidingExpiration;
			}

			public int CompareTo(CacheItem other)
			{
				return other == null ? 1:
					_count > other._count ? 1:
					_count < other._count ? -1: TouchStamp.CompareTo(other.TouchStamp);
			}
		}
	}
}


