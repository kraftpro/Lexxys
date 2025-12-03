namespace Lexxys.Argument.Tests;

public class AutoArgumentsTests
{
	private string[] Args1 = ["-a=", "1,", "2", "-b:2", "-c", "--alpha=a"];

	[Test]
	public async Task Args1_HasCorrectParametersCount()
	{
		// Arrange
		var arguments = Arguments.Parse(Args1);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.HelpRequested).IsFalse();
		await Assert.That(arguments.Count).IsEqualTo(4);
	}

	[Test]
	[Arguments("a", "1,2")]
	[Arguments("b", "2")]
	[Arguments("c", "")]
	[Arguments("alpha", "a")]
	[Arguments("beta", "2")]
	public async Task Args1_StringValuesAreCorrect(string parameter, string value)
	{
		// Arrange
		var arguments = Arguments.Parse(Args1);

		// Assert
		await Assert.That(arguments[parameter].StringValue).IsEqualTo(value);
	}

	[Test]
	[Arguments("-h")]
	[Arguments("--help")]
	[Arguments("/?")]
	[Arguments("-?")]
	public async Task HelpArgument_AnyFormIsAccepted(string h)
	{
		// Arrange
		var args = new string[] { "-a=1", "-b:2", "-c", "--alpha=a", h };
		var arguments = Arguments.Parse(args, allowSlash: true);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.HelpRequested).IsTrue();
		await Assert.That(arguments.Count).IsEqualTo(4);
	}

	[Test]
	public async Task TestArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "-a=1", "-a:2,", "3", "-c", "--alpha=a,b,c,d" };
		var arguments = Arguments.Parse(args);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Count).IsEqualTo(3);
		await Assert.That(String.Join(";", arguments.GetCollection("a"))).IsEqualTo("1;2;3");
		await Assert.That(arguments["c"].HasValue).IsTrue();
		await Assert.That(String.Join(";", arguments.GetCollection("alpha"))).IsEqualTo("a;b;c;d");
	}

	[Test]
	[Arguments(new string[] { "-a=", ",1,2", "-a:3,", "4,", ",5,", "-a", "6,7", ",4" }, "1;2;3;4;;5;6;7")]
	public async Task TestArgumentsCollectionCollected(string[] args, string expected)
	{
		// Arrange
		var arguments = Arguments.Parse(args);
		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(String.Join(";", arguments["a"].ToArray())).IsEqualTo(expected);
	}

	[Test]
	public async Task TestCaseSensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = Arguments.Parse(args, ignoreCase: false, allowSlash: true);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Count).IsEqualTo(4);
		await Assert.That(arguments["a"].ToString()).IsEqualTo("2");
		await Assert.That(arguments["A"].ToString()).IsEqualTo("1");
		await Assert.That(arguments["c"].HasValue).IsFalse();
		await Assert.That(arguments["C"].HasValue).IsTrue();
		await Assert.That(String.Join(";", arguments["Alpha"].ToArray())).IsEqualTo("a;b;c;d");
	}

	[Test]
	public async Task TestCaseInsensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = Arguments.Parse(args, ignoreCase: true, allowSlash: true);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Count).IsEqualTo(3);
		await Assert.That(String.Join(";", arguments["a"].ToArray())).IsEqualTo("1;2");
		await Assert.That(arguments["c"].HasValue).IsTrue();
		await Assert.That(arguments["C"].HasValue).IsTrue();
		await Assert.That(String.Join(";", arguments["ALPHA"].ToArray())).IsEqualTo("a;b;c;d");
	}

	[Test]
	public async Task TestPositionalArguments()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2", "item3" };
		var arguments = Arguments.Parse(args, allowSlash: true);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Count).IsEqualTo(3);
		await Assert.That(String.Join(";", arguments.Positional.ToArray())).IsEqualTo("item1;item2;item3");
	}

	[Test]
	public async Task TestPositionalArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
		var arguments = Arguments.Parse(args, allowSlash: true);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Count).IsEqualTo(3);
		await Assert.That(String.Join(";", arguments.Positional.ToArray())).IsEqualTo("item1;item2;item2a;item3");
	}

	//[Test]
	//public async Task TestSplitPositionalArguments()
	//{
	//	// Arrange
	//	var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
	//	var arguments = new Arguments(args, splitPositional: true, allowSlash: true);

	//	// Assert
	//	await Assert.That(arguments.Errors).IsEmpty();
	//	await Assert.That(arguments.Count).IsEqualTo(5);
	//	await Assert.That(String.Join(";", arguments.GetValue(Arguments.PositionalParameterName, "").ToArray())).IsEqualTo("item1");
	//	await Assert.That(String.Join(";", arguments.GetValue(Arguments.PositionalParameterName + ".1", "").ToArray())).IsEqualTo("item2;item2a");
	//	await Assert.That(String.Join(";", arguments.GetValue(Arguments.PositionalParameterName + ".2", "").ToArray())).IsEqualTo("item3");
	//}

	[Test]
	public async Task TestDisableSlashPrefix()
	{
		// Arrange
		var args = new string[] { "-a=1", "-a:2", "/c", "--alpha=a,b,c,d" };
		var arguments = Arguments.Parse(args, allowSlash: false);

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.HelpRequested).IsFalse();
		await Assert.That(arguments.Count).IsEqualTo(3);
		await Assert.That(String.Join(";", arguments["a"].ToArray())).IsEqualTo("1;2");
		await Assert.That(arguments["c"].StringValue).IsNull();
		await Assert.That(arguments.Positional.StringValue).IsEqualTo("/c");
		await Assert.That(String.Join(";", arguments["alpha"].ToArray())).IsEqualTo("a;b;c;d");
	}

	[Test]
	public async Task TestColonSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = Arguments.Parse(args, equalSeparator: false, colonSeparator: true);

		// Act

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.HelpRequested).IsFalse();
		await Assert.That(arguments.Count).IsEqualTo(2);
		await Assert.That(arguments["a="].StringValue).IsEqualTo("1");
		await Assert.That(arguments["b"].StringValue).IsEqualTo("=2");
	}

	[Test]
	public async Task TestEqualSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = Arguments.Parse(args, equalSeparator: true, colonSeparator: false);

		// Act

		// Assert
		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.HelpRequested).IsFalse();
		await Assert.That(arguments.Count).IsEqualTo(2);
		await Assert.That(arguments["a"].StringValue).IsEqualTo(":1");
		await Assert.That(arguments["b:"].StringValue).IsEqualTo("2");
	}

	//[Test]
	//public void TestConstructorWithBlankSeparator()
	//{
	//	// Arrange
	//	var args = new string[] { "-a 1", "-b 2" };
	//	var arguments = new Arguments(args, blankSeparator: true);

	//	// Act

	//	// Assert
	//	await Assert.That(arguments.Errors).IsEmpty();
	//	await Assert.That(arguments.HelpRequested).IsFalse();
	//	await Assert.That(arguments.Args.Count).IsEqualTo(2);
	//}

	//[Test]
	//public void TestConstructorWithIgnoreNameSeparators()
	//{
	//	// Arrange
	//	var args = new string[] { "-a.b-c_1", "-b:2" };
	//	var arguments = new Arguments(args, ignoreCase: true, ignoreNameSeparators: true);

	//	// Act

	//	// Assert
	//	await Assert.That(arguments.Errors).IsEmpty();
	//	await Assert.That(arguments.HelpRequested).IsFalse();
	//	await Assert.That(arguments.Args.Count).IsEqualTo(2);
	//}

	//[Test]
	//public async Task TestConstructorWithCombineLastParameter()
	//{
	//	// Arrange
	//	var args = new string[] { "-a", "1", "-b", "-c" };
	//	var arguments = new Arguments(args, combinePositional: true);

	//	// Act

	//	// Assert
	//	await Assert.That(arguments.Errors).IsEmpty();
	//	await Assert.That(arguments.HelpRequested).IsFalse();
	//	await Assert.That(arguments.Count).IsEqualTo(3);
	//}

	//[Test]
	//public async Task TestConstructorWithCommands()
	//{
	//	// Arrange
	//	var args = new string[] { "command1", "--param1", "1", "--param2", "2", "command2", "-p", "3" };
	//	var commands = new CommandDefinition[] {
	//		new CommandDefinition("command1", null, new List<CommandDefinition> {
	//			new CommandDefinition("subCommand1", "sub Description 1").WithArgument("subParam1", typeof(int)).WithArgument("subparam2", typeof(string)) }),
	//		new CommandDefinition("command2", "Description 2").WithArgument("param", typeof(string))};
	//	var arguments = new Arguments(args, new ArgumentsBuilder().AddDefinition(commands));

	//	// Act

	//	// Assert
	//	await Assert.That(arguments.Errors).IsEmpty();
	//	await Assert.That(arguments.HelpRequested).IsFalse();
	//	await Assert.That(arguments.Args.Count).IsEqualTo(5);
	//	await Assert.That(arguments.CommandInfo.Name).IsEqualTo("command2");

	//	var command = arguments.Parameters.GetCommand("command1");
	//	await Assert.That(command).IsNotNull();
	//	await Assert.That(command.Name).IsEqualTo("subCommand1");
	//	await Assert.That(command.Count).IsEqualTo(2);
	//}
}

