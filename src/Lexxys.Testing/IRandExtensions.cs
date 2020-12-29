using System;

namespace Lexxys.Testing
{
	public static class IRandExtensions
	{
		public static int NextInt(this IRand rnd, int max)
		{
			return max == 0 ? 0 : rnd.NextInt() % max;
		}

		public static int NextInt(this IRand rnd, int min, int max)
		{
			return min == max ? min : min + rnd.NextInt() % (max - min);
		}

		public static double NextDouble(this IRand rnd, double maxValue)
		{
			return rnd.NextDouble() * maxValue;
		}

		public static double NextDouble(this IRand rnd, double minValue, double maxValue)
		{
			return minValue + rnd.NextDouble() * (maxValue - minValue);
		}

		public static decimal NextDecimal(this IRand rnd)
		{
			return new Decimal(rnd.NextInt(268435456), rnd.NextInt(1042612834), rnd.NextInt(542101087), false, 28);
		}

		public static decimal NextDecimal(this IRand rnd, decimal maxValue)
		{
			return rnd.NextDecimal() * maxValue;
		}

		public static decimal NextDecimal(this IRand rnd, decimal minValue, decimal maxValue)
		{
			return minValue + rnd.NextDecimal() * (maxValue - minValue);
		}

		public static long NextLong(this IRand rnd)
		{
			return (long)(rnd.NextDouble() * long.MaxValue);
		}

		public static long NextLong(this IRand rnd, long maxValue)
		{
			return (long)(rnd.NextDouble() * maxValue);
		}

		public static long NextLong(this IRand rnd, long minValue, long maxValue)
		{
			return minValue + (long)(rnd.NextDouble() * (maxValue - minValue));
		}
	}
}