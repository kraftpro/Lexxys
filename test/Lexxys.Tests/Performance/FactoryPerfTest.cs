using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;

namespace Lexxys.Tests.Performance;


[SimpleJob(RuntimeMoniker.Net462)]
[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class FactoryPerfTest
{
	private string Name { get; set; }
	private int Count { get; set; }

    public FactoryPerfTest(string name, int count)
    {
		Name = name;
		Count = count;
    }

    public FactoryPerfTest()
    {
    }

    [Benchmark]
	public object TestActivator()
	{
		return Activator.CreateInstance(typeof(FactoryPerfTest), ["Ax", 34]);
		//var aa = new object[5];
		//for (int i = 0; i < aa.Length; ++i)
		//	aa[i] = Activator.CreateInstance(typeof(FactoryPerfTest), ["Ax", i]);
		//return aa;
	}

	[Benchmark]
	public object TestFactory()
	{
		return Factory.TryConstruct(typeof(FactoryPerfTest), ["Ax", 34]);
		//var aa = new object[5];
		//for (int i = 0; i < aa.Length; ++i)
		//	aa[i] = Factory.TryConstruct(typeof(FactoryPerfTest), ["Ax", i]);
		//return aa;
	}
}
