namespace Lexxys.Configuration.Tests;

public class ConfigNodeTests
{
    [Test]
    public async Task Constructor_ValueOnly_ShouldSetValueAndEmptyCollection()
    {
        var node = new ConfigNode("test");
        await Assert.That(node.Value).IsEqualTo("test");
        await Assert.That(node.Collection.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Constructor_CollectionOnly_ShouldSetCollection()
    {
        var collection = new ConfigNodeCollection();
        var node = new ConfigNode(collection);
        await Assert.That(node.Collection).IsEqualTo(collection);
        await Assert.That(node.Value).IsNull();
    }

    [Test]
    public async Task IsEmpty_ShouldBeTrueForEmptyNode()
    {
        var node = new ConfigNode();
        await Assert.That(node.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_ShouldBeFalseForValue()
    {
        var node = new ConfigNode("value");
        await Assert.That(node.IsEmpty).IsFalse();
    }

    [Test]
    public async Task IsEmpty_ShouldBeFalseForNonEmptyCollection()
	{
		var collection = new ConfigNodeCollection
		([
			new ConfigNode("item")
		]);
        var node = new ConfigNode(collection);
        await Assert.That(node.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Equals_ShouldCompareValueAndCollection()
    {
        var node1 = new ConfigNode("x");
        var node2 = new ConfigNode("x");
        await Assert.That(node1.Equals(node2)).IsTrue();
        var node3 = new ConfigNode("y");
        await Assert.That(node1.Equals(node3)).IsFalse();
        var coll1 = new ConfigNodeCollection
		([
            new ConfigNode("a")
        ]);
        var node4 = new ConfigNode(coll1);
        var node5 = new ConfigNode(coll1);
        await Assert.That(node4.Equals(node5)).IsTrue();
    }

    [Test]
    public async Task GetHashCode_ShouldBeConsistent()
    {
        var node1 = new ConfigNode("hash");
        var node2 = new ConfigNode("hash");
        await Assert.That(node2.GetHashCode()).IsEqualTo(node1.GetHashCode());
    }

    [Test]
    public async Task ToString_ShouldReturnValueOrCollectionString()
    {
        var node = new ConfigNode("val");
        await Assert.That(node.ToString()).IsEqualTo("val");
        var coll = new ConfigNodeCollection
		([
            new ConfigNode("item")
        ]);
        var node2 = new ConfigNode(coll);
        await Assert.That(node2.ToString().Contains("[item]") || node2.ToString().Contains("item")).IsTrue();
    }

    [Test]
    public async Task OperatorEquals_ShouldWork()
    {
        var node1 = new ConfigNode("eq");
        var node2 = new ConfigNode("eq");
        await Assert.That(node1 == node2).IsTrue();
        await Assert.That(node1 != node2).IsFalse();
    }
}
