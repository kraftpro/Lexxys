using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

#nullable enable

namespace Lexxys.Xml
{
	public static partial class XmlTools
	{
		#region Parse Primitives

		public static byte GetByte(string? value)
		{
			return Byte.TryParse(value, out byte result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static byte GetByte(string? value, byte defaultValue)
		{
			return Byte.TryParse(value, out byte result) ? result : defaultValue;
		}

		public static byte? GetByte(string? value, byte? defaultValue)
		{
			return Byte.TryParse(value, out byte result) ? result : defaultValue;
		}

		public static sbyte GetSByte(string? value)
		{
			return SByte.TryParse(value, out sbyte result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static sbyte GetSByte(string? value, sbyte defaultValue)
		{
			return SByte.TryParse(value, out sbyte result) ? result : defaultValue;
		}

		public static sbyte? GetSByte(string? value, sbyte? defaultValue)
		{
			return SByte.TryParse(value, out sbyte result) ? result : defaultValue;
		}

		public static short GetInt16(string? value)
		{
			return Int16.TryParse(value, out short result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static short GetInt16(string? value, short defaultValue)
		{
			return Int16.TryParse(value, out short result) ? result : defaultValue;
		}

		public static short? GetInt16(string? value, short? defaultValue)
		{
			return Int16.TryParse(value, out short result) ? result : defaultValue;
		}

		public static ushort GetUInt16(string? value)
		{
			return UInt16.TryParse(value, out ushort result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static ushort GetUInt16(string? value, ushort defaultValue)
		{
			return UInt16.TryParse(value, out ushort result) ? result : defaultValue;
		}

		public static ushort? GetUInt16(string? value, ushort? defaultValue)
		{
			return UInt16.TryParse(value, out ushort result) ? result : defaultValue;
		}

		public static int GetInt32(string? value)
		{
			return Int32.TryParse(value, out int result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static int GetInt32(string? value, int defaultValue)
		{
			return Int32.TryParse(value, out int result) ? result : defaultValue;
		}

		public static int? GetInt32(string? value, int? defaultValue)
		{
			return Int32.TryParse(value, out int result) ? result : defaultValue;
		}

		public static int GetInt32(string? value, int defaultValue, int minValue, int maxValue)
		{
			return !Int32.TryParse(value, out int result) ? defaultValue : result < minValue ? minValue : result > maxValue ? maxValue : result;
		}

		public static uint GetUInt32(string? value)
		{
			return UInt32.TryParse(value, out uint result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static uint GetUInt32(string? value, uint defaultValue)
		{
			return UInt32.TryParse(value, out uint result) ? result : defaultValue;
		}

		public static uint? GetUInt32(string? value, uint? defaultValue)
		{
			return UInt32.TryParse(value, out uint result) ? result : defaultValue;
		}

		public static long GetInt64(string? value)
		{
			return Int64.TryParse(value, out long result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static long GetInt64(string? value, long defaultValue)
		{
			return Int64.TryParse(value, out long result) ? result : defaultValue;
		}

		public static long? GetInt64(string? value, long? defaultValue)
		{
			return Int64.TryParse(value, out long result) ? result : defaultValue;
		}

		public static ulong GetUInt64(string? value)
		{
			return UInt64.TryParse(value, out ulong result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static ulong GetUInt64(string? value, ulong defaultValue)
		{
			return UInt64.TryParse(value, out ulong result) ? result : defaultValue;
		}

		public static ulong? GetUInt64(string? value, ulong? defaultValue)
		{
			return UInt64.TryParse(value, out ulong result) ? result : defaultValue;
		}

		public static float GetSingle(string? value)
		{
			return Single.TryParse(value, out float result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static float GetSingle(string? value, float defaultValue)
		{
			return Single.TryParse(value, out float result) ? result : defaultValue;
		}

		public static float? GetSingle(string? value, float? defaultValue)
		{
			return Single.TryParse(value, out float result) ? result : defaultValue;
		}

		public static double GetDouble(string? value)
		{
			return Double.TryParse(value, out double result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static double GetDouble(string? value, double defaultValue)
		{
			return Double.TryParse(value, out double result) ? result : defaultValue;
		}

		public static double? GetDouble(string? value, double? defaultValue)
		{
			return Double.TryParse(value, out double result) ? result : defaultValue;
		}

		public static decimal GetDecimal(string? value)
		{
			return Decimal.TryParse(value, out decimal result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static decimal GetDecimal(string? value, decimal defaultValue)
		{
			return Decimal.TryParse(value, out decimal result) ? result : defaultValue;
		}

		public static decimal? GetDecimal(string? value, decimal? defaultValue)
		{
			return Decimal.TryParse(value, out decimal result) ? result : defaultValue;
		}

		public static char GetChar(string? value)
		{
			return TryGetChar(value) ?? throw new FormatException(SR.FormatException(value));
		}

		public static char GetChar(string? value, char defaultValue)
		{
			return TryGetChar(value) ?? defaultValue;
		}

		public static char? GetChar(string? value, char? defaultValue)
		{
			return TryGetChar(value) ?? defaultValue;
		}

		private static char? TryGetChar(string? value)
		{
			if (value == null)
				return null;
			if (value.Length != 1 && (value = value.Trim()).Length != 1)
				return null;
			return value[0];
		}

		private static bool TryGetChar(string value, out char result)
		{
			if (value == null)
			{
				result = '\0';
				return false;
			}
			if (value.Length != 1)
			{
				value = value.Trim();
				if (value.Length != 1)
				{
					result = ' ';
					return false;
				}
			}
			result = value[0];
			return true;
		}


		public static TimeSpan GetTimeSpan(string? value)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static TimeSpan GetTimeSpan(string? value, TimeSpan defaultValue)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result : defaultValue;
		}

		public static TimeSpan? GetTimeSpan(string? value, TimeSpan? defaultValue)
		{
			return TryGetTimeSpan(value, out TimeSpan result) ? result : defaultValue;
		}

		public static TimeSpan GetTimeSpan(string? value, TimeSpan defaultValue, TimeSpan minValue, TimeSpan maxValue)
		{
			return !TryGetTimeSpan(value, out TimeSpan result) ? defaultValue : result < minValue ? minValue : result > maxValue ? maxValue : result;
		}

		/// <summary>
		/// Converts string representation of time span into <see cref="System.TimeSpan"/>.
		///	  syntax: [P] [days 'D'] [T] [hours 'H'][minutes 'M'][seconds 'S'][milliseconds 'MS']
		///     [[[days] hours:]minutes:]seconds [AM/PM]
		/// </summary>
		/// <param name="value">A string to convert.</param>
		/// <param name="result">result of conversion.</param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryGetTimeSpan(string? value, out TimeSpan result)
		{
			result = new TimeSpan();
			if (value == null || value.Length == 0)
				return false;

			var text = new Tokenizer.CharStream(value, false, 1);
			var last = new NumberScale();
			var temp = new TimeSpan();

			bool MatchNumber(char ch, int pos) => last.Append(ch, pos);
			static bool Space(char c) => c <= ' ';

			if (text[0] == 'P' || text[0] == 'p')
				text.Forward(1, Space);
			else
				text.Forward(Space);
			text.Match(MatchNumber);
			if (!last.HasValue)
			{
				if (last.Size != 0)
					return false;
				if (text[0] != 'T' && text[0] != 't')
					return false;
			}
			text.Forward(last.Size, Space);

			if (text.Eof)
			{
				result = last.Time(TimeSpan.TicksPerSecond);
				return true;
			}
			char c0 = text[0];
			if (c0 == ':' || (c0 >= '0' && c0 <= '9'))
				goto ShortFormat;

			if (c0 == 'D' || c0 == 'd')
			{
				temp = last.Time(TimeSpan.TicksPerDay);
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				text.Forward(last.Size, Space);
				c0 = text[0];
				if (!last.HasValue)
				{
					if (last.Size != 0)
						return false;
					if (c0 != 'T' && c0 != 't')
						return false;
				}
			}

			if (c0 == 'T' || c0 == 't')
			{
				if (last.HasValue)
					return false;
				text.Forward(1, Space);
				if (text.Eof)
					return false;
				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if (c0 == 'H' || c0 == 'h')
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerHour));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if ((c0 == 'M' || c0 == 'm') && !(text[1] == 'S' || text[1] == 's'))
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerMinute));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if (c0 == 'S' || c0 == 's')
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerSecond));
				text.Forward(1, Space);
				if (text.Eof)
				{
					result = temp;
					return true;
				}

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				c0 = text[0];
			}

			if ((c0 == 'M' || c0 == 'm') && (text[1] == 'S' || text[1] == 's'))
			{
				temp = temp.Add(last.Time(TimeSpan.TicksPerMillisecond));
				text.Forward(2, Space);
			}
			else
			{
				return false;
			}

			if (!text.Eof)
				return false;

			result = temp;
			return true;


		ShortFormat:

			TimeSpan days;
			NumberScale part1;
			bool hoursRequired = false;

			// [[days] hours:]minutes:seconds
			if (text[0] == ':')
			{
				// [hours:]minutes:seconds
				days = new TimeSpan();
				part1 = last;
			}
			else // text[0] >= '0' && text[0] <= '9'
			{
				// days hours:minutes:seconds

				hoursRequired = true;
				days = last.Time(TimeSpan.TicksPerDay);

				last.Reset();
				text.Match(MatchNumber);
				if (!last.HasValue)
					return false;
				text.Forward(last.Size, Space);
				if (text[0] != ':')
					return false;
				part1 = last;

				// {days} {hours} :minutes:seconds
			}

			// {days} {hours} :minutes:seconds
			// {minutes} :seconds
			text.Forward(1, Space);
			last.Reset();
			text.Match(MatchNumber);
			if (!last.HasValue)
				return false;
			text.Forward(last.Size, Space);

			if (text.Eof)
			{
				if (hoursRequired)
					return false;
				if (part1.Point)
					// days.hours:minutes
					result = new TimeSpan(part1.Left * TimeSpan.TicksPerDay)
						.Add(new TimeSpan(part1.Right * TimeSpan.TicksPerHour))
						.Add(last.Time(TimeSpan.TicksPerMinute));
				else
					// minutes:seconds
					result = part1.Time(TimeSpan.TicksPerMinute)
						.Add(last.Time(TimeSpan.TicksPerSecond));
				return true;
			}
			if (text[0] != ':')
				return false;

			NumberScale part2 = last;
			// :seconds
			text.Forward(1, Space);
			last.Reset();
			text.Match(MatchNumber);
			if (!last.HasValue)
				return false;
			text.Forward(last.Size, Space);
			bool pm = false;
			if (!text.Eof)
			{
				if (text.Length != 2)
					return false;
				string ap = text.Substring(0, 2);
				if (String.Equals(ap, "PM", StringComparison.OrdinalIgnoreCase))
					pm = true;
				else if (!String.Equals(ap, "AM", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (!hoursRequired && part1.Point) // days.hours:minutes:seconds
			{
				days = new TimeSpan(part1.Left * TimeSpan.TicksPerDay);
				part1 = new NumberScale(part1.Right, 0, part1.Size - part1.Scale - 1, 0);
			}

			result = days.Add(part1.Time(TimeSpan.TicksPerHour)).Add(part2.Time(TimeSpan.TicksPerMinute)).Add(last.Time(TimeSpan.TicksPerSecond));
			if (pm)
			{
				if (result.Hours == 12)
					result -= TimeSpan.FromHours(12);
				else if (result.Hours < 12)
					result += TimeSpan.FromHours(12);
			}
			return true;
		}

		[DebuggerDisplay("Number = {_left},{_right}; Scale={_scale}; Point={_point}")]
		private struct NumberScale
		{
			#pragma warning disable CA2207 // Initialize value type static fields inline
			private int _width;
			private long _left;
			private long _right;
			private int _scale;
			private bool _point;
			private static readonly long[] ScaleTable;
			private static readonly long[] OverflowTable;
			private const int ScaleLength = 19;

			static NumberScale()
			{
				ScaleTable = new long[ScaleLength];
				OverflowTable = new long[ScaleLength];
				long x = 1;
				for (int i = 0; i < ScaleTable.Length; ++i)
				{
					ScaleTable[i] = x;
					OverflowTable[i] = Int64.MaxValue / x;
					x *= 10;
				}
			}

			public NumberScale(long left, long right, int width, int scale)
			{
				_left = left;
				_right = right;
				_width = width;
				_scale = scale;
				_point = scale > 0;
			}

			public void Reset()
			{
				_width = 0;
				_left = 0;
				_right = 0;
				_scale = 0;
				_point = false;
			}

			public long Left => _left;
			public long Right => _right;
			public int Scale => _scale;
			public bool Point => _point;
			public int Size => _width;
			public decimal Value => _left + (decimal)_right / ScaleTable[_scale];

			public bool Append(char value, int position)
			{
				if (value < '0' || value > '9')
				{
					if (value != '.' || _point)
						return false;
					_point = true;
					++_width;
					return true;
				}
				if (_point)
				{
					if (_scale >= ScaleLength - 1)
					{
						_scale = ~_scale;
						return false;
					}
					++_scale;
					_right = _right * 10 + (value - '0');
				}
				else
				{
					if (position >= ScaleLength && _left > (Int64.MaxValue - (value - '0')) / 10)
					{
						_scale = ~_scale;
						return false;
					}
					_left = _left * 10 + (value - '0');
				}
				++_width;
				return true;
			}

			[Pure]
			public bool HasValue => _scale >= 0 && (_width > 1 || _width == 1 && !_point);

			[Pure]
			public TimeSpan Time(long ticksPerItem)
			{
				return ticksPerItem < OverflowTable[_scale] ?
					new TimeSpan(_left * ticksPerItem + _right * ticksPerItem / ScaleTable[_scale]) :
					new TimeSpan(_left * ticksPerItem + (long)((decimal)_right * ticksPerItem / ScaleTable[_scale] + 0.5m));
			}
		}

		public static DateTime GetDateTime(string? value)
		{
			return TryGetDateTime(value, out DateTime result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static DateTime GetDateTime(string? value, DateTime defaultValue)
		{
			return TryGetDateTime(value, out DateTime result) ? result : defaultValue;
		}

		public static DateTime GetDateTime(string? value, DateTime defaultValue, DateTime minValue, DateTime maxValue)
		{
			return !TryGetDateTime(value, out DateTime result) ? defaultValue : result < minValue ? minValue : result > maxValue ? maxValue : result;
		}

		public static bool TryGetDateTime(string? value, out DateTime result)
		{
			if (!TryGetDateTimeOffset(value, out DateTimeOffset dto, out bool zone))
			{
				result = new DateTime();
				return false;
			}
			result = !zone ? dto.DateTime : dto.Offset == TimeSpan.Zero ? dto.UtcDateTime : dto.LocalDateTime;
			return true;
		}

		public static DateTimeOffset GetDateTimeOffset(string? value)
		{
			return TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static DateTimeOffset GetDateTimeOffset(string? value, DateTimeOffset defaultValue)
		{
			return TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? result : defaultValue;
		}

		public static DateTimeOffset GetDateTimeOffset(string? value, DateTimeOffset defaultValue, DateTimeOffset minValue, DateTimeOffset maxValue)
		{
			return !TryGetDateTimeOffset(value, out DateTimeOffset result, out _) ? defaultValue : result < minValue ? minValue : result > maxValue ? maxValue : result;
		}

		public static bool TryGetDateTimeOffset(string? value, out DateTimeOffset result)
		{
			return TryGetDateTimeOffset(value, out result, out _);
		}

		private static int MatchTwo(Tokenizer.CharStream text)
		{
			char a = text[0];
			char b = text[1];
			if (a < '0' || a > '9' || b < '0' || b > '9')
				return -1;
			text.Forward(2);
			return (a - '0') * 10 + (b - '0');
		}

		/// <summary>
		/// Converts string representation of date and time into <see cref="System.DateTimeOffset"/>.
		///   syntax: [yyyy[-]mm[-]dd] [T] [hh[:]mm[:]ss[.ccc]] [zone]
		/// </summary>
		/// <param name="value">A string to convert.</param>
		/// <param name="result">result of conversion.</param>
		///	<param name="timeZone">Time zone indicator</param>
		/// <returns>true if s was converted successfully; otherwise, false.</returns>
		public static bool TryGetDateTimeOffset(string? value, out DateTimeOffset result, out bool timeZone)
		{
			result = new DateTimeOffset();
			timeZone = false;
			if (value == null || value.Length == 0)
				return false;

			static bool Space(char c) => c <= ' ';

			var text = new Tokenizer.CharStream(value, false, 1);
			int year = 1;
			int month = 1;
			int day = 1;

			text.Forward(Space);

			if (text[0] == 'T' || text[0] == 't')
			{
				text.Forward(1, Space);
				goto SetHour;
			}
			if (text[2] == ':')
				goto SetHour;

			int x = MatchTwo(text);
			if (x < 0)
				return false;
			year = MatchTwo(text);
			if (year < 0)
				return false;
			year += x * 100;
			if (year < 1)
				return false;
			bool delimiter = text[0] == '-';
			if (delimiter)
				text.Forward(1);
			month = MatchTwo(text);
			if (month < 1 || month > 12)
				return false;
			if (delimiter)
				if (text[0] == '-')
					text.Forward(1);
				else
					return false;
			day = MatchTwo(text);
			if (day < 1 || (day > 28 && day > DateTime.DaysInMonth(year, month)))
				return false;

			text.Forward(Space);
			if (text[0] == 'T' || text[0] == 't')
				text.Forward(1, Space);

			if (text.Eof)
			{
				result = new DateTime(year, month, day);
				return true;
			}

		SetHour:
			int hour = MatchTwo(text);
			if (hour < 0 || hour > 23)
				return false;
			delimiter = text[0] == ':';
			if (delimiter)
				text.Forward(1);
			int minute = MatchTwo(text);
			if (minute < 0 || minute > 59)
				return false;
			if (delimiter)
				if (text[0] == ':')
					text.Forward(1);
				else
					return false;
			int second = MatchTwo(text);
			if (second < 0 || second > 59)
				return false;

			long ticks = 0;
			if (text[0] != '.')
			{
				text.Forward(Space);
			}
			else
			{
				int k = 0;
				char b;
				long w = TimeSpan.TicksPerSecond;
				while ((b = text[++k]) >= '0' && b <= '9')
				{
					w /= 10;
					ticks += w * (b - '0');
				}
				text.Forward(k, Space);
			}

			TimeSpan offset;
			if (text[0] == 'Z')
			{
				text.Forward(1, Space);
				offset = TimeSpan.Zero;
				timeZone = true;
			}
			else if (text[0] == 'G' && text[1] == 'M' && text[2] == 'T')
			{
				text.Forward(3, Space);
				offset = TimeSpan.Zero;
				timeZone = true;
			}
			else if (text[0] == '+' || text[0] == '-')
			{
				bool minus = text[0] == '-';
				text.Forward(1, Space);
				char b = text[0];
				if (b < '0' || b > '9')
					return false;
				int h = b - '0';
				if ((b = text[1]) >= '0' && b <= '9')
				{
					h = h * 10 + (b - '0');
					text.Forward(2);
				}
				else
				{
					text.Forward(1);
				}
				int m = 0;
				if (text[0] == ':')
				{
					text.Forward(1);
					m = MatchTwo(text);
					if (m < 0 || m > 59)
						return false;
				}
				text.Forward(Space);
				offset = minus ? new TimeSpan(-h, -m, 0) : new TimeSpan(h, m, 0);
				timeZone = true;
			}
			else
			{
				offset = DateTimeOffset.Now.Offset;
			}

			bool pm = false;
			if (!text.Eof)
			{
				if (hour > 12 || text.Length != 2)
					return false;
				string ap = text.Substring(0, 2);
				if (string.Equals(ap, "PM", StringComparison.OrdinalIgnoreCase))
					pm = true;
				else if (!string.Equals(ap, "AM", StringComparison.OrdinalIgnoreCase))
					return false;
				if (hour > 12 || pm && hour == 12)
					return false;
				if (pm)
					hour += 12;
			}


			result = new DateTimeOffset(year, month, day, hour, minute, second, offset);
			if (ticks > 0)
				result += TimeSpan.FromTicks(ticks);
			return true;
		}

		public static Guid GetGuid(string? value)
		{
			return Guid.TryParse(value, out Guid result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static Guid GetGuid(string? value, Guid defaultValue)
		{
			return Guid.TryParse(value, out Guid result) ? result : defaultValue;
		}

		public static Guid? GetGuid(string? value, Guid? defaultValue)
		{
			return Guid.TryParse(value, out Guid result) ? result : defaultValue;
		}

		public static bool TryGetGuid(string? value, out Guid result)
		{
			return Guid.TryParse(value, out result);
		}

		public static Type GetType(string? value)
		{
			return TryGetType(value, out Type? result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static Type GetType(string? value, Type defaultValue)
		{
			return TryGetType(value, out Type? result) ? result : defaultValue;
		}

		public static bool TryGetType(string? value, [MaybeNullWhen(false)] out Type result)
		{
			if (String.IsNullOrWhiteSpace(value))
			{
				result = null;
				return false;
			}
			result = Factory.GetType(value);
			return result != null;
		}

		public static bool GetBoolean(string? value, bool defaultValue)
		{
			return TryGetBoolean(value, out bool result) ? result : defaultValue;
		}

		public static bool GetBoolean(string? value)
		{
			return TryGetBoolean(value, out bool result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static bool TryGetBoolean(string? value, out bool result)
		{
			if (value != null && value.Length > 0)
			{
				value = value.Trim().ToUpperInvariant();
				if (value == "TRUE" || value == "ON" || value == "YES" || value == "1" || value == "GRANT")
				{
					result = true;
					return true;
				}
				if (value == "FALSE" || value == "OFF" || value == "NO" || value == "0" || value == "DENY")
				{
					result = false;
					return true;
				}
			}
			result = false;
			return false;
		}

		public static Ternary GetTernary(string? value)
		{
			return TryGetTernary(value, out Ternary result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static Ternary GetTernary(string? value, Ternary defaultValue)
		{
			return TryGetTernary(value, out Ternary result) ? result : defaultValue;
		}

		public static bool TryGetTernary(string? value, out Ternary result)
		{
			if (value == null)
			{
				result = Ternary.Unknown;
				return false;
			}
			value = value.Trim().ToUpperInvariant();
			if (value == "TRUE" || value == "ON" || value == "YES" || value == "1" || value == "GRANT")
			{
				result = Ternary.True;
				return true;
			}
			if (value == "FALSE" || value == "OFF" || value == "NO" || value == "0" || value == "DENY")
			{
				result = Ternary.False;
				return true;
			}
			if (value == "UNKNOWN" || value == "SOME" || value == "ANY" || value == "ALL" || value == "2" || value == "BOTH" || value == "DEFAULT")
			{
				result = Ternary.Unknown;
				return true;
			}
			result = Ternary.Unknown;
			return false;
		}

		public static int GetIndex(string? value, params string[] variants)
		{
			if (variants == null)
				throw new ArgumentNullException(nameof(variants));

			if (value != null && (value = value.Trim()).Length == 0)
				value = null;

			for (int i = 0; i < variants.Length; ++i)
			{
				if (String.Equals(variants[i], value, StringComparison.OrdinalIgnoreCase))
					return i;
			}
			return -1;
		}

		public static T GetEnum<T>(string? value)
			where T : struct
		{
			if (TryGetEnum(value, out T result))
				return result;
			throw new FormatException(SR.FormatException(value));
		}

		public static T GetEnum<T>(string? value, T defaultValue)
			where T : struct
		{
			return TryGetEnum(value, out T result) ? result : defaultValue;
		}

		public static T? GetEnum<T>(string? value, T? defaultValue)
			where T : struct
		{
			return TryGetEnum(value, out T result) ? result : defaultValue;
		}

		private static bool IsEnum(Type type)
		{
			return type.IsEnum;
		}

		public static bool TryGetEnum<T>(string? value, out T result)
			where T : struct
		{
			if (value == null || value.Length == 0 || !IsEnum(typeof(T)))
			{
				result = default;
				return false;
			}
			if (Enum.TryParse(value.Trim(), true, out result))
				return true;

			if (Int64.TryParse(value, out long x))
			{
				object y = Enum.ToObject(typeof(T), x);
				foreach (var item in Enum.GetValues(typeof(T)))
				{
					if (Object.Equals(y, item))
					{
						result = (T)y;
						return true;
					}
				}
			}
			return false;
		}

		public static object GetEnum(string? value, Type enumType)
		{
			return TryGetEnum(value, enumType, out object? result) ? result : throw new FormatException(SR.FormatException(value));
		}

		[return: NotNullIfNotNull("defaultValue")]
		public static object? GetEnum(string? value, Type enumType, object? defaultValue)
		{
			return TryGetEnum(value, enumType, out object? result) ? result : defaultValue;
		}

		public static bool TryGetEnum(string? value, Type enumType, [MaybeNullWhen(false)] out object result)
		{
			if (enumType is null || !IsEnum(enumType))
			{
				result = null;
				return false;
			}
			result = Enum.ToObject(enumType, 0);
			if (value == null || (value = value.Trim()).Length == 0)
				return false;

			string[] names = Enum.GetNames(enumType);
			for (int i = 0; i < names.Length; ++i)
			{
				if (String.Equals(names[i], value, StringComparison.OrdinalIgnoreCase))
				{
					result = Enum.ToObject(enumType, Enum.GetValues(enumType).GetValue(i)!);
					return true;
				}
			}

			if (!Int64.TryParse(value, out long x))
				return false;

			object y = Enum.ToObject(enumType, x);
			foreach (var item in Enum.GetValues(enumType))
			{
				if (Object.Equals(y, item))
				{
					result = y;
					return true;
				}
			}
			return false;
		}

		[return: NotNullIfNotNull("defaultValue")]
		public static string? GetString(string? value, string? defaultValue)
		{
			if (value == null)
				return defaultValue;
			value = value.Trim();
			return value.Length == 0 ? defaultValue : value;
		}

		#endregion

		#region Parse Value

		[return: NotNullIfNotNull("defaultValue")]
		public static object? GetValue(string? value, Type type, object? defaultValue)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return TryGetValue(value, type, out object? result) ? result : defaultValue;
		}

		public static object GetValue(string? value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return TryGetValue(value, type, out object? result) ? result! : throw new FormatException(SR.FormatException(value));
		}

		[return: NotNullIfNotNull("defaultValue")]
		public static T GetValue<T>(string? value, T defaultValue)
		{
			return TryGetValue<T>(value, out var result) ? result : defaultValue;
		}

		public static T GetValue<T>(string? value)
		{
			return TryGetValue<T>(value, out var result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static bool TryGetValue<T>(string? value, [MaybeNullWhen(false)] out T result)
		{
			if (TryGetValue(value, typeof(T), out var temp))
			{
				result = (T)temp;
				return true;
			}
			result = default;
			return false;
		}

		public static object GetValueOrDefault(string? value, Type returnType)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));
			return TryGetValue(value, returnType, out object? result) ? result : Factory.DefaultValue(returnType);
		}

		public static bool TryGetValue(string? value, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));

			if (value == null || __missingConverters.ContainsKey(returnType))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser? parser))
				return parser(value, out result);

			if (String.IsNullOrWhiteSpace(value))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (returnType.IsEnum || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>) && returnType.GetGenericArguments()[0].IsEnum)
			{
				bool nullable = returnType.IsGenericType;
				Type type1 = nullable ? returnType.GetGenericArguments()[0] : returnType;
				Type type2 = nullable ? returnType : typeof(Nullable<>).MakeGenericType(returnType);
				__stringConditionalConstructor.TryAdd(type1, (string x, [MaybeNullWhen(false)] out object y) => TryGetEnum(x, type1, out y));
				__stringConditionalConstructor.TryAdd(type2, (string x, [MaybeNullWhen(false)] out object y) =>
				{
					if (String.IsNullOrWhiteSpace(x))
					{
						y = null;
						return false;
					}
					return TryGetEnum(x, type1, out y);
				});

				if (String.IsNullOrWhiteSpace(value))
				{
					result = null;
					return nullable;
				}
				return TryGetEnum(value, type1, out result);
			}

			TypeConverter? converter = GetTypeConverter(returnType);
			if (converter != null)
				return TryConverter(value, returnType, converter, out result);

			parser = GetExplicitConverter(returnType);
			if (parser != null)
			{
				__stringConditionalConstructor.TryAdd(returnType, parser);
				return parser(value, out result);
			}

			__missingConverters.TryAdd(returnType, true);
			result = Factory.DefaultValue(returnType);
			return false;
		}
		private static readonly ConcurrentDictionary<Type, bool> __missingConverters = new ConcurrentDictionary<Type, bool>();

		private static bool TryConverter(string value, Type returnType, TypeConverter converter, [MaybeNullWhen(false)] out object result)
		{
			try
			{
				result = converter.ConvertFromInvariantString(value);
				return result != null;
			}
			catch (NotSupportedException)
			{
			}
			catch (ArgumentException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		private static TypeConverter? GetTypeConverter(Type targetType)
		{
			if (__typeConverterTypeMap.TryGetValue(targetType, out var converterType))
				return converterType == null ? null : Factory.Construct(converterType) as TypeConverter;

			converterType = GetConverterType(targetType);
			TypeConverter? converter = null;
			if (converterType != null)
			{
				converter = Factory.Construct(converterType) as TypeConverter;
				if (converter == null || !converter.CanConvertFrom(typeof(string)))
				{
					converterType = null;
					converter = null;
				}
			}
			__typeConverterTypeMap.TryAdd(targetType, converterType);
			return converter;
		}
		private static readonly ConcurrentDictionary<Type, Type?> __typeConverterTypeMap = new ConcurrentDictionary<Type, Type?>();

		private static Type? GetConverterType(Type? type)
		{
			while (type != null)
			{
				CustomAttributeTypedArgument argument = CustomAttributeData.GetCustomAttributes(type)
					.Where(o => o.Constructor.ReflectedType == typeof(TypeConverterAttribute) && o.ConstructorArguments.Count == 1)
					.Select(o => o.ConstructorArguments[0]).FirstOrDefault();

				if (argument != default)
				{
					var qualifiedType = argument.Value as Type;
					if (qualifiedType == null && argument.Value is string qualifiedTypeName)
						qualifiedType = Factory.GetType(qualifiedTypeName);
					return Factory.IsPublicType(qualifiedType) ? qualifiedType : null;
				}
				type = type.BaseType;
			}
			return null;
		}

		private static ValueParser? GetExplicitConverter(Type type)
		{
			MethodInfo? parser = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), type.MakeByRefType() }, null);
			if (parser != null)
				return Tgv(parser, type, Factory.IsNullableType(type));

			//		result = null;
			//		try
			//		{
			//
			//	3:		xtype tmp1;
			//			if (Convert(value, out tmp1))
			//			{
			//	3.1:		result = (object)operator(tmp1);
			//	3.2:		result = (object)new operator(tmp1);
			//				return true;
			//			}
			//		...
			//	3:		xtype tmp2;
			//			if (Convert(value, out tmp2))
			//			{
			//	3.1:		result = (object)operator(tmp2);
			//	3.2:		result = (object)new operator(tmp2);
			//				return true;
			//			}
			//		...
			//			string:
			//	1:			result = (object)operator(value);
			//	2:			result = (object)new operator(value);
			//				return true;
			//
			//			return false;
			//		}
			//		catch (Exception ex)
			//		{
			//			if (EX.IsCriticalException(ex))
			//				throw;
			//			return false;
			//		}


			ParameterExpression value = Expression.Parameter(typeof(string), "value");
			ParameterExpression result = Expression.Parameter(typeof(object).MakeByRefType(), "result");

			List<(MethodInfo Method, ParameterInfo Parameter, MethodInfo Parser)> operators = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Select(o =>
				{
					if (!type.IsAssignableFrom(o.ReturnType))
						return default;
					if (!(o.IsSpecialName && (o.Name == "op_Explicit" || o.Name == "op_Implicit")))
						return default;
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer)
						return default;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return default;
					return (o, pp[0], ps.Method);
				})
				.Where(o => o.Method != null).ToList();

			if (operators.Count > 1)
				operators = __stringTypedParsers.Select(o => operators.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			List<(ConstructorInfo Constructor, ParameterInfo Parameter, MethodInfo Parser)> constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
				.Select(o =>
				{
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer ||
						operators.Any(x => x.Parameter.ParameterType == pp[0].ParameterType))
						return default;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return default;

					return (Constructor: o, Parameter: pp[0], Parser: ps.Method);
				})
				.Where(o => o.Constructor != null).ToList();

			if (constructors.Count > 1)
				constructors = __stringTypedParsers.Select(o => constructors.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			LabelTarget end = Expression.Label(typeof(bool));
			var convert = new List<Expression>();
			var variable = new List<ParameterExpression>();
			foreach (var item in operators)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result,
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.Call(item.Method, tmp), typeof(object)) :
									Expression.Call(item.Method, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			foreach (var item in constructors)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)new operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result,
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.New(item.Constructor, tmp), typeof(object)) :
									Expression.New(item.Constructor, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			var stringOperator = operators.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
			if (stringOperator.Method != null)
			{
				Expression last = Expression.Assign(result,
					type.IsValueType ? (Expression)
						Expression.TypeAs(Expression.Call(stringOperator.Method, value), typeof(object)) :
						Expression.Call(stringOperator.Method, value));
				convert.Add(last);
				convert.Add(Expression.Goto(end, Expression.Constant(true)));
			}
			else
			{
				var stringConstructor = constructors.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
				if (stringConstructor.Constructor != null)
				{
					Expression last = Expression.Assign(result,
						type.IsValueType ? (Expression)
							Expression.TypeAs(Expression.New(stringConstructor.Constructor, value), typeof(object)) :
							Expression.New(stringConstructor.Constructor, value));
					convert.Add(last);
					convert.Add(Expression.Goto(end, Expression.Constant(true)));
				}
			}

			if (convert.Count == 0)
				return null;

			convert.Add(Expression.Label(end, Expression.Constant(false)));

			ParameterExpression ex = Expression.Variable(typeof(Exception), "ex");

			Expression body = Expression.Block(
				Expression.Assign(result, Expression.Constant(null)),
				Expression.TryCatch(
					Expression.Block(variable, convert),
					Expression.Catch(ex, Expression.Block(
						Expression.IfThen(
							Expression.Call(((Func<Exception, bool>)ExceptionExtensions.IsCriticalException).Method, ex),
							Expression.Throw(ex)),
						Expression.Constant(false))
						)
					));
			return Expression.Lambda<ValueParser>(body, value, result).Compile();
		}

		public static T GetValue<T>(XmlLiteNode node)
		{
			return TryGetValue<T>(node, out var result) ? result : throw new FormatException(SR.FormatException(node.ToString().Left(1024), typeof(T)));
		}

		public static T GetValue<T>(XmlLiteNode node, T defaultValue)
		{
			return TryGetValue<T>(node, out var result) ? result : defaultValue;
		}

		public static bool TryGetValue<T>(XmlLiteNode node, [MaybeNullWhen(false)] out T result)
		{
			if (TryGetValue(node, typeof(T), out var temp))
			{
				result = (T)temp;
				return true;
			}
			result = default;
			return false;
		}

		public static object GetValue(XmlLiteNode node, Type returnType)
		{
			return TryGetValue(node, returnType, out var result) ? result : throw new FormatException(SR.FormatException(node.ToString().Left(1024), returnType));
		}

		public static bool TryGetValue(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			if (node == null || returnType == null)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			try
			{
				if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser? stringParser))
					return stringParser(node.Value, out result);

				if (__nodeConditionalConstructor.TryGetValue(returnType, out TryGetNodeValue? nodeParser))
				{
					if (nodeParser == null)
					{
						result = Factory.DefaultValue(returnType);
						return false;
					}
					return nodeParser(node, returnType, out result);
				}
				if (returnType.IsGenericType && __nodeGenericConstructor.TryGetValue(returnType.GetGenericTypeDefinition(), out nodeParser))
				{
					if (nodeParser == null)
					{
						result = Factory.DefaultValue(returnType);
						return false;
					}
					__nodeConditionalConstructor[returnType] = nodeParser;
					return nodeParser(node, returnType, out result);
				}
				if (returnType.IsEnum)
				{
					__stringConditionalConstructor.TryAdd(returnType, (string x, [MaybeNullWhen(false)] out object y) => TryGetEnum(x, returnType, out y));
					return TryGetEnum(node.Value, returnType, out result);
				}

				nodeParser = TryReflection;
				if (TestFromXmlLite(node, returnType, out result))
					nodeParser = TryFromXmlLite;
				else if (TestFromXmlReader(node, returnType, out result))
					nodeParser = TryFromXmlReader;
				else if (TestSerializer(node, returnType, out result))
					nodeParser = TrySerializer;
				else
					TryReflection(node, returnType, out result);

				__nodeConditionalConstructor.TryAdd(returnType, nodeParser);
				return result != null;
			}
			catch (Exception e)
			{
				throw new FormatException(SR.CannotParseValue(node.ToString().Left(1024), returnType), e);
			}
		}

		private static bool TryFromXmlLite(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			value = __fromXmlLiteNodeParsers[returnType]?.Invoke(node);
			return value != null;
		}

		private static bool TryFromXmlReader(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			using (XmlReader reader = node.ReadSubtree())
			{
				value = __fromXmlReaderParsers[returnType]?.Invoke(reader);
			}
			return value != null;
		}

		private static bool TrySerializer(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			try
			{
				var xs = new XmlSerializer(returnType, new XmlRootAttribute(node.Name));
				using XmlReader reader = node.ReadSubtree();
				if (xs.CanDeserialize(reader))
				{
					result = xs.Deserialize(reader);
					if (result != null)
						return true;
				}
			}
			catch (InvalidOperationException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		#endregion
	}
}
