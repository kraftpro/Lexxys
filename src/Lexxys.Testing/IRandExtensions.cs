// Lexxys Infrastructural library.
// file: Rand.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys.Testing;

public static class RandExtensions
{
	public static int NextInt(this IRand rnd, int max)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return max == 0 ? 0 : rnd.NextInt() % max;
	}

	public static int NextInt(this IRand rnd, int min, int max)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return min == max ? min : min + rnd.NextInt() % (max - min);
	}

	public static double NextDouble(this IRand rnd, double maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return rnd.NextDouble() * maxValue;
	}

	public static double NextDouble(this IRand rnd, double minValue, double maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return minValue + rnd.NextDouble() * (maxValue - minValue);
	}

	public static decimal NextDecimal(this IRand rnd)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return new Decimal(rnd.NextInt(268435456), rnd.NextInt(1042612834), rnd.NextInt(542101087), false, 28);
	}

	public static decimal NextDecimal(this IRand rnd, decimal maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return rnd.NextDecimal() * maxValue;
	}

	public static decimal NextDecimal(this IRand rnd, decimal minValue, decimal maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return minValue + rnd.NextDecimal() * (maxValue - minValue);
	}

	public static long NextLong(this IRand rnd)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return (long)(rnd.NextDouble() * long.MaxValue);
	}

	public static long NextLong(this IRand rnd, long maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return (long)(rnd.NextDouble() * maxValue);
	}

	public static long NextLong(this IRand rnd, long minValue, long maxValue)
	{
		if (rnd is null)
			throw new ArgumentNullException(nameof(rnd));
		return minValue + (long)(rnd.NextDouble() * (maxValue - minValue));
	}
}