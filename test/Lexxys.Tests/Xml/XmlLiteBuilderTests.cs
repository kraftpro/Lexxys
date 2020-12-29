using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Lexxys.Tests.Xml
{
	using Lexxys.Xml;

	[TestClass]
	public class XmlLiteBuilderTests
	{
		[TestMethod]
		public void BuildXmlTest()
		{
			var xmlt = new XmlLiteBuilder();
			xmlt.Element("data1");
			xmlt.Element("data1.1");
			xmlt.End();
			xmlt.End();
			Assert.AreEqual(1, xmlt.GetNodes().Count);
			Assert.AreEqual("<data1><data1.1/></data1>", String.Join("", xmlt.GetNodes()));

			xmlt = new XmlLiteBuilder();
			xmlt.Element("data")
					.Element("node").Attrib("one", 1).Attrib("tho", 2).Value("text").End()
					.Element("second")
						.Element("td", new DateTime(2018, 5, 15)).End()
					.End()
					.Element("self").Attrib("name", "self").End()
				.End()
				.Element("item").Attrib("script1", "execute(\"\");").Attrib("script2", "execute(\"\");").Attrib("script2", "'execute(\"\");'").End();

			var x = xmlt.GetNodes();
			Assert.AreEqual(2, x.Count);
			Assert.AreEqual(3, x[0].Elements.Count);
			Assert.AreEqual("<data><node one=\"1\" tho=\"2\">text</node><second><td>2018-05-15</td></second><self name=\"self\"/></data><item script1='execute(\"\");' script2=\"'execute(&quot;&quot;);'\"/>", String.Join("", x));
		}
	}
}
