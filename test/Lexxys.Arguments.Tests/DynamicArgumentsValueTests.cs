namespace Lexxys.Argument.Tests;

public class DynamicArgumentsValueTests
{
	private static readonly string[] Args1 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

	[Fact]
	public void ArgsTest()
	{
		var a = new Arguments(Enumerable.Empty<string>());
		Assert.NotNull(a.Args);
		Assert.Empty(a.Args);
		a = new Arguments(Args1);
		Assert.Equal(Args1, a.Args.ToList());
	}

	[Fact]
	public void SwitchTest()
	{
		var a = new Arguments(Args1);
		Assert.True(a.Switch("ca"));
		Assert.True(a.Switch("category"));
		Assert.True(a.Switch("cross across"));
		Assert.False(a.Switch("data base"));
		Assert.False(a.Switch("database"));
		Assert.False(a.Switch("c"));
		Assert.False(a.Switch("cat balance"));
	}

	[Fact]
	public void StringValueTest()
	{
		var a = new Arguments(Args1);
		Assert.Equal("true", a.Value("ca", "default"));
		Assert.Equal("default", a.Value("xx", "default"));
		Assert.Equal("D", a.Value("db", "default"));
	}

	[Fact]
	public void IntValueTest()
	{
		var args = Args1.ToList();
		args.Add("-i:123");
		args.Add("-j:");
		args.Add("234");
		var a = new Arguments(args);
		Assert.Equal(-1, a.Value("ca", -1));
		Assert.Equal(-1, a.Value("xx", -1));
		Assert.Equal(-1, a.Value("db", -1));
		Assert.Equal(123, a.Value("i", -1));
		Assert.Equal(234, a.Value("j", -1));
	}

	[Fact]
	public void DecimalValueTest()
	{
		var args = Args1.Append("-io:123.11", "-j:", "234");
		var a = new Arguments(args);
		Assert.Equal(-1m, a.Value("ca", -1m));
		Assert.Equal(-1m, a.Value("xx", -1m));
		Assert.Equal(-1m, a.Value("db", -1m));
		Assert.Equal(123.11m, a.Value("index of", -1m));
		Assert.Equal(234, a.Value("j", default(decimal?)));
	}

	[Fact]
	public void DateTimeValueTest()
	{
		var args = Args1.Append("-io:2011-11-11", "-j:", "20111122");
		var a = new Arguments(args);
		Assert.Equal(default, a.Value("ca", default(DateTime)));
		Assert.Equal(default, a.Value("xx", default(DateTime)));
		Assert.Equal(default, a.Value("db", default(DateTime)));
		Assert.Equal(new DateTime(2011, 11, 11), a.Value<DateTime?>("index of"));
		Assert.Equal(new DateTime(2011, 11, 22), a.Value("june", DateTime.MinValue));
	}

	[Fact]
	public void PositionalTest()
	{
		var args = Args1.Append("-xx:", "X", "Y");
		var a = new Arguments(args);
		Assert.Equal(new [] { "a", "b", "C", "Y" }, a.Positional.SelectMany(o => o.ToArray()).ToList());
	}
}
