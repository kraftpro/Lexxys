// Lexxys Infrastructural library.
// file: DictionaryExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using System.Runtime.InteropServices;

namespace Lexxys;

public static class DictionaryExtensions
{
#if !NETCOREAPP
	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> that, TKey key, TValue value)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		if (that.ContainsKey(key))
			return false;
		that.Add(key, value);
		return true;
	}

	public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out var value) ? value: default;
	}

	public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> that, TKey key, TValue @default)
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		return that.TryGetValue(key, out TValue value) ? value: @default;
	}
#endif

#if NET6_0_OR_GREATER

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key, TValue value)
		where TKey : notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));

		ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(that, key, out var exists);
		if (!exists)
			val = value;
		return val!;
	}

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key, Func<TKey, TValue> factory)
		where TKey : notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));
		if (factory is null) throw new ArgumentNullException(nameof(factory));

		IDictionary<TKey, TValue> dict = that;
		dict.TryAdd(key, factory(key));


		ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(that, key, out var exists);
		if (!exists)
			val = factory(key);
		return val!;
	}

	public static bool TryUpdate<TKey, TValue>(this Dictionary<TKey, TValue> that, TKey key, TValue value)
		where TKey : notnull
	{
		if (that is null) throw new ArgumentNullException(nameof(that));

		ref var val = ref CollectionsMarshal.GetValueRefOrNullRef(that, key);
		if (Unsafe.IsNullRef(ref val))
			return false;
		val = value;
		return true;
	}

#endif

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


