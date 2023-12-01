// Lexxys Infrastructural library.
// file: XmlToolsPexTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Lexxys.Testing;
using Lexxys.Xml;

namespace Lexxys.Tests.Xml
{
	/// <summary>
	///This is a test class for XmlToolsTest and is intended
	///to contain all XmlToolsTest Unit Tests
	///</summary>
	[TestClass()]
	public class XmlToolsParseStringTest
	{
		/// <summary>
		///A test for GetIndex
		///</summary>
		[TestMethod]
		[DataRow("zero", 0, "Zero", "One", "Two")]
		[DataRow("ZeroOne", -1, "Zero", "One", "Two")]
		[DataRow(null, -1, "Zero", "One", "Two")]
		[DataRow(null, 3, "Zero", "One", "Two", null)]
		[DataRow("", 3, "Zero", "One", "Two", null)]
		public void GetIndexTest(string value, int index, params string[] items)
		{
			var actual = Strings.GetIndex(value, items);
			Assert.AreEqual(index, actual, value);
		}

		enum EnumTest1
		{
			MinusOne = -1,
			Zero = 0,
			One = 1
		}
		enum EnumTest2: sbyte
		{
			MinusOne = -1,
			Zero = 0,
			One = 1
		}

		[TestMethod]
		[DataRow(typeof(EnumTest1), "One", true, EnumTest1.One)]
		[DataRow(typeof(EnumTest1), "One1", false, default(EnumTest1))]
		[DataRow(typeof(EnumTest1), " ONE ", true, EnumTest1.One)]
		[DataRow(typeof(EnumTest1), "MinusOne", true, EnumTest1.MinusOne)]
		[DataRow(typeof(EnumTest1), "-1", true, EnumTest1.MinusOne)]
		[DataRow(typeof(EnumTest1), "-2", false, EnumTest1.Zero)]

		[DataRow(typeof(EnumTest2), "One", true, EnumTest2.One)]
		[DataRow(typeof(EnumTest2), "One1", false, default(EnumTest2))]
		[DataRow(typeof(EnumTest2), " ONE ", true, EnumTest2.One)]
		[DataRow(typeof(EnumTest2), "MinusOne", true, EnumTest2.MinusOne)]
		[DataRow(typeof(EnumTest2), "-1", true, EnumTest2.MinusOne)]
		[DataRow(typeof(EnumTest2), "-2", false, EnumTest2.Zero)]
		public void GetEnumTest(Type enumType, string value, bool shouldSuccess, object expected)
		{
			bool success;
			object actual;
			success = Strings.TryGetEnum(value, enumType, out actual);
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(shouldSuccess, success);
		}

		/// <summary>
		///A test for EncodeAttribute
		///</summary>
		[TestMethod]
		[DataRow("on<e", "\"on&lt;e\"")]
		[DataRow(">one'", "\"&gt;one'\"")]
		[DataRow("on<>e\"", "'on&lt;&gt;e\"'")]
		[DataRow("'o'ne\"", "\"'o'ne&quot;\"")]
		[DataRow(" ", "\" \"")]
		[DataRow(null, "\"\"")]
		public void EncodeAttributeTest(string value, string expected)
		{
			string actual;
			actual = XmlTools.EncodeAttribute(value);
			Assert.AreEqual(expected, actual, value);
		}

		[TestMethod]
		public void ParseDateTimeOffsetTestA()
		{
			for (int i = 0; i < 10000; ++i)
			{
				int year = Rand.Int(1, 10000);
				int month = Rand.Int(1, 13);
				int day = Rand.Int(1, DateTime.DaysInMonth(year, month) + 1);
				int hour = Rand.Int(0, 24);
				int minute = Rand.Int(0, 60);
				int second = Rand.Int(0, 60);
				int millisecond = Rand.Int(0, 1000);
				TimeSpan offset = TimeSpan.FromMinutes(Rand.Int(0, 60 * 14));
				ParseDateTimeOffsetTest(new DateTimeOffset(year, month, day, hour, minute, second, millisecond, offset));
			}
		}

		private void ParseDateTimeOffsetTest(DateTimeOffset value)
		{
			string[] dateFormats =
			[
				"+yyyy-MM-dd",
				"+yyyyMMdd",
				"--yyyy-MM-dd",
				"-yyyyMM-dd",
				"-yyyy-MMdd",
			];
			string[] timeFormats =
			[
				"+HH:mm:ss",
				"+HHmmss",
				"-HH:mmss",
				"-HH:mm:0ss",
			];
			foreach (var dateFormat in dateFormats)
			{
				bool correct = dateFormat[0] == '+';
				string date = dateFormat.Substring(1);
				string format = date;
				var actual = Strings.GetDateTimeOffset(value.ToString(format), default);
				if (!correct)
				{
					Assert.AreEqual(default, actual, format);
				}
				else
				{
					Assert.AreEqual(value.Date, actual.Date, format);

					format = date + "THH:mm:ss";
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(DateTimeOffset.Now.Offset, actual.Offset, format);

					format = date + "THH:mm:ss.fffffff";
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(value.Millisecond, actual.Millisecond, format);
					Assert.AreEqual(DateTimeOffset.Now.Offset, actual.Offset, format);

					format = date + "THH:mm:ss.fffffffK";
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(value.Millisecond, actual.Millisecond, format);
					Assert.AreEqual(value.Offset, actual.Offset, format);
				}

				foreach (var timeFormat in timeFormats)
				{
					correct &= timeFormat[0] == '+';
					string time = timeFormat.Substring(1);
					format = date + "T" + time;
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					if (!correct)
						Assert.AreEqual(default, actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
					format = date + "T" + time + " z";
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					if (!correct)
						Assert.AreEqual(default, actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
					format = date + "T" + time + "zz";
					actual = Strings.GetDateTimeOffset(value.ToString(format), default);
					if (!correct)
						Assert.AreEqual(default, actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
				}
			}
		}

		[TestMethod]
		[DataRow(0.0)]
		[DataRow(0.0000001)]
		public void GetTimeSpanVariants2(double value)
		{
			GetTimeSpanPex2(TimeSpan.FromSeconds(value));
		}

		[TestMethod]
		[DataRow(long.MaxValue)]
		[DataRow(long.MinValue)]
		[DataRow(long.MaxValue - 1)]
		[DataRow(long.MinValue + 1)]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(long.MaxValue / 2)]
		public void GetTimeSpanVariants2Ticks(long value)
		{
			GetTimeSpanPex2(TimeSpan.FromTicks(value));
		}

		[TestMethod]
		[DataRow(0.0)]
		[DataRow(0.0000001)]
		public void TimeSpanVariants3(double value)
		{
			RunVariant3(TimeSpan.FromSeconds(value));
		}

		[TestMethod]
		[DataRow(long.MaxValue)]
		[DataRow(long.MinValue)]
		[DataRow(long.MaxValue - 1)]
		[DataRow(long.MinValue + 1)]
		[DataRow(0)]
		[DataRow(1)]
		[DataRow(2)]
		[DataRow(3)]
		[DataRow(long.MaxValue / 2)]
		public void GetTimeSpanVariants3Ticks(long value)
		{
			RunVariant3(TimeSpan.FromTicks(value));
		}

		private void RunVariant3(TimeSpan value)
		{
			for (int i = 0; i < 64; i++)
			{
				GetTimeSpanPex3(value, T(i, 1), T(i, 2), T(i, 3), T(i, 4), T(i, 5), T(i, 6));
			}

			static bool T(int i, int j) => (i & (1 << j)) != 0;
		}

		public void GetTimeSpanPex(string value)
		{
			if (value != null && __19Digiits.IsMatch(value))
				return;
			bool test = !IsEmpty(value) && __timespanRex.IsMatch(value!);
			Assert.AreEqual(test, Strings.TryGetTimeSpan(value, out _));
		}
		private static readonly Regex __timespanRex = new Regex(@"\A
			([\0- ]*
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Dd][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Hh][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Mm][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Ss][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Mm][Ss][\0- ]*)?
			|(([\0- ]*(\d+(\.\d*)?|\.\d+)?
				[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*[:][\0- ]*)?
					[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*[:][\0- ]*)?
						[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*
			)\z", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		private static readonly Regex __19Digiits = new Regex(@"([1-9]|\.[0-9])[0-9]{18}");

		private static bool IsEmpty(string value)
		{
			if (value == null)
				return true;

			for (int i = 0; i < value.Length; ++i)
			{
				if (value[i] > ' ')
					return false;
			}
			return true;
		}

		private static decimal Millisecs(TimeSpan value)
		{
			return (decimal)value.Ticks / TimeSpan.TicksPerMillisecond;
		}

		private static decimal Secs(TimeSpan value)
		{
			return (decimal)value.Ticks / TimeSpan.TicksPerSecond;
		}

		private static decimal Mins(TimeSpan value)
		{
			return (decimal)value.Ticks / TimeSpan.TicksPerMinute;
		}

		private static decimal Hours(TimeSpan value)
		{
			return (decimal)value.Ticks / TimeSpan.TicksPerHour;
		}

		private static decimal Days(TimeSpan value)
		{
			return (decimal)value.Ticks / TimeSpan.TicksPerDay;
		}

		public void GetTimeSpanPex3(TimeSpan value, bool named, bool day, bool hour, bool minute, bool second, bool millisec)
		{
			var text = new StringBuilder();
			text.Append(value);
			if (named) text.Append(" named");
			if (day) text.Append(" day");
			if (hour) text.Append(" hour");
			if (minute) text.Append(" minute");
			if (second) text.Append(" second");
			if (millisec) text.Append(" millisec");

			value = TimeSpan.FromTicks(value.Ticks == long.MinValue ? long.MaxValue: Math.Abs(value.Ticks));
			string testOverflow = Mins(value).ToString(CultureInfo.InvariantCulture);
			if (testOverflow.Length - testOverflow.IndexOf('.') - 1 >= 19)
				minute = false;
			testOverflow = Hours(value).ToString(CultureInfo.InvariantCulture);
			if (testOverflow.Length - testOverflow.IndexOf('.') - 1 >= 19)
				hour = false;
			testOverflow = Days(value).ToString(CultureInfo.InvariantCulture);
			if (testOverflow.Length - testOverflow.IndexOf('.') - 1 >= 19)
				day = false;
			TimeSpan expected = value;
			value = expected;
			string s = "";
			if (named)
			{
				if (millisec)
				{
					if (day || hour || minute || second)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerSecond);
						s = Millisecs(t).ToString(CultureInfo.InvariantCulture) + "ms";
						value -= t;
					}
					else
					{
						s = Millisecs(value).ToString(CultureInfo.InvariantCulture) + "ms";
					}
				}
				if (second)
				{
					if (day || hour || minute)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerMinute);
						s = Secs(t).ToString(CultureInfo.InvariantCulture) + "s" + s;
						value -= t;
					}
					else
					{
						s = Secs(value).ToString(CultureInfo.InvariantCulture) + "s" + s;
					}
				}
				if (minute)
				{
					if (day || hour)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerHour);
						s = Secs(t).ToString(CultureInfo.InvariantCulture) + "m" + s;
						value -= t;
					}
					else
					{
						s = Mins(value).ToString(CultureInfo.InvariantCulture) + "m" + s;
					}
				}
				if (hour)
				{
					if (day)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerDay);
						s = Secs(t).ToString(CultureInfo.InvariantCulture) + "h" + s;
						value -= t;
					}
					else
					{
						s = Hours(value).ToString(CultureInfo.InvariantCulture) + "h" + s;
					}
				}
				if (day)
				{
					s = Days(value).ToString(CultureInfo.InvariantCulture) + "d" + s;
				}
			}
			else
			{
				if (day)
				{
					s = value.Days.ToString() + " ";
					value -= TimeSpan.FromTicks(value.Days * TimeSpan.TicksPerDay);
				}
				if (hour)
				{
					long x = (value.Ticks / TimeSpan.TicksPerHour);
					s += x.ToString() + ":";
					value -= TimeSpan.FromTicks(x * TimeSpan.TicksPerHour);
				}
				else if (day)
				{
					s += "0:";
				}
				if (minute)
				{
					long x = (value.Ticks / TimeSpan.TicksPerMinute);
					s += x.ToString() + ":";
					value -= TimeSpan.FromTicks(x * TimeSpan.TicksPerMinute);
				}
				else if (hour || day)
				{
					s += "0:";
				}
				s += Secs(value).ToString(CultureInfo.InvariantCulture);
			}
			TimeSpan actual;
			if (s.Length > 0)
			{
				var ms = text.ToString();
				Assert.IsTrue(Strings.TryGetTimeSpan(s, out actual), s + " for " + ms);
				Assert.AreEqual(expected, actual, ms);
			}
		}

		public void GetTimeSpanPex2(TimeSpan value)
		{
			value = TimeSpan.FromTicks(value.Ticks == long.MinValue ? long.MaxValue: Math.Abs(value.Ticks));
			string x = Hours(value).ToString(CultureInfo.InvariantCulture);
			if (x.Length - x.IndexOf('.') - 1 >= 19)
				return;
			x = Days(value).ToString(CultureInfo.InvariantCulture);
			if (x.Length - x.IndexOf('.') - 1 < 19)
			{
				Assert.AreEqual(value, Strings.GetTimeSpan(Days(value) + "d"));
				Assert.AreEqual(value, Strings.GetTimeSpan(Days(value) + " d"));
			}
			Assert.AreEqual(value, Strings.GetTimeSpan(Hours(value) + "h"));
			Assert.AreEqual(value, Strings.GetTimeSpan(Mins(value) + "m"));
			Assert.AreEqual(value, Strings.GetTimeSpan(Secs(value) + "s"));
			Assert.AreEqual(value, Strings.GetTimeSpan(Millisecs(value) + "ms"));
			Assert.AreEqual(value, Strings.GetTimeSpan(Secs(value).ToString(CultureInfo.InvariantCulture)));

			TimeSpan d = TimeSpan.FromDays(value.Days);
			TimeSpan h = TimeSpan.FromHours(value.Hours);
			TimeSpan m = TimeSpan.FromMinutes(value.Minutes);

			Assert.AreEqual(value, Strings.GetTimeSpan(value.Days.ToString() + " " + Hours(value - d) + ":0:0"));
			Assert.AreEqual(value, Strings.GetTimeSpan(value.Days.ToString() + " " + value.Hours.ToString() + ":" + Mins(value - d - h) + ":0"));
			Assert.AreEqual(value, Strings.GetTimeSpan(value.Days.ToString() + " " + value.Hours.ToString() + ":" + value.Minutes.ToString() + ":" + Secs(value - d - h - m)));

			TimeSpan hours = value - d;
			Assert.AreEqual(hours, Strings.GetTimeSpan(hours.Hours.ToString() + ":" + Mins(hours - h) + ":0"));
			Assert.AreEqual(hours, Strings.GetTimeSpan(hours.Hours.ToString() + ":" + hours.Minutes.ToString() + ":" + Secs(hours - h - m)));

			TimeSpan minutes = hours - h;
			Assert.AreEqual(minutes, Strings.GetTimeSpan(Mins(minutes) + ":0"));
			Assert.AreEqual(minutes, Strings.GetTimeSpan(minutes.Minutes.ToString() + ":" + Secs(minutes - m)));
			Assert.AreEqual(hours, Strings.GetTimeSpan(Mins(hours) + ":0"));
			Assert.AreEqual(value, Strings.GetTimeSpan(Mins(value) + ":0"));

			TimeSpan seconds = minutes - m;
			Assert.AreEqual(seconds, Strings.GetTimeSpan(Secs(seconds).ToString(CultureInfo.InvariantCulture)));
			Assert.AreEqual(minutes, Strings.GetTimeSpan(Secs(minutes).ToString(CultureInfo.InvariantCulture)));
			Assert.AreEqual(hours, Strings.GetTimeSpan(Secs(hours).ToString(CultureInfo.InvariantCulture)));
			Assert.AreEqual(value, Strings.GetTimeSpan(Secs(value).ToString(CultureInfo.InvariantCulture)));
		}
	}
}
