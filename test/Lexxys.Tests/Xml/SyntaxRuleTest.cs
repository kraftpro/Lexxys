// Lexxys Infrastructural library.
// file: SyntaxRuleTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Collections;

namespace Lexxys.Tests.Xml
{
	using Lexxys.Xml;
	// ReSharper disable UnusedMember.Local

	//[TestClass()]
	public class SyntaxRuleCollectionTest
	{
		#region Private Accessors

		class SyntaxRuleCollection
		{
			private PrivateObject _o;
			private IList _list;

			public SyntaxRuleCollection()
			{
				PrivateObject root = new PrivateObject(typeof(TextToXmlConverter));
				_o = new PrivateObject(root, "_syntaxRules");
				_list = (IList)_o.GetField("_rule");
			}

			private SyntaxRuleCollection(object obj)
			{
				_o = new PrivateObject(obj);
				_list = (IList)_o.GetField("_rule");
			}

			public static SyntaxRuleCollection Create(object obj)
			{
				return obj == null ? null: new SyntaxRuleCollection(obj);
			}

			public void Add(string path, string pattern, bool ignoreCase, string[] attribs)
			{
				_o.Invoke("Add", path, pattern, ignoreCase, attribs);
			}

			public SyntaxRuleCollection GetApplicatbleRules(string root)
			{
				return SyntaxRuleCollection.Create(_o.Invoke("GetApplicatbleRules", root));
			}

			public SyntaxRule Find(string root)
			{
				return SyntaxRule.Create(_o.Invoke("Find", root));
			}

			public SyntaxRule Find(string root, string name)
			{
				return SyntaxRule.Create(_o.Invoke("Find", root, name));
			}

			public int Count
			{
				get { return _list.Count; }
			}

			public SyntaxRule this[int i]
			{
				get { return SyntaxRule.Create(_list[i]); }
			}
		}

		class SyntaxRule
		{
			private PrivateObject _o;

			public SyntaxRule(object obj)
			{
				_o = new PrivateObject(obj);
			}

			public static SyntaxRule Create(object obj)
			{
				return obj == null ? null: new SyntaxRule(obj);
			}

			public string NodeName
			{
				get { return _o.GetProperty("NodeName") as string; }
			}

			public string Pattern
			{
				get
				{
					object x = _o.GetField("_pattern");
					return x == null ? null: x.ToString();
				}
			}

			public string FixedPath
			{
				get { return (string)_o.GetField("_start"); }
			}
		}
		#endregion

		[TestMethod]
		public void TestSyntaxRules()
		{
			SyntaxRuleCollection rules = new SyntaxRuleCollection();
			SyntaxRule r;
			SyntaxRuleCollection rr;

			rules.Add("a/b", "x/y/Z", true, new[] { "p", "q" });

			foreach (var item in new[] { "a/b/x/y", "a/B/X/Y", "/A/B/X/Y/" })
			{
				rr = rules.GetApplicatbleRules(item);
				Assert.IsNotNull(rr, item);

				r = rules.Find(item);
				Assert.IsNotNull(r, item);
				Assert.AreEqual("Z", r.NodeName);

				r = rr.Find(item);
				Assert.IsNotNull(r, item);
				Assert.AreEqual("Z", r.NodeName);

			}

			foreach (var item in new[] { "a/b", "a", "a/b/x/", "a/b/x/y/z" })
			{
				rr = rules.GetApplicatbleRules(item);
				Assert.IsNull(rr, item);

				r = rules.Find(item);
				Assert.IsNull(r, item);
			}

			rules.Add("a/b", "*/y/Z1", true, new[] { "p1", "q1" });
			Assert.AreEqual(2, rules.Count);
			r = rules[1];
			Assert.IsNotNull(r);
			Assert.AreEqual("a/b", r.FixedPath);
			Assert.AreEqual(@"\Aa/b/([^/]*)/y\z", r.Pattern);
			Assert.AreEqual("Z1", r.NodeName);

			rr = rules.GetApplicatbleRules("a/b/x/y");
			Assert.AreEqual(2, rr.Count);

			r = rr.Find("a/b/x/yyy");
			Assert.IsNull(r);
			r = rr.Find("a/b/x1/y");
			Assert.IsNotNull(r);
			Assert.IsNull(r.Pattern);
			Assert.AreEqual("a/b/x1/y", r.FixedPath);

			rules.Add("a/b/(*)", "*/Z1", true, new[] { "p1", "q1" });
			Assert.AreEqual(3, rules.Count);
			r = rules[2];
			Assert.IsNotNull(r);
			Assert.AreEqual("a/b", r.FixedPath);
			Assert.AreEqual(@"\Aa/b/[^/]*/([^/]*)\z", r.Pattern);
			Assert.AreEqual("Z1", r.NodeName);

			rr = rules.GetApplicatbleRules("a/b/x/y");
			Assert.AreEqual(2, rr.Count);
			rr = rules.GetApplicatbleRules("a/b/x1/y");
			Assert.AreEqual(2, rr.Count);

			r = rr.Find("a/bb/xx/yy");
			Assert.IsNull(r);
			r = rr.Find("a/b/xx/yy");
			Assert.IsNotNull(r);
			Assert.AreEqual(@"\Aa/b/[^/]*/yy\z", r.Pattern);
			Assert.AreEqual("a/b", r.FixedPath);


			r = rr.Find("a/b/x/yyy");
			Assert.IsNull(r);

			foreach (var item in new[] { "a/b/xx/y", "a/b/x/y/zz" })
			{
				SyntaxRule rule = rules.Find(item);
				Assert.IsNull(rule, item);
			}



		}
	}
}