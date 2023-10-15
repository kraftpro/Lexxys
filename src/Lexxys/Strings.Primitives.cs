using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Globalization;

namespace Lexxys;

public static partial class Strings
{
	public static byte GetByte(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static sbyte GetSByte(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static short GetInt16(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static ushort GetUInt16(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static int GetInt32(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static uint GetUInt32(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static long GetInt64(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static ulong GetUInt64(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static float GetSingle(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static double GetDouble(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static decimal GetDecimal(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static char GetChar(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
		return TryGetChar(value, out var c) ? c: throw new FormatException(SR.FormatException(value));
	}

	public static char GetChar(string? value, char defaultValue)
	{
		return TryGetChar(value, out var c) ? c: defaultValue;
	}

	public static char? GetChar(string? value, char? defaultValue)
	{
		return TryGetChar(value, out var c) ? c: defaultValue;
	}

	public static bool TryGetChar(string? value, out char result)
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

	public static TimeSpan GetTimeSpan(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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
	///   syntax: [P] [days 'D'] [T] [hours 'H'][minutes 'M'][seconds 'S'][milliseconds 'MS']
	///	    [[[days] hours:]minutes:]seconds [AM/PM]
	/// </summary>
	/// <param name="value">A string to convert.</param>
	/// <param name="result">result of conversion.</param>
	/// <returns>true if s was converted successfully; otherwise, false.</returns>
	public static bool TryGetTimeSpan(string? value, out TimeSpan result)
	{
		result = new TimeSpan();
		if (value is not { Length: >0 })
			return false;

		var text = new Tokenizer.CharStream(value, 1);
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

		if (c0 is 'D' or 'd')
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

		if (c0 is 'T' or 't')
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

		if (c0 is 'H' or 'h')
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

		if (c0 is 'M' or 'm' && text[1] is not ('S' or 's'))
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

		if (c0 is 'S' or 's')
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

		if (c0 is 'M' or 'm' && text[1] is 'S' or 's')
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
			if (value is <'0' or >'9')
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

	public static DateTime GetDateTime(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static DateTimeOffset GetDateTimeOffset(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	private static int MatchTwo(ref Tokenizer.CharStream text)
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
		if (value is not { Length: >0 })
			return false;

		static bool Space(char c) => c <= ' ';

		var text = new Tokenizer.CharStream(value, 1);
		int year = 1;
		int month = 1;
		int day = 1;

		text.Forward(Space);

		if (text[0] is 'T' or 't')
		{
			text.Forward(1, Space);
			goto SetHour;
		}
		if (text[2] == ':')
			goto SetHour;

		int x = MatchTwo(ref text);
		if (x < 0)
			return false;
		year = MatchTwo(ref text);
		if (year < 0)
			return false;
		year += x * 100;
		if (year < 1)
			return false;
		bool delimiter = text[0] == '-';
		if (delimiter)
			text.Forward(1);
		month = MatchTwo(ref text);
		if (month is <1 or >12)
			return false;
		if (delimiter)
			if (text[0] == '-')
				text.Forward(1);
			else
				return false;
		day = MatchTwo(ref text);
		if (day < 1 || (day > 28 && day > DateTime.DaysInMonth(year, month)))
			return false;

		text.Forward(Space);
		if (text[0] is 'T' or 't')
			text.Forward(1, Space);

		if (text.Eof)
		{
			result = new DateTime(year, month, day);
			return true;
		}

	SetHour:
		int hour = MatchTwo(ref text);
		if (hour is <0 or >23)
			return false;
		delimiter = text[0] == ':';
		if (delimiter)
			text.Forward(1);
		int minute = MatchTwo(ref text);
		if (minute is <0 or >59)
			return false;
		if (delimiter)
			if (text[0] == ':')
				text.Forward(1);
			else
				return false;
		int second = MatchTwo(ref text);
		if (second is <0 or >59)
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
		else if (text[0] is '+' or '-')
		{
			bool minus = text[0] == '-';
			text.Forward(1, Space);
			char b = text[0];
			if (b < '0' || b > '9')
				return false;
			int h = b - '0';
			if ((b = text[1]) is >='0' and <='9')
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
				m = MatchTwo(ref text);
				if (m is <0 or >59)
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
			if (String.Equals(ap, "PM", StringComparison.OrdinalIgnoreCase))
				pm = true;
			else if (!String.Equals(ap, "AM", StringComparison.OrdinalIgnoreCase))
				return false;
			if (pm && hour == 12)
				return false;
			if (pm)
				hour += 12;
		}

		result = new DateTimeOffset(year, month, day, hour, minute, second, offset);
		if (ticks > 0)
			result += TimeSpan.FromTicks(ticks);
		return true;
	}

	public static Guid GetGuid(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
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

	public static Type GetType(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
		return TryGetType(value, out Type? result) ? result : throw new FormatException(SR.FormatException(value));
	}

	public static Type GetType(string? value, Type defaultValue)
	{
		return TryGetType(value, out Type? result) ? result: defaultValue;
	}

	public static bool TryGetType([NotNullWhen(true)] string? value, [MaybeNullWhen(false)] out Type result)
	{
		if (String.IsNullOrWhiteSpace(value))
		{
			result = null!;
			return false;
		}
		result = Factory.GetType(value)!;
		return result != null;
	}

	public static bool GetBoolean(string? value, bool defaultValue)
	{
		return TryGetBoolean(value, out bool result) ? result : defaultValue;
	}

	public static bool GetBoolean(string value)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
		return TryGetBoolean(value, out bool result) ? result : throw new FormatException(SR.FormatException(value));
	}

	public static bool TryGetBoolean(string? value, out bool result)
	{
		switch (value?.Trim().ToUpperInvariant())
		{
			case "TRUE" or "ON" or "YES" or "1" or "GRANT":
				result = true;
				return true;
			case "FALSE" or "OFF" or "NO" or "0" or "DENY":
				result = false;
				return true;
			default:
				result = false;
				return false;
		}
	}

	public static int GetIndex(string? value, params string?[] variants)
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

	public static T GetEnum<T>(string value) where T : struct
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
		if (TryGetEnum(value, out T result))
			return result;
		throw new FormatException(SR.FormatException(value));
	}

	public static T GetEnum<T>(string? value, T defaultValue) where T : struct
	{
		return TryGetEnum(value, out T result) ? result : defaultValue;
	}

	public static T? GetEnum<T>(string? value, T? defaultValue) where T : struct
	{
		return TryGetEnum(value, out T result) ? result : defaultValue;
	}

	public static bool TryGetEnum<T>(string? value, out T result) where T : struct
	{
		return Enum.TryParse(value, true, out result);
	}

	public static object GetEnum(string value, Type enumType)
	{
		if (value is not { Length: >0 })
			throw new ArgumentNullException(nameof(value));
		return TryGetEnum(value, enumType, out object? result) ? result : throw new FormatException(SR.FormatException(value));
	}

	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static object? GetEnum(string? value, Type enumType, object? defaultValue)
	{
		return TryGetEnum(value, enumType, out object? result) ? result : defaultValue;
	}

	public static bool TryGetEnum([NotNullWhen(true)] string? value, Type enumType, out object result)
	{
		if (enumType is null)
			throw new ArgumentNullException(nameof(enumType));

#if NETCOREAPP
		if (Enum.TryParse(enumType, value, true, out result!))
		{
			var s = result.ToString()!;
			if (!(s.Length == 0 || s[0] is >= '0' and <= '9' or '-' or '+'))
				return true;
		}
		result = Enum.ToObject(enumType, 0);
		return false;
#else
		if (!enumType.IsEnum)
			throw new ArgumentException("The type of parameter must be Enum type", nameof(enumType));

		if (value == null || (value = value.Trim()).Length == 0)
		{
			result = Enum.ToObject(enumType, 0);
			return false;
		}

		if (value[0] is >= '0' and <= '9' or '-' or '+')
		{
			if (long.TryParse(value, out var lv))
			{
				result = Enum.ToObject(enumType, lv);
				var s = result.ToString();
				if (!(s.Length == 0 || s[0] is >= '0' and <= '9' or '-' or '+'))
					return true;
			}
			result = Enum.ToObject(enumType, 0);
			return false;
		}

		var parts = value.Split(__enumSeparators, StringSplitOptions.RemoveEmptyEntries);

		ulong sum = 0;
		bool found = false;
		Func<object, ulong> converter = Type.GetTypeCode(enumType) switch
		{
			TypeCode.Byte => o => (ulong)(byte)o,
			TypeCode.SByte => o => (ulong)(sbyte)o,
			TypeCode.Int16 => o => (ulong)(short)o,
			TypeCode.UInt16 => o => (ulong)(ushort)o,
			TypeCode.Int32 => o => (ulong)(int)o,
			TypeCode.UInt32 => o => (ulong)(uint)o,
			TypeCode.Int64 => o => (ulong)(long)o,
			TypeCode.UInt64 => o => (ulong)o,
			_ => o => System.Convert.ToUInt64(o.ToString(), CultureInfo.InvariantCulture)
		};
		foreach (var item in enumType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Select(o => (Name: o.Name, Value: o.GetRawConstantValue())))
		{
			for (int i = 0; i < parts.Length; ++i)
			{
				if (String.Equals(parts[i], item.Name, StringComparison.OrdinalIgnoreCase))
				{
					found = true;
					sum |= converter(item.Value);
					break;
				}
			}
		}

		result = Enum.ToObject(enumType, sum);
		return found;
#endif
	}
#if !NETCOREAPP
	private static readonly char[] __enumSeparators = [','];
#endif

	[return: NotNullIfNotNull(nameof(defaultValue))]
	public static string? GetString(string? value, string? defaultValue)
	{
		if (value == null)
			return defaultValue;
		value = value.Trim();
		return value.Length == 0 ? defaultValue : value;
	}

	public static (Type Regular, Type? Nullable) NullableTypes(Type type)
	{
		if (!type.IsValueType)
			return (type, null);
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			return (type.GetGenericArguments()[0], type);
		return (type, typeof(Nullable<>).MakeGenericType(type));
	}
}
