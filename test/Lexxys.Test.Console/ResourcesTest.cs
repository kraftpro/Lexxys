using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Testing;

namespace Lexxys.Test.Con
{
	public class ResourcesTest
	{
		public static void Go()
		{
			var emails = Resources.EMail;
			var lor = Resources.Lorems;
			for (int i = 0; i < 10; ++i)
			{
				Console.WriteLine(emails.NextValue());
			}
		}
	}
}
