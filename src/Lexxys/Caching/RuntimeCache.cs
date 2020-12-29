// Lexxys Infrastructural library.
// file: RuntimeCache.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NETSTANDARD
using Microsoft.Extensions.Caching.Memory;
#else
using System.Runtime.Caching;
#endif

namespace Lexxys
{
	public static class RuntimeCache
	{
#if NETSTANDARD
		private static readonly MemoryCache Default = new MemoryCache(new OptOut<MemoryCacheOptions>(() => new MemoryCacheOptions { CompactionPercentage = 0.5, ExpirationScanFrequency = TimeSpan.FromMinutes(2) }));
#else
		private static readonly MemoryCache Default = MemoryCache.Default;
#endif

		/// <summary>10 Seconds</summary>
		public static readonly TimeSpan Local = new TimeSpan(0, 0, 10);
		/// <summary>3 Minutes</summary>
		public static readonly TimeSpan Short = new TimeSpan(0, 3, 0);
		/// <summary>30 Minutes</summary>
		public static readonly TimeSpan Long = new TimeSpan(0, 30, 0);
		/// <summary>16 hours</summary>
		public static readonly TimeSpan Permanent = new TimeSpan(16, 0, 0);

		/// <summary>10 Minutes</summary>
		public static readonly TimeSpan DefaultTimeToLive = new TimeSpan(0, 10, 0);
		/// <summary>5 Seconds</summary>
		public static readonly TimeSpan MinTimeToLive = new TimeSpan(0, 0, 5);
		/// <summary>100 Days</summary>
		public static readonly TimeSpan MaxTimeToLive = new TimeSpan(100, 0, 0, 0);

		/// <summary>20 hours</summary>
		public static readonly TimeSpan DefaultCollectionTimeToLive = new TimeSpan(20, 0, 0);
		/// <summary>4 hours</summary>
		public static readonly TimeSpan DefaultCollectionSlidingExpiration = new TimeSpan(4, 0, 0);

		/// <summary>2 seconds</summary>
		public static readonly TimeSpan MinSlidingExpiration = new TimeSpan(0, 0, 2);
		/// <summary>10 minutes</summary>
		public static readonly TimeSpan DefaultSlidingExpiration = new TimeSpan(0, 10, 0);
		/// <summary>24 hours</summary>
		public static readonly TimeSpan MaxSlidingExpiration = new TimeSpan(24, 0, 0);

		/// <summary>16 hours</summary>
		public static readonly TimeSpan DefaultTimeToLiveWithSliding = Permanent;

		public static TValue Get<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive = default)
			where TValue: class
		{
#if NETSTANDARD
			if (Default.TryGetValue<TValue>(key, out var value))
#else
			if (Default[key] is TValue value)
#endif
				return value;

			if (timeToLive == default)
				timeToLive = DefaultTimeToLive;
			else if (timeToLive > MaxTimeToLive)
				timeToLive = MaxTimeToLive;
			else if (timeToLive < MinTimeToLive)
				timeToLive = MinTimeToLive;
			value = producer();
			Default.Set(key, value, DateTime.UtcNow + timeToLive);
			return value;
		}

		public static TValue Get<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive, TimeSpan slidingExpiration)
			where TValue: class
		{
#if NETSTANDARD
			if (Default.TryGetValue<TValue>(key, out var value))
#else
			if (Default[key] is TValue value)
#endif
				return value;

			if (slidingExpiration == default)
				slidingExpiration = DefaultSlidingExpiration;
			else if (slidingExpiration > MaxSlidingExpiration)
				slidingExpiration = MaxSlidingExpiration;
			else if (slidingExpiration <= MinSlidingExpiration)
				slidingExpiration = MinSlidingExpiration;

			if (timeToLive == default)
				timeToLive = TimeSpan.FromTicks(slidingExpiration.Ticks * 32);
			else if (timeToLive > MaxTimeToLive)
				timeToLive = MaxTimeToLive;
			else if (timeToLive < MinTimeToLive)
				timeToLive = MinTimeToLive;
			value = producer();
			Default.Set(key, value,
#if NETSTANDARD
				new MemoryCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow + timeToLive, SlidingExpiration = slidingExpiration }
#else
				new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow + timeToLive, SlidingExpiration = slidingExpiration }
#endif
				);
			return value;
		}

		public static TValue GetValue<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive = default)
			where TValue : struct
		{
			return (TValue?)Get(key, () => (object)producer(), timeToLive) ?? default;
		}

		public static TValue GetValue<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive, TimeSpan slidingExpiration)
			where TValue : struct
		{
			return (TValue?)Get(key, () => (object)producer(), timeToLive, slidingExpiration) ?? default;
		}

		public static void Remove(string key)
		{
			Default.Remove(key);
		}

		private struct CollectionDefinition
		{
			public readonly Func<object> Constructor;
			public readonly TimeSpan TimeToLive;

			public CollectionDefinition(Func<object> constructor, TimeSpan timeToLive)
			{
				Constructor = constructor;
				TimeToLive = timeToLive;
			}
		}
		private static readonly ConcurrentDictionary<string, CollectionDefinition> __collectionDefinitions = new ConcurrentDictionary<string, CollectionDefinition>();

		public static CollectionKey<TKey, TValue> DefineCollection<TKey, TValue>(string key, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, TimeSpan collectionTimeToLive = default, int capacity = 0, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null, int growFactor = 0)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (collectionTimeToLive < MinTimeToLive)
				collectionTimeToLive = DefaultCollectionTimeToLive;

			object Fact() => new LocalCache<TKey, TValue>(key, capacity, timeToLive: timeToLive, slidingExpiration: slidingExpiration, factory: factory, comparer: comparer, growFactor: growFactor);
			__collectionDefinitions.AddOrUpdate(key, new CollectionDefinition(Fact, collectionTimeToLive), (o, _) => new CollectionDefinition(Fact, collectionTimeToLive));
			return new CollectionKey<TKey, TValue>(key);
		}

		public static LocalCache<TKey, TValue> Collection<TKey, TValue>(CollectionKey<TKey, TValue> key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var value = Default.Get(key.Value);
			if (value != null)
				return value as LocalCache<TKey, TValue> ?? throw new ArgumentOutOfRangeException(nameof(key), key, $"Wrong collection type. Expected: {typeof(LocalCache<TKey, TValue>).FullName}, actual: {value.GetType().FullName}.");
			if (!__collectionDefinitions.TryGetValue(key.Value, out var definition))
				throw new ArgumentOutOfRangeException(nameof(key), key.Value, $"Cannot find collection definition for '{key.Value}'.");
			value = definition.Constructor();
			if (value == null)
				throw new ArgumentOutOfRangeException(nameof(key), key.Value, $"Cannot construct collection {typeof(LocalCache<TKey, TValue>).FullName}.");
			if (!(value is LocalCache<TKey, TValue> collection))
				throw new ArgumentOutOfRangeException(nameof(key), key.Value, $"Wrong constructed type. Expected: {typeof(LocalCache<TKey, TValue>).FullName}, actual: {value.GetType().FullName}.");
			Default.Set(key.Value, collection, DateTime.UtcNow + definition.TimeToLive);
			return collection;
		}

		//public static LocalCache<TKey, TValue> Collection<TKey, TValue>(string collectionKey, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, TimeSpan collectionTimeToLive = default, int capacity = 0, Func<TKey, TValue> factory = null, IEqualityComparer<TKey> comparer = null, int growFactor = 0)
		//{
		//	if (collectionTimeToLive == default)
		//		collectionTimeToLive = DefaultCollectionTimeToLive;
		//	else if (collectionTimeToLive > MaxTimeToLive)
		//		collectionTimeToLive = MaxTimeToLive;
		//	else if (collectionTimeToLive < MinTimeToLive)
		//		collectionTimeToLive = timeToLive;

		//	return Get(collectionKey, () => new LocalCache<TKey, TValue>(collectionKey, capacity, timeToLive: timeToLive, slidingExpiration: slidingExpiration, factory: factory, comparer: comparer, growFactor: growFactor), collectionTimeToLive);
		//}
	}

	public class CollectionKey<TKey, TValue>
	{
		internal string Value { get; }

		public CollectionKey(string value) => Value = value;
	}
}


