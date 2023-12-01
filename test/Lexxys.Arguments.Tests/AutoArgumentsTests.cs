namespace Lexxys.Argument.Tests;

public class AutoArgumentsTests
{
	[Fact]
	public void TestSimpleParameters()
	{
		// Arrange
		var args = new string[] { "-a=", "1,", "2", "-b:2", "/c", "--alpha=a" };
		var arguments = new Arguments(args);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.False(arguments.HelpRequested);
		Assert.Equal(4, arguments.Parameters.Count);
		Assert.Equal("1,2", arguments["a"].StringValue);
		Assert.Equal("2", arguments["b"].StringValue);
		Assert.Equal("true", arguments["c"].StringValue);
		Assert.Equal("a", arguments["alpha"].StringValue);
		Assert.Equal("2", arguments["beta"].StringValue);
	}

	[Fact]
	public void TestHelpArgument()
	{
		// Arrange
		var args = new string[] { "-a=1", "-b:2", "/c", "--alpha=a", "-?" };
		var arguments = new Arguments(args);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.True(arguments.HelpRequested);
		Assert.Equal(4, arguments.Parameters.Count);
	}

	[Fact]
	public void TestArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "-a=1", "-a:2,", "3", "/c", "--alpha=a,b,c,d" };
		var arguments = new Arguments(args);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.Equal(3, arguments.Parameters.Count);
		Assert.Equal("1;2;3", String.Join(";", arguments["a"].ToArray()));
		Assert.Equal("true", arguments["c"].StringValue);
		Assert.Equal("a;b;c;d", String.Join(";", arguments["alpha"].ToArray()));
	}

	[Fact]
	public void TestCaseSensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = new Arguments(args, ignoreCase: false);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.Equal(4, arguments.Parameters.Count);
		Assert.Equal("2", arguments["a"].ToString());
		Assert.Equal("1", arguments["A"].ToString());
		Assert.False(arguments["c"].HasValue);
		Assert.Equal("true", arguments["C"].StringValue);
		Assert.False(arguments["ALpha"].HasValue);
		Assert.Equal("a;b;c;d", String.Join(";", arguments["Alpha"].ToArray()));
	}

	[Fact]
	public void TestCaseInsensitive()
	{
		// Arrange
		var args = new string[] { "-A=1", "-a:2", "/C", "--Alpha=a,b,c,d" };
		var arguments = new Arguments(args, ignoreCase: true);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.Equal(3, arguments.Parameters.Count);
		Assert.Equal("1;2", String.Join(";", arguments["a"].ToArray()));
		Assert.Equal("true", arguments["c"].StringValue);
		Assert.Equal("a;b;c;d", String.Join(";", arguments["ALPHA"].ToArray()));
	}

	[Fact]
	public void TestPositionalArguments()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2", "item3" };
		var arguments = new Arguments(args);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.Equal(3, arguments.Parameters.Count);
		Assert.Equal("item1;item2;item3", String.Join(";", arguments["positional"].ToArray()));
	}

	[Fact]
	public void TestPositionalArgumentsCollection()
	{
		// Arrange
		var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
		var arguments = new Arguments(args);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.Equal(3, arguments.Parameters.Count);
		Assert.Equal("item1;item2;item2a;item3", String.Join(";", arguments["positional"].ToArray()));
	}

    [Fact]
    public void TestSplitPositionalArguments()
    {
        // Arrange
        var args = new string[] { "--a=a", "/file:file", "item1", "item2,item2a", "item3" };
        var arguments = new Arguments(args, splitPositional: true);

        // Assert
        Assert.False(arguments.HasErrors);
        Assert.Equal(5, arguments.Parameters.Count);
        Assert.Equal("item1", String.Join(";", arguments["positional"].ToArray()));
        Assert.Equal("item2;item2a", String.Join(";", arguments["positional.1"].ToArray()));
        Assert.Equal("item3", String.Join(";", arguments["positional.2"].ToArray()));
    }

	[Fact]
	public void TestDisableSlashPrefix()
	{
        // Arrange
        var args = new string[] { "-a=1", "-a:2", "/c", "--alpha=a,b,c,d" };
        var arguments = new Arguments(args, allowSlash: false);

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.False(arguments.HelpRequested);
        Assert.Equal(3, arguments.Parameters.Count);
        Assert.Equal("1;2", String.Join(";", arguments["a"].ToArray()));
        Assert.Null(arguments["c"].StringValue);
        Assert.Equal("/c", arguments["positional"].StringValue);
        Assert.Equal("a;b;c;d", String.Join(";", arguments["alpha"].ToArray()));
    }

    [Fact]
	public void TestColonSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = new Arguments(args, equalSeparator: false, colonSeparator: true);

		// Act

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.False(arguments.HelpRequested);
		Assert.Equal(2, arguments.Parameters.Count);
        Assert.Equal("1", arguments["a="].StringValue);
        Assert.Equal("=2", arguments["b"].StringValue);
    }

    [Fact]
	public void TestEqualSeparatorOnly()
	{
		// Arrange
		var args = new string[] { "-a=:1", "-b:=2" };
		var arguments = new Arguments(args, equalSeparator: true, colonSeparator: false);

		// Act

		// Assert
		Assert.False(arguments.HasErrors);
		Assert.False(arguments.HelpRequested);
		Assert.Equal(2, arguments.Parameters.Count);
        Assert.Equal(":1", arguments["a"].StringValue);
        Assert.Equal("2", arguments["b:"].StringValue);
    }

    //[Fact]
    //public void TestConstructorWithBlankSeparator()
    //{
    //	// Arrange
    //	var args = new string[] { "-a 1", "-b 2" };
    //	var arguments = new Arguments(args, blankSeparator: true);

    //	// Act

    //	// Assert
    //	Assert.False(arguments.HasErrors);
    //	Assert.False(arguments.HelpRequested);
    //	Assert.Equal(2, arguments.Args.Count);
    //}

    //[Fact]
    //public void TestConstructorWithIgnoreNameSeparators()
    //{
    //	// Arrange
    //	var args = new string[] { "-a.b-c_1", "-b:2" };
    //	var arguments = new Arguments(args, ignoreCase: true, ignoreNameSeparators: true);

    //	// Act

    //	// Assert
    //	Assert.False(arguments.HasErrors);
    //	Assert.False(arguments.HelpRequested);
    //	Assert.Equal(2, arguments.Args.Count);
    //}

    //[Fact]
    //public void TestConstructorWithCombineLastParameter()
    //{
    //	// Arrange
    //	var args = new string[] { "-a", "1", "-b", "-c" };
    //	var arguments = new Arguments(args, combinePositional: true);

    //	// Act

    //	// Assert
    //	Assert.False(arguments.HasErrors);
    //	Assert.False(arguments.HelpRequested);
    //	Assert.Equal(3, arguments.Parameters.Count);
    //}

    //[Fact]
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
    //	Assert.False(arguments.HasErrors);
    //	Assert.False(arguments.HelpRequested);
    //	Assert.Equal(5, arguments.Args.Count);
    //	Assert.Equal("command2", arguments.CommandInfo.Name);

    //	var command = arguments.Parameters.GetCommand("command1");
    //	Assert.NotNull(command);
    //	Assert.Equal("subCommand1", command.Name);
    //	Assert.Equal(2, command.Parameters.Count);
    //}
}
