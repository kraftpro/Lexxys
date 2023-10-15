// Lexxys Infrastructural library.
// file: Rand.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;

#pragma warning disable CA1720 // Identifier contains type name

namespace Lexxys.Testing;

/// <summary>
/// Static class for random values generation.
/// </summary>
public static class Rand
{
	private static IRand __rnd = new RndKnuth();

	/// <summary>
	/// Resets the random number generator with the specified <paramref name="seed"/>.
	/// </summary>
	/// <param name="seed">Seed value for the random number generator.</param>
	public static void Reset(int seed) => __rnd.Reset(seed);

	/// <summary>
	/// Sets the random number generator.
	/// </summary>
	/// <param name="rnd"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void Reset(IRand rnd) => __rnd = rnd ?? throw new ArgumentNullException(nameof(rnd));

	/// <summary>
	/// Returns an array of random bytes.
	/// </summary>
	/// <param name="length">Size of the array.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static byte[] Bits(int length)
	{
		if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, null);
		if (length == 0)
			return Array.Empty<byte>();
		byte[] result = new byte[length];
		__rnd.NextBytes(result);
		return result;
	}

	/// <summary>
	/// Returns a random integer value grater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static int Int(int minValue, int maxValue) => __rnd.NextInt(minValue, maxValue);

	/// <summary>
	/// Returns a random integer value greater or equal to zero and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static int Int(int maxValue) => __rnd.NextInt(maxValue);

	/// <summary>
	/// Returns a random integer value.
	/// </summary>
	/// <returns></returns>
	public static int Int() => __rnd.NextInt();

	/// <summary>
	/// Returns a random long value grater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">the exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static long Long(long minValue, long maxValue) => __rnd.NextLong(minValue, maxValue);

	/// <summary>
	/// Returns a random long value greater or equal to zero and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="maxValue">the exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static long Long(long maxValue) => __rnd.NextLong(maxValue);

	/// <summary>
	/// Returns non-negative random long value.
	/// </summary>
	/// <returns></returns>
	public static long Long() => __rnd.NextLong();

	/// <summary>
	/// Returns random boolean value.
	/// </summary>
	/// <param name="bound">Probability of true value.</param>
	/// <returns></returns>
	public static bool Bool(double bound = 0.5) => __rnd.NextDouble() < bound;

	/// <summary>
	/// Returns random double greater or equal to zero and less than one.
	/// </summary>
	/// <returns></returns>
	public static double Dbl() => __rnd.NextDouble();

	/// <summary>
	/// Returns random double value grater or equal to zero and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static double Dbl(double maxValue) => __rnd.NextDouble(maxValue);

	/// <summary>
	/// Returns random double value grater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static double Dbl(double minValue, double maxValue) => __rnd.NextDouble(minValue, maxValue);

	/// <summary>
	/// Returns random decimal value in range 0..0.9(28)
	/// </summary>
	/// <returns></returns>
	public static decimal Dec() => __rnd.NextDecimal();

	/// <summary>
	/// Returns random decimal value grater or equal to zero and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static decimal Dec(decimal maxValue) => __rnd.NextDecimal(maxValue);

	/// <summary>
	/// Returns random decimal value grater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	public static decimal Dec(decimal minValue, decimal maxValue) => __rnd.NextDecimal(minValue, maxValue);

	/// <summary>
	/// Returns <paramref name="trueValue"/> with probability <paramref name="bound"/> and <paramref name="falseValue"/> otherwise.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="bound">The event probability.</param>
	/// <param name="trueValue">The value to return if the event occurs.</param>
	/// <param name="falseValue">The value to return if the event does not occur.</param>
	/// <returns></returns>
	public static T Case<T>(double bound, T trueValue, T falseValue) => Bool(bound) ? trueValue : falseValue;

	/// <summary>
	/// Returns <paramref name="case1"/> with probability <paramref name="bound1"/>, <paramref name="case2"/> with probability <paramref name="bound2"/> and <paramref name="falseValue"/> otherwise.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="bound1">The probability of the first event.</param>
	/// <param name="case1">The value to return if the first event occurs.</param>
	/// <param name="bound2">The probability of the second event.</param>
	/// <param name="case2">The value to return if the second event occurs.</param>
	/// <param name="falseValue">The value to return if the events do not occur.</param>
	/// <returns></returns>
	public static T Case<T>(double bound1, T case1, double bound2, T case2, T falseValue) =>
		Bool(bound1) ? case1 :
		Bool(bound2) ? case2 : falseValue;

	/// <summary>
	/// Returns <paramref name="case1"/> with probability <paramref name="bound1"/>, <paramref name="case2"/> with probability <paramref name="bound2"/>, <paramref name="case3"/> with probability <paramref name="bound3"/> and <paramref name="falseValue"/> otherwise.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="bound1">The probability of the first event.</param>
	/// <param name="case1">The value to return if the first event occurs.</param>
	/// <param name="bound2">The probability of the second event.</param>
	/// <param name="case2">The value to return if the second event occurs.</param>
	/// <param name="bound3">The probability of the third event.</param>
	/// <param name="case3">The value to return if the third event occurs.</param>
	/// <param name="falseValue">The value to return if the events do not occur.</param>
	/// <returns></returns>
	public static T Case<T>(double bound1, T case1, double bound2, T case2, double bound3, T case3, T falseValue) =>
		Bool(bound1) ? case1 :
		Bool(bound2) ? case2 :
		Bool(bound3) ? case3 : falseValue;

	/// <summary>
	/// Returns <paramref name="case1"/> with probability <paramref name="bound1"/>, <paramref name="case2"/> with probability <paramref name="bound2"/>, <paramref name="case3"/> with probability <paramref name="bound3"/>, <paramref name="case4"/> with probability <paramref name="bound4"/> and <paramref name="falseValue"/> otherwise.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="bound1">The probability of the first event.</param>
	/// <param name="case1">The value to return if the first event occurs.</param>
	/// <param name="bound2">The probability of the second event.</param>
	/// <param name="case2">The value to return if the second event occurs.</param>
	/// <param name="bound3">The probability of the third event.</param>
	/// <param name="case3">The value to return if the third event occurs.</param>
	/// <param name="bound4">The probability of the fourth event.</param>
	/// <param name="case4">The value to return if the fourth event occurs.</param>
	/// <param name="falseValue">The value to return if the events do not occur.</param>
	public static T Case<T>(double bound1, T case1, double bound2, T case2, double bound3, T case3, double bound4, T case4, T falseValue) =>
		Bool(bound1) ? case1 :
		Bool(bound2) ? case2 :
		Bool(bound3) ? case3 :
		Bool(bound4) ? case4 : falseValue;

	/// <summary>
	/// Returns a random element from the specified <paramref name="values"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="values">The items to choose from.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T Item<T>(params T[] values)
	{
		if (values is not { Length: >0 }) throw new ArgumentNullException(nameof(values));
		return values[Int(values.Length)];
	}

	/// <summary>
	/// Returns a random element from the specified <paramref name="values"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="values">The items to choose from.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T Item<T>(IReadOnlyList<T> values)
	{
		if (values is not { Count: >0 }) throw new ArgumentNullException(nameof(values));
		return values[Int(values.Count)];
	}

	/// <summary>
	/// Returns a random element from the specified <paramref name="values"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="values">The items to choose from.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T Item<T>(IList<T> values)
	{
		if (values is not { Count: >0 }) throw new ArgumentNullException(nameof(values));
		return values[Int(values.Count)];
	}

	/// <summary>
	/// Returns a random element from the specified <paramref name="values"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="values">The items to choose from.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T Item<T>(List<T> values)
	{
		if (values is not { Count: >0 }) throw new ArgumentNullException(nameof(values));
		return values[Int(values.Count)];
	}

	/// <summary>
	/// Returns a random <paramref name="count"/> or less elements from the specified <paramref name="values"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="count">Number of items to return.</param>
	/// <param name="values">The items to choose from.</param>
	/// <returns></returns>
	public static T[] Items<T>(int count, IReadOnlyList<T>? values)
	{
		if (count <= 0 || values == null)
			return Array.Empty<T>();
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

