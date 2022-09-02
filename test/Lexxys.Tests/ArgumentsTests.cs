using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests
{
	[TestClass()]
	public class ArgumentsTests
	{
		private static readonly string[] Args1 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

		[TestMethod()]
		public void ArgsTest()
		{
			var a = new Arguments(null);
			Assert.IsNotNull(a.Args);
			Assert.AreEqual(0, a.Args.Count);
			a = new Arguments(Args1);
			CollectionAssert.AreEqual(Args1, a.Args);
		}

		[TestMethod()]
		public void AppendTest()
		{
			var a = new Arguments(Args1);
			a.Append("a");
			var expected = Args1.ToList();
			expected.Add("a");
			CollectionAssert.AreEqual(expected, a.Args);
			a.Append("x", null, "z");
			expected.AddRange(new[] { "x", null, "z" });
			CollectionAssert.AreEqual(expected, a.Args);
		}

		[TestMethod()]
		public void ExistsTest()
		{
			var a = new Arguments(Args1);
			Assert.IsTrue(a.Exists("ca"));
			Assert.IsTrue(a.Exists("category"));
			Assert.IsTrue(a.Exists("cross across"));
			Assert.IsTrue(a.Exists("data base"));
			Assert.IsFalse(a.Exists("database"));
			Assert.IsFalse(a.Exists("c"));
			Assert.IsFalse(a.Exists("cat balance"));
		}

		[TestMethod()]
		public void OptionTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual(true, a.Option("ca"));
			Assert.AreEqual(true, a.Option("cat"));
			Assert.AreEqual(true, a.Option("cross across"));
			Assert.AreEqual(false, a.Option("data base"));
			Assert.AreEqual(null, a.Option("database"));
			Assert.AreEqual(null, a.Option("c"));
			Assert.AreEqual(null, a.Option("cat balance"));
		}

		[TestMethod()]
		public void StringValueTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual("default", a.Value("ca", "default", "missing"));
			Assert.AreEqual("missing", a.Value("xx", "default", "missing"));
			Assert.AreEqual("D", a.Value("db", "default", "missing"));
		}

		[TestMethod()]
		public void IntValueTest()
		{
			var a = new Arguments(Args1);
			a.Append("-i:123", "-j:", "234");
			Assert.AreEqual(-1, a.Value("ca", -1, -2));
			Assert.AreEqual(-2, a.Value("xx", -1, -2));
			Assert.AreEqual(-1, a.Value("db", -1, -2));
			Assert.AreEqual(123, a.Value("i", -1, -2));
			Assert.AreEqual(234, a.Value("j", -1, -2));
		}

		[TestMethod()]
		public void DecimalValueTest()
		{
			var a = new Arguments(Args1);
			a.Append("-io:123.11", "-j:", "234");
			Assert.AreEqual(-1m, a.Value("ca", -1m, -2m));
			Assert.AreEqual(-2m, a.Value("xx", -1m, -2m));
			Assert.AreEqual(-1m, a.Value("db", -1m, -2m));
			Assert.AreEqual(123.11m, a.Value("index of", -1m));
			Assert.AreEqual(234, a.Value("j", default(decimal?)));
		}

		[TestMethod()]
		public void DateTimeValueTest()
		{
			var a = new Arguments(Args1);
			a.Append("-io:2011-11-11", "-j:", "20111122");
			Assert.AreEqual(default(DateTime), a.Value("ca", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(DateTime.MaxValue, a.Value("xx", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(default(DateTime), a.Value("db", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(new DateTime(2011, 11, 11), a.Value<DateTime?>("index of"));
			Assert.AreEqual(new DateTime(2011, 11, 22), a.Value("june", DateTime.MinValue, DateTime.MaxValue));
		}

		[TestMethod()]
		public void FirstTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual("", a.First());
			a.Args.InsertRange(0, new [] { "-xx:", "X", "Y"});
			Assert.AreEqual("Y", a.First());
		}

		[TestMethod()]
		public void PositionalTest()
		{
			var a = new Arguments(Args1);
			a.Append("-xx:", "X", "Y");
			CollectionAssert.AreEqual(new [] { "", "a", "b", "C", "Y" }, a.Positional.ToList());
		}
	}
}