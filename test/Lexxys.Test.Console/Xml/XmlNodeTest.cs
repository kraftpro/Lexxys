using Lexxys.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Lexxys.Test.Con.Xml
{
	class XmlNodeTest
	{
		private static string[] Xml = new string[]
		{
@"
<root>
	Root Value1
	<node1 a='a1' b='b1'>
		Node1 Value
	</node1>
	Root Value2
	<node2 c='c2' d='d2' e='' />
	Root Value3
</root>"
		};

		public static void Go()
		{
			XmlLiteNodeXPathNavigatorConstructorTest();
		}

		public static void XmlLiteNodeXPathNavigatorConstructorTest()
		{
			bool ignoreCase = false;
			do
			{
				for (int i = 0; i < Xml.Length; ++i)
				{
					var rdr = XmlReader.Create(new StringReader(Xml[i]));
					var expected = XmlLiteNode.FromXml(rdr, ignoreCase);

					rdr = XmlReader.Create(new StringReader(Xml[i]));
					XPathNavigator navigator = new XPathDocument(rdr).CreateNavigator();
					navigator.MoveToFirstChild();
					var actual = new XmlLiteNode(navigator, ignoreCase);

					Debug.Assert(expected.ToString() == actual.ToString());
				}
				ignoreCase = !ignoreCase;
			} while (ignoreCase);
		}
	}
}
