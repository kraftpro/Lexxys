#pragma warning disable CS0612 // Type or member is obsolete

#nullable enable

using System.Text.RegularExpressions;

using Lexxys;
using Lexxys.Logging;

Statics.AddServices(s => s
	.AddConfigService()
	.AddLoggingService(c => c.AddConsole())
	);

Parameters pp = new Parameters()
	.Parameter<string[]>(
		name: "data file",
		description: "Specifies the file to be processed.",
		positional: true,
		required: true)
	.Parameter(
		name: "input",
		abbrev: new[] { "I" },
		description: "Specifies the input file to be processed.")
	.Parameter(
		name: "output",
		abbrev: new[] { "O" },
		description: "Specifies the output file to be created.")
	.Parameter<Regex>(
		name: "match expression",
		abbrev: new[] { "X" },
		description: "Specifies the regular expression to be used.",
		parser: RegExParser,
		required: true)
	.Parameter<string[]>(
		name: "values",
		description: "Specifies the values to be processed.")

	.Command("find", "Find something")
		.Parameter(
			name: "find",
			description: "text to find")

	.Command("search", "Search something")
		.Parameter(
			name: "search",
			description: "text to search");

if (!pp.Apply(args, out var message))
{
	if (message != null)
		Console.WriteLine(message);
	pp.Usage("TestApp");
}

foreach (var item in pp.CommonParameters.Where(o => o.Value != null))
{
	Console.WriteLine($"{item.Name} = {item.Value}");
}
if (pp.SelectedCommand.Name != "")
{
	Console.WriteLine($"Command = {pp.SelectedCommand.Name}");
	foreach (var item in pp.SelectedCommand.Parameters.Where(o => o.Value != null))
	{
		Console.WriteLine($"{item.Name} = {item.Value}");
	}
}


// pp.Command("");
// pp.Usage("TestApp");
// pp.Usage("TestApp", true);
// pp.Command("find");
// pp.Usage("TestApp");
// pp.Usage("TestApp", true);

static bool RegExParser(string? value, out Regex result)
{
	if (value == null)
	{
		result = default!;
		return false;
	}
	try
	{
		result = new Regex(value);
		result.Match("");
		return true;
	}
	catch
	{
		result = default!;
		return false;
	}
}


Console.WriteLine();
Console.WriteLine($"Testing {Lxx.Framework}.");
Console.WriteLine();

var log = Statics.GetLogger("Test");
log.Log(LogType.Information, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Log(LogType.Information, 2, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Trace($"One {Lxx.ProductName} at <{Lxx.Framework}>.");
