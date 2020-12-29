using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Test.Con
{
	unsafe class TestTokenizer
	{
		ref struct Abc
		{
			public int IntField1;
			public int IntField2;
			public int IntField3;
			public int IntField4;
			public int IntField5;

			public int IntProperty
			{
				get
				{
					return IntField1;
				}
				set
				{
					IntField1 = value;
				}
			}

			public int GetValue()
			{
				//fixed (Abc* th = &this)
				//{
				//	Console.WriteLine($"GET {((IntPtr)th).ToString("x")}: Value = {th->IntField1}");
				//}
				return IntProperty;
			}

			public void SetValue(int value)
			{
				IntProperty = value;
				//fixed (Abc* th = &this)
				//{
				//	Console.WriteLine($"SET {((IntPtr)th).ToString("x")}: Value = {th->IntField1}");
				//}
			}
		}

		public static void TestStructs()
		{
			var a = new Abc();
			Console.WriteLine($"TST {Addr(&a)}: Value = {a.IntField1}");
			TestVal(a);
			TestIn(in a);
			TestRef(ref a);
			Console.WriteLine("done");

			void TestVal(Abc abc)
			{
				Console.WriteLine($"VAL {Addr(&abc)}: Value = {abc.IntField1}");
				int i = abc.GetValue();
				abc.SetValue(i + 1);
			}

			void TestIn(in Abc abc)
			{
				fixed (Abc* p = &abc)
					Console.WriteLine($"IN  {Addr(&p)}: Value = {p->IntField1}");
				int i = abc.GetValue();
				abc.SetValue(i + 1);
			}

			void TestRef(ref Abc abc)
			{
				fixed (Abc* p = &abc)
					Console.WriteLine($"REF {Addr(&p)}: Value = {p->IntField1}");
				Console.WriteLine("REF");
				int i = abc.GetValue();
				abc.SetValue(i + 1);
			}

			string Addr(void *ptr)
			{
				return ((IntPtr)ptr).ToString("x");
			}
		}
	}
}
