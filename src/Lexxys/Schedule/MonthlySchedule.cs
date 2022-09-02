// Lexxys Infrastructural library.
// file: MonthlySchedule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lexxys
{
	public enum ScheduleWeekType
	{
		None,
		First,
		Second,
		Third,
		Fourth,
		Last
	}

	public class MonthlySchedule: Schedule, IDump, IDumpXml, IDumpJson, IEquatable<MonthlySchedule>
	{
		public const int ScheduleLastDay = 31;

		public new const string Type = "monthly";

		public MonthlySchedule(int day = 0, IEnumerable<int>? monthList = null, ScheduleReminder? reminder = null): base(Type, reminder)
		{
			Day = Math.Max(1, Math.Min(day, ScheduleLastDay));
			MonthList = Months(monthList);
		}

		public MonthlySchedule(ScheduleWeekType week, DayOfWeek weekDay, IEnumerable<int>? monthList = null, ScheduleReminder? reminder = null) : base(Type, reminder)
		{
			if (week == ScheduleWeekType.None)
				throw new ArgumentOutOfRangeException(nameof(week), week, null);

			Week = week;
			WeekDay = weekDay;
			MonthList = Months(monthList);
		}

		private static IReadOnlyList<int> Months(IEnumerable<int>? monthList)
		{
			if (monthList == null)
				return AllMonths;
			var ss = new SortedSet<int>(monthList.Where(o => o >= 1 && o <= 12)).ToList();
			return ss.Count == 0 || ss.Count == 12 ? AllMonths : ReadOnly.Wrap(ss);
		}
		private static readonly IReadOnlyList<int> AllMonths = ReadOnly.Wrap(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

		public int Day { get; }
		public ScheduleWeekType Week { get; }
		public DayOfWeek WeekDay { get; }
		public IReadOnlyList<int> MonthList { get; }

		public override DateTime? Next(DateTime startTime, DateTime? previousTime = null)
		{
			DateTime start = startTime.Date;
			DateTime previous = (previousTime ?? default).Date;
			if (previous >= start)
				start = previous + TimeSpan.FromTicks(TimeSpan.TicksPerDay);

			int year = start.Year;
			int month = start.Month;
			if (Week == ScheduleWeekType.None)
			{
				for (;;)
				{
					foreach (var m in MonthList)
					{
						if (m < month)
							continue;
						var next = new DateTime(year, m, Math.Min(Day, DateTime.DaysInMonth(year, m)));
						if (next >= start)
							return next + startTime.TimeOfDay;
					}
					month = 1;
					++year;
				}
			}

			for (;;)
			{
				int week = (int)Week - 1;
				foreach (var m in MonthList)
				{
					if (m < month)
						continue;
					var dx = new DateTime(year, m, 1);
					int n = (int)WeekDay - (int)dx.DayOfWeek;
					if (n < 0)
						n += 7;
					var next = dx.AddDays(n + 7 * week);
					if (next.Month > m)
						next = next.AddDays(-7);
					if (next >= start)
						return next + startTime.TimeOfDay;
				}
				month = 1;
				++year;
			}
		}

		public override StringBuilder ToString(StringBuilder text, IFormatProvider? provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			var format = (DateTimeFormatInfo?)provider?.GetFormat(typeof(DateTimeFormatInfo)) ?? CultureInfo.CurrentCulture.DateTimeFormat;
			text.Append("the ");
			if (Week == ScheduleWeekType.None)
			{
				if (Day == 1)
					text.Append("first");
				else if (Day == 31)
					text.Append("last");
				else
					text.Append(Lingua.Ord(Day));
				text.Append(" day");
			}
			else
			{
				text.Append(Week.ToString().ToLowerInvariant())
					.Append(' ')
					.Append(abbreviateDayName ? format.GetAbbreviatedDayName(WeekDay): format.GetDayName(WeekDay));
			}

			if (MonthList.Count == 12)
				text.Append(" of every month");
			else
				text.Append(" of ").Append(Strings.JoinAnd(MonthList.Select(abbreviateMonthName ? (Func<int, string>)(o => format.GetAbbreviatedMonthName(o)): o => format.GetMonthName(o))));
			Reminder.ToString(text, provider);
			return text;
		}

		public bool Equals(MonthlySchedule? other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Day == other.Day && Week == other.Week && WeekDay == other.WeekDay && Comparer.Equals(MonthList, other.MonthList);
		}

		public override bool Equals(Schedule? other)
		{
			return other is MonthlySchedule ms && Equals(ms);
		}

		public override bool Equals(object? obj)
		{
			return obj is MonthlySchedule ms && Equals(ms);
		}

		public override int GetHashCode()
		{
			return HashCode.Join(HashCode.Join(base.GetHashCode(), Day.GetHashCode(), Week.GetHashCode()), MonthList);
		}

		public override DumpWriter DumpContent(DumpWriter writer)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			return base.DumpContent(writer)
				.Then("Day", Day)
				.Then("Week", Week)
				.Then("WeekDay", WeekDay)
				.Then("Months", MonthList);
		}

		public override XmlBuilder ToXmlContent(XmlBuilder builder)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			builder.Item("type", ScheduleType);
			if (Week == ScheduleWeekType.None)
			{
				if (Day != 1)
					builder.Item("day", Day);
			}
			else
			{
				builder.Item("week", Week)
					.Item("weekDay", WeekDay);
			}
			if (!Object.ReferenceEquals(MonthList, AllMonths))
				builder.Item("months", String.Join(",", MonthList.Select(o => o.ToString(CultureInfo.InvariantCulture))));
			if (!Reminder.IsEmpty)
				builder.Element("reminder").Value(Reminder).End();
			return builder;
		}

		public override JsonBuilder ToJsonContent(JsonBuilder json)
		{
			if (json is null)
				throw new ArgumentNullException(nameof(json));

			json.Item("type").Val(ScheduleType);
			if (Week == ScheduleWeekType.None)
			{
				if (Day != 1)
					json.Item("day").Val(Day);
			}
			else
			{
				json.Item("week").Val(Week)
					.Item("weekDay").Val(WeekDay);
			}
			if (!Object.ReferenceEquals(MonthList, AllMonths))
				json.Item("months").Val(MonthList);
			if (!Reminder.IsEmpty)
				json.Item("reminder").Val(Reminder);
			return json;
		}

		public new static MonthlySchedule FromXml(Xml.XmlLiteNode xml)
		{
			if (xml is null)
				throw new ArgumentNullException(nameof(xml));
			if (xml.IsEmpty)
				throw new ArgumentOutOfRangeException(nameof(xml), xml, null);
			if (xml["type"] != Type)
				throw new ArgumentOutOfRangeException(nameof(xml), xml, null);
			IEnumerable<int>? months = xml["months"] != null ?
				xml["months"]?.Split(',').Select(o => o.AsInt32(0)):
				xml.Element("months").Elements.Select(o => o.Value.AsInt32());

			return xml["week"].AsEnum(default(ScheduleWeekType)) == ScheduleWeekType.None ?
				new MonthlySchedule(xml["day"].AsInt32(1), months, ScheduleReminder.FromXml(xml.Element("reminder"))):
				new MonthlySchedule(xml["week"].AsEnum(default(ScheduleWeekType)), xml["weekDay"].AsEnum(DayOfWeek.Friday), months, ScheduleReminder.FromXml(xml.Element("reminder")));
		}

		public new static MonthlySchedule FromJson(string json)
		{
			if (json is null || json.Length <= 0)
				throw new ArgumentNullException(nameof(json));

			return FromXml(Xml.XmlLiteNode.FromJson(json, "schedule", forceAttributes: true));
		}
	}
}


