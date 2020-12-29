// Lexxys Infrastructural library.
// file: DecimalExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	public static class DecimalExtensions
	{
		/// <summary>
		/// Gets an actual precision of the <see cref="Decimal"/> value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int GetScale(this decimal value)
		{
			return (int)((uint)(decimal.GetBits(value / 1.000000000000000000000000000000m)[3])>>16 & 31);
		}
	}
}


