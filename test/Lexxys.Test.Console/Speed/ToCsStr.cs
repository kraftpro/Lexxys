using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Lexxys;
using Lexxys.Testing;

namespace Lexxys.Test.Con.Speed
{
	public class ToCsStr
	{
		public static void Go()
		{
			BenchmarkRunner.Run<ToCsStr>();
		}

		public ToCsStr(int count = 100)
		{
			Values = new string[count];
			for (int i = 0; i < Values.Length; ++i)
			{
				Values[i] = R.Ascii0(Rand.Int(10, 500));
			}
		}
		string[] Values;

		[Benchmark]
		public int UseString0()
		{
			int x = 0;
			for (int i = 0; i < Values.Length; ++i)
			{
				x += Strings.EscapeCsString(new StringBuilder(), Values[i], '"').Length;
			}
			return x;
		}

		[Benchmark]
		public int UseString1()
		{
			int x = 0;
			for (int i = 0; i < Values.Length; ++i)
			{
				x += Strings.EscapeCsString(new StringBuilder(), Values[i], '"').Length;
			}
			return x;
		}


		[Benchmark]
		public int UseCharArr()
		{
			int x = 0;
			for (int i = 0; i < Values.Length; ++i)
			{
				var t = new StringBuilder();
				foreach (var item in Strings.EscapeCsCharArray(Values[i], '"'))
				{
					t.Append(item);
				}
				x += t.Length;
			}
			return x;
		}
	}
}
