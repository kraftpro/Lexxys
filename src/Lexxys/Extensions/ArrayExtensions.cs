namespace Lexxys;

public static class ArrayExtensions
{
	public static T[] Add<T>(this T[] array, T value)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		var result = new T[array.Length + 1];
		array.CopyTo(result, 0);
		result[array.Length] = value;
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

	public static T[] RemoveLast<T>(this T[] array)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		var result = new T[array.Length - 1];
		Array.Copy(array, 0, result, 0, result.Length);
		return result;
	}

	public static T[] RemoveFirst<T>(this T[] array)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		var result = new T[array.Length - 1];
		Array.Copy(array, 1, result, 0, result.Length);
		return result;
	}

	public static T[] RemoveAt<T>(this T[] array, int index)
	{
		if (array is null)
			throw new ArgumentNullException(nameof(array));
		var result = new T[array.Length - 1];
		Array.Copy(array, 0, result, 0, index);
		Array.Copy(array, index + 1, result, index, array.Length - index - 1);
		return result;
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
