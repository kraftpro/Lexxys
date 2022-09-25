// Lexxys Infrastructural library.
// file: DecimalExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Runtime.InteropServices;

namespace Lexxys
{
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
		private struct DecOverlay
		{
			[FieldOffset(0)]
			public decimal Value;
			[FieldOffset(0)]
			public int Flags;
			[FieldOffset(4)]
			public int Hi;
			[FieldOffset(8)]
			public ulong Lo;

			public DecOverlay(decimal value)
			{
				Value = value;
			}
		}

		/// <summary>
		/// Splits the <see cref="decimal"/> value into two baskets according to the the specified <paramref name="ratio"/>.
		/// </summary>
		/// <param name="value">The value to split</param>
		/// <param name="scale"></param>
		/// <param name="ratio">The ratio osed to splint the <see cref="decimal"/> value.</param>
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

			var first = Math.Round(value * numerator / denominator, scale);
			return (first, value - first);
		}

		/// <summary>
		/// Distribute the money value by two baskets according to their weights.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="scale"></param>
		/// <param name="basket1">Weight of the basket 1</param>
		/// <param name="basket2">Weight of the basket 2</param>
		/// <returns></returns>
		public static decimal[] Allocate(this decimal value, int scale, double basket1, double basket2)
		{
			if (scale is < -1 or > 28)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
			if (!(basket1 >= 0) || double.IsPositiveInfinity(basket1))
				throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
			if (!(basket2 >= 0) || double.IsPositiveInfinity(basket2))
				throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);

			if (scale == -1)
				scale = value.GetScale(true);

			double total = basket1 + basket2;
			var first = Math.Round((decimal)((double)value * basket1 / total), scale);
			var second = value - first;
			return new[] { first, second };
		}

		/// <summary>
		/// Distribute the money value by three baskets according to their weights.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="scale"></param>
		/// <param name="basket1">Weight of the basket 1</param>
		/// <param name="basket2">Weight of the basket 2</param>
		/// <param name="basket3">Weight of the basket 3</param>
		/// <returns></returns>
		public static decimal[] Allocate(this decimal value, int scale, double basket1, double basket2, double basket3)
		{
			if (scale is < -1 or > 28)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
			if (!(basket1 >= 0) || double.IsPositiveInfinity(basket1))
				throw new ArgumentOutOfRangeException(nameof(basket1), basket1, null);
			if (!(basket2 >= 0) || double.IsPositiveInfinity(basket2))
				throw new ArgumentOutOfRangeException(nameof(basket2), basket2, null);
			if (!(basket3 >= 0) || double.IsPositiveInfinity(basket3))
				throw new ArgumentOutOfRangeException(nameof(basket3), basket3, null);

			if (scale == -1)
				scale = value.GetScale(true);

			double total = basket1 + basket2 + basket3;

			var first = Math.Round((decimal)((double)value * basket1 / total), scale);
			var second = Math.Round((decimal)((double)(value - first) * basket2 / (total - basket1)), scale);
			var third = value - first - second;
			return new[] { first, second, third };
		}

		/// <summary>
		/// Distribute the money value by baskets according to their weights.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="scale"></param>
		/// <param name="baskets">Array of the baskets weights</param>
		/// <returns></returns>
		public static decimal[] Allocate(this decimal value, int scale, params double[] baskets)
		{
			if (scale is < -1 or > 28)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
			if (baskets == null)
				throw new ArgumentNullException(nameof(baskets));

			if (baskets.Length < 2)
				return baskets.Length == 0 ? Array.Empty<decimal>() : new[] { value };

			if (scale == -1)
				scale = value.GetScale(true);

			double total = 0;
			for (int i = 0; i < baskets.Length; ++i)
			{
				var basket = baskets[i];
				if (!(basket >= 0) || double.IsPositiveInfinity(basket))
					throw new ArgumentOutOfRangeException($"{nameof(baskets)}[{i}]", basket, null);
				total += basket;
			}
			var result = new decimal[baskets.Length];
			var rest = value;
			for (int i = 0; i < baskets.Length - 1; ++i)
			{
				double dv = (double)rest * baskets[i] / total;
				var item = Math.Round((decimal)dv, scale);
				rest -= item;
				total -= baskets[i];
				result[i] = item;
			}
			result[result.Length - 1] = rest;
			return result;
		}

		/// <summary>
		/// Distributes the money value evenly by specified <paramref name="count"/> of baskets.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="scale"></param>
		/// <param name="count">Number of buskets.</param>
		public static decimal[] Distribute(this decimal value, int scale, int count)
		{
			if (scale is < -1 or > 28)
				throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, null);

			if (count < 2)
				return count == 0 ? Array.Empty<decimal>(): new[] { value };

			if (scale == -1)
				scale = value.GetScale(true);

			var result = new decimal[count];
			var rest = value;
			for (int i = 0; i < result.Length - 1; ++i)
			{
				var item = Math.Round(rest / count, scale);
				rest -= item;
				--count;
				result[i] = item;
			}
			result[result.Length - 1] = rest;
			return result;
		}
	}
}


