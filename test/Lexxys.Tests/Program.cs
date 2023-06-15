#nullable enable

using System.Text.RegularExpressions;

using BenchmarkDotNet.Running;

using Lexxys;
using Lexxys.Logging;
using Lexxys.Tests;

Statics.AddServices(s => s
	.AddConfigService()
	.AddLoggingService(c => c.AddConsole())
	);


// Lexxys.Tests.Performance.StringPerformanceFormatTests.Compare();
// BenchmarkRunner.Run<Lexxys.Tests.Performance.StringPerformanceFormatTests>();

return 0;

ArgumentsBuilder pp = new ArgumentsBuilder()
	.Array(
		name: "data file",
		description: "Specifies the file to be processed.",
		positional: true,		
		required: true)
	.Parameter(
		name: "input",
		abbrev: "I",
		description: "Specifies the input file to be processed.")
	.Parameter(
		name: "output",
		abbrev: "O",
		description: "Specifies the output file to be created.")
	.Parameter(
		name: "match expression",
		abbrev: "X",
		description: "Specifies the regular expression to be used.",
		valueName: "regex",
		required: true)
	.Array(
		name: "values",
		description: "Specifies the values to be processed.")
	.Command("find", "Find something")
		.Parameter(
			name: "find",
			description: "text to find")

	.Command("search", "Search something")
		.Parameter(
			name: "search",
			description: "text to search")
	.UnixStyle();

var aa = pp.Build(args.Union(new[] 
	{ "data.txt", "--input", "input.txt", "--output", "output.txt", "--m-e", ".*", "--hel" }
	));

if (aa.HelpRequested)
{
	aa.Usage("TestApp");
}
else if (aa.HasErrors)
{
	foreach (var item in aa.Errors)
	{
		Console.WriteLine(item);
	}
	aa.Usage("TestApp", brief: true);
}


if (aa.Command != null)
{
	Console.WriteLine($"Command = {aa.Command}");
}
foreach (var item in aa.Parameters)
{
	Console.WriteLine($"{item.Name} = {item.Value}");
}


Console.WriteLine("LS");

aa = ArgumentsTests.LsParameters()
	.UnixStyle()
	.Build(args.Union(new[] { "--help" }));

if (aa.HelpRequested)
{
	aa.Usage("TestApp");
}
else if (aa.HasErrors)
{
	foreach (var item in aa.Errors)
	{
		Console.WriteLine(item);
	}
	aa.Usage("TestApp", brief: true);
}

foreach (var item in aa.Parameters)
{
	Console.WriteLine($"{item.Name} = {item.Value}");
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
