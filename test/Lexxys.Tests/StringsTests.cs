using System.Text.RegularExpressions;

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
		[DataRow("", "")]
		[DataRow(" (  ", "(")]
        [DataRow("( ( )", "(")]
        [DataRow("( ( .   ) )", ".")]
		public void RemoveExtraBracesTest(string original, string expected)
		{
			var value = Strings.RemoveExtraBraces(original);
			Assert.AreEqual(expected, value);
		}

		[TestMethod]
		[DataRow("A", "A")]
		[DataRow("abcDef", "abc;Def")]
		[DataRow("ABCDef", "ABC;Def")]
		[DataRow("ABCDefGHI", "ABC;Def;GHI")]
		[DataRow("ABCD_._ef,,,ghi", "ABCD;_._;ef;,,,;ghi")]
		public void SplitByCapitalsTest(string value, string expected)
		{
			var results = String.Join(";", Strings.SplitByCapitals(value).Select(o => value.Substring(o.Index, o.Length)));
			Assert.AreEqual(expected, results);
		}

		[TestMethod]
		[DataRow("asd der fereis", 3, 5)]
		[DataRow("  abc       def ghij s ssss ", 3, 8)]
		[DataRow("\n\n  abc       def gh\n\n\nij s\n ssss ", 3, 8)]
		public void SplitByWordBoundTest(string value, int min, int max)
		{
			for (int i = min; i <= max; ++i)
			{
				var xx = Strings.SplitByWordBound(value, i);
				var expected = Regex.Replace(value, @"\s+", "");
				var constructed = String.Join("", xx.Select(o => value.Substring(o.Index, o.Length)));
				var condensed = Regex.Replace(constructed, @"\s+", "");
				Assert.AreEqual(expected, condensed);
				foreach (var x in xx)
				{
					Assert.IsTrue(x.Length > 0, $"\"{x}\".Length == 0");
					Assert.IsTrue(x.Length <= i, $"\"{x}\".Length = {x.Length} <= {i}");
				}
			}
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
			var lines = new []
			{
				"\t\tselect",
				"\t\t\t1,",
				"\t\t\t2,",
				"\t\t\t3",
				"\tfrom Here"
			};
			var expected = String.Join(Environment.NewLine, lines.Select(o => o.Substring(1).Replace("\t", "    ")));
			var actual = Strings.CutIndents(lines, 4);
			Assert.AreEqual(expected, actual);

			lines =
			[
				"  select",
				"\t\t\t1,",
				"\t\t\t2,",
				"\t\t\t3",
				"\tfrom Here"
			];
			expected = String.Join(Environment.NewLine, lines.Select(o => o.Substring(1).Replace("\t", "  "))).TrimStart();
			actual = Strings.CutIndents(lines, 2);
			Assert.AreEqual(expected, actual);

			lines =
			[
				"  select",
				"\t\t1,",
				"\tfrom Here"
			];
			expected = "select" + Environment.NewLine +
				"      1," + Environment.NewLine +
				"  from Here";
			actual = Strings.CutIndents(lines, 4);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void EncodeUrlTest()
		{
			//Strings.EncodeUrl
			Assert.Inconclusive();
		}

		[TestMethod]
		public void DecodeUrlTest()
		{
			Assert.Inconclusive();
		}
	}
}