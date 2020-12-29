using Lexxys;
using Lexxys.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	class ConfigTest
	{
		public static void Go()
		{
			Config.AddConfiguration(@"C:\Application\Config\fsadmin.config.txt");
			var c = Config.GetValue("FsAdmin", XmlLiteNode.Empty);
			Console.WriteLine(c.Elements.Count);
		}

	}
}
