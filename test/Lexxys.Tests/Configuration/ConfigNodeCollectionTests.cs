using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexxys.Configuration.New;

namespace Lexxys.Tests.Configuration;

[TestClass]
public class ConfigNodeCollectionTests
{
    [TestMethod]
    public void EmptyCollection_IsEmpty()
    {
        var collection = new ConfigNodeCollection();
        Assert.AreEqual(0, collection.Count);
        Assert.IsTrue(collection.IsEmpty());
        Assert.IsTrue(collection.IsArray);
        Assert.IsFalse(collection.IsMap);
        Assert.IsFalse(collection.IsMixed);
    }

    [TestMethod]
    public void AddNode_AddsToArray()
    {
        var node = new ConfigNode("value");
        var collection = new ConfigNodeCollection();
        collection.Add(node);
        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual(node, collection[0]);
        Assert.IsTrue(collection.IsArray);
    }

    [TestMethod]
    public void AddKeyedNode_AddsToMap()
    {
        var node = new ConfigNode("value");
        var collection = new ConfigNodeCollection();
        collection.Add("key", node);
        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual(node, collection["key"]);
        Assert.IsTrue(collection.IsMap);
        Assert.IsFalse(collection.IsArray);
    }

    [TestMethod]
    public void AddDuplicateKey_JoinsNodes()
    {
        var node1 = new ConfigNode("value1");
        var node2 = new ConfigNode("value2");
        var collection = new ConfigNodeCollection();
        collection.Add("key", node1);
        collection.Add("key", node2);
        var joined = collection["key"];
        Assert.IsFalse(joined.IsEmpty);
        Assert.IsNull(joined.Value);
        Assert.AreEqual(2, joined.Collection.Count);
        Assert.AreEqual("value1", joined.Collection[0].Value);
        Assert.AreEqual("value2", joined.Collection[1].Value);
    }

    [TestMethod]
    public void AddRange_AddsMultipleNodes()
    {
        var nodes = new[] { new ConfigNode("a"), new ConfigNode("b") };
        var collection = new ConfigNodeCollection();
        collection.AddRange(nodes);
        Assert.AreEqual(2, collection.Count);
        Assert.AreEqual("a", collection[0].Value);
        Assert.AreEqual("b", collection[1].Value);
    }

    [TestMethod]
    public void AddRange_Keyed_AddsMultipleKeyedNodes()
    {
        var nodes = new List<(string?, ConfigNode)>
        {
            ("x", new ConfigNode("1")),
            ("y", new ConfigNode("2"))
        };
        var collection = new ConfigNodeCollection();
        collection.AddRange(nodes);
        Assert.AreEqual(2, collection.Count);
        Assert.AreEqual("1", collection["x"].Value);
        Assert.AreEqual("2", collection["y"].Value);
        Assert.IsTrue(collection.IsMap);
    }

    [TestMethod]
    public void ContainsKey_ReturnsTrueForExistingKey()
    {
        var collection = new ConfigNodeCollection();
        collection.Add("foo", new ConfigNode("bar"));
        Assert.IsTrue(collection.ContainsKey("foo"));
        Assert.IsFalse(collection.ContainsKey("baz"));
    }

    [TestMethod]
    public void TryGetValue_ReturnsTrueAndValue()
    {
        var collection = new ConfigNodeCollection();
        var node = new ConfigNode("bar");
        collection.Add("foo", node);
        Assert.IsTrue(collection.TryGetValue("foo", out var result));
        Assert.AreEqual(node, result);
        Assert.IsFalse(collection.TryGetValue("baz", out _));
    }

    [TestMethod]
    public void Indexer_ThrowsForMissingKey()
    {
        var collection = new ConfigNodeCollection();
        Assert.ThrowsException<KeyNotFoundException>(() => { var _ = collection["missing"]; });
    }

    [TestMethod]
    public void AsDictionary_EnumeratesKeyedNodes()
    {
        var collection = new ConfigNodeCollection();
        collection.Add("a", new ConfigNode("1"));
        collection.Add("b", new ConfigNode("2"));
        var dict = collection.AsDictionary();
        Assert.AreEqual(2, dict.Count);
        Assert.AreEqual("1", dict["a"].Value);
        Assert.AreEqual("2", dict["b"].Value);
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, dict.Keys.ToArray());
    }

    [TestMethod]
    public void AsCollection_EnumeratesArrayNodes()
    {
        var collection = new ConfigNodeCollection();
        collection.Add(new ConfigNode("x"));
        collection.Add(new ConfigNode("y"));
        var arr = collection.AsCollection();
        Assert.AreEqual(2, arr.Count);
        CollectionAssert.AreEqual(new[] { "x", "y" }, arr.Select(n => n.Value).ToArray());
    }

    [TestMethod]
    public void Equality_WorksForSameContent()
    {
        var c1 = new ConfigNodeCollection();
        var c2 = new ConfigNodeCollection();
        c1.Add("a", new ConfigNode("1"));
        c2.Add("a", new ConfigNode("1"));
        Assert.AreEqual(c1, c2);
        Assert.IsTrue(c1 == c2);
        Assert.IsFalse(c1 != c2);
    }

    [TestMethod]
    public void ToString_ReturnsExpectedFormat()
    {
        var c1 = new ConfigNodeCollection();
        c1.Add(new ConfigNode("x"));
        c1.Add(new ConfigNode("y"));
        Assert.AreEqual("[x, y]", c1.ToString());

        var c2 = new ConfigNodeCollection();
        c2.Add("a", new ConfigNode("1"));
        c2.Add("b", new ConfigNode("2"));
        Assert.AreEqual("{a: 1, b: 2}", c2.ToString());
    }

    [TestMethod]
    public void MixedCollection_IsMixed()
    {
        var c = new ConfigNodeCollection();
        c.Add("a", new ConfigNode("1"));
        c.Add(new ConfigNode("2"));
        Assert.IsTrue(c.IsMixed);
        Assert.IsFalse(c.IsArray);
        Assert.IsFalse(c.IsMap);
    }
}
