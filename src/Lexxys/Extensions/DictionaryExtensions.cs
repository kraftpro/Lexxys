// Lexxys Infrastructural library.
// file: DictionaryExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections.Generic;

namespace Lexxys
{
	public static class DictionaryExtensions
	{
		public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key)
		{
			return that.TryGetValue(key, out var value) ? value : default;
		}
		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> that, TKey key)
		{
			return that.TryGetValue(key, out var value) ? value : default;
		}
#if NETFRAMEWORK || !NET5_0_OR_GREATER
		public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key)
		{
			return that.TryGetValue(key, out var value) ? value : default;
		}
#endif
		public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key, TValue @default)
		{
			return that.TryGetValue(key, out TValue value) ? value : @default;
		}
		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> that, TKey key, TValue @default)
		{
			return that.TryGetValue(key, out TValue value) ? value : @default;
		}
#if NETFRAMEWORK || !NET5_0_OR_GREATER
		public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key, TValue @default)
		{
			return that.TryGetValue(key, out TValue value) ? value : @default;
		}
#endif

		public static IEnumerable<TValue> CollectValues<TKey, TValue>(this Dictionary<TKey, TValue> that, IEnumerable<TKey> keys)
		{
			foreach (var key in keys)
			{
				if (that.TryGetValue(key, out TValue value))
					yield return value;
			}
		}
		public static IEnumerable<TValue> CollectValues<TKey, TValue>(this IDictionary<TKey, TValue> that, IEnumerable<TKey> keys)
		{
			foreach (var key in keys)
			{
				if (that.TryGetValue(key, out TValue value))
					yield return value;
			}
		}
		public static IEnumerable<TValue> CollectValues<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, IEnumerable<TKey> keys)
		{
			foreach (var key in keys)
			{
				if (that.TryGetValue(key, out TValue value))
					yield return value;
			}
		}

	}
}


