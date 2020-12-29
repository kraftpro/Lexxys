// Lexxys Infrastructural library.
// file: XmlToolsTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Lexxys;
﻿using Lexxys.Testing;
﻿using Lexxys.Xml;

namespace Lexxys.Tests.Xml
{
	/// <summary>
	///This is a test class for XmlToolsTest and is intended
	///to contain all XmlToolsTest Unit Tests
	///</summary>
	[TestClass]
	public partial class XmlToolsTest
	{
		/// <summary>
		///A test for GetTernary
		///</summary>
		[TestMethod]
		public void GetTernaryTest()
		{
			string value;
			Ternary defaultValue;
			Ternary expected;
			Ternary actual;
			Ternary[] expectedTable = new Ternary[] { Ternary.True, Ternary.False, Ternary.Unknown };
			string[][] table = new string[][]
				{
					new[] { "True", "On", "Yes", "Grant", "1" },
					new[] { "False", "Off", "No", "Deny", "0" },
					new[] { "Unknown", "Some", "Any", "All", "Both", "Default", "2" },
				};

			defaultValue = Ternary.False;
			for (int i = 0; i < table.Length; ++i)
			{
				expected = expectedTable[i];
				string[] vv = table[i];
				for (int j = 0; j < vv.Length; ++j)
				{
					value = vv[j];
					actual = XmlTools.GetTernary(value, defaultValue);
					Assert.AreEqual(expected, actual, value);

					value = vv[j].ToUpperInvariant();
					actual = XmlTools.GetTernary(value, defaultValue);
					Assert.AreEqual(expected, actual, value);

					value = vv[j].ToLowerInvariant();
					actual = XmlTools.GetTernary(value, defaultValue);
					Assert.AreEqual(expected, actual, value);

					value = new string(' ', Testing.Rand.Int(5)) + vv[j] + new string(' ', Testing.Rand.Int(5));
					actual = XmlTools.GetTernary(value, defaultValue);
					Assert.AreEqual(expected, actual, value);
				}
				defaultValue = Ternary.True;
			}
		}

		/// <summary>
		///A test for GetIndex
		///</summary>
		[TestMethod]
		public void GetIndexTest()
		{
			string[][] variants = new string[][]
				{
					new[] { "Zero", "One", "Two", },
					new[] { "Zero", "One", "Two", },
					new[] { "Zero", "One", "Two", },
					new[] { "Zero", "One", "Two", null, },
					new[] { "Zero", "One", "Two", null, },
				};
			string[] value = new[] 
				{
					"zero ",
					"OneTow",
					null,
					null,
					"",
				};
			int[] expected = new[]
				{
					0,
					-1,
					-1,
					3,
					3
				};
			int actual;

			for (int i = 0; i < variants.Length; ++i)
			{
				actual = XmlTools.GetIndex(value[i], variants[i]);
				Assert.AreEqual(expected[i], actual, value[i]);
			}
		}

		struct TestEnumItem
		{
			public Type EnumType;
			public string Value;
			public bool Success;
			public object Expected;

			public TestEnumItem(Type enumType, string value, bool success, object expected)
			{
				EnumType = enumType;
				Value = value;
				Success = success;
				Expected = expected;
			}
		}
		enum EnumTest1
		{
			MinusOne = -1,
			Zero = 0,
			One = 1
		}
		enum EnumTest2: sbyte
		{
			MinusOne = -1,
			Zero = 0,
			One = 1
		}

		[TestMethod]
		public void GetEnumTest()
		{
			TestEnumItem[] table = new TestEnumItem[]
				{
					new TestEnumItem(typeof(EnumTest1), "One",		true, EnumTest1.One),
					new TestEnumItem(typeof(EnumTest1), "One1",		false, default(EnumTest1)),
					new TestEnumItem(typeof(EnumTest1), " ONE ",	true, EnumTest1.One),
					new TestEnumItem(typeof(EnumTest1), "MinusOne", true, EnumTest1.MinusOne),
					new TestEnumItem(typeof(EnumTest1), "-1",		true, EnumTest1.MinusOne),
					new TestEnumItem(typeof(EnumTest1), "-2",		false, EnumTest1.Zero),

					new TestEnumItem(typeof(EnumTest2), "One",		true, EnumTest2.One),
					new TestEnumItem(typeof(EnumTest2), "One1",		false, default(EnumTest2)),
					new TestEnumItem(typeof(EnumTest2), " ONE ",	true, EnumTest2.One),
					new TestEnumItem(typeof(EnumTest2), "MinusOne", true, EnumTest2.MinusOne),
					new TestEnumItem(typeof(EnumTest2), "-1",		true, EnumTest2.MinusOne),
					new TestEnumItem(typeof(EnumTest2), "-2",		false, EnumTest2.Zero),

					new TestEnumItem(typeof(EnumTest1), null,		false, EnumTest1.Zero),
				};

			for (int i = 0; i < table.Length; ++i)
			{
				bool success;
				object actual;
				success = XmlTools.TryGetEnum(table[i].Value, table[i].EnumType, out actual);
				Assert.AreEqual(table[i].Expected, actual, i.ToString());
				Assert.AreEqual(table[i].Success, success, i.ToString());
			}
		}

		/// <summary>
		///A test for EncodeAttribute
		///</summary>
		[TestMethod]
		public void EncodeAttributeTest()
		{
			string[][] table = new string[][]
				{
					new[] {"on<e", "\"on&lt;e\""},
					new[] {">one'", "\"&gt;one'\""},
					new[] {"on<>e\"", "'on&lt;&gt;e\"'"},
					new[] {"'o'ne\"", "\"'o'ne&quot;\""},
					new[] {" ", "\" \""},
					new[] {null, "\"\""},
				};

			for (int i = 0; i < table.Length; ++i)
			{
				string value = table[i][0];
				string expected = table[i][1];
				string actual;
				actual = XmlTools.EncodeAttribute(value);
				Assert.AreEqual(expected, actual, value);
			}
		}

		[TestMethod]
		public void ParseDateTimeOffsetTestA()
		{
			for (int i = 0; i < 10000; ++i)
			{
				int year = Rand.Int(1, 10000);
				int month = Rand.Int(1, 13);
				int day = Rand.Int(1, DateTime.DaysInMonth(year, month) + 1);
				int hour = Rand.Int(0, 24);
				int minute = Rand.Int(0, 60);
				int second = Rand.Int(0, 60);
				int millisecond = Rand.Int(0, 1000);
				TimeSpan offset = TimeSpan.FromMinutes(Rand.Int(0, 60 * 14));
				ParseDateTimeOffsetTest(new DateTimeOffset(year, month, day, hour, minute, second, millisecond, offset));
			}
		}

		private void ParseDateTimeOffsetTest(DateTimeOffset value)
		{
			string[] dateFormats =
			{
				"+yyyy-MM-dd",
				"+yyyyMMdd",
				"--yyyy-MM-dd",
				"-yyyyMM-dd",
				"-yyyy-MMdd",
			};
			string[] timeFormats =
			{
				"+HH:mm:ss",
				"+HHmmss",
				"-HH:mmss",
				"-HH:mm:0ss",
			};
			foreach (var item1 in dateFormats)
			{
				bool correct = item1[0] == '+';
				string date = item1.Substring(1);
				string format = date;
				var actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
				if (!correct)
				{
					Assert.AreEqual(default(DateTimeOffset), actual, format);
				}
				else
				{
					Assert.AreEqual(value.Date, actual.Date, format);

					format = date + "THH:mm:ss";
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(DateTimeOffset.Now.Offset, actual.Offset, format);

					format = date + "THH:mm:ss.fffffff";
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(value.Millisecond, actual.Millisecond, format);
					Assert.AreEqual(DateTimeOffset.Now.Offset, actual.Offset, format);

					format = date + "THH:mm:ss.fffffffK";
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					Assert.AreEqual(value.Date, actual.Date, format);
					Assert.AreEqual(value.Hour, actual.Hour, format);
					Assert.AreEqual(value.Minute, actual.Minute, format);
					Assert.AreEqual(value.Second, actual.Second, format);
					Assert.AreEqual(value.Millisecond, actual.Millisecond, format);
					Assert.AreEqual(value.Offset, actual.Offset, format);
				}

				foreach (var item2 in timeFormats)
				{
					correct &= item2[0] == '+';
					string time = item2.Substring(1);
					format = date + "T" + time;
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					if (!correct)
						Assert.AreEqual(default(DateTimeOffset), actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
					format = date + "T" + time + " z";
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					if (!correct)
						Assert.AreEqual(default(DateTimeOffset), actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
					format = date + "T" + time + "zz";
					actual = XmlTools.GetDateTimeOffset(value.ToString(format), default(DateTimeOffset));
					if (!correct)
						Assert.AreEqual(default(DateTimeOffset), actual, format);
					else
						Assert.AreEqual(value.Date, actual.Date, format);
				}
			}
		}
	}
}
