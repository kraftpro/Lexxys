using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Lexxys.Testing;

namespace Lexxys.Test.Con.Speed
{
	public static class SpeedTest
	{
		public static void TestCollectionExtensions(int count)
		{
			BenchmarkRunner.Run<BmIListIEmum>();
		}

		//[BenchmarkDotNet.Attributes.Columns.AllStatisticsColumn]
		public class BmIListIEmum
		{
			public BmIListIEmum(int width = 0)
			{
				if (width == 0)
					width = Rand.Int(10, 10000);
				IntValues = new List<int>(width);
				StrValues = new List<string>(width);
				for (int i = 0; i < width; ++i)
				{
					int k = Rand.Int(int.MaxValue);
					IntValues.Add(k);
					StrValues.Add($"string [{k}]");
				}

			}

			public List<int> IntValues { get; }
			public List<string> StrValues { get; }


			[Benchmark]
			public int IntIEnumerabe() => Lexxys.CollectionExtensions.FindIndex((IEnumerable<int>)IntValues, o => o % 100 == 0);
			[Benchmark]
			public int IntIList() => Lexxys.CollectionExtensions.FindIndex((IList<int>)IntValues, o => o % 100 == 0);

			[Benchmark]
			public int StrIEnumerabe() => Lexxys.CollectionExtensions.FindIndex((IEnumerable<string>)StrValues, o => o.StartsWith("string [100"));
			[Benchmark]
			public int StrIList() => Lexxys.CollectionExtensions.FindIndex((IList<string>)StrValues, o => o.StartsWith("string [100"));
		}




	}
}
