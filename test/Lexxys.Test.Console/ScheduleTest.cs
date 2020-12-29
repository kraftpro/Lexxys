using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;

namespace Lexxys.Test.Con
{
	class ScheduleTest
	{
		public static void Go()
		{
			var start = new DateTime(2017, 1, 1);
			var today = DateTime.Today;
			var ds = new DailySchedule(9);
			var dn = ds.Next(start, today);

			var ws = new WeeklySchedule(4);
			var wn = ws.Next(start.AddDays(5), start.AddDays(4));

			var ms = new MonthlySchedule(15);
			var mn = ms.Next(start, today);

			Console.WriteLine();
			Console.WriteLine($"start {start:D}  [{start.DayOfYear}]");
			Console.WriteLine($"today {today:D}  [{today.DayOfYear}]");
			Console.WriteLine($"{ds} -> {dn:D}  [{dn.GetValueOrDefault().DayOfYear}]");
			Console.WriteLine($"{ws} -> {wn:D}  [{wn.GetValueOrDefault().DayOfYear}]");
			Console.WriteLine($"{ms} -> {mn:D}  [{mn.GetValueOrDefault().DayOfYear}]");
			Console.ReadKey();

			//var dd = Schedule0.FromJson("{\"type\":\"none\",\"sh\":1,\"sh\":1,\"rm\":4,\"rb\":true}");
			//var ss = Lexxys.Data.Dc.GetList<string, string>("select Schedule, Schedule2 from TimeSchedules");
			//foreach (var ts in ss)
			//{
			//	var s = ts.Item1;
			//	var t = ts.Item2;

			//	var ds = Schedule0.FromJson(s);
			//	var s2 = ds.ToJson();
			//	if (s2 != s)
			//	{
			//		Console.WriteLine(s);
			//		Console.WriteLine(s2);
			//		Console.WriteLine();
			//	}
			//	var dj = Schedule0.FromJson(s2);
			//	if (ds != dj)
			//	{
			//		Console.WriteLine(ds);
			//		Console.WriteLine(dj);
			//		Console.WriteLine();
			//	}
			//	var sx = ds.ToXml();
			//	var dx = Schedule0.FromXml(sx);
			//	if (ds != dx)
			//	{
			//		Console.WriteLine(ds);
			//		Console.WriteLine(dx);
			//		Console.WriteLine();
			//	}

			//	var dt = Schedule.FromJson(t);
			//	var t2 = dt.ToJson();
			//	if (t2 != t)
			//	{
			//		Console.WriteLine(t);
			//		Console.WriteLine(t2);
			//		Console.WriteLine();
			//	}
			//	var dk = Schedule.FromJson(t2);
			//	if (dt != dk)
			//	{
			//		Console.WriteLine(dt);
			//		Console.WriteLine(dk);
			//		Console.WriteLine();
			//	}
			//	var ty = dt.ToXml();
			//	var dy = Schedule.FromXml(ty);
			//	if (dt != dy)
			//	{
			//		Console.WriteLine(dt);
			//		Console.WriteLine(dy);
			//		Console.WriteLine();
			//	}
			//}

			//var sc = Schedule0.CreateOneTime(3, true, BusinessDayShiftType.None);
			//var d = DateTime.Today;
			//var d1 = sc.Next(d);
			//var d2 = sc.Shift(d1, o => o != d);
		}
	}
}
