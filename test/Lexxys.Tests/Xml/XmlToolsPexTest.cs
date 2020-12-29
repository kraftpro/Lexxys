// Lexxys Infrastructural library.
// file: XmlToolsPexTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexxys;
using Lexxys.Xml;
using System.Text.RegularExpressions;
﻿using Lexxys.Testing;
//using Microsoft.Pex.Framework;
﻿//using Microsoft.Pex.Framework.Generated;
//using Microsoft.Pex.Framework.Validation;

namespace Lexxys.Tests.Xml
{
	
	
	/// <summary>
	///This is a test class for XmlToolsTest and is intended
	///to contain all XmlToolsTest Unit Tests
	///</summary>
	[TestClass()]
	public partial class XmlToolsPexTest
	{
		private TestContext _testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return _testContextInstance;
			}
			set
			{
				_testContextInstance = value;
			}
		}

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion



		[TestMethod]
		public void GetTomeSpanPexVariants()
		{
			GetTimeSpanPex2(TimeSpan.FromSeconds(0));
			GetTimeSpanPex2(TimeSpan.FromSeconds(0.0000001));
		}

		/// <summary>Test stub for GetTernary(String)</summary>
		//[PexMethod(MaxRunsWithoutNewTests = 200)]
		//[PexAllowedExceptionFromTypeUnderTest(typeof(FormatException)), PexAllowedException(typeof(FormatException))]
		public Ternary GetTernary(string value)
		{
			Ternary result = XmlTools.GetTernary(value, Ternary.Unknown);

			string[] xtrue =  new string[] { "TRUE", "ON", "YES", "1", "GRANT" };
			string[] xfalse = new string[] { "FALSE", "OFF", "NO", "0", "DENY" };

			if (value == null)
				Assert.AreEqual(Ternary.Unknown, result);
			else if (Array.IndexOf(xtrue, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.True, result);
			else if (Array.IndexOf(xfalse, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.False, result);
			else
				Assert.AreEqual(Ternary.Unknown, result);

			return result;
		}

		/// <summary>Test stub for GetTernary(String, Ternary)</summary>
		//[PexMethod(MaxRunsWithoutNewTests = 200)]
		public Ternary GetTernary01(string value, Ternary defaultValue)
		{
			Ternary result = XmlTools.GetTernary(value, defaultValue);

			string[] xtrue =  new string[] { "TRUE", "ON", "YES", "1", "GRANT" };
			string[] xfalse = new string[] { "FALSE", "OFF", "NO", "0", "DENY" };
			string[] xunknown = new string[] { "UNKNOWN", "SOME", "ANY", "ALL", "2", "BOTH", "DEFAULT" };

			if (value == null)
				Assert.AreEqual(defaultValue, result);
			else if (Array.IndexOf(xtrue, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.True, result);
			else if (Array.IndexOf(xfalse, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.False, result);
			else if (Array.IndexOf(xunknown, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.Unknown, result);
			else
				Assert.AreEqual(defaultValue, result);

			return result;
		}

		//[PexMethod]
		public void GetTimeSpanPex(string value)
		{
			if (value != null && __19Digiits.IsMatch(value))
				return;
			TimeSpan target;
			bool test = !IsEmpty(value) && __timespanRex.IsMatch(value);
			Assert.AreEqual(test, XmlTools.TryGetTimeSpan(value, out target));
		}
		private static readonly Regex __timespanRex = new Regex(@"\A
			([\0- ]*
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Dd][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Hh][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Mm][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Ss][\0- ]*)?
				((\d+(\.\d*)?|\.\d+)[\0- ]*[Mm][Ss][\0- ]*)?
			|(([\0- ]*(\d+(\.\d*)?|\.\d+)?
				[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*[:][\0- ]*)?
					[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*[:][\0- ]*)?
						[\0- ]*(\d+(\.\d*)?|\.\d+)[\0- ]*
			)\z", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		private static readonly Regex __19Digiits = new Regex(@"([1-9]|\.[0-9])[0-9]{18}");

		private static bool IsEmpty(string value)
		{
			if (value == null)
				return true;

			for (int i = 0; i < value.Length; ++i)
			{
				if (value[i] > ' ')
					return false;
			}
			return true;
		}

		private static decimal Millisecs(TimeSpan value)
		{
			return new decimal(value.Ticks) / TimeSpan.TicksPerMillisecond;
		}

		private static decimal Secs(TimeSpan value)
		{
			return new decimal(value.Ticks) / TimeSpan.TicksPerSecond;
		}

		private static decimal Mins(TimeSpan value)
		{
			return new decimal(value.Ticks) / TimeSpan.TicksPerMinute;
		}

		private static decimal Hours(TimeSpan value)
		{
			return new decimal(value.Ticks) / TimeSpan.TicksPerHour;
		}

		private static decimal Days(TimeSpan value)
		{
			return new decimal(value.Ticks) / TimeSpan.TicksPerDay;
		}

		//[PexMethod(MaxConstraintSolverTime = 2), PexAllowedException(typeof(AssertFailedException))]
		public void GetTimeSpanPex3(TimeSpan value, bool named, bool day, bool hour, bool minute, bool second, bool millisec)
		{
			value = TimeSpan.FromTicks(value.Ticks == long.MinValue ? long.MaxValue: Math.Abs(value.Ticks));
			string testOverflow = Hours(value).ToString();
			if (testOverflow.Length - testOverflow.IndexOf('.') - 1 >= 19)
				hour = false;
			testOverflow = Days(value).ToString();
			if (testOverflow.Length - testOverflow.IndexOf('.') - 1 >= 19)
				day = false;
			TimeSpan expected = value;
			value = expected;
			string s = "";
			if (named)
			{
				if (millisec)
				{
					if (day || hour || minute || second)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerSecond);
						s = Millisecs(t).ToString() + "ms";
						value -= t;
					}
					else
					{
						s = Millisecs(value).ToString() + "ms";
					}
				}
				if (second)
				{
					if (day || hour || minute)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerMinute);
						s = Secs(t).ToString() + "s" + s;
						value -= t;
					}
					else
					{
						s = Secs(value).ToString() + "s" + s;
					}
				}
				if (minute)
				{
					if (day || hour)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerHour);
						s = Secs(t).ToString() + "m" + s;
						value -= t;
					}
					else
					{
						s = Mins(value).ToString() + "m" + s;
					}
				}
				if (hour)
				{
					if (day)
					{
						TimeSpan t = TimeSpan.FromTicks(value.Ticks % TimeSpan.TicksPerDay);
						s = Secs(t).ToString() + "h" + s;
						value -= t;
					}
					else
					{
						s = Hours(value).ToString() + "h" + s;
					}
				}
				if (day)
				{
					s = Days(value).ToString() + "d" + s;
				}
			}
			else
			{
				if (day)
				{
					s = value.Days.ToString() + " ";
					value -= TimeSpan.FromTicks(value.Days * TimeSpan.TicksPerDay);
				}
				if (hour)
				{
					long x = (value.Ticks / TimeSpan.TicksPerHour);
					s += x.ToString() + ":";
					value -= TimeSpan.FromTicks(x * TimeSpan.TicksPerHour);
				}
				else if (day)
				{
					s += "0:";
				}
				if (minute)
				{
					long x = (value.Ticks / TimeSpan.TicksPerMinute);
					s += x.ToString() + ":";
					value -= TimeSpan.FromTicks(x * TimeSpan.TicksPerMinute);
				}
				else if (hour || day)
				{
					s += "0:";
				}
				s += Secs(value).ToString();
			}
			TimeSpan actual;
			if (s.Length > 0)
			{
				Assert.IsTrue(XmlTools.TryGetTimeSpan(s, out actual));
				Assert.AreEqual(expected, actual);
			}
		}

		//[PexMethod]
		public void GetTimeSpanPex2(TimeSpan value)
		{
			value = TimeSpan.FromTicks(value.Ticks == long.MinValue ? long.MaxValue: Math.Abs(value.Ticks));
			string x = Hours(value).ToString();
			if (x.Length - x.IndexOf('.') - 1 >= 19)
				return;
			x = Days(value).ToString();
			if (x.Length - x.IndexOf('.') - 1 < 19)
			{
				Assert.AreEqual(value, XmlTools.GetTimeSpan(Days(value) + "d"));
				Assert.AreEqual(value, XmlTools.GetTimeSpan(Days(value) + " d"));
			}
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Hours(value) + "h"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Mins(value) + "m"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Secs(value) + "s"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Millisecs(value) + "ms"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Secs(value).ToString()));

			TimeSpan d = TimeSpan.FromDays(value.Days);
			TimeSpan h = TimeSpan.FromHours(value.Hours);
			TimeSpan m = TimeSpan.FromMinutes(value.Minutes);
			TimeSpan s = TimeSpan.FromSeconds(value.Seconds);

			Assert.AreEqual(value, XmlTools.GetTimeSpan(value.Days.ToString() + " " + Hours(value - d) + ":0:0"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(value.Days.ToString() + " " + value.Hours.ToString() + ":" + Mins(value - d - h) + ":0"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(value.Days.ToString() + " " + value.Hours.ToString() + ":" + value.Minutes.ToString() + ":" + Secs(value - d - h - m)));

			TimeSpan hours = value - d;
			Assert.AreEqual(hours, XmlTools.GetTimeSpan(hours.Hours.ToString() + ":" + Mins(hours - h) + ":0"));
			Assert.AreEqual(hours, XmlTools.GetTimeSpan(hours.Hours.ToString() + ":" + hours.Minutes.ToString() + ":" + Secs(hours - h - m)));

			TimeSpan minutes = hours - h;
			Assert.AreEqual(minutes, XmlTools.GetTimeSpan(Mins(minutes) + ":0"));
			Assert.AreEqual(minutes, XmlTools.GetTimeSpan(minutes.Minutes.ToString() + ":" + Secs(minutes - m)));
			Assert.AreEqual(hours, XmlTools.GetTimeSpan(Mins(hours) + ":0"));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Mins(value) + ":0"));

			TimeSpan seconds = minutes - m;
			Assert.AreEqual(seconds, XmlTools.GetTimeSpan(Secs(seconds).ToString()));
			Assert.AreEqual(minutes, XmlTools.GetTimeSpan(Secs(minutes).ToString()));
			Assert.AreEqual(hours, XmlTools.GetTimeSpan(Secs(hours).ToString()));
			Assert.AreEqual(value, XmlTools.GetTimeSpan(Secs(value).ToString()));
		}
	}
}
