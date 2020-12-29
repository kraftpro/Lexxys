using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Test.Con.Speed
{
	public class CharArr
	{
		private const string CharString = "abcdefghijklmnopqrstuvwxyz.ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
		private static readonly char[] CharArray = CharString.ToCharArray();

		public static void Go()
		{
			BenchmarkRunner.Run<CharArr>();
		}

		[Params(100_000)]
		public int Count;


		[GlobalSetup]
		public void GlobalSetup()
		{
			Data = new byte[Count];
			new Random().NextBytes(Data);
		}

		private byte[] Data;

		[Benchmark]
		public int TrySystemConvert()
		{
			return System.Convert.ToBase64String(Data).Length;
		}

		[Benchmark]
		public int TryBinaryTools()
		{
			return BinaryTools.ToBase64CharArray(Data).Length;
		}

		[Benchmark]
		public int TryBuffers()
		{
			var buffer = new byte[Base64.GetMaxEncodedToUtf8Length(Data.Length)];
			Base64.EncodeToUtf8(Data, buffer, out var count, out var writen);
			return writen;
		}



		//[Benchmark]
		public int IndexString()
		{
			int n = 0;
			for (int i = 0; i < Count; ++i)
			{
				n += CharString[i & 0x3F];
			}
			return n;
		}

		//[Benchmark]
		public int IndexArray()
		{
			int n = 0;
			for (int i = 0; i < Count; ++i)
			{
				n += CharArray[i & 0x3F];
			}
			return n;
		}

	}
}
