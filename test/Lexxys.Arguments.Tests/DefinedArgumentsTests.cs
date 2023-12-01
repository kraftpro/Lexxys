namespace Lexxys.Argument.Tests;

public class DefinedArgumentsTests
{
    #region Arguments and Parameters

    private static ArgumentsBuilder Parms() => new ArgumentsBuilder()
        .Switch("version", "V", description: "show version value")
        .Switch("test", "T", description: "test mode")
        .Switch("verbose", "V", description: "verbose output")
        .Switch("debug", "D", description: "output debut info")
        .Positional("@script-file", description: "script file to execute")
        .BeginCommand("create", "creates a new object")
            .Positional("name", description: "name of the object", required: true)
            .Parameter("type", "T", description: "type of the object")
            .Parameter("value", description: "value of the object")
            .Switch("readonly", "R", description: "read only object")
            .BeginCommand("factory", "creates an object factory")
                .Positional("name", description: "name of the factory (default: <object-name>1-factory)")
                .Switch("readonly", "R", description: "set the factory as a read-only")
            .EndCommand()
        .EndCommand()
        .BeginCommand("delete", "deletes an object")
            .Positional("name", description: "name of the object", required: true)
            .Switch("force", "F", description: "force delete")
            .Switch("permanent", "P", description: "permanent delete")
        .EndCommand()
        .BeginCommand("list", "lists objects")
            .Parameter("filter", "F", description: "filter for the list")
            .Switch("table", "T", description: "table mode")
            .Switch("list", "T", description: "table mode")
            .Parameter("sort", "O", description: "sort objects (N - by name; T - by time of creation)")
        .EndCommand()
        .BeginCommand("update", "updates an object")
            .Positional("name", description: "name of the object", required: true)
            .Positional("value", description: "new value of the object", required: true)
            .Switch("force", "F", description: "force update")
        .EndCommand()
        .BeginCommand("run", "runs an factory")
            .Positional("name", description: "name of the object", required: true)
            .Parameter("count", "C", description: "number of objects to create (1)")
        .EndCommand()
        ;


    #endregion

    [Fact]
    public void MyTestMethod()
    {

    }

}
