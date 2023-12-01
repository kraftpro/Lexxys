// Lexxys Infrastructural library.
// file: DecimalExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Runtime.InteropServices;

namespace Lexxys;

/// <summary>
/// Extension methods for <see cref="Decimal"/> type.
/// </summary>
public static class DecimalExtensions
{
	/// <summary>
	/// Gets an actual precision of the <see cref="Decimal"/> value.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="actual"></param>
	/// <returns></returns>
	public static int GetScale(this decimal value, bool actual = false)
	{
		return (new DecOverlay(actual ? value : value / 1.000000000000000000000000000000m).Flags >> 16) & 31;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct DecOverlay(decimal value)
	{
		[FieldOffset(0)]
		public decimal Value = value;
		[FieldOffset(0)]
		public int Flags;
		[FieldOffset(4)]
		public int Hi;
		[FieldOffset(8)]
		public ulong Lo;
	}

	/// <summary>
	/// Splits the <see cref="decimal"/> value into two baskets according to the the specified <paramref name="ratio"/>.
	/// </summary>
	/// <param name="value">The value to split</param>
	/// <param name="scale"></param>
	/// <param name="ratio">The ratio used to splint the <see cref="decimal"/> value.</param>
	/// <returns></returns>
	public static (decimal, decimal) Split(this decimal value, int scale, double ratio)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);

		if (scale == -1)
			scale = value.GetScale(true);

		var first = Math.Round((decimal)((double)value * ratio), scale);
		return (first, value - first);
	}

	/// <summary>
	/// Splits the <see cref="decimal"/> value into two baskets according to the the specified ratio as <paramref name="numerator"/>/<paramref name="denominator"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="scale"></param>
	/// <param name="numerator">Numerator value of the ratio.</param>
	/// <param name="denominator">Denominator value of the ratio.</param>
	/// <returns></returns>
	public static (decimal, decimal) Split(this decimal value, int scale, int numerator, int denominator)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		if (denominator == 0)
			throw new ArgumentOutOfRangeException(nameof(denominator), denominator, null);

		if (scale == -1)
			scale = value.GetScale(true);

		decimal first = Math.Round(value * numerator / denominator, scale);
		return (first, value - first);
	}

	/// <summary>
	/// Distributes the specified <paramref name="value"/> by two baskets according to their weights.
	/// </summary>
	/// <param name="value">The value to distribute.</param>
	/// <param name="scale">The scale of the result items (-1 to use the same scale as the <paramref name="value"/>).</param>
	/// <param name="basket1">Weight of the basket 1</param>
	/// <param name="basket2">Weight of the basket 2</param>
	/// <returns></returns>
	public static decimal[] Allocate(this decimal value, int scale, double basket1, double basket2)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		if (!(basket1 >= 0) || Double.IsPositiveInfinity(basket1))
			throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
		if (!(basket2 >= 0) || Double.IsPositiveInfinity(basket2))
			throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);

		if (scale == -1)
			scale = value.GetScale(true);

		double total = basket1 + basket2;
		decimal first = Math.Round((decimal)((double)value * basket1 / total), scale);
		return [first, value - first];
	}

	/// <summary>
	/// Distributes the specified <paramref name="value"/> by three baskets according to their weights.
	/// </summary>
	/// <param name="value">The value to distribute.</param>
	/// <param name="scale">The scale of the result items (-1 to use the same scale as the <paramref name="value"/>).</param>
	/// <param name="basket1">Weight of the basket 1</param>
	/// <param name="basket2">Weight of the basket 2</param>
	/// <param name="basket3">Weight of the basket 3</param>
	/// <returns></returns>
	public static decimal[] Allocate(this decimal value, int scale, double basket1, double basket2, double basket3)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		if (!(basket1 >= 0) || Double.IsPositiveInfinity(basket1))
			throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
		if (!(basket2 >= 0) || Double.IsPositiveInfinity(basket2))
			throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);
		if (!(basket3 >= 0) || Double.IsPositiveInfinity(basket3))
			throw new ArgumentOutOfRangeException(nameof(basket3), basket3, null);

		if (scale == -1)
			scale = value.GetScale(true);

		double total = basket1 + basket2 + basket3;

		decimal first = Math.Round((decimal)((double)value * basket1 / total), scale);
		decimal second = Math.Round((decimal)((double)(value - first) * basket2 / (total - basket1)), scale);
		return [first, second, value - first - second];
	}

	/// <summary>
	/// Distributes the specified <paramref name="value"/> by baskets according to their weights.
	/// </summary>
	/// <param name="value">The value to distribute.</param>
	/// <param name="scale">The scale of the result items (-1 to use the same scale as the <paramref name="value"/>).</param>
	/// <param name="baskets">Array of the baskets weights for the <paramref name="value"/> distribution.</param>
	/// <returns></returns>
	public static decimal[] Allocate(this decimal value, int scale, params double[] baskets)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		if (baskets == null)
			throw new ArgumentNullException(nameof(baskets));

		if (baskets.Length < 2)
			return baskets.Length == 0 ? [] : [value];

		if (scale == -1)
			scale = value.GetScale(true);

		double total = 0;
		foreach (double basket in baskets)
		{
			if (!(basket >= 0) || Double.IsPositiveInfinity(basket))
				throw new ArgumentOutOfRangeException(nameof(baskets), basket, null);
			total += basket;
		}
		var result = new decimal[baskets.Length];
		decimal rest = value;
		int n = baskets.Length - 1;
		for (int i = 0; i < n; ++i)
		{
			double basket = baskets[i];
			double dv = (double)rest * basket / total;
			decimal item = Math.Round((decimal)dv, scale);
			rest -= item;
			total -= basket;
			result[i] = item;
		}
		result[n] = rest;
		return result;
	}

	/// <summary>
	/// Distributes the specified <paramref name="value"/> evenly by specified <paramref name="count"/> of baskets.
	/// </summary>
	/// <param name="value">The value to distribute.</param>
	/// <param name="scale">The scale of the result items (-1 to use the same scale as the <paramref name="value"/>).</param>
	/// <param name="count">Number of baskets.</param>
	public static decimal[] Distribute(this decimal value, int scale, int count)
	{
		if (scale is < -1 or > 28)
			throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, null);

		if (count <= 1)
			return count == 0 ? []: [value];

		if (scale == -1)
			scale = value.GetScale(true);

		var result = new decimal[count];
		var rest = value;
		int n = result.Length - 1;
		for (int i = 0; i < n; ++i)
		{
			var item = Math.Round(rest / count, scale);
			rest -= item;
			--count;
			result[i] = item;
		}
		result[n] = rest;
		return result;
	}
}


