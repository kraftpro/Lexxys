// Lexxys Infrastructural library.
// file: WeeklySchedule.cs
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
	public class WeeklySchedule: Schedule, IDump, IDumpXml, IDumpJson, IEquatable<WeeklySchedule>
	{
		public new const string Type = "weekly";

		public WeeklySchedule(int weekPeriod = 0, IEnumerable<DayOfWeek> dayList = null, ScheduleReminder reminder = null): base(Type, reminder)
		{
			WeekPeriod = Math.Max(1, weekPeriod);
			if (dayList == null)
			{
				DayList = FridayOnly;
			}
			else
			{
				var ss = new SortedSet<DayOfWeek>(dayList.Where(o => o >= DayOfWeek.Sunday && o <= DayOfWeek.Saturday)).ToList();
				DayList = ss.Count == 0 || ss.Count == 1 && ss[0] == DayOfWeek.Friday ? FridayOnly : ReadOnly.Wrap(ss);
			}
		}
		private static readonly IReadOnlyList<DayOfWeek> FridayOnly = ReadOnly.Wrap(new[] { DayOfWeek.Friday });

		public int WeekPeriod { get; }
		public IReadOnlyList<DayOfWeek> DayList { get; }

		public override DateTime? Next(DateTime startTime, DateTime? previousTime = null)
		{
			DateTime start = startTime.Date;
			DateTime prev = previousTime ?? start.AddDays(-1);
			DateTime week = start.AddDays(-(int)start.DayOfWeek);
			var diff = (prev - week).Days;
			int period = WeekPeriod * 7;
			if (diff > 6)
				week = week.AddDays(((diff - 1) / period) * period);

			for (;;)
			{
				var go = TestWeek(week, prev);
				if (go != null)
					return go + startTime.TimeOfDay;
				week = week.AddDays(period);
			}
		}

		private DateTime? TestWeek(DateTime week, DateTime previous)
		{
			foreach (var day in DayList)
			{
				DateTime dx = week.AddDays((int)day);
				if (dx > previous)
					return dx;
			}
			return null;
		}

		public override StringBuilder ToString(StringBuilder text, IFormatProvider provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
		{
			if (text is null)
				throw new ArgumentNullException(nameof(text));

			var format = (DateTimeFormatInfo)provider?.GetFormat(typeof(DateTimeFormatInfo)) ?? CultureInfo.CurrentCulture.DateTimeFormat;
			text.Append("every ")
				.Append(
					DayList.Count == 7 ? "day" :
					DayList.Count == 5 && DayList[0] == DayOfWeek.Monday && DayList[4] == DayOfWeek.Friday ? "weekday" :
					Strings.JoinAnd(DayList.Select(abbreviateMonthName ? (Func<DayOfWeek, string>)(o => format.GetAbbreviatedDayName(o)) : o => format.GetDayName(o)))
					);
			if (WeekPeriod > 1)
				text.Append(" of every ").Append(Lingua.Ord(Lingua.NumWord(WeekPeriod))).Append(" week");
			Reminder.ToString(text, provider);
			return text;
		}

		public bool Equals(WeeklySchedule other)
		{
			if (other is null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return WeekPeriod == other.WeekPeriod && Comparer.Equals(DayList, other.DayList);
		}

		public override bool Equals(Schedule that)
		{
			return that is WeeklySchedule ws && Equals(ws);
		}

		public override bool Equals(object obj)
		{
			return obj is WeeklySchedule ws && Equals(ws);
		}

		public override int GetHashCode()
		{
			return HashCode.Join(HashCode.Join(base.GetHashCode(), WeekPeriod.GetHashCode()), DayList);
		}

		public override DumpWriter DumpContent(DumpWriter writer)
		{
			return base.DumpContent(writer)
				.Text(",WeekPeriod=").Dump(WeekPeriod)
				.Text(",DayList=").Dump(DayList);
		}

		public override XmlBuilder ToXmlContent(XmlBuilder xml)
		{
			if (xml is null)
				throw new ArgumentNullException(nameof(xml));

			xml.Item("type", ScheduleType);
			if (WeekPeriod != 1)
				xml.Item("week", WeekPeriod);
			if (!Object.ReferenceEquals(DayList, FridayOnly))
				xml.Item("days", String.Join(",", DayList.Select(o => ((int)o).ToString(CultureInfo.InvariantCulture))));
			if (!Reminder.IsEmpty)
				xml.Element("reminder").Value(Reminder).End();
			return xml;
		}

		public override JsonBuilder ToJsonContent(JsonBuilder json)
		{
			if (json is null)
				throw new ArgumentNullException(nameof(json));

			json.Item("type").Val(ScheduleType);
			if (WeekPeriod != 1)
				json.Item("week").Val(WeekPeriod);
			if (!Object.ReferenceEquals(DayList, FridayOnly))
				json.Item("days").Val(DayList.Select(o => (int)o));
			if (!Reminder.IsEmpty)
				json.Item("reminder").Val(Reminder);
			return json;
		}

		public new static WeeklySchedule FromXml(Xml.XmlLiteNode xml)
		{
			if (xml == null || xml.IsEmpty)
				return null;
			if (xml["type"] != Type)
				throw new ArgumentOutOfRangeException(nameof(xml) + ".type", xml["type"], null);
			IEnumerable<DayOfWeek> days = xml["days"] != null ?
				xml["days"]?.Split(',').Select(o => o.AsEnum(DayOfWeek.Friday)) :
				xml.Element("days").Elements.Select(o => o.Value.AsEnum<DayOfWeek>());
			return new WeeklySchedule(xml["week"].AsInt32(1), days, ScheduleReminder.FromXml(xml.Element("reminder")));
		}

		public new static WeeklySchedule FromJson(string json)
		{
			return FromXml(Xml.XmlLiteNode.FromJson(json, "schedule", forceAttributes: true));
		}
	}
}

