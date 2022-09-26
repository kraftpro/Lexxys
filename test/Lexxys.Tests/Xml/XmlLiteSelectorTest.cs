// Lexxys Infrastructural library.
// file: XmlLiteSelectorTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Xml
{
	using Lexxys.Testing;
	using Lexxys.Xml;

	[TestClass]
	public class XmlLiteSelectorTest
	{
		[TestMethod]
		public void XmlLiteNodeSelectTest()
		{
			string text = "<root>" + RandomXml(15, 10, 3) + "</root>";
			var source = "source: " + text;
			XmlLiteNode xml = XmlLiteNode.FromXml(text);
			var xx = XmlLiteNode.Select("*", xml);
			var aa = xx.ToArray();
			Assert.AreEqual(1, aa.Length, source);
			Assert.AreEqual(xml.ToString(), aa[0].ToString(), source);
			xx = XmlLiteNode.Select("*.node1", xml);
			aa = xx.ToArray();
			Assert.IsTrue(aa.All(o => o.Name == "node1"), source);
			xx = XmlLiteNode.Select("**.node1", xml);
			var bb = xx.ToArray();
			Assert.IsTrue(bb.All(o => o.Name == "node1"), source);
			Assert.IsTrue(aa.Length <= bb.Length);
			xx = XmlLiteNode.Select("**.node1.**.node2", xml);
			aa = xx.ToArray();
			Assert.IsTrue(aa.All(o => o.Name == "node2"), source);
		}

		private string RandomXml(int depth, int nodes, int attribs)
		{
			return RandomXml(new StringBuilder(), depth, nodes, attribs).ToString();
		}

		private StringBuilder RandomXml(StringBuilder text, int depth, int nodes, int attribs)
		{
			if (depth == 0)
				return text;

			int n = Rand.Int(nodes);
			int a = r.Next(attribs);
			int k = Rand.Int(n) + 1;
			text.Append(Environment.NewLine);
			text.Append("<node").Append(k);
			if (a > 0)
			{
				for (int i = 0; i < a; ++i)
				{
					text.Append(" a").Append(i + 1).Append('=').Append(XmlTools.EncodeAttribute(R.LetterOrDigit(Rand.Int(3, 15))));
				}
			}
			string value = R.LetterOrDigit(Rand.Int(0, 20));

			if (n == 0 && value.Length == 0)
			{
				text.Append("/>");
			}
			else
			{
				text.Append(">");
				if (depth > 1)
					text.Append(Environment.NewLine);
				text.Append(XmlTools.Encode(value));
				for (int i = 0; i < n; ++i)
				{
					RandomXml(text, Rand.Int(depth), nodes, attribs);
				}
				if (depth > 1)
					text.Append(Environment.NewLine);
				text.Append("</node").Append(k).Append(">");
			}
			return text;
		}

		private static Random r = new Random();
	}
}
