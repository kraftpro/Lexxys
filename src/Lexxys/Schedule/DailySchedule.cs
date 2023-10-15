// Lexxys Infrastructural library.
// file: DailySchedule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

[Serializable]
public class DailySchedule: Schedule, IEquatable<DailySchedule>
{
	public new const string Type = "daily";

	public DailySchedule(int dayPeriod = 0, ScheduleReminder? reminder = null): base(Type, reminder)
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

	public override StringBuilder ToString(StringBuilder text, IFormatProvider? provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));
		if (DayPeriod > 1)
			text.Append("every ").Append(Lingua.Ord(Lingua.NumWord(DayPeriod))).Append(" day");
		else
			text.Append("everyday");
		Reminder.ToString(text, provider);
		return text;
	}

	public bool Equals(DailySchedule? other)
		=> other is not null && (
			ReferenceEquals(this, other) ||
			base.Equals(other) && DayPeriod == other.DayPeriod);

	public override bool Equals(Schedule? other) => other is DailySchedule ds && Equals(ds);

	public override bool Equals(object? obj) => obj is DailySchedule ds && Equals(ds);

	public override int GetHashCode() => HashCode.Join(base.GetHashCode(), DayPeriod.GetHashCode());

	public override DumpWriter DumpContent(DumpWriter writer)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));
		return base.DumpContent(writer)
			.Then("DayPeriod", DayPeriod);
	}

	public override XmlBuilder ToXmlContent(XmlBuilder builder)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));
		builder.Item("type", ScheduleType);
		if (DayPeriod != 1)
			builder.Item("day", DayPeriod);
		if (!Reminder.IsEmpty)
			builder.Element("reminder").Value(Reminder).End();
		return builder;
	}

	public override JsonBuilder ToJsonContent(JsonBuilder json)
	{
		if (json is null) throw new ArgumentNullException(nameof(json));
		json.Item("type").Val(ScheduleType);
		if (DayPeriod != 1)
			json.Item("day").Val(DayPeriod);
		if (!Reminder.IsEmpty)
			json.Item("reminder").Val(Reminder);
		return json;
	}

	public new static DailySchedule FromXml(Xml.IXmlReadOnlyNode xml)
	{
		if (xml is null)
			throw new ArgumentNullException(nameof(xml));
		if (xml.IsEmpty)
			throw new ArgumentOutOfRangeException(nameof(xml), xml, null);
		if (xml["type"] != Type)
			throw new ArgumentOutOfRangeException(nameof(xml), xml, null);

		return new DailySchedule(xml["day"].AsInt32(1), ScheduleReminder.FromXml(xml.Element("reminder")));
	}

	public new static DailySchedule FromJson(string json)
	{
		if (json is not { Length: > 0 }) throw new ArgumentNullException(nameof(json));
		return FromXml(Xml.JsonToXmlConverter.Convert(json, "schedule", forceAttributes: true));
	}
}

