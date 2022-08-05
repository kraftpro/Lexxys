// Lexxys Infrastructural library.
// file: WatchTimer.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lexxys
{
	public static class WatchTimer
	{
		public static readonly long TicksPerMillisecond;
		public static readonly long TicksPerSecond;
		public static readonly long TicksPerMinute;

		static WatchTimer()
		{
			NativeMethods.QueryPerformanceFrequency(out TicksPerSecond);
			TicksPerMinute = TicksPerSecond * 60;
			TicksPerMillisecond = (TicksPerSecond + 500) / 1000;
		}

		/// <summary>
		/// Initializes a new instance and starts measuring elapsed time.
		/// </summary>
		/// <returns>Object to be used as a parameter in <see cref="Lexxys.WatchTimer.Stop"/> and <see cref="Lexxys.WatchTimer.Query"/> methods.</returns>
		public static long Start()
		{
			NativeMethods.QueryPerformanceCounter(out long now);
			return now;
		}

		/// <summary>
		///  Stops measuring elapsed time for an interval.
		/// </summary>
		/// <param name="timer">Object returned by <see cref="Lexxys.WatchTimer.Start"/>.</param>
		/// <returns>Total elapsed time measured by <paramref name="timer"/>, in microsecond.</returns>
		public static long Stop(long timer)
		{
			NativeMethods.QueryPerformanceCounter(out long now);
			return now - timer;
		}

		/// <summary>
		/// Gets the total elapsed time measured by <paramref name="timer"/>, in microsecond.
		/// </summary>
		/// <param name="timer">Object returned by <see cref="Lexxys.WatchTimer.Start"/>.</param>
		/// <returns>Total elapsed time measured by <paramref name="timer"/>, in microsecond.</returns>
		public static long Query(long timer)
		{
			NativeMethods.QueryPerformanceCounter(out long now);
			return now - timer;
		}

		/// <summary>
		/// Gets the total elapsed time measured by <paramref name="timer"/>.
		/// </summary>
		/// <param name="timer">Object returned by <see cref="Lexxys.WatchTimer.Start"/>.</param>
		/// <returns>Total elapsed time measured by <paramref name="timer"/>.</returns>
		public static TimeSpan Watch(long timer)
		{
			NativeMethods.QueryPerformanceCounter(out long now);
			return TimeSpan.FromTicks(now - timer);
		}

		/// <summary>
		/// Convert QPC ticks to <see cref="System.TimeSpan"/>.
		/// </summary>
		/// <param name="time">Number of QPC ticks. (from <see cref="Stop(long)"/> or <see cref="Query(long)"/>)</param>
		/// <returns><see cref="System.TimeSpan"/> equivalent of <paramref name="time"/> value.</returns>
		public static TimeSpan ToTimeSpan(long time)
		{
			return TicksPerSecond == TimeSpan.TicksPerSecond ? TimeSpan.FromTicks(time): TimeSpan.FromTicks(time * TimeSpan.TicksPerSecond / TicksPerSecond);
		}

		/// <summary>
		/// Convert QPC ticks to number of seconds.
		/// </summary>
		/// <param name="timer">Number of QPC ticks. (from <see cref="Stop(long)"/> or <see cref="Query(long)"/>)</param>
		/// <returns>Number of seconds.</returns>
		public static double ToSeconds(long timer)
		{
			return (double)timer / TicksPerSecond;
		}

		/// <summary>
		/// Format duration time
		/// </summary>
		/// <param name="timer">Timer value</param>
		/// <returns>Formatted string in form: [mm:]ss.ccccc</returns>
		public static string ToString(long timer)
		{
			string sign = timer < 0 ? "-": "";
			timer = Math.Abs(timer);
			long ms = ((timer % TicksPerSecond) * 100000 + TicksPerSecond / 2) / TicksPerSecond;

			return timer >= TicksPerMinute ?
				String.Format(CultureInfo.InvariantCulture, "{0}{1:00}:{2:00}.{3:00000}", sign, timer / TicksPerMinute, timer % TicksPerMinute / TicksPerSecond, ms):
				String.Format(CultureInfo.InvariantCulture, "{0}{1}.{2:00000}", sign, timer / TicksPerSecond, ms);
		}

		/// <summary>
		/// Format duration time
		/// </summary>
		/// <param name="timer">Timer value</param>
		/// <param name="includeMinutes">Add minutes value to output</param>
		/// <returns>Formatted string in form: mm:ss.ccccc or sss.ccccc</returns>
		public static string ToString(long timer, bool includeMinutes)
		{
			string sign = timer < 0 ? "-": "";
			timer = Math.Abs(timer);
			long ms = ((timer % TicksPerSecond) * 100000 + TicksPerSecond / 2) / TicksPerSecond;

			return includeMinutes ?
				String.Format(CultureInfo.InvariantCulture, "{0}{1:00}:{2:00}.{3:00000}", sign, timer / TicksPerMinute, timer % TicksPerMinute / TicksPerSecond, ms):
				String.Format(CultureInfo.InvariantCulture, "{0}{1}.{2:00000}", sign, timer / TicksPerSecond, ms);
		}

		private static class NativeMethods
		{
			[DllImport("kernel32.dll")]
			[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
			public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

			[DllImport("kernel32.dll")]
			[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
			public static extern bool QueryPerformanceFrequency(out long lpFrequency);
		}
	}
}


