// Lexxys Infrastructural library.
// file: Dat.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Data;
using System.IO;

namespace Lexxys.Test.Con
{
	class Dat
	{
		public static void Run()
		{
//			var rr = new List<Tuple<int, int, string, string>>();
//			Dc.SetConnection("red\\b", "CharityPlanner", "sa", "post+Office2");
//			//Dc.SetConnection("nest-fly", "CharityPlanner", "sa", "post+Office2");
//			Dc.Map("select ID,Format,Name,Text from DocumentTemplates", (r, i) =>
//			{
//				rr.Add(Tuple.Create(r[0] as int? ?? 0, r[1] as int? ?? 0, r[2] as string, r[3] as string));
//				return true;
//			});
//			foreach (var r in rr)
//			{
//				using (var f = File.CreateText(String.Format(@"D:\Temp\ReportsTemplates\{0:000}-{2}.{3}", r.Item1, r.Item2, r.Item3, r.Item2 == 2 ? "rtf": "xsl")))
//				{
//					f.Write(r.Item4);
//				}
//			}
		}

		public static void TestDc()
		{
			int? foundation = 6;
			string name = null;
			string symbol = "";
			string cusip = "";
			using (var dc = new DataContext())
			{
				int assetId = dc.GetValue<int>(@"
				select coalesce
					(
					(select ID from GlobalAssets where CUSIP = @C),
					(select ID from GlobalAssets where CUSIP = @S),
					(select ID from GlobalAssets where CUSIP = @N),
					(select ID from GlobalAssets where Symbol = @S),
					(select ID from GlobalAssets where Symbol = @C),
					(select ID from GlobalAssets where Symbol = @N),
					(select case when 1 =
							(
							select count(a.ID) from Assets a
								left join GlobalAssets ga ON ga.ID = a.ID
								left join LocalAssets la ON la.ID = a.ID and la.Foundation = @F
							where a.Name IN (@N, @C, @S)
								and (la.ID IS NOT NULL OR ga.ID IS NOT NULL)
							)
						then
							(
							select top 1 a.ID from Assets a
								left join GlobalAssets ga ON ga.ID = a.ID
								left join LocalAssets la ON la.ID = a.ID and la.Foundation = @F
							where a.Name IN (@N, @C, @S)
								and (la.ID IS NOT NULL OR ga.ID IS NOT NULL)
							)
						else null end)
					);",
				Dc.Parameter("@F", foundation),
				Dc.Parameter("@N", name),
				Dc.Parameter("@C", cusip),
				Dc.Parameter("@S", symbol));
				return;
			}
		}
	}
}
