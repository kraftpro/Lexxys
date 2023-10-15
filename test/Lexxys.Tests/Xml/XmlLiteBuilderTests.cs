namespace Lexxys.Tests.Xml
{
	using Lexxys.Xml;

	[TestClass]
	public class XmlLiteBuilderTests
	{
		[TestMethod]
		public void BuildReadOnlyXmlTest()
		{
			var xmlt = XmlFragBuilder.Create<IXmlReadOnlyNode>();
			xmlt.Begin("data1");
			xmlt.Begin("data1.1");
			xmlt.End();
			xmlt.End();
			Assert.AreEqual(1, xmlt.GetNodes().Count);
			Assert.AreEqual("<data1><data1.1/></data1>", String.Join("", xmlt.GetNodes()));

			xmlt = XmlFragBuilder.Create<IXmlReadOnlyNode>();
			xmlt.Begin("data")
					.Begin("node").Attrib("one", 1).Attrib("tho", 2).Value("text").End()
					.Begin("second")
						.Begin("td").Value(new DateTime(2018, 5, 15)).End()
					.End()
					.Begin("self").Attrib("name", "self").End()
				.End()
				.Begin("item").Attrib("script1", "execute(\"\");").Attrib("script2", "execute(\"\");").Attrib("script2", "'execute(\"\");'").End();

			var x = xmlt.GetNodes();
			Assert.AreEqual(2, x.Count);
			Assert.AreEqual(3, x[0].Elements.Count);
			Assert.AreEqual("<data><node one=\"1\" tho=\"2\">text</node><second><td>2018-05-15</td></second><self name=\"self\"/></data><item script1='execute(\"\");' script2=\"'execute(&quot;&quot;);'\"/>", String.Join("", x));
		}

		[TestMethod]
		public void BuildReadWriteXmlTest()
		{
			var xmlt = XmlFragBuilder.Create<IXmlNode>();
			xmlt.Begin("data1");
			xmlt.Begin("data1.1");
			xmlt.End();
			xmlt.End();
			Assert.AreEqual(1, xmlt.GetNodes().Count);
			Assert.AreEqual("<data1><data1.1/></data1>", String.Join("", xmlt.GetNodes()));

			xmlt = XmlFragBuilder.Create<IXmlNode>();
			xmlt.Begin("data")
					.Begin("node").Attrib("one", 1).Attrib("tho", 2).Value("text").End()
					.Begin("second")
						.Begin("td").Value(new DateTime(2018, 5, 15)).End()
					.End()
					.Begin("self").Attrib("name", "self").End()
				.End()
				.Begin("item").Attrib("script1", "execute(\"\");").Attrib("script2", "execute(\"\");").Attrib("script2", "'execute(\"\");'").End();

			var x = xmlt.GetNodes();
			Assert.AreEqual(2, x.Count);
			Assert.AreEqual(3, x[0].Elements.Count);
			Assert.AreEqual("<data><node one=\"1\" tho=\"2\">text</node><second><td>2018-05-15</td></second><self name=\"self\"/></data><item script1='execute(\"\");' script2=\"'execute(&quot;&quot;);'\"/>", String.Join("", x));
		}
	}
}
