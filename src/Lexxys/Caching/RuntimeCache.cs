// Lexxys Infrastructural library.
// file: RuntimeCache.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NETCOREAPP
using Microsoft.Extensions.Caching.Memory;
#else
using System.Runtime.Caching;
#endif

#nullable enable

namespace Lexxys
{
	/// <summary>
	/// Represent a set of methods to work with in-local memory cache.
	/// </summary>
	public static class RuntimeCache
	{
#if NET5_0_OR_GREATER
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

		/// <summary>
		/// Gets or adds the item associated with this <paramref name="key"/> if present.
		/// </summary>
		/// <param name="key">An object identifying the requested entry.</param>
		/// <param name="producer">A function produces a new value if the item not found.</param>
		/// <param name="timeToLive">Time to live for the item.</param>
		public static TValue Get<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive = default)
			where TValue: class
		{
#if NET5_0_OR_GREATER
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

		/// <summary>
		/// Gets or adds the item associated with this <paramref name="key"/> if present.
		/// </summary>
		/// <param name="key">An object identifying the requested entry.</param>
		/// <param name="producer">A function produces a new value if the item not found.</param>
		/// <param name="timeToLive">The time to live for the item.</param>
		/// <param name="slidingExpiration">The sliding expiration.</param>
		public static TValue Get<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive, TimeSpan slidingExpiration)
			where TValue: class
		{
#if NET5_0_OR_GREATER
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
#if NET5_0_OR_GREATER
				new MemoryCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow + timeToLive, SlidingExpiration = slidingExpiration }
#else
				new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow + timeToLive, SlidingExpiration = slidingExpiration }
#endif
				);
			return value;
		}

		/// <summary>
		/// Gets or adds the item associated with this <paramref name="key"/> if present.
		/// </summary>
		/// <param name="key">An object identifying the requested entry.</param>
		/// <param name="producer">A function produces a new value if the item not found.</param>
		/// <param name="timeToLive">Time to live for the item.</param>
		public static TValue GetValue<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive = default)
			where TValue : struct
		{
			return (TValue?)Get(key, () => (object)producer(), timeToLive) ?? default;
		}

		/// <summary>
		/// Gets or adds the item associated with this <paramref name="key"/> if present.
		/// </summary>
		/// <param name="key">An object identifying the requested entry.</param>
		/// <param name="producer">A function produces a new value if the item not found.</param>
		/// <param name="timeToLive">The time to live for the item.</param>
		/// <param name="slidingExpiration">The sliding expiration.</param>
		public static TValue GetValue<TValue>(string key, Func<TValue> producer, TimeSpan timeToLive, TimeSpan slidingExpiration)
			where TValue : struct
		{
			return (TValue?)Get(key, () => (object)producer(), timeToLive, slidingExpiration) ?? default;
		}

		/// <summary>
		/// Removes the object associated with the given key.
		/// </summary>
		/// <param name="key">An object identifying the entry.</param>
		public static void Remove(string key)
		{
			Default.Remove(key);
		}

		private readonly struct CollectionDefinition: IEquatable<CollectionDefinition>
		{
			public readonly Func<object> Constructor;
			public readonly TimeSpan TimeToLive;

			public CollectionDefinition(Func<object> constructor, TimeSpan timeToLive)
			{
				Constructor = constructor;
				TimeToLive = timeToLive;
			}

			public override bool Equals(object obj)
			{
				return obj is CollectionDefinition definition && Equals(definition);
			}

			public bool Equals(CollectionDefinition other)
			{
				return Object.ReferenceEquals(Constructor, other.Constructor) &&
					TimeToLive == other.TimeToLive;
			}

			public override int GetHashCode()
			{
				return HashCode.Join(Constructor?.GetHashCode() ?? 0, TimeToLive.GetHashCode());
			}
		}
		private static readonly ConcurrentDictionary<string, CollectionDefinition> __collectionDefinitions = new ConcurrentDictionary<string, CollectionDefinition>();

		/// <summary>
		/// Defines a new caches collection for the specified <paramref name="key"/> and the collection parameters. 
		/// </summary>
		/// <param name="key">Collection key.</param>
		/// <param name="timeToLive">Cache time to live.</param>
		/// <param name="slidingExpiration">Cache sliding expiration.</param>
		/// <param name="collectionTimeToLive">Collection item sliding expiration</param>
		/// <param name="capacity">Collection capacity</param>
		/// <param name="factory">Default collection item factory.</param>
		/// <param name="comparer">Collection item key comparer.</param>
		/// <param name="growFactor">Collection grow factor</param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns>The <see cref="CollectionKey{TKey,TValue}"/> for the created collection definition.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static CollectionKey<TKey, TValue> DefineCollection<TKey, TValue>(string key, TimeSpan timeToLive = default, TimeSpan slidingExpiration = default, TimeSpan collectionTimeToLive = default, int capacity = 0, Func<TKey, TValue>? factory = null, IEqualityComparer<TKey>? comparer = null)
			where TKey: notnull
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (collectionTimeToLive < MinTimeToLive)
				collectionTimeToLive = DefaultCollectionTimeToLive;

			object Fact() => new LocalCache<TKey, TValue>(key, capacity, timeToLive: timeToLive, slidingExpiration: slidingExpiration, factory: factory, comparer: comparer);
			__collectionDefinitions.AddOrUpdate(key, new CollectionDefinition(Fact, collectionTimeToLive), (_, _) => new CollectionDefinition(Fact, collectionTimeToLive));
			return new CollectionKey<TKey, TValue>(key);
		}

		/// <summary>
		/// Returns the cashes collection for the specified collection <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Collection key</param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static LocalCache<TKey, TValue> Collection<TKey, TValue>(CollectionKey<TKey, TValue> key)
			where TKey : notnull
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
			if (value is not LocalCache<TKey, TValue> collection)
				throw new ArgumentOutOfRangeException(nameof(key), key.Value, $"Wrong constructed type. Expected: {typeof(LocalCache<TKey, TValue>).FullName}, actual: {value.GetType().FullName}.");
			Default.Set(key.Value, collection, DateTime.UtcNow + definition.TimeToLive);
			return collection;
		}
	}

	/// <summary>
	/// Represents a collection key for the specified <typeparamref name="TKey"/> / <typeparamref	name="TValue"/> pair.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class CollectionKey<TKey, TValue>
	{
		internal string Value { get; }

		/// <summary>
		/// Created a collection key.
		/// </summary>
		/// <param name="value">The collection key value.</param>
		public CollectionKey(string value) => Value = value;
	}
}
