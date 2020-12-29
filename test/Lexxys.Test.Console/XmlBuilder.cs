using Lexxys;
using Lexxys.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Lexxys.Test.Con
{
	public class XmlBuilderTest
	{
		public static string Go()
		{
			var xmlt = new XmlStringBuilder();
			xmlt.Element("node")
				.Attrib("one").Value(1)
				.Item("tho", 2)
				.Value("")
				.End();
			xmlt.Element("second").Element("td", DateTime.Today);
			xmlt.Element("self").Value(xmlt).End();
			return xmlt.ToString();
		}
	}
}
