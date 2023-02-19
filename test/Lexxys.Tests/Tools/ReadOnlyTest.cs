// Lexxys Infrastructural library.
// file: ReadOnlyTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading;

namespace Lexxys.Tests.Tools
{

	/// <summary>
	///This is a test class for ReadOnlyTest and is intended
	///to contain all ReadOnlyTest Unit Tests
	///</summary>
	[TestClass()]
	public class ReadOnlyTest
	{


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion

		Dictionary<int, string> GetTestDictionary()
		{
			return new Dictionary<int, string>()
			{
				{ 1, "one" },
				{ 2, "two" },
				{ 3, "three" },
			};
		}
		private static int _itemId = 20;

		KeyValuePair<int, string> NextDictionaryItem()
		{
			int id = Interlocked.Increment(ref _itemId);
			return new KeyValuePair<int, string>(id, "item #" + id.ToString());
		}

		List<int> GetTestList()
		{
			return new List<int>()
			{
				1,
				2,
				3
			};
		}

		int NextListItem()
		{
			return Interlocked.Increment(ref _itemId);
		}

		void TestGenericIDictionary(IDictionary<int, string> actual)
		{
			KeyValuePair<int, string> first = actual.First();
			string result;

			result = null;
			try
			{
				actual[first.Key] = first.Value + " UPDATED";
				result = "value updated";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				KeyValuePair<int, string> item = NextDictionaryItem();
				actual.Add(item.Key, item.Value);
				result = "value added";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Remove(first.Key);
				result = "value removed";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Clear();
				result = "collection cleared";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Add(NextDictionaryItem());
				result = "item added";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Remove(first);
				result = "item removed";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
		}

		void TestIDictionary(IDictionary actual)
		{
			object[] key = actual.Keys.Cast<object>().ToArray();
			object[] value = actual.Values.Cast<object>().ToArray();
			string result;

			result = null;
			try
			{
				actual[key[0]] = value[value.Length - 1];
				result = "value updated";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				KeyValuePair<int, string> item = NextDictionaryItem();
				actual.Add(item.Key, item.Value);
				result = "value added";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Remove(key[0]);
				result = "value removed";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Clear();
				result = "collection cleared";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void DictionaryWrapTest()
		{
			IDictionary<int, string> actual;

			actual = ReadOnly.Wrap((IDictionary<int, string>)null);
			Assert.IsNull(actual);

			Dictionary<int, string> value = GetTestDictionary();
			actual = ReadOnly.Wrap(value);
			Assert.IsTrue(actual.IsReadOnly);

			CollectionAssert.AreEqual(value, (ICollection)actual);

			KeyValuePair<int, string> item = NextDictionaryItem();
			value.Add(item.Key, item.Value);
			var four = value.First(x => x.Key == item.Key);
			CollectionAssert.Contains((ICollection)actual, four);

			value[2] = "KEY # TWO";
			Assert.AreEqual("KEY # TWO", actual[2]);

			TestGenericIDictionary(actual);

			IDictionary actual2 = actual as IDictionary;
			Assert.IsNotNull(actual2);

			TestIDictionary(actual2);
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void DictionaryWrapCopyTest()
		{
			IDictionary<int, string> actual;

			actual = ReadOnly.WrapCopy((IDictionary<int, string>)null);
			Assert.IsNull(actual);

			Dictionary<int, string> value = GetTestDictionary();
			actual = ReadOnly.WrapCopy(value);
			Assert.IsTrue(actual.IsReadOnly);

			CollectionAssert.AreEqual(value, (ICollection)actual);

			KeyValuePair<int, string> item = NextDictionaryItem();
			value.Add(item.Key, item.Value);
			var four = value.First(x => x.Key == item.Key);
			CollectionAssert.DoesNotContain((ICollection)actual, four);

			value[2] = "KEY # TWO";
			Assert.AreNotEqual("KEY # TWO", actual[2]);

			TestGenericIDictionary(actual);

			IDictionary actual2 = actual as IDictionary;
			Assert.IsNotNull(actual2);

			TestIDictionary(actual2);
		}

		void TestGenericIList(IList<int> actual)
		{
			string result;

			result = null;
			try
			{
				actual[0] = -1;
				result = "value updated";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Add(-2);
				result = "item added";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				var item = actual.First();
				actual.Remove(item);
				result = "item removed";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Clear();
				result = "collection cleared";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void ListWrapTest()
		{
			IList<int> actual;

			actual = ReadOnly.Wrap((IList<int>)null);
			Assert.IsNull(actual);

			List<int> value = GetTestList();
			actual = ReadOnly.Wrap(value);
			Assert.IsTrue(actual.IsReadOnly);

			Assert.IsTrue(Lexxys.Comparer.Equals(value, actual, null));

			var item = NextListItem();
			value.Add(item);
			Assert.IsTrue(actual.Contains(item));

			item = NextListItem();
			value[2] = item;
			Assert.IsTrue(actual.Contains(item));

			TestGenericIList(actual);
		}


		void TestGenericICollection(ICollection<int> actual)
		{
			string result;

			result = null;
			try
			{
				actual.Add(-1);
				result = "item added";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				var item = actual.First();
				actual.Remove(item);
				result = "item removed";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);
			try
			{
				actual.Clear();
				result = "collection cleared";
			}
			catch (Exception)
			{
			}
			Assert.IsNull(result);

			Assert.IsTrue(actual.IsReadOnly);
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void CollectionWrapTest()
		{
			ICollection<int> actual;

			actual = ReadOnly.Wrap((ICollection<int>)null);
			Assert.IsNull(actual);

			List<int> value = GetTestList();
			actual = ReadOnly.Wrap((ICollection<int>)value);
			Assert.IsTrue(actual.IsReadOnly);

			CollectionAssert.AreEqual(value, (ICollection)actual);

			var item = NextListItem();
			value.Add(item);
			Assert.IsTrue(actual.Contains(item));

			item = NextListItem();
			value[2] = item;
			Assert.IsTrue(actual.Contains(item));

			TestGenericICollection(actual);
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void CollectionWrapCopyTest()
		{
			ICollection<int> actual;

			actual = ReadOnly.WrapCopy((ICollection<int>)null);
			Assert.IsNull(actual);

			List<int> value = GetTestList();
			actual = ReadOnly.WrapCopy((ICollection<int>)value);
			Assert.IsTrue(actual.IsReadOnly);

			Assert.IsTrue(Lexxys.Comparer.Equals(value, actual, null));

			var item = NextListItem();
			value.Add(item);
			Assert.IsFalse(actual.Contains(item));

			item = NextListItem();
			value[2] = item;
			Assert.IsFalse(actual.Contains(item));

			TestGenericICollection(actual);
		}


		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void EnumerableWrapTest()
		{
			IEnumerable<int> actual;

			actual = ReadOnly.Wrap((IEnumerable<int>)null);
			Assert.IsNull(actual);

			List<int> value = GetTestList();
			actual = ReadOnly.Wrap((IEnumerable<int>)value);

			Assert.IsTrue(Lexxys.Comparer.Equals(value, actual, null));

			var item = NextListItem();
			value.Add(item);
			Assert.IsTrue(actual.Contains(item));

			item = NextListItem();
			value[2] = item;
			Assert.IsTrue(actual.Contains(item));
		}

		/// <summary>
		///A test for Wrap
		///</summary>
		[TestMethod()]
		public void EnumerableWrapCopyTest()
		{
			IEnumerable<int> actual;

			actual = ReadOnly.WrapCopy((IEnumerable<int>)null);
			Assert.IsNull(actual);

			List<int> value = GetTestList();
			actual = ReadOnly.WrapCopy((IEnumerable<int>)value);

			Assert.IsTrue(Lexxys.Comparer.Equals(value, actual, null));

			var item = NextListItem();
			value.Add(item);
			Assert.IsFalse(actual.Contains(item));

			item = NextListItem();
			value[2] = item;
			Assert.IsFalse(actual.Contains(item));
		}

	}
}
