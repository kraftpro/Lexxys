// Lexxys Infrastructural library.
// file: Rand.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Testing;

/// <summary>
/// Extension methods for <see cref="IRand"/> interface.
/// </summary>
public static class RandExtensions
{
	/// <summary>
	/// Returns a random integer value greater or equal to 0 and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd">The random number generator.</param>
	/// <param name="maxValue">Maximum value (exclusive).</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static int NextInt(this IRand rnd, int maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return maxValue == 0 ? 0 : rnd.NextInt() % maxValue;
	}

	/// <summary>
	/// Returns a random integer value greater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd">The random number generator.</param>
	/// <param name="minValue">Minimum value (inclusive).</param>
	/// <param name="maxValue">Maximum value (exclusive).</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is negative or <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
	public static int NextInt(this IRand rnd, int minValue, int maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue == maxValue ? minValue : minValue + rnd.NextInt() % (maxValue - minValue);
	}

	/// <summary>
	/// Returns a random non-negative <see cref="double"/> value less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="maxValue"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is negative or infinity or NaN.</exception>
	public static double NextDouble(this IRand rnd, double maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0 || double.IsNaN(maxValue) || double.IsInfinity(maxValue)) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return rnd.NextDouble() * maxValue;
	}

	/// <summary>
	/// Returns a random <see cref="double"/> value greater or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is negative or infinity or NaN or <paramref name="maxValue"/> is less than <paramref name="minValue"/> or infinity or NaN.</exception>"/>
	public static double NextDouble(this IRand rnd, double minValue, double maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0 || double.IsNaN(minValue) || double.IsInfinity(minValue)) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue || double.IsNaN(maxValue) || double.IsInfinity(maxValue)) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + rnd.NextDouble() * (maxValue - minValue);
	}

	/// <summary>
	/// Returns a non-negative random decimal value in range 0, 0.(9).
	/// </summary>
	/// <param name="rnd"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static decimal NextDecimal(this IRand rnd)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		return new Decimal(rnd.NextInt(268435456), rnd.NextInt(1042612834), rnd.NextInt(542101087), false, 28);
	}

	/// <summary>
	/// Returns a non-negative random decimal value less than <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is negative.</exception>
	public static decimal NextDecimal(this IRand rnd, decimal maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return rnd.NextDecimal() * maxValue;
	}

	/// <summary>
	/// Returns a non-negative random decimal value in the range from <paramref name="minValue"/> to <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is negative or <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
	public static decimal NextDecimal(this IRand rnd, decimal minValue, decimal maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + rnd.NextDecimal() * (maxValue - minValue);
	}

	/// <summary>
	/// Returns a non-negative random long value.
	/// </summary>
	/// <param name="rnd"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static long NextLong(this IRand rnd)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		return (long)(rnd.NextDouble() * long.MaxValue);
	}

	/// <summary>
	/// Returns a non-negative random long value less then <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than zero.</exception>
	public static long NextLong(this IRand rnd, long maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return (long)(rnd.NextDouble() * maxValue);
	}

	/// <summary>
	/// Returns a random long value in the range of <paramref name="minValue"/> and <paramref name="maxValue"/>.
	/// </summary>
	/// <param name="rnd"></param>
	/// <param name="minValue">The inclusive lower bound of the random number returned.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is less than zero or <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
	public static long NextLong(this IRand rnd, long minValue, long maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + (long)(rnd.NextDouble() * (maxValue - minValue));
	}
}