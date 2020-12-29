// Lexxys Infrastructural library.
// file: XmlLiteSelectorTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Linq;
using System.Text;
using Lexxys.Testing;
using Lexxys.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Xml
{
	[TestClass]
	public class XmlLiteSelectorTest
	{
		#region Data Tables
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
		#endregion

		[TestMethod]
		public void XmlLiteNodeSelectTest()
		{
			string[][] tests = new[]
				{
					new[] { "",					"" },
				};
			string text = RandomXml(15, 10, 3);
			XmlLiteNode xml = XmlLiteNode.FromXml(text);
			var xx = XmlLiteNode.Select("**", xml);
			var aa = xx.ToArray();
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
