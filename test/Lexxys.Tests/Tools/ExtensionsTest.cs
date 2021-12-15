using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Tools
{
	[TestClass()]
	public class ExtensionsTest
	{
		[TestMethod]
		[DataRow(typeof(A), false, "A")]
		[DataRow(typeof(A), true, "Lexxys.Tests.Tools.A")]
		[DataRow(typeof(A.B), false, "A.B")]
		[DataRow(typeof(A.B), true, "Lexxys.Tests.Tools.A.B")]
		[DataRow(typeof(A.B.C), false, "A.B.C")]
		[DataRow(typeof(A.B.C), true, "Lexxys.Tests.Tools.A.B.C")]

		[DataRow(typeof(A<int, long>.B), false, "A<int,long>.B")]
		[DataRow(typeof(A<int, long>.B), true, "Lexxys.Tests.Tools.A<int,long>.B")]
		[DataRow(typeof(A<int, long>.B.C), false, "A<int,long>.B.C")]
		[DataRow(typeof(A<int, long>.B.C), true, "Lexxys.Tests.Tools.A<int,long>.B.C")]

		[DataRow(typeof(A<int, long>.B<short>), false, "A<int,long>.B<short>")]
		[DataRow(typeof(A<int, long>.B<short>), true, "Lexxys.Tests.Tools.A<int,long>.B<short>")]
		[DataRow(typeof(A<int, long>.B<short>.C<byte>), false, "A<int,long>.B<short>.C<byte>")]
		[DataRow(typeof(A<int, long>.B<short>.C<byte>), true, "Lexxys.Tests.Tools.A<int,long>.B<short>.C<byte>")]

		[DataRow(typeof(A<int, long>.B<short, byte>), false, "A<int,long>.B<short,byte>")]
		[DataRow(typeof(A<int, long>.B<short, byte>), true, "Lexxys.Tests.Tools.A<int,long>.B<short,byte>")]
		[DataRow(typeof(A<int, long>.B<short, byte>.C<char>), false, "A<int,long>.B<short,byte>.C<char>")]
		[DataRow(typeof(A<int, long>.B<short, byte>.C<char>), true, "Lexxys.Tests.Tools.A<int,long>.B<short,byte>.C<char>")]
		[DataRow(typeof(A<int, long>.B<short, byte>.C<char,int>), false, "A<int,long>.B<short,byte>.C<char,int>")]
		[DataRow(typeof(A<int, long>.B<short, byte>.C<char,int>), true, "Lexxys.Tests.Tools.A<int,long>.B<short,byte>.C<char,int>")]

		[DataRow(typeof(A<,>), false, "A<T1,T2>")]
		[DataRow(typeof(A<,>.B<>.C<>), false, "A<T1,T2>.B<T3>.C<T4>")]

		[DataRow(typeof(int[]), false, "int[]")]
		[DataRow(typeof(byte[][,]), false, "byte[][,]")]
		[DataRow(typeof(A<int, long>.B<short>[,,][][,]), false, "A<int,long>.B<short>[,,][][,]")]
		[DataRow(typeof(int*), false, "int*")]
		[DataRow(typeof(void**), false, "void**")]

		[DataRow(typeof(B), false, "B")]
		[DataRow(typeof(ValueTuple), false, "ValueTuple")]
		[DataRow(typeof((int, int)), false, "(int,int)")]
		[DataRow(typeof((int One, DateTime Two)), false, "(int,DateTime)")]
		public void GetTypeNameTest(Type type, bool fullName, string expected)
		{
			var actual = type.GetTypeName(fullName);
			Assert.AreEqual(expected, actual);
		}
	}

	class A
	{
		public class B
		{
			public enum C
			{

			}
		}
	}

	class A<T1, T2>
	{
		public class B
		{
			public enum C
			{

			}
		}

		public class B<T3>
		{
			public struct C<T4>
			{

			}
		}

		public class B<T3, T4>
		{
			public struct C<T5>
			{

			}
			public struct C<T5, T6>
			{

			}
		}
	}

	ref struct B
	{
	}
}
