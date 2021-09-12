using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests
{
	[TestClass]
	public class CollectionsTests
	{

		[TestMethod]
		public void IterageOrderedBagTest()
		{
			var bag = new OrderedBag<string, object>
			{
				{ "one", 1 },
				{ "two", 2 },
				{ "one", "One" }
			};
			Assert.AreEqual(3, bag.Count);

			int one = 0;
			int two = 0;
			foreach (var item in bag)
			{
				if (item.Key == "one") ++one;
				if (item.Key == "two") ++two;
			}
			Assert.AreEqual(2, one);
			Assert.AreEqual(1, two);

			var dic = (IDictionary<string, object>)bag;
			one = 0;
			two = 0;
			foreach (var item in dic)
			{
				if (item.Key == "one") ++one;
				if (item.Key == "two") ++two;
			}
			Assert.AreEqual(2, one);
			Assert.AreEqual(1, two);

			var idic = (IDictionary)bag;
			one = 0;
			two = 0;
			foreach (var item in idic)
			{
				if (item is DictionaryEntry e)
				{
					if (Equals(e.Key, "one")) ++one;
					if (Equals(e.Key, "two")) ++two;
				}
			}
			Assert.AreEqual(2, one);
			Assert.AreEqual(1, two);
		}

	}
}
