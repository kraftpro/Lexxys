// Lexxys Infrastructural library.
// file: IniToXmlConverterTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using Lexxys.Xml;

namespace Lexxys.Tests.Xml
{

	/// <summary>
	///This is a test class for IniToXmlConverterTest and is intended
	///to contain all IniToXmlConverterTest Unit Tests
	///</summary>
	[TestClass()]
	public class IniToXmlConverterTest
	{
		/// <summary>
		///A test for Convert
		///</summary>
		[TestMethod()]
		public void ConvertIniToXmlTest()
		{
			string[] tables =
			[
@"[s_A] ; comment
a=1
b = 2
; comment
",
@"<s_A a='1' b='2'/>",

			];

			for (int i = 1; i < tables.Length; i += 2)
			{
				string xml = IniToXmlConverter.Convert(tables[i - 1]);
				Assert.IsNotNull(xml);
				IXmlReadOnlyNode actual = XmlTools.FromXml(xml, true);
				IXmlReadOnlyNode expected = XmlTools.FromXml(tables[i], true);
				Assert.AreEqual(expected, actual, tables[i-1]);
			}
		}
	}
}
