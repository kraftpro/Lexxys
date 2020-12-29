// Lexxys Infrastructural library.
// file: DailySchedule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Globalization;
using System.Text;

namespace Lexxys
{
	public class DailySchedule: Schedule, IDump, IDumpXml, IDumpJson, IEquatable<DailySchedule>
	{
		public new const string Type = "daily";

		public DailySchedule(int dayPeriod = 0, ScheduleReminder reminder = null): base(Type, reminder)
		{
			DayPeriod = Math.Max(1, dayPeriod);
		}

		public int DayPeriod { get; }

		public override DateTime? Next(DateTime startTime, DateTime? previousTime = null)
		{
			try
			{
				DateTime start = startTime.Date;
				DateTime previous = (previousTime ?? default).Date;
				if (previous < start)
					previous = start - TimeSpan.FromTicks(TimeSpan.TicksPerDay);

				int dayPeriod = DayPeriod;
				int n = (previous - start).Days + dayPeriod;
				DateTime result = start.AddDays(n - n % dayPeriod);
				if (result <= previous)
					result = result.AddDays(dayPeriod);
				if (result <= previous)
					return null;

				return result + startTime.TimeOfDay;
			}
			catch (Exception flaw)
			{
				flaw
					.Add(nameof(startTime), startTime)
					.Add(nameof(previousTime), previousTime)
					.Add("Dump", this.Dump());
				throw;
			}
		}

		public override StringBuilder ToString(StringBuilder text, IFormatProvider provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
		{
			if (DayPeriod > 1)
				text.Append("every ").Append(Lingua.Ord(Lingua.NumWord(DayPeriod))).Append(" day");
			else
				text.Append("everyday");
			Reminder.ToString(text, provider);
			return text;
		}

		public bool Equals(DailySchedule that)
		{
			if (that is null)
				return false;
			if (ReferenceEquals(this, that))
				return true;
			return base.Equals(that) && DayPeriod == that.DayPeriod;
		}

		public override bool Equals(Schedule that)
		{
			return that is DailySchedule ds && Equals(ds);
		}

		public override bool Equals(object obj)
		{
			return obj is DailySchedule ds && Equals(ds);
		}

		public override int GetHashCode()
		{
			return HashCode.Join(base.GetHashCode(), DayPeriod.GetHashCode());
		}

		public override DumpWriter DumpContent(DumpWriter writer)
		{
			return base.DumpContent(writer)
				.Text(",DayPeriod=").Dump(DayPeriod);
		}

		public override XmlBuilder ToXmlContent(XmlBuilder xml)
		{
			xml.Item("type", ScheduleType);
			if (DayPeriod != 1)
				xml.Item("day", DayPeriod);
			if (!Reminder.IsEmpty)
				xml.Element("reminder").Value(Reminder).End();
			return xml;
		}

		public override JsonBuilder ToJsonContent(JsonBuilder json)
		{
			json.Item("type").Val(ScheduleType);
			if (DayPeriod != 1)
				json.Item("day").Val(DayPeriod);
			if (!Reminder.IsEmpty)
				json.Item("reminder").Val(Reminder);
			return json;
		}

		public new static DailySchedule FromXml(Xml.XmlLiteNode xml)
		{
			if (xml == null || xml.IsEmpty)
				return null;
			if (xml["type"] != Type)
				throw new ArgumentOutOfRangeException("xml.type", xml["type"], null);
			return new DailySchedule(xml["day"].AsInt32(1), ScheduleReminder.FromXml(xml.Element("reminder")));
		}

		public new static DailySchedule FromJson(string json)
		{
			return FromXml(Xml.XmlLiteNode.FromJson(json, "schedule", forceAttributes: true));
		}
	}
}

