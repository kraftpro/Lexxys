using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys.Data;

namespace Lexxys.Test.Con
{
	class Caches
	{
		public static void Go()
		{
			using (var dc = new DataContext())
			{
				var items = dc.GetList<int>("select ID from Users");
				for (int i = 0; i < items.Count; ++i)
				{
					if (!Check.ReferenceKey(dc, items[i], "Users", "ID"))
						Debugger.Break();
				}
			}
			Console.WriteLine("DONE");
			Console.ReadKey();
		}
	}
}
