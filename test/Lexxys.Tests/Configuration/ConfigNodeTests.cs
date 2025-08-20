using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexxys.Configuration.New;

namespace Lexxys.Tests.Configuration;

[TestClass]
public class ConfigNodeTests
{
    [TestMethod]
    public void Constructor_ValueOnly_ShouldSetValueAndEmptyCollection()
    {
        var node = new ConfigNode("test");
        Assert.AreEqual("test", node.Value);
        Assert.IsTrue(node.Collection.IsEmpty());
    }

    [TestMethod]
    public void Constructor_CollectionOnly_ShouldSetCollection()
    {
        var collection = new ConfigNodeCollection();
        var node = new ConfigNode(collection);
        Assert.AreEqual(collection, node.Collection);
        Assert.IsNull(node.Value);
    }

    [TestMethod]
    public void IsEmpty_ShouldBeTrueForEmptyNode()
    {
        var node = new ConfigNode(null);
        Assert.IsTrue(node.IsEmpty);
    }

    [TestMethod]
    public void IsEmpty_ShouldBeFalseForValue()
    {
        var node = new ConfigNode("value");
        Assert.IsFalse(node.IsEmpty);
    }

    [TestMethod]
    public void IsEmpty_ShouldBeFalseForNonEmptyCollection()
    {
        var collection = new ConfigNodeCollection();
        collection.Add(new ConfigNode("item"));
        var node = new ConfigNode(collection);
        Assert.IsFalse(node.IsEmpty);
    }

    [TestMethod]
    public void Equals_ShouldCompareValueAndCollection()
    {
        var node1 = new ConfigNode("x");
        var node2 = new ConfigNode("x");
        Assert.IsTrue(node1.Equals(node2));
        var node3 = new ConfigNode("y");
        Assert.IsFalse(node1.Equals(node3));
        var coll1 = new ConfigNodeCollection();
        coll1.Add(new ConfigNode("a"));
        var node4 = new ConfigNode(coll1);
        var node5 = new ConfigNode(coll1);
        Assert.IsTrue(node4.Equals(node5));
    }

    [TestMethod]
    public void GetHashCode_ShouldBeConsistent()
    {
        var node1 = new ConfigNode("hash");
        var node2 = new ConfigNode("hash");
        Assert.AreEqual(node1.GetHashCode(), node2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldReturnValueOrCollectionString()
    {
        var node = new ConfigNode("val");
        Assert.AreEqual("val", node.ToString());
        var coll = new ConfigNodeCollection();
        coll.Add(new ConfigNode("item"));
        var node2 = new ConfigNode(coll);
        Assert.IsTrue(node2.ToString().Contains("[item]") || node2.ToString().Contains("item"));
    }

    [TestMethod]
    public void OperatorEquals_ShouldWork()
    {
        var node1 = new ConfigNode("eq");
        var node2 = new ConfigNode("eq");
        Assert.IsTrue(node1 == node2);
        Assert.IsFalse(node1 != node2);
    }
}
