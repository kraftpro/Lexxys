// Lexxys Infrastructural library.
// file: Rand.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexxys.Testing
{
	public static class Rand
	{

		private static IRand __rnd = new RndKnuth();

		public static void Reset(int seed)
		{
			__rnd.Reset(seed);
		}

		public static void Reset(IRand rnd)
		{
			__rnd = rnd;
		}

		public static T[] Array<T>(int count, Func<T> constructor)
		{
			var values = new T[count];
			for (int i = 0; i < values.Length; ++i)
			{
				values[i] = constructor();
			}
			return values;
		}

		public static T[] Array<T>(int minCount, int maxCount, Func<T> constructor)
		{
			var values = new T[Int(minCount, maxCount + 1)];
			for (int i = 0; i < values.Length; ++i)
			{
				values[i] = constructor();
			}
			return values;
		}

		public static T[] Array<T>(int minCount, int maxCount, Func<int, T> constructor)
		{
			var values = new T[Int(minCount, maxCount + 1)];
			for (int i = 0; i < values.Length; ++i)
			{
				values[i] = constructor(i);
			}
			return values;
		}

		public static byte[] Bits(int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, null);
			if (length == 0)
				return System.Array.Empty<byte>();
			byte[] result = new byte[length];
			__rnd.NextBytes(result);
			return result;
		}

		public static int Int(int minValue, int maxValue)
		{
			return __rnd.NextInt(minValue, maxValue);
		}

		public static int Int(int maxValue)
		{
			return __rnd.NextInt(maxValue);
		}

		public static int Int()
		{
			return __rnd.NextInt();
		}

		public static long Long(long minValue, long maxValue)
		{
			return __rnd.NextLong(minValue, maxValue);
		}

		public static long Long(long maxValue)
		{
			return __rnd.NextLong(maxValue);
		}

		public static long Long()
		{
			return __rnd.NextLong();
		}

		public static bool Bool(double bound = 0.5)
		{
			return __rnd.NextDouble() < bound;
		}

		public static double Dbl()
		{
			return __rnd.NextDouble();
		}

		public static double Dbl(double maxValue)
		{
			return __rnd.NextDouble(maxValue);
		}

		public static double Dbl(double minValue, double maxValue)
		{
			return __rnd.NextDouble(minValue, maxValue);
		}

		/// <summary>
		/// Returns random decimal value in range 0..0.9(28)
		/// </summary>
		/// <returns></returns>
		public static decimal Dec()
		{
			return __rnd.NextDecimal();
		}

		public static decimal Dec(decimal maxValue)
		{
			return __rnd.NextDecimal(maxValue);
		}

		public static decimal Dec(decimal minValue, decimal maxValue)
		{
			return __rnd.NextDecimal(minValue, maxValue);
		}

		public static T Case<T>(double bound, T trueValue, T falseValue = default)
		{
			return Bool(bound) ? trueValue: falseValue;
		}

		public static T Case<T>(double bound1, T case1, double bound2, T case2, T falseValue = default)
		{
			return
				Bool(bound1) ? case1:
				Bool(bound2) ? case2: falseValue;
		}

		public static T Case<T>(double bound1, T case1, double bound2, T case2, double bound3, T case3, T falseValue = default)
		{
			return
				Bool(bound1) ? case1:
				Bool(bound2) ? case2:
				Bool(bound3) ? case3: falseValue;
		}

		public static T Case<T>(double bound1, T case1, double bound2, T case2, double bound3, T case3, double bound4, T case4, T falseValue = default)
		{
			return
				Bool(bound1) ? case1:
				Bool(bound2) ? case2:
				Bool(bound3) ? case3:
				Bool(bound4) ? case4: falseValue;
		}

		public static T Item<T>(params T[] values)
		{
			return values == null || values.Length == 0 ? default: values[Int(values.Length)];
		}

		public static T Item<T>(IReadOnlyList<T> values)
		{
			return values == null || values.Count == 0 ? default: values[Int(values.Count)];
		}

		public static T Item<T>(IList<T> values)
		{
			return values == null || values.Count == 0 ? default : values[Int(values.Count)];
		}

		public static T Item<T>(List<T> values)
		{
			return values == null || values.Count == 0 ? default : values[Int(values.Count)];
		}

		public static T Item<T>(double p, IReadOnlyList<T> values)
		{
			return values == null || values.Count == 0 || !Bool(p)  ? default: values[Int(values.Count)];
		}

		public static T[] Items<T>(int count, IReadOnlyList<T> values)
		{
			if (count <= 0 || values == null)
				return System.Array.Empty<T>();
			if (values.Count <= count)
				return values.ToArray();

			var result = new T[count];
			var selected = new BitArray(values.Count);
			for (int i = 0; i < result.Length; ++i)
			{
				int j = Int(values.Count);
				while (selected[j])
				{
					j = Int(values.Count);
				}
				selected[j] = true;
				result[i] = values[j];
			}
			return result;
		}
	}
}
