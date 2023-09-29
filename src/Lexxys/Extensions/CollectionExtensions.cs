// Lexxys Infrastructural library.
// file: CollectionExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

using Lexxys;
namespace Lexxys;

public static class CollectionExtensions
{
	public static int FindIndex<T>(this IEnumerable<T> collection, Predicate<T> match)
	{
		if (collection == null)
			throw new ArgumentNullException(nameof(collection));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		int i = 0;
		foreach (var item in collection)
		{
			if (match(item))
				return i;
			++i;
		}
		return -1;
	}

	public static int FindIndex<T>(this IEnumerable<T> collection, T value)
	{
		if (collection == null)
			throw new ArgumentNullException(nameof(collection));

		int i = 0;
		foreach (var item in collection)
		{
			if (Object.Equals(item, value))
				return i;
			++i;
		}
		return -1;
	}

	public static int FindIndex<T>(this IList<T> list, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = 0; i < list.Count; ++i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	public static int FindIndex<T>(this ISafeList<T> list, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = 0; i < list.Count; ++i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	public static int FindIndex<T>(this IList<T> list, int startIndex, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = startIndex; i < list.Count; ++i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	public static int FindIndex<T>(this ISafeList<T> list, int startIndex, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = startIndex; i < list.Count; ++i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	public static int FindLastIndex<T>(this IList<T> list, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = list.Count - 1; i <= 0; --i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	public static int FindLastIndex<T>(this ISafeList<T> list, Predicate<T> match)
	{
		if (list is null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = list.Count - 1; i <= 0; --i)
		{
			if (match(list[i]))
				return i;
		}
		return -1;
	}

	[return: MaybeNull]
	public static T LastOrDefault<T>(this IList<T> list, Predicate<T> match)
	{
		if (list == null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = list.Count - 1; i >= 0; --i)
		{
			if (match(list[i]))
				return list[i];
		}
		return default!;
	}

	[return: MaybeNull]
	public static T LastOrDefault<T>(this ISafeList<T> list, Predicate<T> match)
	{
		if (list == null)
			throw new ArgumentNullException(nameof(list));
		if (match == null)
			throw new ArgumentNullException(nameof(match));

		for (int i = list.Count - 1; i >= 0; --i)
		{
			if (match(list[i]))
				return list[i];
		}
		return default!;
	}

	/// <summary>
	/// Create an array from specified collection.
	/// </summary>
	/// <typeparam name="T">Type of collection's element</typeparam>
	/// <param name="collection"></param>
	/// <returns></returns>
	public static T[] ToArray<T>(this IReadOnlyCollection<T> collection)
	{
		if (collection is null)
			throw new ArgumentNullException(nameof(collection));

		var result = new T[collection.Count];
		if (collection is ICollection<T> cc)
		{
			cc.CopyTo(result, 0);
			return result;
		}

		int i = 0;
		foreach (T item in collection)
		{
			result[i++] = item;
		}
		return result;
	}

	public static IList<T> ToIList<T>(this IEnumerable<T> value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value as IList<T> ?? value.ToList();
	}

	public static IReadOnlyList<T> ToIReadOnlyList<T>(this IEnumerable<T> value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value switch
		{
			IReadOnlyList<T> irol => irol,
			IList<T> il => ReadOnly.Wrap(il)!,
			_ => value.ToList()
		};
	}

	public static ICollection<T> ToICollection<T>(this IEnumerable<T> value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value as ICollection<T> ?? value.ToList();
	}

	public static IReadOnlyCollection<T> ToIReadOnlyCollection<T>(this IEnumerable<T> value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		return value switch
		{
			IReadOnlyCollection<T> iroc => iroc,
			ICollection<T> ic => ReadOnly.Wrap(ic)!,
			_ => value.ToList()
		};
	}

	public static IReadOnlyList<TOut> ConvertAll<TIn, TOut>(this IReadOnlyList<TIn> value, Func<TIn, TOut> convert)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (convert is null)
			throw new ArgumentNullException(nameof(convert));

		var result = new TOut[value.Count];
		for (int i = 0; i < result.Length; i++)
		{
			result[i] = convert(value[i]);
		}
		return result;
	}

	public static void CopyTo<T>(this IReadOnlyCollection<T> value, T[] array, int index)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		if (index < 0 || index > array.Length)
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		if (array.Length < value.Count + index)
			throw new ArgumentException("The number of elements in the source is greater than the available space from index to the end of the destination array.");

		if (value is ICollection<T> c)
		{
			c.CopyTo(array, index);
			return;
		}
		int i = index;
		foreach (T item in value)
		{
			array[i++] = item;
		}
	}
}


