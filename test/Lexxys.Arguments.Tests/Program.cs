using Lexxys;
using Lexxys.Argument.Tests;
using Lexxys.Tests;

var tt = Strings.CutIndents("  abcd\r\n def\r\n  \r\n");
var dd = Strings.CutIndents(["  abcd", " def", "  ", ""]);

TestAttrib.Go();

args = ["-a=1", "-b=2", "-c:3"];

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

Arguments aa = pp.Build(args.Union(
	//[ "data.txt", "--input", "input.txt", "--output", "output.txt", "find", "replace", "--m-e", ".*", "--help", "data 2", "data 3", "data 4" ]
	[ "data.txt", "--input", "input.txt", "--output", "output.txt" ]
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

	foreach (string item in aa.Errors)
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
foreach (ArgumentParameter item in aa.Parameters)
{
	Console.WriteLine($"{item.Name} = {item.Value}");
}


//Console.WriteLine("LS");

//aa = Parameters.LsParameters()
//	.UnixStyle()
//	.Build(args.Union(new[] { "--help" }));

//if (aa.HelpRequested)
//{
//	aa.Usage("TestApp", alignAbbreviation: true);
//}
//else if (aa.HasErrors)
//{
//	foreach (var item in aa.Errors)
//	{
//		Console.WriteLine(item);
//	}
//	aa.Usage("TestApp", brief: true, alignAbbreviation: true);
//}

//foreach (var item in aa.Parameters)
//{
//	Console.WriteLine($"{item.Name} = {item.Value}");
//}

aa = Parameters.ObjParameters()
	.UnixStyle()
	.Build(args.Union([ "--help" ]));
aa.Usage("obj");
aa.Usage("obj", alignAbbreviation: true);

Console.WriteLine(DateOnly.FromDateTime(DateTime.Now).ToString("r"));

#if NET7_0_OR_GREATER

ParsedArguments<SampleOption> arg = Arguments.Parse<SampleOption>([
	"-a:1",
	"ne", "-b=22",
	"-i:input.txt",
	"-h"]);
SampleOption option = arg.Value;
Console.WriteLine($"a={option.Alpha}, b={option.Beta}, i={option.Input}, o={option.Output}");
Console.WriteLine($"create a={option.Create?.Alpha}, b={option.Create?.Beta}, g={option.Create?.Gamma}");
Console.WriteLine($"delete a={option.Delete?.Alpha}, b={option.Delete?.Beta}, g={option.Delete?.Gamma}");
Console.WriteLine($"help={arg.HelpRequested}");
Console.WriteLine($"errors={arg.HasErrors}");
if (arg.HasErrors || arg.HelpRequested)
{
	arg.Arguments.Usage(alignAbbreviation: true);
}


//var args2 = SampleOption.Parse([
//	"-a:1",
//	"ne", "-b=22",
//	"-i:input.txt",
//	"-h"]);

var xx = Arguments.Parse<SampleOption>(args);

SampleOption3.CreateBuilder();

var b = new ArgumentsBuilder();


var x3a = SampleOption3.Parse(args);
string[] args2 = ["-i=input.txt", "-o", "output.txt", .. args];
var x2 = SampleOption2.Parse(args2);
var x3b = Arguments.Parse<SampleOption3>(args);

Console.WriteLine("----------");

x2.Usage();

#endif
