using System.Numerics;

namespace Lexxys.Tests.Xml
{
	using Lexxys.Xml;

	[TestClass]
	public class XmlGetValue
	{

		[TestMethod]
		public void GetStructValue()
		{
			A actual;

			actual = XmlTools.GetValue(X(@"
				value
					Int 1
				"), new A());
			Assert.AreEqual(new A { Int=1 }, actual);
			
			actual = XmlTools.GetValue(X(@"
				value
					:Int 3
					Date 2001-01-01
				"), new A());
			Assert.AreEqual(new A { Int=3, Date=new DateTime(2001, 1, 1) }, actual);
			actual = XmlTools.GetValue(X(@"
				value (Magic = 29)
					XDate 1812-10-10
				"), new A());
			Assert.AreEqual(new A(magic:29), actual);

			actual = XmlTools.GetValue(X(@"
				value (Magic = 10)
					:Int 3
					Date 1812-10-10
				"), new A());
			Assert.AreEqual(new A(magic:10) { Date=new DateTime(1812, 10, 10), Int=3 }, actual);
		}

		[TestMethod]
		public void CreateArrayParameter()
		{
			B actual;
			actual = XmlTools.GetValue(X(@"
				value
					:nm One Name
					itm
						item (i = 1)
						item (i = 2)
				"), new B());
			Assert.AreEqual(new B("One Name", [new A(i:1), new A(i:2)]), actual);
		}

		public class B
		{
			// ReSharper disable UnusedAutoPropertyAccessor.Global
			public string Name { get; }
			public A[] Items { get; }

			public B()
			{
			}

			public B(string nm, A[] itm)
			{
				Name = nm;
				Items = itm;
			}

			public override string ToString() => DumpWriter.ToString(this);

			public override bool Equals(object obj)
			{
				return obj is B b && b.ToString() == ToString();
			}

			public override int GetHashCode()
			{
				return ToString().GetHashCode();
			}
		}

		public struct A
		{
			public int Int;
			public bool? Bool;
			public DateTime Date;
			public Money Money;
			public BigInteger BigInt;

			public A(int magic)
			{
				Int = magic + 27;
				Bool = (magic & 1) == 0;
				Date = new DateTime(1812, 5, magic);
				Money = (Money)magic * Math.PI;
				BigInt = (BigInteger)magic * long.MaxValue * long.MaxValue;
			}

			public A(int i = default, bool? b = default, DateTime d = default, Money m = default, BigInteger n = default)
			{
				Int = i;
				Bool = b;
				Date = d;
				Money = m;
				BigInt = n;
			}

			public override string ToString() => DumpWriter.ToString(this);
		}

		private static IXmlReadOnlyNode X(string value) => TextToXmlConverter.ConvertLite(value).Single();

	}
}
