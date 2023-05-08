// Lexxys Infrastructural library.
// file: DateTimeExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public static class DateTimeExtensions
{
	private static readonly long __unixEpochTicks = new DateTime(1970, 1, 1).Ticks;

	public static long ToUnixTimeTicks(this DateTime value) => (value.ToUniversalTime().Ticks - __unixEpochTicks);

	public static long ToUnixTimeMilliseconds(this DateTime value) => value.ToUnixTimeTicks() / TimeSpan.TicksPerMillisecond;

	public static long ToUnixTime(this DateTime value) => value.ToUnixTimeTicks() / TimeSpan.TicksPerSecond;

	public static DateTime FromUnixTimeTicks(this long value) => new DateTime(__unixEpochTicks + value, DateTimeKind.Utc).ToLocalTime();

	public static DateTime FromUnixTimeMilliseconds(this long value) => (value * TimeSpan.TicksPerMillisecond).FromUnixTimeTicks();

	public static DateTime FromUnixTime(this long value) => (value * TimeSpan.TicksPerSecond).FromUnixTimeTicks();

#if NET6_0_OR_GREATER
	public static DateOnly ToDateOnly(this DateTime value) => DateOnly.FromDateTime(value);
#endif
}


