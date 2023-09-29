// Lexxys Infrastructural library.
// file: DictionaryExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public static class DictionaryExtensions
{
	public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key) where TKey: notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: default;
	}
	public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> that, TKey key)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: default;
	}
#if !NETCOREAPP
	public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: default;
	}
#endif
	public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key, TValue @default) where TKey: notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: @default;
	}
	public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> that, TKey key, TValue @default)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: @default;
	}
#if !NETCOREAPP
	public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key, TValue @default)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out TValue value) ? value: @default;
	}
#endif

	public static IEnumerable<TValue> CollectValues<TKey, TValue>(this Dictionary<TKey, TValue> that, IEnumerable<TKey> keys) where TKey: notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		if (keys is null) throw new ArgumentNullException(nameof(keys));

		foreach (var key in keys)
		{
			if (that.TryGetValue(key, out var value))
				yield return value;
		}
	}
	public static IEnumerable<TValue> CollectValues<TKey, TValue>(this IDictionary<TKey, TValue> that, IEnumerable<TKey> keys)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		if (keys is null) throw new ArgumentNullException(nameof(keys));

		foreach (var key in keys)
		{
			if (that.TryGetValue(key, out var value))
				yield return value;
		}
	}
	public static IEnumerable<TValue> CollectValues<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, IEnumerable<TKey> keys)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		if (keys is null) throw new ArgumentNullException(nameof(keys));

		foreach (var key in keys)
		{
			if (that.TryGetValue(key, out var value))
				yield return value;
		}
	}

}


