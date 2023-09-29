namespace Lexxys.Argument.Tests;

[TestClass]
public class DynamicArgumentsTests
{
	[TestMethod]
	public void TestSimpleParameters()
	{
		// Arrange
		var args = new string[] { "-a=", "1,", "2", "-b:2", "/c", "--alpha=a" };
		var arguments = new Arguments(args);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.IsFalse(arguments.HelpRequested);
		Assert.AreEqual(4, arguments.Parameters.Count);
		Assert.AreEqual("1,2", arguments["a"].StringValue);
		Assert.AreEqual("2", arguments["b"].StringValue);
		Assert.AreEqual("true", arguments["c"].StringValue);
		Assert.AreEqual("a", arguments["alpha"].StringValue);
		Assert.AreEqual("2", arguments["beta"].StringValue);
	}

	[TestMethod]
	public void TestHelpArgument()
	{
		// Arrange
		var args = new string[] { "-a=1", "-b:2", "/c", "--alpha=a", "-?" };
		var arguments = new Arguments(args);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.IsTrue(arguments.HelpRequested);
		Assert.AreEqual(4, arguments.Parameters.Count);
	}

	[TestMethod]
	public void TestArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "-a=1", "-a:2,", "3", "/c", "--alpha=a,b,c,d" };
		var arguments = new Arguments(args);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.AreEqual(3, arguments.Parameters.Count);
		Assert.AreEqual("1;2;3", String.Join(";", arguments["a"].ToArray()));
		Assert.AreEqual("true", arguments["c"].StringValue);
		Assert.AreEqual("a;b;c;d", String.Join(";", arguments["alpha"].ToArray()));
	}

	[TestMethod]
	public void TestCaseSensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = new Arguments(args, ignoreCase: false);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.AreEqual(4, arguments.Parameters.Count);
		Assert.AreEqual("2", arguments["a"].ToString());
		Assert.AreEqual("1", arguments["A"].ToString());
		Assert.IsFalse(arguments["c"].HasValue);
		Assert.AreEqual("true", arguments["C"].StringValue);
		Assert.IsFalse(arguments["ALpha"].HasValue);
		Assert.AreEqual("a;b;c;d", String.Join(";", arguments["Alpha"].ToArray()));
	}

	[TestMethod]
	public void TestCaseInsensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = new Arguments(args, ignoreCase: true);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.AreEqual(3, arguments.Parameters.Count);
		Assert.AreEqual("1;2", String.Join(";", arguments["a"].ToArray()));
		Assert.AreEqual("true", arguments["c"].StringValue);
		Assert.AreEqual("a;b;c;d", String.Join(";", arguments["ALPHA"].ToArray()));
	}

	[TestMethod]
	public void TestPositionalArguments()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2", "item3" };
		var arguments = new Arguments(args);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.AreEqual(3, arguments.Parameters.Count);
		Assert.AreEqual("item1;item2;item3", String.Join(";", arguments["positional"].ToArray()));
	}

	[TestMethod]
	public void TestPositionalArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
		var arguments = new Arguments(args);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.AreEqual(3, arguments.Parameters.Count);
		Assert.AreEqual("item1;item2;item2a;item3", String.Join(";", arguments["positional"].ToArray()));
	}

    [TestMethod]
    public void TestSplitPositionalArguments()
    {
        // Arrange
        var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
        var arguments = new Arguments(args, splitPositional: true);

        // Assert
        Assert.IsFalse(arguments.HasErrors);
        Assert.AreEqual(5, arguments.Parameters.Count);
        Assert.AreEqual("item1", String.Join(";", arguments["positional"].ToArray()));
        Assert.AreEqual("item2;item2a", String.Join(";", arguments["positional.1"].ToArray()));
        Assert.AreEqual("item3", String.Join(";", arguments["positional.2"].ToArray()));
    }

	[TestMethod]
	public void TestDisableSlashPrefix()
	{
        // Arrange
        var args = new string[] { "-a=1", "-a:2", "/c", "--alpha=a,b,c,d" };
        var arguments = new Arguments(args, allowSlash: false);

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.IsFalse(arguments.HelpRequested);
        Assert.AreEqual(3, arguments.Parameters.Count);
        Assert.AreEqual("1;2", String.Join(";", arguments["a"].ToArray()));
        Assert.AreEqual(null, arguments["c"].StringValue);
        Assert.AreEqual("/c", arguments["positional"].StringValue);
        Assert.AreEqual("a;b;c;d", String.Join(";", arguments["alpha"].ToArray()));
    }

    [TestMethod]
	public void TestColonSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = new Arguments(args, equalSeparator: false, colonSeparator: true);

		// Act

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.IsFalse(arguments.HelpRequested);
		Assert.AreEqual(2, arguments.Parameters.Count);
        Assert.AreEqual("1", arguments["a="].StringValue);
        Assert.AreEqual("=2", arguments["b"].StringValue);
    }

    [TestMethod]
	public void TestEqualSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = new Arguments(args, equalSeparator: true, colonSeparator: false);

		// Act

		// Assert
		Assert.IsFalse(arguments.HasErrors);
		Assert.IsFalse(arguments.HelpRequested);
		Assert.AreEqual(2, arguments.Parameters.Count);
        Assert.AreEqual(":1", arguments["a"].StringValue);
        Assert.AreEqual("2", arguments["b:"].StringValue);
    }

    //[TestMethod]
    //public void TestConstructorWithBlankSeparator()
    //{
    //	// Arrange
    //	var args = new string[] { "-a 1", "-b 2" };
    //	var arguments = new Arguments(args, blankSeparator: true);

    //	// Act

    //	// Assert
    //	Assert.IsFalse(arguments.HasErrors);
    //	Assert.IsFalse(arguments.HelpRequested);
    //	Assert.AreEqual(2, arguments.Args.Count);
    //}

    //[TestMethod]
    //public void TestConstructorWithIgnoreNameSeparators()
    //{
    //	// Arrange
    //	var args = new string[] { "-a.b-c_1", "-b:2" };
    //	var arguments = new Arguments(args, ignoreCase: true, ignoreNameSeparators: true);

    //	// Act

    //	// Assert
    //	Assert.IsFalse(arguments.HasErrors);
    //	Assert.IsFalse(arguments.HelpRequested);
    //	Assert.AreEqual(2, arguments.Args.Count);
    //}

    //[TestMethod]
    //public void TestConstructorWithCombineLastParameter()
    //{
    //	// Arrange
    //	var args = new string[] { "-a", "1", "-b", "-c" };
    //	var arguments = new Arguments(args, combinePositional: true);

    //	// Act

    //	// Assert
    //	Assert.IsFalse(arguments.HasErrors);
    //	Assert.IsFalse(arguments.HelpRequested);
    //	Assert.AreEqual(3, arguments.Parameters.Count);
    //}

    //[TestMethod]
    //public void TestConstructorWithCommands()
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
    //	Assert.IsFalse(arguments.HasErrors);
    //	Assert.IsFalse(arguments.HelpRequested);
    //	Assert.AreEqual(5, arguments.Args.Count);
    //	Assert.AreEqual("command2", arguments.CommandInfo.Name);

    //	var command = arguments.Parameters.GetCommand("command1");
    //	Assert.IsNotNull(command);
    //	Assert.AreEqual("subCommand1", command.Name);
    //	Assert.AreEqual(2, command.Parameters.Count);
    //}
}
