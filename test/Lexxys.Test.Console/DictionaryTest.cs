// Lexxys Infrastructural library.
// file: DictionaryTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lexxys;

namespace Lexxys.Test.Con
{

	public class RunResult
	{
		public long MemoryUsed;
		public long InsertTicks;
		public long SearchTicks;
		public long ForEachTicks;
		public int Dummy;
	}

	public class RunResultAvg
	{
		public decimal MemoryUsed;
		public decimal InsertTicks;
		public decimal SearchTicks;
		public decimal ForEachTicks;
	}

	public class DictionaryTest
	{

		private static int SearchIndex = 27;
		//private const int NumberInsertedKeys = 50;
		//private const int NumberInsertedKeys = 500;
		//private const int NumberInsertedKeys = 5000;
		private static int NumberInsertedKeys = 10;
		//private const int NumberInsertedKeys = 100000;
		private const int NumberTests = 10000;

		private static readonly string[] Letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

		public static void Test(string[] args)
		{
			if (args == null || args.Length == 0 || !Int32.TryParse(args[0], out NumberInsertedKeys))
				NumberInsertedKeys = 10;

			Console.WriteLine("{0} keys", NumberInsertedKeys);

			try
			{
				// TRY STARTS HERE ----------

				string[] names = { "Dictionary", "SortedDictionary", "SortedList", "OrderedDictionary" };
				List<RunResult>[] results = new List<RunResult>[names.Length];
				for (int i = 0; i < results.Length; ++i)
				{
					results[i] = new List<RunResult>();
				}

				Stopwatch watch = Stopwatch.StartNew();

				for (int i = 0; i < NumberTests; i++)
				{
					SearchIndex += 1;
					Random rand = new Random();
					RunResult rr;
					int index = rand.Next(0, names.Length);
					switch (index)
					{
						case 0:
							rr = Test(names[index], new Dictionary<string, string>(), i);
							break;
						case 1:
							rr = Test(names[index], new SortedDictionary<string, string>(), i);
							break;
						case 2:
							rr = Test(names[index], new SortedList<string, string>(), i);
							break;
						case 3:
							rr = Test(names[index], new OrderedDictionary<string, string>(), i);
							break;
						default:
							continue;
					}
					results[index].Add(rr);
				}

				Console.WriteLine("{4,-20} {0,11} {1,13} {2,14} {3,14}", "Memory", "Insert", "Search", "ForEach", "");
				for (int i = 0; i < names.Length; ++i)
				{
					RunResultAvg avg = CalculateAvg(results[i]);
					Console.WriteLine("{4,-20} {0,11:N} {1,13:N} {2,14:N} {3,14:N}", avg.MemoryUsed, avg.InsertTicks, avg.SearchTicks, avg.ForEachTicks, names[i]);
				}
			}
			catch (Exception ex)
			{
				Msg("{0}", ex);
			}
		}

		private static RunResultAvg CalculateAvg(List<RunResult> list)
		{
			long sumMemory = 0;
			long sumInsert = 0;
			long sumSearch = 0;
			long sumForEach = 0;
			for (int i = 0; i < list.Count; i++)
			{
				RunResult curr = list[i];
				sumMemory += curr.MemoryUsed;
				sumInsert += curr.InsertTicks;
				sumSearch += curr.SearchTicks;
				sumForEach += curr.ForEachTicks;
				// uncomment to print each line
				//Msg("{0,11} {1,13} {2,14}", 
				//curr.MemoryUsed, curr.InsertTicks, curr.SearchTicks);
			}
			return new RunResultAvg
			{
				MemoryUsed = (decimal)sumMemory / list.Count,
				InsertTicks = (decimal)sumInsert / list.Count,
				SearchTicks = (decimal)sumSearch / list.Count,
				ForEachTicks = (decimal)sumForEach / list.Count,
			};
		}

		private static void PrintResults(RunResult result, int count, RunResult min, string name)
		{
			Msg("{4,-20} {0,11:N} {1,13:N} {2,14:N} {3,14:N}", result.MemoryUsed, result.InsertTicks, result.SearchTicks, result.ForEachTicks, name);

			//Msg("--------- Results for {0}", name);
			//Msg("# Tests {0}", count);
			//Msg("Memory Used    Insert Ticks    Search Ticks    ForEach Ticks");
			//Msg("Average Values:");
			//Msg("{0,11:N} {1,13:N} {2,14:N} {3,14:N}",
			//	result.MemoryUsed,
			//	result.InsertTicks,
			//	result.SearchTicks,
			//	result.ForEachTicks);
			//Msg("Performance Coefficient:");
			//Msg("{0,11:N} {1,13:N} {2,14:N} {3,14:N}",
			//	min.MemoryUsed/result.MemoryUsed,
			//	min.InsertTicks/result.InsertTicks,
			//	min.SearchTicks/result.SearchTicks,
			//	min.ForEachTicks/result.ForEachTicks);
			//Msg("");
		}

		private static void Msg(string name, params object[] args)
		{
			Console.WriteLine(name, args);
		}

		private static RunResult Test(string name, IDictionary<string, string> dict, int n)
		{
			//Console.Clear();
			//Msg("Currently executing test {1} of {2} for {0} object", name, n + 1, NumberTests);
			int dummy = 0;
			RunResult rr = new RunResult();
			Stopwatch watch;
			Random rand = new Random();
			long memoryStart = GC.GetTotalMemory(true);
			long insertTicksSum = 0;
			string[] keys = new string[NumberInsertedKeys];
			for (int i = 0; i < keys.Length; ++i)
			{
				keys[i] = Letters[rand.Next(0, Letters.Length)] + "_key_" + i.ToString();
			}
			for (int i = 0; i < keys.Length; i++)
			{
				string key = keys[i];
				string value = "value_" + i.ToString();

				watch = Stopwatch.StartNew();
				dict.Add(key, value);
				watch.Stop();

				insertTicksSum += watch.ElapsedTicks;
			}
			rr.MemoryUsed = GC.GetTotalMemory(true) - memoryStart;

			rr.InsertTicks = insertTicksSum;

			for (int i = 0; i < keys.Length; ++i)
			{
				string key = keys[i];
				watch = Stopwatch.StartNew();
				object searchResult = dict[key];
				watch.Stop();
				dummy += ((string)searchResult).Length;
				rr.SearchTicks = watch.ElapsedTicks;
			}

			watch = Stopwatch.StartNew();
			foreach (var curr in dict) { dummy += 2; }
			watch.Stop();

			rr.ForEachTicks = watch.ElapsedTicks;
			rr.Dummy = dummy;
			return rr;
		}

		private static string GetRandomLetter(Random rand, int i)
		{
			return Letters[rand.Next(0, Letters.Length)];
		}

	}
}
