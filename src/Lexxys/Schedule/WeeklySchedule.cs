// Lexxys Infrastructural library.
// file: WeeklySchedule.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Globalization;
using System.Text;

namespace Lexxys;

[Serializable]
public class WeeklySchedule: Schedule, IEquatable<WeeklySchedule>
{
	public new const string Type = "weekly";

	public WeeklySchedule(int weekPeriod = 0, IEnumerable<DayOfWeek>? dayList = null, ScheduleReminder? reminder = null): base(Type, reminder)
	{
		WeekPeriod = Math.Max(1, weekPeriod);
		if (dayList == null)
		{
			DayList = FridayOnly;
		}
		else
		{
			List<DayOfWeek> ss = new SortedSet<DayOfWeek>(dayList.Where(o => o is >= DayOfWeek.Sunday and <= DayOfWeek.Saturday)).ToList();
			DayList = ss.Count == 0 ? FridayOnly: ss.Count == 1 ? OneDayOnly[(int)ss[0]]: ReadOnly.Wrap(ss)!;
		}
	}
	private static readonly IReadOnlyList<DayOfWeek>[] OneDayOnly =
	[
		ReadOnly.Wrap(new[] { DayOfWeek.Sunday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Monday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Tuesday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Wednesday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Thursday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Friday })!,
		ReadOnly.Wrap(new[] { DayOfWeek.Saturday })!,
	];
	private static readonly IReadOnlyList<DayOfWeek> FridayOnly = OneDayOnly[(int)DayOfWeek.Friday];

	public int WeekPeriod { get; }
	public IReadOnlyList<DayOfWeek> DayList { get; }

	public override DateTime? Next(DateTime startTime, DateTime? previousTime = null)
	{
		DateTime start = startTime.Date;
		DateTime prev = previousTime ?? start.AddDays(-1);
		DateTime week = start.AddDays(-(int)start.DayOfWeek);
		var diff = (prev - week).Days - 1;
		int period = WeekPeriod * 7;
		if (diff > 6 - 1)
			week = week.AddDays(diff - diff % period);

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

	public override StringBuilder ToString(StringBuilder text, IFormatProvider? provider, bool abbreviateDayName = false, bool abbreviateMonthName = false)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));

		var format = provider?.GetFormat(typeof(DateTimeFormatInfo)) as DateTimeFormatInfo ?? CultureInfo.CurrentCulture.DateTimeFormat;
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

	public bool Equals(WeeklySchedule? other) =>
		other is not null && (
			ReferenceEquals(this, other) ||
			WeekPeriod == other.WeekPeriod && Comparer.Equals(DayList, other.DayList));

	public override bool Equals(Schedule? other) => other is WeeklySchedule ws && Equals(ws);

	public override bool Equals(object? obj) => obj is WeeklySchedule ws && Equals(ws);

	public override int GetHashCode()
		=> HashCode.Join(HashCode.Join(base.GetHashCode(), WeekPeriod.GetHashCode()), DayList);

	public override DumpWriter DumpContent(DumpWriter writer)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));
		return base.DumpContent(writer)
			.Then("WeekPeriod", WeekPeriod)
			.Then("DayList", DayList);
	}

	public override XmlBuilder ToXmlContent(XmlBuilder builder)
	{
		if (builder is null) throw new ArgumentNullException(nameof(builder));

		builder.Item("type", ScheduleType);
		if (WeekPeriod != 1)
			builder.Item("week", WeekPeriod);
		if (!Object.ReferenceEquals(DayList, FridayOnly))
			builder.Item("days", String.Join(",", DayList.Select(o => ((int)o).ToString())));
		if (!Reminder.IsEmpty)
			builder.Element("reminder").Value(Reminder).End();
		return builder;
	}

	public override JsonBuilder ToJsonContent(JsonBuilder json)
	{
		if (json is null) throw new ArgumentNullException(nameof(json));

		json.Item("type").Val(ScheduleType);
		if (WeekPeriod != 1)
			json.Item("week").Val(WeekPeriod);
		if (!Object.ReferenceEquals(DayList, FridayOnly))
			json.Item("days").Val(DayList.Select(o => (int)o));
		if (!Reminder.IsEmpty)
			json.Item("reminder").Val(Reminder);
		return json;
	}

	public new static WeeklySchedule FromXml(Xml.IXmlReadOnlyNode xml)
	{
		if (xml is null) throw new ArgumentNullException(nameof(xml));
		if (xml.IsEmpty) throw new ArgumentOutOfRangeException(nameof(xml), xml, null);
		if (xml["type"] != Type) throw new ArgumentOutOfRangeException(nameof(xml), xml, null);

		IEnumerable<DayOfWeek>? days = xml["days"] != null ?
			xml["days"]?.Split(',').Select(o => o.AsEnum(DayOfWeek.Friday)) :
			xml.Element("days").Elements.Select(o => o.Value.AsEnum<DayOfWeek>());
		return new WeeklySchedule(xml["week"].AsInt32(1), days, ScheduleReminder.FromXml(xml.Element("reminder")));
	}

	public new static WeeklySchedule FromJson(string json)
	{
		if (json is not { Length: > 0 }) throw new ArgumentNullException(nameof(json));
		return FromXml(Xml.XmlTools.FromJson(json, "schedule", forceAttributes: true));
	}
}

