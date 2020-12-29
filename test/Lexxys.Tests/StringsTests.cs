using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests
{
	[TestClass]
	public class StringsTests
	{
		[TestMethod]
		[DataRow("a\tb\"", @"""a\tb\""""", '"')]
		[DataRow("za\tb\"", @"z\za\tb""z", 'z')]
		[DataRow("a'\tb\"", @"'a\'\tb""'", '\'')]
		[DataRow("a\t\0b\"", @"a\t\0b""", '\0')]
		[DataRow("a\v\x0b-ad", @"a\v\v-ad", '\0')]
		[DataRow("a\t\f\v\0b\"\x0e-ad", @"a\t\f\v\0b""\x000e-ad", '\0')]
		public void EscapeCsStringTest(string value, string escaped, char marker)
		{
			Assert.AreEqual(escaped, Strings.EscapeCsString(value, marker));
		}

		[TestMethod]
		public void RemoveExtraBracesTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void SplitByCapitalsTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void SplitByWordBoundTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToTitleCaseTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToCamelCaseTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToPascalCaseTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void EllipsisTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void JoinAndTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToHexCharArrayTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToHexCharArrayTest1()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToHexCharArrayTest2()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToHexStringTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void ToBitsStringTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void CutIndentsTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void EncodeUrlTest()
		{
			Assert.Inconclusive();
		}

		[TestMethod]
		public void DecodeUrlTest()
		{
			Assert.Inconclusive();
		}
	}
}