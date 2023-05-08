using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

using Lexxys;
using Lexxys.Logging;
using Lexxys.Xml;

using Microsoft.Extensions.Primitives;

namespace Lexxys.Tests
{
	[TestClass]
	public class ArgumentsTests
	{
		private static readonly string[] Args1 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

		[TestMethod]
		public void ArgsTest()
		{
			var a = new Arguments(null);
			Assert.IsNotNull(a.Args);
			Assert.AreEqual(0, a.Args.Count);
			a = new Arguments(Args1);
			CollectionAssert.AreEqual(Args1, a.Args.ToList());
		}

		[TestMethod]
		public void ExistsTest()
		{
			var a = new Arguments(Args1);
			Assert.IsTrue(a.Option("ca"));
			Assert.IsTrue(a.Option("category"));
			Assert.IsTrue(a.Option("cross across"));
			Assert.IsTrue(a.Option("data base"));
			Assert.IsFalse(a.Option("database"));
			Assert.IsFalse(a.Option("c"));
			Assert.IsFalse(a.Option("cat balance"));
		}

		[TestMethod]
		public void OptionTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual(true, a.Option("ca", null));
			Assert.AreEqual(true, a.Option("cat", null));
			Assert.AreEqual(true, a.Option("cross across", null));
			Assert.AreEqual(false, a.Option("data base", null));
			Assert.AreEqual(null, a.Option("database", null));
			Assert.AreEqual(null, a.Option("c", null));
			Assert.AreEqual(null, a.Option("cat balance", null));
		}

		[TestMethod]
		public void StringValueTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual("default", a.Value("ca", "default", "missing"));
			Assert.AreEqual("missing", a.Value("xx", "default", "missing"));
			Assert.AreEqual("D", a.Value("db", "default", "missing"));
		}

		[TestMethod]
		public void IntValueTest()
		{
			var a = new Arguments(Args1);
			a.Args.Add("-i:123");
			a.Args.Add("-j:");
			a.Args.Add("234");
			Assert.AreEqual(-1, a.Value("ca", -1, -2));
			Assert.AreEqual(-2, a.Value("xx", -1, -2));
			Assert.AreEqual(-1, a.Value("db", -1, -2));
			Assert.AreEqual(123, a.Value("i", -1, -2));
			Assert.AreEqual(234, a.Value("j", -1, -2));
		}

		[TestMethod]
		public void DecimalValueTest()
		{
			var a = new Arguments(Args1);
			a.Args.Add("-io:123.11");
			a.Args.Add("-j:");
			a.Args.Add("234");
			Assert.AreEqual(-1m, a.Value("ca", -1m, -2m));
			Assert.AreEqual(-2m, a.Value("xx", -1m, -2m));
			Assert.AreEqual(-1m, a.Value("db", -1m, -2m));
			Assert.AreEqual(123.11m, a.Value("index of", -1m));
			Assert.AreEqual(234, a.Value("j", default(decimal?)));
		}

		[TestMethod]
		public void DateTimeValueTest()
		{
			var a = new Arguments(Args1);
			a.Args.Add("-io:2011-11-11");
			a.Args.Add("-j:");
			a.Args.Add("20111122");
			Assert.AreEqual(default(DateTime), a.Value("ca", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(DateTime.MaxValue, a.Value("xx", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(default(DateTime), a.Value("db", default(DateTime), DateTime.MaxValue));
			Assert.AreEqual(new DateTime(2011, 11, 11), a.Value<DateTime?>("index of"));
			Assert.AreEqual(new DateTime(2011, 11, 22), a.Value("june", DateTime.MinValue, DateTime.MaxValue));
		}

		[TestMethod]
		public void FirstTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual("", a.First());
			a.Args.Insert(0, "Y");
			a.Args.Insert(0, "X");
			a.Args.Insert(0, "-xx:");
			Assert.AreEqual("Y", a.First());
		}

		[TestMethod]
		public void PositionalTest()
		{
			var a = new Arguments(Args1);
			a.Args.Add("-xx:");
			a.Args.Add("X");
			a.Args.Add("Y");
			CollectionAssert.AreEqual(new [] { "", "a", "b", "C", "Y" }, a.Positional.ToList());
		}

		[TestMethod]
		public void NegativeTest()
		{
//			var param = new Parameters()
//				.Switch("test")
//				.Parameter<decimal>("percent", "percent value")
//				.Parameter<string>("output file", "path to the output file")
//				.Parameter<string>("input file");
		}
	}
}