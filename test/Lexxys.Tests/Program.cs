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

// pp.Command("");
// pp.Usage("TestApp");
// pp.Usage("TestApp", true);
// pp.Command("find");
// pp.Usage("TestApp");
// pp.Usage("TestApp", true);

//static bool RegExParser(string? value, out Regex result)
//{
//	if (value == null)
//	{
//		result = default!;
//		return false;
//	}
//	try
//	{
//		result = new Regex(value);
//		result.Match("");
//		return true;
//	}
//	catch
//	{
//		result = default!;
//		return false;
//	}
//}


Console.WriteLine();
Console.WriteLine($"Testing {Lxx.Framework}.");
Console.WriteLine();

var log = Statics.GetLogger("Test");
log.Log(LogType.Information, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Log(LogType.Information, 2, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Trace($"One {Lxx.ProductName} at <{Lxx.Framework}>.");
