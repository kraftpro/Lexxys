using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Testing;

public static partial class R
{
	/// <summary>
	/// Creates a random integer generator that produces values within the specified inclusive range.
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number range. Must be greater than or equal to 0 and less than or equal to
	/// <paramref name="max"/>.</param>
	/// <param name="max">The inclusive upper bound of the random number range. Must be greater than or equal to <paramref name="min"/>.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;int&gt;</see> that generates random integers greater than or equal to <paramref name="min"/> and less than <paramref name="max"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is less than 0, or if <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	public static RandItem<int> Int(int min, int max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<int>(() => Rand.Int(min, max));
	}

	/// <summary>
	/// Creates a random integer generator that produces values in the range from 0 to the specified maximum value,
	/// exclusive.
	/// </summary>
	/// <param name="max">The exclusive upper bound of the random number to be generated. Must be greater than or equal to 0.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;int&gt;</see> that generates random integers greater than or equal to 0 and less than the specified maximum
	/// value.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if max is less than 0.</exception>
	public static RandItem<int> Int(int max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<int>(() => Rand.Int(max));
	}

	/// <summary>
	/// Creates a random value generator that produces 64-bit integers within the specified inclusive range.
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number range. Must be greater than or equal to 0 and less than or equal to
	/// <paramref name="max"/>.</param>
	/// <param name="max">The exclusive upper bound of the random number range. Must be greater than or equal to <paramref name="min"/>.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;long&gt;</see> that generates random 64-bit integers greater then or equal to <paramref name="min"/> and less then <paramref name="max"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is less than 0, or if <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	public static RandItem<long> Int(long min, long max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<long>(() => Rand.Long(min, max));
	}

	/// <summary>
	/// Creates a random value generator that produces non-negative 64-bit integers less than the specified maximum value.
	/// </summary>
	/// <param name="max">The exclusive upper bound for the random values to generate. Must be greater than or equal to 0.</param>
	/// <returns>A random value generator that produces 64-bit integers in the range [0, max).</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when max is less than 0.</exception>
	public static RandItem<long> Int(long max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<long>(() => Rand.Long(max));
	}

	/// <summary>
	/// Creates a random decimal value generator that produces values within the specified inclusive range.
	/// </summary>
	/// <param name="min">The lower bound of the random decimal values to generate. Must be greater than or equal to 0 and less
	/// than or equal to <paramref name="max"/>.</param>
	/// <param name="max">The upper bound of the random decimal values to generate. Must be greater than or equal to <paramref name="min"/>.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;decimal&gt;</see> that generates random decimal values greater then of equal to <paramref name="min"/> and less then <paramref name="max"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is less than 0, or if <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	public static RandItem<decimal> Dec(decimal min, decimal max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<decimal>(() => Rand.Dec(min, max));
	}

	/// <summary>
	/// Creates a random decimal value generator that produces values in the range from 0 up to the specified maximum
	/// value.
	/// </summary>
	/// <param name="max">The exclusive upper bound for the generated decimal values. Must be greater than or equal to 0.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;decimal&gt;</see> that generates random decimal values greater than or equal to 0 and less than the specified
	/// maximum.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if max is less than 0.</exception>
	public static RandItem<decimal> Dec(decimal max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<decimal>(() => Rand.Dec(max));
	}

	/// <summary>
	/// Creates a random value generator that produces double-precision floating-point numbers within the specified range.
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number range. Must be greater than or equal to 0, finite, and not NaN.</param>
	/// <param name="max">The exclusive upper bound of the random number range. Must be greater than or equal to <paramref name="min"/>,
	/// finite, and not NaN.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;double&gt;</see> that generates random double values greater than or equal to <paramref name="min"/>
	/// and less than <paramref name="max"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is less than 0, is infinite, or is NaN; or if <paramref name="max"/> is less than
	/// <paramref name="min"/>, is infinite, or is NaN.</exception>
	public static RandItem<double> Dbl(double min, double max)
	{
		if (min < 0 || double.IsInfinity(min) || double.IsNaN(min)) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min || double.IsInfinity(max) || double.IsNaN(max)) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<double>(() => Rand.Dbl(min, max));
	}

	/// <summary>
	/// Creates a random value generator that produces double-precision floating-point numbers less than the specified
	/// maximum value.
	/// </summary>
	/// <param name="max">The exclusive upper bound for generated values. Must be a non-negative, finite number.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;double&gt;</see> that generates double values greater than or equal to 0.0 and less than <paramref name="max"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="max"/> is negative, infinity, or not a number (NaN).</exception>
	public static RandItem<double> Dbl(double max)
	{
		if (max < 0 || double.IsInfinity(max) || double.IsNaN(max)) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<double>(() => Rand.Dbl(max));
	}
}
