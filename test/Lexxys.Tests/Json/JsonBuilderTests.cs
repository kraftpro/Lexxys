using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Json
{
	[TestClass]
	public class JsonBuilderTests
	{
		[TestMethod]
		public void CollectionTest()
		{
			object nil = null;
			var j = new JsonStringBuilder();
			j.Obj();
			j.Item("ints");
			j.Val(new[] { 1, 2, 3 });
			j.Item("strs");
			j.Val(new[] { "1", "2", "3" });
			j.Item("chrs");
			j.Val(new[] { '1', '2', '3' });
			j.Item("itms");
			j.Val(new[] { new Item(1), new Item(2), new Item(3) });
			j.Item("empty").Val("");
			j.Item("nil").Val(nil);
			j.Item("obj").Val(new object());
			j.Item("nop").Obj().End();
			j.End();
			var expected = "{\"ints\":[1,2,3],\"strs\":[\"1\",\"2\",\"3\"],\"chrs\":[\"1\",\"2\",\"3\"],\"itms\":[1,2,3],\"empty\":\"\",\"nil\":null,\"obj\":{},\"nop\":{}}";
			var result = j.ToString();
			Assert.AreEqual(expected, result);
		}

		public class Item: IDumpJson
		{
			[DebuggerStepThrough]
			public Item(int value)
			{
				Value = value;
			}

			public int Value { get; }

			public JsonBuilder ToJsonContent(JsonBuilder json)
			{
				return json.Val(Value);
			}
		}
	}
}
