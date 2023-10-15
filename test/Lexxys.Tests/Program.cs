#nullable enable

using System.Buffers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Xml;

using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Running;

using Lexxys;
using Lexxys.Logging;
using Lexxys.Tests;
using Lexxys.Xml;

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


// Console.WriteLine();
// Console.WriteLine($"Testing {Lxx.Framework}.");
// Console.WriteLine();

var aa = AppDomain.CurrentDomain.GetAssemblies();
var xx = aa
	.Select(a =>
	{
		if (a.IsDynamic || String.IsNullOrEmpty(a.Location)) return null;
		var dd = a.GetCustomAttributesData();
		if (dd.Count == 0) return null;
		if (dd.Any(o => o.AttributeType == typeof(Lexxys.ConfigFileAttribute)))
			return TryConfigFile(a, out var name) ? GetConfigLocation(a, name): null;
		if (dd.Any(o => o.AttributeType == typeof(AssemblyMetadataAttribute)))
			return TryMetadata(a, out var name) ? GetConfigLocation(a, name): null;
		return null;
	})
	.Where(o => o != null)
	.ToList();

var log = Statics.GetLogger("Test");
log.Log(LogType.Information, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Log(LogType.Information, 2, "none", $"One {Lxx.ProductName} at <{Lxx.Framework}>.", null, null);

log.Trace($"One {Lxx.ProductName} at <{Lxx.Framework}>.");

var xml = TextToXmlConverter.ConvertLite(File.ReadAllText(@"C:\Application\Config\fsadmin.config.txt"));

// var w = new ArrayBufferWriter<byte>();
// w.Write(true);
// w.Write(false);
// w.Write("Hello");


static string GetConfigLocation(Assembly assembly, string? name)
{
	return String.IsNullOrEmpty(name) ?
		Path.ChangeExtension(assembly.Location, null) :
		Path.Combine(Path.GetDirectoryName(assembly.Location) ?? String.Empty, name);
}

static bool TryConfigFile(Assembly assembly, out string? name)
{
	var a = assembly.GetCustomAttributes(typeof(ConfigFileAttribute), false)
		.OfType<ConfigFileAttribute>().FirstOrDefault();
	name = a?.Name;
	return a != null;
}

static bool TryMetadata(Assembly assembly, out string? name)
{
	var aa = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
	var a = aa.OfType<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "ConfigFile");
	name = a?.Value;
	return a != null;
}


public record Schedule(DateTime Reminder);

public record DailySchedule(DateTime Reminder, int DayPeriod): Schedule(Reminder);
