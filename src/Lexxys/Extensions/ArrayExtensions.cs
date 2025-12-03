using Lexxys;

namespace Lexxys;

public static class ArrayExtensions
{
	public static T[] Append<T>(this T[]? array, T value)
	{
		if (array is not { Length: >0 })
			return [value];
		return [..array, value];
	}

    public static T[] Append<T>(this T[]? array, T value1, T value2)
    {
		if (array is not { Length: >0 })
			return [value1, value2];
		return [..array, value1, value2];
    }

    public static T[] Append<T>(this T[]? array, T value1, T value2, T value3)
    {
		if (array is not { Length: >0 })
			return [value1, value2, value3];
		return [..array, value1, value2, value3];
    }

    public static T[] Append<T>(this T[]? array, params T[]? value)
    {
		if (array is not { Length: >0 })
			return value ?? [];
		if (value is not { Length: >0 })
			return array;
		return [..array, ..value];
    }

	public static T[] Insert<T>(this T[]? array, int position, T value)
	{
		if (array is not { Length: > 0 })
			return position == 0 ? [value]: throw new ArgumentOutOfRangeException(nameof(position), position, null);
		if (position < 0 || position > array.Length)
			throw new ArgumentOutOfRangeException(nameof(position), position, null);
		var result = new T[array.Length + 1];
		if (position > 0)
			array.AsSpan(0, position).CopyTo(result);
		result[position] = value;
		if (position < array.Length)
			array.AsSpan(position).CopyTo(result.AsSpan(position));
		return result;
	}

	public static int BinarySearch<T>(this T[] array, T value, IComparer<T>? comparer = null) => Array.BinarySearch(array, value, comparer);

	public static int BinarySearch<T>(this T[] array, int index, int length, T value, IComparer<T>? comparer = null) => Array.BinarySearch(array, index, length, value, comparer);

	public static TOut[] ConvertAll<TIn, TOut>(this TIn[] array, Converter<TIn, TOut> converter) => Array.ConvertAll(array, converter);

	public static bool Exists<T>(this T[] array, Predicate<T> match) => Array.Exists(array, match);

	public static T? Find<T>(this T[] array, Predicate<T> match) => Array.Find(array, match);

	public static T[] FindAll<T>(this T[] array, Predicate<T> match) => Array.FindAll(array, match);

	public static int FindIndex<T>(this T[] array, Predicate<T> match) => Array.FindIndex(array, match);

	public static int FindIndex<T>(this T[] array, int startIndex, Predicate<T> match) => Array.FindIndex(array, startIndex, match);

	public static int FindIndex<T>(this T[] array, int startIndex, int count, Predicate<T> match) => Array.FindIndex(array, startIndex, count, match);

	public static T? FindLast<T>(this T[] array, Predicate<T> match) => Array.FindLast(array, match);

	public static int FindLastIndex<T>(this T[] array, Predicate<T> match) => Array.FindLastIndex(array, match);

	public static int FindLastIndex<T>(this T[] array, int startIndex, Predicate<T> match) => Array.FindLastIndex(array, startIndex, match);

	public static int FindLastIndex<T>(this T[] array, int startIndex, int count, Predicate<T> match) => Array.FindLastIndex(array, startIndex, count, match);

	public static T[] ForEach<T>(this T[] array, Action<T> action)
	{
		Array.ForEach(array, action);
		return array;
	}

	public static int IndexOf<T>(this T[] array, T value) => Array.IndexOf(array, value);

	public static int IndexOf<T>(this T[] array, T value, int startIndex) => Array.IndexOf(array, value, startIndex);

	public static int IndexOf<T>(this T[] array, T value, int startIndex, int count) => Array.IndexOf(array, value, startIndex, count);

	public static int LastIndexOf<T>(this T[] array, T value) => Array.LastIndexOf(array, value);

	public static int LastIndexOf<T>(this T[] array, T value, int startIndex) => Array.LastIndexOf(array, value, startIndex);

	public static int LastIndexOf<T>(this T[] array, T value, int startIndex, int count) => Array.LastIndexOf(array, value, startIndex, count);

	public static T[] Reverse<T>(this T[] array)
	{
		Array.Reverse(array);
		return array;
	}

	public static T[] RemoveLast<T>(this T[]? array) => array is { Length: > 1 } ? array.AsSpan(0, array.Length - 1).ToArray(): [];

	public static T[] RemoveFirst<T>(this T[]? array) => array is { Length: > 1 } ? array.AsSpan(1).ToArray(): [];

	public static T[] RemoveAt<T>(this T[]? array, int index)
	{
		if (array is not { Length: >1 })
			return index == 0 ? []: throw new ArgumentOutOfRangeException(nameof(index), index, null);
		return [.. array.AsSpan(0, index), .. array.AsSpan(index + 1)];
	}

	public static T[] Sort<T>(this T[] array)
	{
		Array.Sort(array);
		return array;
	}

	public static T[] Sort<T>(this T[] array, Comparison<T> comparison)
	{
		Array.Sort(array, comparison);
		return array;
	}

	public static T[] Sort<T>(this T[] array, IComparer<T>? comparer)
	{
		Array.Sort(array, comparer);
		return array;
	}

	public static T[] Sort<T>(this T[] array, int index, int length)
	{
		Array.Sort(array, index, length);
		return array;
	}

	public static T[] Sort<T>(this T[] array, int index, int length, IComparer<T>? comparer)
	{
		Array.Sort(array, index, length, comparer);
		return array;
	}
}
