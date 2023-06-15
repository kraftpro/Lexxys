using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;


#nullable enable

namespace Lexxys.Tests.Performance;

[SimpleJob(RuntimeMoniker.Net472)]
[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net70)]
[MemoryDiagnoser]
public class StringPerformanceFormatTests
{
	public static readonly Dictionary<string, object?> Parameters = new Dictionary<string, object?>
	{
		{ "twice", 2 },
		{ "quick", "QUICK" },
		{ "brown", "BROWN" },
		{ "lazy", "LAZY" },
		{ "fox", "FOX" },
		{ "dog", "DOG" },
	};

	//public static void Compare()
	//{
	//	var s1 = "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format(Parameters);
	//	Console.WriteLine("1:" + s1);
	//	var s2 = "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format2(Parameters);
	//	if (s1 != s2)
	//		Console.WriteLine("2:" + s2);
	//	var s3 = "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format3(Parameters);
	//	if (s1 != s3)
	//		Console.WriteLine("3:" + s3);
	//}

	//[Benchmark]
	//public string TestFormat1()
	//{
	//	return "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format(Parameters);
	//}

	//[Benchmark]
	//public string TestFormat2()
	//{
	//	return "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format2(Parameters);
	//}

	//[Benchmark]
	//public string TestFormat3()
	//{
	//	return "The {twice:00} {quick} {brown} {fox} jumps over the {lazy} {dog:-6}--.".Format3(Parameters);
	//}
}
