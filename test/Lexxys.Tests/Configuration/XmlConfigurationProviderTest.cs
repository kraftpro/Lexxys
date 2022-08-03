// Lexxys Infrastructural library.
// file: XmlConfigurationProviderTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Lexxys;
using Lexxys.Configuration;
using Lexxys.Xml;


namespace Lexxys.Tests.Configuration
{


	/// <summary>
	///This is a test class for XmlConfigurationProviderTest and is intended
	///to contain all XmlConfigurationProviderTest Unit Tests
	///</summary>
	[TestClass]
	public class XmlConfigurationProviderTest
	{
		public TestContext TestContext { get; set; }

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
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


		/// <summary>
		///A test for GetValue
		///</summary>
		[TestMethod]
		public void ReflectionParserTest()
		{
			var provider = GetConfiguration();
			object obj = provider.GetValue("Setting", typeof(Setting));
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(Setting));
			var test = obj as Setting;
			Assert.AreEqual(100, test.IntItem);
			Assert.AreEqual(new DateTime(2010, 1, 12), test.DateTimeItem);
			Assert.AreEqual("Aa Bb Cc", test.StringItem);
			Assert.IsNotNull(test.IntBasedItem);
			Assert.AreEqual(200, test.IntBasedItem.Value);
			CollectionAssert.AreEqual(
				new int[] { 1, 2, 3 },
				test.IntArray);
			CollectionAssert.AreEqual(
				new int[] { 1, 2, 3 },
				test.NewIntArray);
			CollectionAssert.AreEqual(
				new int[] { 1, 2, 1 },
				test.MoreIntArray);
			CollectionAssert.AreEqual(
				new Item2[] { new Item2("one", 1.1m), new Item2("two", 2.2m) },
				test.Item2s);
			CollectionAssert.AreEqual(
				new Item2[] { new Item2("ONE", 11.11m), new Item2("TWO", 22.22m) },
				test.ItemsIList.ToArray());
		}

		[TestMethod]
		public void XmlLiteNodeParserTest()
		{
			var provider = GetConfiguration();
			object obj = provider.GetValue("Setting", typeof(Setting2));
			Assert.IsNotNull(obj);
			Assert.IsInstanceOfType(obj, typeof(Setting2));
			Setting test = obj as Setting2;
			Assert.AreEqual(100, test.IntItem);
			Assert.AreEqual(new DateTime(2010, 1, 12), test.DateTimeItem);
			Assert.AreEqual("Aa Bb Cc", test.StringItem);
			Assert.IsNotNull(test.IntBasedItem);
			Assert.AreEqual(200, test.IntBasedItem.Value);
			CollectionAssert.AreEqual(
				new int[] { 1, 2, 3 },
				test.IntArray);
			CollectionAssert.AreEqual(
				new Item2[] { new Item2("one", 1.1m), new Item2("two", 2.2m) },
				test.Item2s);
			CollectionAssert.AreEqual(
				new Item2[] { new Item2("ONE", 11.11m), new Item2("TWO", 22.22m) },
				test.ItemsIList.ToArray());
		}

		[TestMethod, DoNotParallelize]
		public void PerformanceTest()
		{
			var provider = GetConfiguration();
			int count = 50000;
			long x = WatchTimer.Start();
			for (int i = 0; i < count; ++i)
			{
				object obj = provider.GetValue("Setting", typeof(Setting));
			}
			x = WatchTimer.Stop(x);

			long y = WatchTimer.Start();
			for (int i = 0; i < count; ++i)
			{
				object obj = provider.GetValue("Setting", typeof(Setting2));
			}
			y = WatchTimer.Stop(y);
			TestContext.WriteLine("Reflection: {0}, XmlLiteNode: {1}", WatchTimer.ToString(x, false), WatchTimer.ToString(y, false));
		}


		private static IConfigProvider GetConfiguration()
		{
			return CreateConfig(
@"string:[txt]?
%%ignore-case
Setting
	:intItem		100
	:dateTimeItem	2010-01-12 #comment
	stringItem		Aa Bb <#inline comment #>Cc
	intBasedItem	200
	intArray
		item	1 # comments
		item	2 // comments
		item	3
	newIntArray [1, 2, 3] <#
	params (p1=1, p2:""pe = p2
	#>
	params (p1=1, p2:""pe = p2""
		p3:true,
		p5:false)
	item2s
		%item	name	value
		-		one		1.1
		-		two		2.2
	ItemsIList
		%item	name	value
		-		ONE		11.11
		-
			Name	TWO
			Value	22.22
	moreIntArray
		[
			1,
			2,
			1,
# 345
		]
		
			
		
	
", null);
		}

		class Setting2: Setting
		{
			public Setting2()
			{

			}

			public static Setting2 Create(XmlLiteNode node)
			{
				if (node == null)
					return null;
				var x = new Setting2
				{
					IntItem = XmlTools.GetInt32(node["IntItem"], 0),
					DateTimeItem = XmlTools.GetDateTime(node["DateTimeItem"], DateTime.MinValue, DateTime.MinValue, DateTime.MaxValue),
					StringItem = XmlTools.GetString(node.Element("StringItem").Value, null),
					IntBasedItem = new IntConvertable(XmlTools.GetInt32(node.Element("IntBasedItem").Value, 0))
				};
				XmlLiteNode xx = node.Element("IntArray");
				if (!xx.IsEmpty)
				{
					var tmp = new List<int>();
					foreach (var item in xx.Elements)
					{
						tmp.Add(XmlTools.GetInt32(item.Value));
					}
					x.IntArray = tmp.ToArray();
				}
				xx = node.FirstOrDefault("ITEM2S");
				if (xx != null)
				{
					x.Item2s = new LinkedList<Item2>();
					foreach (var item in xx.Elements)
					{
						var t = new Item2(XmlTools.GetString(item["name"], null), XmlTools.GetValue<decimal>(item["value"], 0));
						if (t.Name != null)
							x.Item2s.AddLast(t);
					}
				}
				xx = node.FirstOrDefault("ItemsIList");
				if (xx != null)
				{
					x.ItemsIList = new List<Item2>();
					foreach (var item in xx.Elements)
					{
						var t = new Item2(XmlTools.GetString(item["name"], null), XmlTools.GetValue<decimal>(item["value"], 0));
						if (t.Name != null)
							x.ItemsIList.Add(t);
						else
						{
							t.Name = XmlTools.GetString(item.Element("Name").Value, null);
							t.Value = XmlTools.GetValue<decimal>(item.Element("Value").Value, t.Value);
							if (item.Name != null)
								x.ItemsIList.Add(t);
						}
					}
				}
				return x;
			}
		}

#pragma warning disable 649

		class Setting
		{
			public int IntItem;
			public string StringItem;
			public DateTime DateTimeItem;
			public IntConvertable IntBasedItem;
			public int[] IntArray;
			public int[] NewIntArray;
			public int[] MoreIntArray;
			public Params Params;
			public LinkedList<Item2> Item2s;
			public IList<Item2> ItemsIList;

			public Setting()
			{
			}

			public Setting(int intItem)
			{
				IntItem = intItem;
			}

			public Setting(int intItem, DateTime dateTimeItem)
			{
				IntItem = intItem;
				DateTimeItem = dateTimeItem;
			}
		}

		public class Params
		{
			public Params(int p1, string p2, bool p3, bool? p5)
			{
				P1 = p1;
				P2 = p2;
				P3 = p3;
				P5 = p5;
			}

			public int P1 { get; }
			public string P2 { get; }
			public bool P3 { get; }
			public bool? P5 { get; }
		}

		struct Item2
		{
			public string Name;
			public decimal Value;

			public Item2(string name, decimal value)
			{
				Name = name;
				Value = value;
			}
		}

		class IntConvertable
		{
			private readonly int _value;

			public IntConvertable(int value)
			{
				_value = value;
			}

			public int Value
			{
				get { return _value; }
			}
		}

		public static IConfigProvider CreateConfig(string value, IReadOnlyCollection<string> parameters = null)
		{
			return XmlConfigurationProvider.Create(new Uri(value), parameters) ?? throw new InvalidOperationException("Cannot create configuration");
		}



	}
}
