// Lexxys Infrastructural library.
// file: DateTimeExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	public static class DateTimeExtensions
	{
		private static readonly long __unixEpochTicks = new DateTime(1970, 1, 1).Ticks;

		public static long ToUnixTime(this DateTime value)
		{
			return (value.ToUniversalTime().Ticks - __unixEpochTicks) / TimeSpan.TicksPerSecond;
		}

		public static long ToUnixTimeMilliseconds(this DateTime value)
		{
			return (value.ToUniversalTime().Ticks - __unixEpochTicks) / TimeSpan.TicksPerMillisecond;
		}

		public static long ToUnixTimeTicks(this DateTime value)
		{
			return (value.ToUniversalTime().Ticks - __unixEpochTicks);
		}

		public static DateTime FromUnixTime(this long value)
		{
			return new DateTime(__unixEpochTicks + value * TimeSpan.TicksPerSecond, DateTimeKind.Utc).ToLocalTime();
		}

		public static DateTime FromUnixTimeMilliseconds(this long value)
		{
			return new DateTime(__unixEpochTicks + value * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc).ToLocalTime();
		}

		public static DateTime FromUnixTimeTicks(this long value)
		{
			return new DateTime(__unixEpochTicks + value, DateTimeKind.Utc).ToLocalTime();
		}
	}
}


