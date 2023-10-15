namespace Lexxys.Argument.Tests;

[TestClass]
public class AutoArgumentsValueTests
{
	private static readonly string[] Args1 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

	[TestMethod]
	public void ArgsTest()
	{
		var a = new Arguments(Enumerable.Empty<string>());
		Assert.IsNotNull(a.Args);
		Assert.AreEqual(0, a.Args.Count);
		a = new Arguments(Args1);
		CollectionAssert.AreEqual(Args1, a.Args.ToList());
	}

	[TestMethod]
	public void SwitchTest()
	{
		var a = new Arguments(Args1);
		Assert.IsTrue(a.Switch("ca"));
		Assert.IsTrue(a.Switch("category"));
		Assert.IsTrue(a.Switch("cross across"));
		Assert.IsFalse(a.Switch("data base"));
		Assert.IsFalse(a.Switch("database"));
		Assert.IsFalse(a.Switch("c"));
		Assert.IsFalse(a.Switch("cat balance"));
	}

	[TestMethod]
	public void StringValueTest()
	{
		var a = new Arguments(Args1);
		Assert.AreEqual("true", a.Value("ca", "default"));
		Assert.AreEqual("default", a.Value("xx", "default"));
		Assert.AreEqual("D", a.Value("db", "default"));
	}

	[TestMethod]
	public void IntValueTest()
	{
		var args = Args1.ToList();
		args.Add("-i:123");
		args.Add("-j:");
		args.Add("234");
		var a = new Arguments(args);
		Assert.AreEqual(-1, a.Value("ca", -1));
		Assert.AreEqual(-1, a.Value("xx", -1));
		Assert.AreEqual(-1, a.Value("db", -1));
		Assert.AreEqual(123, a.Value("i", -1));
		Assert.AreEqual(234, a.Value("j", -1));
	}

	[TestMethod]
	public void DecimalValueTest()
	{
		var args = Args1.Append("-io:123.11", "-j:", "234");
		var a = new Arguments(args);
		Assert.AreEqual(-1m, a.Value("ca", -1m));
		Assert.AreEqual(-1m, a.Value("xx", -1m));
		Assert.AreEqual(-1m, a.Value("db", -1m));
		Assert.AreEqual(123.11m, a.Value("index of", -1m));
		Assert.AreEqual(234, a.Value("j", default(decimal?)));
	}

	[TestMethod]
	public void DateTimeValueTest()
	{
		var args = Args1.Append("-io:2011-11-11", "-j:", "20111122");
		var a = new Arguments(args);
		Assert.AreEqual(default, a.Value("ca", default(DateTime)));
		Assert.AreEqual(default, a.Value("xx", default(DateTime)));
		Assert.AreEqual(default, a.Value("db", default(DateTime)));
		Assert.AreEqual(new DateTime(2011, 11, 11), a.Value<DateTime?>("index of"));
		Assert.AreEqual(new DateTime(2011, 11, 22), a.Value("june", DateTime.MinValue));
	}

	[TestMethod]
	public void PositionalTest()
	{
		var args = Args1.Append("-xx:", "X", "Y");
		var a = new Arguments(args);
		CollectionAssert.AreEqual(new [] { "a", "b", "C", "Y" }, a.Positional.SelectMany(o => o.ToArray()).ToList());
	}
}
