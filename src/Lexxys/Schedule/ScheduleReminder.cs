// Lexxys Infrastructural library.
// file: ScheduleReminder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

public enum BusinessDayShiftType
{
	None = 0,
	Backward = 1,
	Forward = 2,
}

[Serializable]
public sealed class ScheduleReminder: IDump, IEquatable<ScheduleReminder>
{
	public static readonly ScheduleReminder Empty = new ScheduleReminder();

	/// <summary>
	/// Date and time interval for remainder.
	/// </summary>
	public TimeSpan Value { get; }
	/// <summary>
	/// Indicates that the <see cref="Value"/> is set in business days.
	/// </summary>
	public bool RemindInBusinessDays { get; }
	/// <summary>
	/// Move the scheduled time to the nearest business day.
	/// </summary>
	public BusinessDayShiftType ShiftToBusinessDay { get; }

	public ScheduleReminder(TimeSpan reminder = default, bool remindInBusinessDays = false, BusinessDayShiftType shiftToBusinessDay = BusinessDayShiftType.None)
	{
		Value = reminder < TimeSpan.Zero ? TimeSpan.Zero:
			new TimeSpan(reminder.Ticks -
				(reminder.Ticks >= TimeSpan.TicksPerDay ?
					reminder.Ticks % TimeSpan.TicksPerHour:
					reminder.Ticks % TimeSpan.TicksPerMinute));
		RemindInBusinessDays = remindInBusinessDays;
		ShiftToBusinessDay = shiftToBusinessDay;
	}

	/// <summary>
	/// Calculates date and time for <see cref="Value"/> of the specified <paramref name="scheduledTime"/>.
	/// </summary>
	/// <param name="scheduledTime">The scheduled date and time</param>
	/// <param name="businessDay">Predicate for business days calculations</param>
	/// <returns>Date and time when for the specified reminder</returns>
	public DateTime? Remind(DateTime? scheduledTime, Func<DateTime, bool>? businessDay = null)
	{
		if (scheduledTime == null) return null;
		var nextDate = scheduledTime.GetValueOrDefault();
		if (ShiftToBusinessDay != BusinessDayShiftType.None && businessDay != null)
		{
			while (!businessDay(nextDate))
			{
				nextDate += ShiftToBusinessDay == BusinessDayShiftType.Forward ? Tomorrow: Yesterday;
			}
		}

		TimeSpan reminder = Value;
		if (reminder.Ticks < TimeSpan.TicksPerDay)
			return nextDate - reminder;
		if (!RemindInBusinessDays || businessDay == null)
			return nextDate - reminder;

		do
		{
			nextDate += Yesterday;
			while (!businessDay(nextDate))
			{
				nextDate += Yesterday;
			}
			reminder += Yesterday;
		} while (reminder.Ticks >= TimeSpan.TicksPerDay);
		return nextDate - reminder;
	}
	private static readonly TimeSpan Tomorrow = new TimeSpan(TimeSpan.TicksPerDay);
	private static readonly TimeSpan Yesterday = new TimeSpan(-TimeSpan.TicksPerDay);

	public bool IsEmpty => ShiftToBusinessDay == BusinessDayShiftType.None && Value == TimeSpan.Zero;

	public override string ToString() => ToString(new StringBuilder(), null).ToString();

	public StringBuilder ToString(StringBuilder text, IFormatProvider? provider)
	{
		if (text is null) throw new ArgumentNullException(nameof(text));

		if (ShiftToBusinessDay != BusinessDayShiftType.None)
		{
			text.Append(" or the nearest business day ")
				.Append(ShiftToBusinessDay == BusinessDayShiftType.Backward ? "prior": "after")
				.Append(" the scheduled date");
		}
		if (Value <= TimeSpan.Zero) return text;
		
		text.Append("; ");
		if (Value.Days > 0)
			text.Append(Lingua.NumWord(Value.Days))
				.Append(' ')
				.Append(RemindInBusinessDays ? "business day": "calendar day").Append(Value.Days == 1 ? "": "s");
		if (Value.Hours > 0)
			text.Append(Value.Days == 0 ? "": Value.Minutes > 0 ? ", ": " and ")
				.Append(Value.Hours)
				.Append(" hour").Append(Value.Hours > 1 ? "s": "");
		if (Value.Minutes > 0)
			text.Append(Value is { Days: 0, Hours: 0 } ? "": " and ")
				.Append(Value.Minutes)
				.Append(" minute").Append(Value.Minutes > 1 ? "s": "");
		text.Append(" before the ").Append(ShiftToBusinessDay == BusinessDayShiftType.None ? "scheduled ": "").Append(Value is { Hours: 0, Minutes: 0 } ? "date": "time");
		return text;
	}

	public bool Equals(ScheduleReminder? other) =>
		other is not null && (
			ReferenceEquals(this, other) ||
			Value == other.Value && RemindInBusinessDays == other.RemindInBusinessDays && ShiftToBusinessDay == other.ShiftToBusinessDay);

	public override bool Equals(object? obj) => obj is ScheduleReminder reminder && Equals(reminder);

	public override int GetHashCode() => HashCode.Join(Value.GetHashCode(), RemindInBusinessDays.GetHashCode(), ShiftToBusinessDay.GetHashCode());

	public void DumpContent(IDumpWriter writer)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));

		if (Value != TimeSpan.Zero || writer.Level > ObjectDumpLevel.Regular)
		{
			writer.Write("Value", Value);
			if (RemindInBusinessDays || writer.Level > ObjectDumpLevel.Regular)
				writer.Write("businessDays", RemindInBusinessDays);
		}
		if (ShiftToBusinessDay != BusinessDayShiftType.None || writer.Level > ObjectDumpLevel.Regular)
			writer.Write("shift", ShiftToBusinessDay);
	}

	public static ScheduleReminder Create(TimeSpan reminder = default, bool remindInBusinessDays = false, BusinessDayShiftType shiftToBusinessDay = BusinessDayShiftType.None) =>
		reminder <= TimeSpan.Zero && shiftToBusinessDay == BusinessDayShiftType.None ? Empty:
			new ScheduleReminder(reminder, remindInBusinessDays, shiftToBusinessDay);

	public static ScheduleReminder FromXml(Xml.IXmlReadOnlyNode? xml) =>
		xml == null || xml.IsEmpty ? Empty:
			new ScheduleReminder(xml["value"].AsTimeSpan(default), xml["businessDays"].AsBoolean(false), xml["shift"].AsEnum(default(BusinessDayShiftType)));
}

