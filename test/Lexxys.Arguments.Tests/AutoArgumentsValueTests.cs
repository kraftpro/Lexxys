namespace Lexxys.Argument.Tests;

public class AutoArgumentsValueTests
{
	private static readonly string[] Args1 = ["", "a", "b", "-ca", "C", "/db:D"];

	[Test]
	public async Task ArgsTest()
	{
		var a = Arguments.Parse([]);
		await Assert.That(a).IsNotNull();
		await Assert.That(a.Count).IsEqualTo(0);
		a = Arguments.Parse(Args1);
		await Assert.That(a.Count + a.Positional.Count).IsEqualTo(6);
	}

	[Test]
	public async Task SwitchTest()
	{
		var a = Arguments.Parse(Args1, new ArgumentsConfig { MatchingType = ParameterMatching.Fluent });
		await Assert.That(a.GetValue<bool>("ca")).IsTrue();
		await Assert.That(a.GetValue<bool>("category")).IsTrue();
		await Assert.That(a.GetValue<bool>("cross across")).IsTrue();
		await Assert.That(a.GetValue<bool>("data base")).IsFalse();
		await Assert.That(a.GetValue<bool>("database")).IsFalse();
		await Assert.That(a.GetValue<bool>("c")).IsFalse();
		await Assert.That(a.GetValue<bool>("cat balance")).IsFalse();
	}

	[Test]
	public async Task StringValueTest()
	{
		var a = Arguments.Parse(Args1, allowSlash: true);
		await Assert.That(a.GetValue<bool>("ca")).IsTrue();
		await Assert.That(a.GetValue("xx", "default")).IsEqualTo("default");
		await Assert.That(a.GetValue("db", "default")).IsEqualTo("D");
	}

	[Test]
	public async Task IntValueTest()
	{
		var args = Args1.ToList();
		args.Add("-i:123");
		args.Add("-j:");
		args.Add("234");
		var a = Arguments.Parse(args);
		await Assert.That(a.GetValue("ca", -1)).IsEqualTo(-1);
		await Assert.That(a.GetValue("xx", -1)).IsEqualTo(-1);
		await Assert.That(a.GetValue("db", -1)).IsEqualTo(-1);
		await Assert.That(a.GetValue("i", -1)).IsEqualTo(123);
		await Assert.That(a.GetValue("j", -1)).IsEqualTo(234);
	}

	[Test]
	public async Task DecimalValueTest()
	{
		var args = Args1.Append("-io:123.11", "-j:", "234");
		var a = Arguments.Parse(args, new ArgumentsConfig { MatchingType = ParameterMatching.Fluent });
		await Assert.That(a.GetValue("ca", -1m)).IsEqualTo(-1m);
		await Assert.That(a.GetValue("xx", -1m)).IsEqualTo(-1m);
		await Assert.That(a.GetValue("db", -1m)).IsEqualTo(-1m);
		await Assert.That(a.GetValue("index of", -1m)).IsEqualTo(123.11m);
		await Assert.That(a.GetValue("j", default(decimal?))).IsEqualTo(234m);
	}

	[Test]
	public async Task DateTimeValueTest()
	{
		var args = Args1.Append("-io:2011-11-11", "-j:", "20111122");
		var a = Arguments.Parse(args, new ArgumentsConfig { MatchingType = ParameterMatching.Fluent });
		await Assert.That(a.GetValue("ca", default(DateTime))).IsEqualTo(default);
		await Assert.That(a.GetValue("xx", default(DateTime))).IsEqualTo(default);
		await Assert.That(a.GetValue("db", default(DateTime))).IsEqualTo(default);
		await Assert.That(a.GetValue<DateTime?>("index of")).IsEqualTo(new DateTime(2011, 11, 11));
		await Assert.That(a.GetValue("june", DateTime.MinValue)).IsEqualTo(new DateTime(2011, 11, 22));
	}

	[Test]
	public async Task PositionalTest()
	{
		var args = Args1.Append("-xx:", "X", "Y");
		var a = Arguments.Parse(args, allowSlash: true);
		await Assert.That(a.Positional.ToArray().ToList()).IsEquivalentTo(["a", "b", "C", "Y"]);
	}
}

