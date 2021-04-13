// Lexxys Infrastructural library.
// file: Schedule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Globalization;
using System.Text;

namespace Lexxys
{
	public class Schedule: IDump, IDumpJson, IDumpXml, IEquatable<Schedule>
	{
		public const string Type = "none";

		public Schedule(ScheduleReminder reminder = null)
		{
			ScheduleType = Type;
			Reminder = reminder;
		}

		protected Schedule(string type, ScheduleReminder reminder)
		{
			ScheduleType = type ?? throw new ArgumentNullException(nameof(type));
			Reminder = reminder ?? ScheduleReminder.Empty;
		}

		/// <summary>
		/// The schedule reminder
		/// </summary>
		public ScheduleReminder Reminder { get; }
		/// <summary>
		/// Type of the schedule
		/// </summary>
		public string ScheduleType { get; }

		/// <summary>
		/// Calculates the next scheduled time
		/// </summary>
		/// <param name="startTime">The initial start time when the schedule</param>
		/// <param name="previousTime">Result of the previous call of the <see cref="Next(DateTime, DateTime?)"/>.</param>
		/// <returns>The next scheduled date and time or null.</returns>
		public virtual DateTime? Next(DateTime startTime, DateTime? previousTime = null)
		{
			return startTime.Date <= previousTime ? (DateTime?)null: startTime;
		}

		/// <summary>
		/// Calculates date and time for <see cref="Reminder"/> of the specified <paramref name="scheduledTime"/>.
		/// </summary>
		/// <param name="scheduledTime">The scheduled date and time</param>
		/// <param name="businessDay">Predicate for business days calcualtions</param>
		/// <returns>Date and time when for the specified reminder</returns>
		public DateTime? Remind(DateTime? scheduledTime, Func<DateTime, bool> businessDay = null)
		{
			return Reminder.Remind(scheduledTime, businessDay);
		}

		public override string ToString()
		{
			return ToString(null);
		}

		public string ToString(IFormatProvider provider)
		{
			return ToString(new StringBuilder(), provider).ToString();
		}

		public virtual StringBuilder ToString(StringBuilder text, IFormatProvider provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
		{
			Reminder.ToString(text, provider);
			return text;
		}

		public override bool Equals(object obj)
		{
			return obj is Schedule sc && Equals(sc);
		}

		public override int GetHashCode()
		{
			var h1 = ScheduleType.GetHashCode();
			var h2 = Reminder.GetHashCode();
			return HashCode.Join(h1, h2);
		}

		public virtual bool Equals(Schedule that)
		{
			if (that is null)
				return false;
			if (ReferenceEquals(this, that))
				return true;
			if (ScheduleType != that.ScheduleType)
				return false;
			if (!Reminder.Equals(that.Reminder))
				return false;
			return true;
		}

		public static bool operator ==(Schedule left, Schedule right)
		{
			return left is null ? right is null : left.Equals(right);
		}

		public static bool operator !=(Schedule left, Schedule right)
		{
			return left is null ? right is null : !left.Equals(right);
		}

		public virtual DumpWriter DumpContent(DumpWriter writer)
		{
			return writer
				.Text("TypeScheduleType=").Dump(ScheduleType)
				.Text(",Reminder=").Dump(Reminder);
		}

		string IDumpXml.XmlElementName => "schedule";

		public virtual XmlBuilder ToXmlContent(XmlBuilder xml)
		{
			xml.Item("type", ScheduleType);
			if (!Reminder.IsEmpty)
				xml.Element("reminder").Value(Reminder).End();
			return xml;
		}

		public virtual JsonBuilder ToJsonContent(JsonBuilder json)
		{
			json.Item("type").Val(ScheduleType);
			if (!Reminder.IsEmpty)
				json.Item("reminder").Val(Reminder);
			return json;
		}

		public static Schedule FromXml(Xml.XmlLiteNode xml)
		{
			if (xml == null || xml.IsEmpty)
				return null;
			return xml["type"] switch
			{
				Type => new Schedule(ScheduleReminder.FromXml(xml.Element("reminder"))),
				DailySchedule.Type => DailySchedule.FromXml(xml),
				WeeklySchedule.Type => WeeklySchedule.FromXml(xml),
				MonthlySchedule.Type => MonthlySchedule.FromXml(xml),
				_ => throw new ArgumentOutOfRangeException(nameof(xml) + ".type", xml["type"], null)
			};
		}

		public static Schedule FromXml(string xml)
		{
			return FromXml(Xml.XmlLiteNode.FromXml(xml));
		}

		public static Schedule FromJson(string json)
		{
			return FromXml(Xml.XmlLiteNode.FromJson(json, "schedule", forceAttributes: true));
		}
	}
}


