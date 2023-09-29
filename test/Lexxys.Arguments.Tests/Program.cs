using Lexxys;
using Lexxys.Argument.Tests;

using System.Security.Cryptography.X509Certificates;

args = new[] { "-a=1", "-b=2", "-c:3" };


ArgumentsBuilder pp = new ArgumentsBuilder()
	.Positional(
		name: "data file",
		description: "Specifies the file to be processed.",
		collection: true,
		required: true)
	.Positional(
		name: "next file",
		description: "Specifies the file to be processed.",
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
	.Parameter(
		name: "values",
		description: "Specifies the values to be processed.",
		collection: true)
	.BeginCommand("find", "Find something")
		.Parameter(
			name: "find",
			description: "text to find")
		.BeginCommand("replace", "Replace something")
			.Parameter(
				name: "replace",
				description: "text to replace")
		.EndCommand()
	.EndCommand()
	.BeginCommand("search", "Search something")
		.Parameter(
			name: "search",
			description: "text to search")
	.EndCommand()
	.Help()
	.EnableUnknownParameters()
	.SeparatePositionalParameters()
    ;

var aa = pp.Build(args.Union(new[]
	{ "data.txt", "--input", "input.txt", "--output", "output.txt", "find", "replace", "--m-e", ".*", "--help", "data 2", "data 3", "data 4" }
	));

Console.WriteLine("TestApp.exe " + String.Join(" ", aa.Args));

Console.Write("JSON: ");
Console.WriteLine(aa.ToJson());

Console.Write("XML:  ");
Console.WriteLine(aa.ToXml());

Console.WriteLine();
aa.Usage("TestApp");

Console.WriteLine();
aa.Usage("TestApp<brief>", brief: true);

if (aa.HasErrors)
{
	Console.WriteLine();

	foreach (var item in aa.Errors)
	{
		Console.WriteLine(item);
	}
	aa.Usage();
}


Console.WriteLine();

if (aa.Command != null)
{
	Console.WriteLine($"Command = {aa.Command.Name}");
}
foreach (var item in aa.Parameters)
{
	Console.WriteLine($"{item.Name} = {item.Value}");
}


Console.WriteLine("LS");

aa = Parameters.LsParameters()
	.UnixStyle()
	.Build(args.Union(new[] { "--help" }));

if (aa.HelpRequested)
{
	aa.Usage("TestApp", alignAbbreviation: true);
}
else if (aa.HasErrors)
{
	foreach (var item in aa.Errors)
	{
		Console.WriteLine(item);
	}
	aa.Usage("TestApp", brief: true, alignAbbreviation: true);
}

foreach (var item in aa.Parameters)
{
	Console.WriteLine($"{item.Name} = {item.Value}");
}
