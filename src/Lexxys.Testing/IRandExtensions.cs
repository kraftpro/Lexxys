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
	/// Returns a non-negative random integer that is less than the specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the random integer. Cannot be null.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to 0.</param>
	/// <returns>A 32-bit signed integer greater than or equal to 0 and less than <paramref name="maxValue"/>. Returns 0 if
	/// <paramref name="maxValue"/> is 0.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxValue"/> is less than 0.</exception>
	public static int NextInt32(this IRand rnd, int maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return maxValue == 0 ? 0: rnd.NextInt32() % maxValue;
	}

	/// <summary>
	/// Returns a random 32-bit integer that is greater than or equal to the specified minimum value and less than the
	/// specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the random value. Cannot be null.</param>
	/// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be greater than or equal to 0 and less than or
	/// equal to <paramref name="maxValue"/>.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to <paramref
	/// name="minValue"/>.</param>
	/// <returns>A 32-bit integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>. If
	/// <paramref name="minValue"/> equals <paramref name="maxValue"/>, returns <paramref name="minValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minValue"/> is less than 0, or if <paramref name="maxValue"/> is less than <paramref
	/// name="minValue"/>.</exception>
	public static int NextInt32(this IRand rnd, int minValue, int maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue == maxValue ? minValue: minValue + rnd.NextInt32() % (maxValue - minValue);
	}

	/// <summary>
	/// Returns a random double-precision floating-point number that is greater than or equal to 0.0 and less than the
	/// specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the value. Cannot be null.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be a finite, non-negative number.</param>
	/// <returns>A double-precision floating-point number greater than or equal to 0.0 and less than <paramref name="maxValue"/>. If
	/// <paramref name="maxValue"/> is 0, the method returns 0.0.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxValue"/> is negative, NaN, or infinite.</exception>
	public static double NextDouble(this IRand rnd, double maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0 || double.IsNaN(maxValue) || double.IsInfinity(maxValue)) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return rnd.NextDouble() * maxValue;
	}

	/// <summary>
	/// Returns a random double-precision floating-point number within the specified range.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the value. Cannot be null.</param>
	/// <param name="minValue">The inclusive lower bound of the random number returned. Must be a finite, non-negative number.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number returned. Must be a finite number greater than or equal to <paramref name="minValue"/>.</param>
	/// <returns>A double-precision floating-point number greater than or equal to <paramref name="minValue"/>, and less than
	/// <paramref name="maxValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minValue"/> is negative, not finite, or if <paramref name="maxValue"/> is less than
	/// <paramref name="minValue"/>, not finite, or NaN.</exception>
	public static double NextDouble(this IRand rnd, double minValue, double maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0 || double.IsNaN(minValue) || double.IsInfinity(minValue)) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue || double.IsNaN(maxValue) || double.IsInfinity(maxValue)) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + rnd.NextDouble() * (maxValue - minValue);
	}

	/// <summary>
	/// Generates a random decimal number with the specified number of decimal places.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the decimal value. Cannot be null.</param>
	/// <param name="scale">The number of decimal places for the generated value. Must be between 0 and 28. Defaults to 0.</param>
	/// <returns>A random decimal number with the specified scale.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="scale"/> is less than 0 or greater than 28.</exception>
	public static decimal NextDecimal(this IRand rnd, int scale = 0)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (scale is <0 or >28) throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		return new Decimal(rnd.NextInt32(268435456), rnd.NextInt32(1042612834), rnd.NextInt32(542101087), false, (byte)(28 - scale)) / 1m;
	}

	/// <summary>
	/// Returns a non-negative random decimal number that is less than the specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the decimal value. Cannot be null.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to 0.</param>
	/// <returns>A decimal number greater than or equal to 0.0 and less than <paramref name="maxValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxValue"/> is less than 0.</exception>
	public static decimal NextDecimal(this IRand rnd, decimal maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return rnd.NextDecimal() * maxValue;
	}

	/// <summary>
	/// Returns a random decimal number that is greater than or equal to the specified minimum value and less than the
	/// specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the decimal value. Cannot be null.</param>
	/// <param name="minValue">The inclusive lower bound of the random number to be generated.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to <paramref
	/// name="minValue"/>.</param>
	/// <returns>A decimal number greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
	public static decimal NextDecimal(this IRand rnd, decimal minValue, decimal maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + rnd.NextDecimal() * (maxValue - minValue);
	}

	/// <summary>
	/// Returns a non-negative random 64-bit integer that is less than the specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the random value. Cannot be null.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to 0.</param>
	/// <returns>A 64-bit integer greater than or equal to 0 and less than <paramref name="maxValue"/>. Returns 0 if <paramref name="maxValue"/> is 0.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxValue"/> is less than 0.</exception>
	public static long NextInt64(this IRand rnd, long maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return maxValue == 0 ? 0: rnd.NextInt64() % maxValue;
	}

	/// <summary>
	/// Returns a random 64-bit signed integer that is greater than or equal to the specified minimum value and less than
	/// the specified maximum value.
	/// </summary>
	/// <param name="rnd">The random number generator to use for producing the random value. Cannot be null.</param>
	/// <param name="minValue">The inclusive lower bound of the random number to be generated. Must be greater than or equal to 0 and less than or
	/// equal to <paramref name="maxValue"/>.</param>
	/// <param name="maxValue">The exclusive upper bound of the random number to be generated. Must be greater than or equal to <paramref name="minValue"/>.</param>
	/// <returns>A 64-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref
	/// name="maxValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="rnd"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minValue"/> is less than 0, or if <paramref name="maxValue"/> is less than <paramref
	/// name="minValue"/>.</exception>
	public static long NextInt64(this IRand rnd, long minValue, long maxValue)
	{
		if (rnd is null) throw new ArgumentNullException(nameof(rnd));
		if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), minValue, null);
		if (maxValue < minValue) throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, null);
		return minValue + rnd.NextInt64(maxValue - minValue);
	}
}