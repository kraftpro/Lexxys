// Lexxys Infrastructural library.
// file: LoggingTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lexxys;
using System.Threading;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	static class LoggingTest
	{
		private static readonly Logger Log = new Logger("LoggingTest");

		public static void Test(int n, int ths = 0)
		{
			int index = -1;

			Log.Write($"START {n:N0} RECORDS BY {ths} THREAD(S).");
			if (ths < 2)
			{
				for (int i = 0; i < n; ++i)
				{
					Log.Write(i.ToString("000000 ") + RandomText());
				}
			}
			else
			{
				Parallel.For(0, ths, i =>
				{
					while (Interlocked.Increment(ref index) < n)
					{
						Log.Write(index.ToString("000000 ") + i.ToString("00 ") + RandomText());
					}
				});
			}
			Log.Write($"END {n:N0} RECORDS (index: {index}).");
		}

		public static void TestEnter(int n)
		{
			Log.Write($"FROM 0 TO {n - 1} (FROM 2 TO {n + 1:X}).");
			for (int i = 0; i < n; ++i)
			{
				using (Log.Enter(RandomText()))
				{
				}
			}
		}


		public static void TestAddWriter()
		{
			
		}

		static string RandomText()
		{
			//return GenerateRandomText(127);
			return __data[Interlocked.Increment(ref __i) % __data.Length];
		}
		private static int __i;

		private static string[] InitialiseData()
		{
			string[] data = new string[512];
			for (int i = 0; i < data.Length; ++i)
			{
				data[i] = GenerateRandomText(127);
			}
			return data;
		}

		static string GenerateRandomText(int maxLength)
		{
			int s = Interlocked.Add(ref __seed, 1609);
			int r = Math.Abs((s + 613) * 1321 % 2147454941);
			__seed = r;
			int w = __s.Length;
			int n = maxLength / 2 + r % (maxLength / 2);
			char[] cc = new char[n];
			for (int i = 0; i < cc.Length; ++i)
			{
				r = Math.Abs((r + 151) * 1201 % 2147454997);
				cc[i] = __s[r % w];
			}
			return new String(cc);
		}
		private static int __seed = 877;
		private static readonly char[] __s = "abcdefghijklmnopqrstvuwxyzABCDEFGHIJKLMNOPQRSTVUWXYZ123456789~!@#$%^&*()_+=`{[}]|\\:;\"'<,>.?/ ".ToCharArray();
		private static readonly string[] __data = InitialiseData();
	}

	class ThreadInterrupt
	{
		public static void Test()
		{
			StayAwake stayAwake = new StayAwake();
			Thread newThread = new Thread(stayAwake.ThreadMethod) { Priority = ThreadPriority.Lowest };
			newThread.Start();

			// The following line causes an exception to be thrown 
			// in ThreadMethod if newThread is currently blocked
			// or becomes blocked in the future.
			newThread.Interrupt();
			Console.WriteLine("Main thread calls Interrupt on newThread.");

			// Tell newThread to go to sleep.
			stayAwake.SleepSwitch = true;

			// Wait for newThread to end.
			newThread.Join(10);
		}
	}

	class StayAwake
	{
		public bool SleepSwitch { get; set; }

		public void ThreadMethod()
		{
			Console.WriteLine("newThread is executing ThreadMethod.");
		//	while (!SleepSwitch)
		//	{
				// Use SpinWait instead of Sleep to demonstrate the 
				// effect of calling Interrupt on a running thread.
				Thread.SpinWait(10000000);
		//	}
			try
			{
				Console.WriteLine("newThread going to sleep.");

				// When newThread goes to sleep, it is immediately 
				// woken up by a ThreadInterruptedException.
				Thread.Sleep(Timeout.Infinite);
			}
			catch (ThreadInterruptedException)
			{
				Console.WriteLine("newThread cannot go to sleep - " +
				"interrupted by main thread.");
			}
			Console.WriteLine("Done");
		}
	}
}
