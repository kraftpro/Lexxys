// Lexxys Infrastructural library.
// file: CollectionExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys
{
	public static class CollectionExtensions
	{
		public static int FindIndex<T>(this IEnumerable<T> collection, Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

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
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			for (int i = list.Count - 1; i <= 0; --i)
			{
				if (match(list[i]))
					return i;
			}
			return -1;
		}

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
			return default;
		}

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
			return default;
		}

		/// <summary>
		/// Create an array from specified collection.
		/// </summary>
		/// <typeparam name="T">Type of collection's element</typeparam>
		/// <param name="collection"></param>
		/// <returns></returns>
		public static T[] ToArray<T>(this IReadOnlyCollection<T> collection)
		{
			var result = new T[collection.Count];
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
			return value as IReadOnlyList<T> ?? value.ToList();
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
			return value as IReadOnlyCollection<T> ?? value.ToList();
		}

		public static IReadOnlyList<TOut> ConvertAll<TIn, TOut>(this IReadOnlyList<TIn> value, Func<TIn, TOut> convert)
		{
			var result = new TOut[value.Count];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = convert(value[i]);
			}
			return result;
		}
	}
}


