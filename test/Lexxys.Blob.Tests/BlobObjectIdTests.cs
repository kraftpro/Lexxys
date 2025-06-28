namespace Lexxys.Blob.Tests;

public class BlobObjectIdTests
{
	#region Constructor Tests

	[Test]
	public async Task Constructor_WithGuid_SetsTypeCorrectly()
	{
		// Arrange
		var guid = Guid.NewGuid();

		// Act
		var id = new BlobObjectId(guid);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Guid);
		await Assert.That(id.GuidId).IsEqualTo(guid);
	}

	[Test]
	public async Task Constructor_WithString_SetsTypeCorrectly()
	{
		// Arrange
		var str = "test-string-id";

		// Act
		var id = new BlobObjectId(str);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.String);
		await Assert.That(id.StringId).IsEqualTo(str);
	}

	[Test]
	public async Task Constructor_WithNullString_UsesEmptyString()
	{
		// Act
		var id = new BlobObjectId(null);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.String);
		await Assert.That(id.StringId).IsEqualTo(string.Empty);
	}

	[Test]
	public async Task Constructor_WithLong_SetsTypeCorrectly()
	{
		// Arrange
		long value = 12345;

		// Act
		var id = new BlobObjectId(value);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Int);
		await Assert.That(id.LongId).IsEqualTo(value);
	}

	#endregion

	#region Property Tests

	[Test]
	public async Task GuidId_WhenTypeIsGuid_ReturnsGuid()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var id = new BlobObjectId(guid);

		// Act & Assert
		await Assert.That(id.GuidId).IsEqualTo(guid);
	}

	[Test]
	public void GuidId_WhenTypeIsNotGuid_ThrowsInvalidCastException()
	{
		// Arrange  
		var id = new BlobObjectId("test");

		// Act & Assert  
		Assert.Throws<InvalidCastException>(() => { _ = id.GuidId; });
	}

	[Test]
	public async Task StringId_WhenTypeIsString_ReturnsString()
	{
		// Arrange
		var str = "test-string";
		var id = new BlobObjectId(str);

		// Act & Assert
		await Assert.That(id.StringId).IsEqualTo(str);
	}

	[Test]
	public void StringId_WhenTypeIsNotString_ThrowsInvalidCastException()
	{
		// Arrange  
		var id = new BlobObjectId(123L);

		// Act & Assert  
		Assert.Throws<InvalidCastException>(() => { _ = id.StringId; });
	}

	[Test]
	public async Task LongId_WhenTypeIsLong_ReturnsLong()
	{
		// Arrange
		long value = 12345;
		var id = new BlobObjectId(value);

		// Act & Assert
		await Assert.That(id.LongId).IsEqualTo(value);
	}

	[Test]
	public void LongId_WhenTypeIsNotLong_ThrowsInvalidCastException()
	{
		// Arrange  
		var id = new BlobObjectId(Guid.NewGuid());

		// Act & Assert  
		Assert.Throws<InvalidCastException>(() => { _ = id.LongId; });
	}

	#endregion

	#region ToString Tests

	[Test]
	public async Task ToString_WithGuidType_ReturnsGuidString()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var id = new BlobObjectId(guid);

		// Act
		var result = id.ToString();

		// Assert
		await Assert.That(result).IsEqualTo(guid.ToString("n"));
	}

	[Test]
	public async Task ToString_WithStringType_ReturnsString()
	{
		// Arrange
		var str = "test-string-id";
		var id = new BlobObjectId(str);

		// Act
		var result = id.ToString();

		// Assert
		await Assert.That(result).IsEqualTo(str);
	}

	[Test]
	public async Task ToString_WithLongType_ReturnsLongString()
	{
		// Arrange
		long value = 12345;
		var id = new BlobObjectId(value);

		// Act
		var result = id.ToString();

		// Assert
		await Assert.That(result).IsEqualTo(value.ToString());
	}

	#endregion

	#region Equality Tests

	[Test]
	public async Task Equals_SameGuidValues_ReturnsTrue()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var id1 = new BlobObjectId(guid);
		var id2 = new BlobObjectId(guid);

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsTrue();
		await Assert.That(id1 == id2).IsTrue();
		await Assert.That(id1 != id2).IsFalse();
	}

	[Test]
	public async Task Equals_DifferentGuidValues_ReturnsFalse()
	{
		// Arrange
		var id1 = new BlobObjectId(Guid.NewGuid());
		var id2 = new BlobObjectId(Guid.NewGuid());

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsFalse();
		await Assert.That(id1 == id2).IsFalse();
		await Assert.That(id1 != id2).IsTrue();
	}

	[Test]
	public async Task Equals_SameStringValues_ReturnsTrue()
	{
		// Arrange
		var id1 = new BlobObjectId("test");
		var id2 = new BlobObjectId("test");

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsTrue();
		await Assert.That(id1 == id2).IsTrue();
		await Assert.That(id1 != id2).IsFalse();
	}

	[Test]
	public async Task Equals_DifferentStringValues_ReturnsFalse()
	{
		// Arrange
		var id1 = new BlobObjectId("test1");
		var id2 = new BlobObjectId("test2");

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsFalse();
		await Assert.That(id1 == id2).IsFalse();
		await Assert.That(id1 != id2).IsTrue();
	}

	[Test]
	public async Task Equals_SameLongValues_ReturnsTrue()
	{
		// Arrange
		var id1 = new BlobObjectId(12345L);
		var id2 = new BlobObjectId(12345L);

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsTrue();
		await Assert.That(id1 == id2).IsTrue();
		await Assert.That(id1 != id2).IsFalse();
	}

	[Test]
	public async Task Equals_DifferentLongValues_ReturnsFalse()
	{
		// Arrange
		var id1 = new BlobObjectId(12345L);
		var id2 = new BlobObjectId(54321L);

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsFalse();
		await Assert.That(id1 == id2).IsFalse();
		await Assert.That(id1 != id2).IsTrue();
	}

	[Test]
	public async Task Equals_DifferentTypes_ReturnsFalse()
	{
		// Arrange
		var id1 = new BlobObjectId(12345L);
		var id2 = new BlobObjectId("12345");

		// Act & Assert
		await Assert.That(id1.Equals(id2)).IsFalse();
		await Assert.That(id1 == id2).IsFalse();
		await Assert.That(id1 != id2).IsTrue();
	}

	[Test]
	public async Task Equals_WithNonBlobObjectId_ReturnsFalse()
	{
		// Arrange
		var id = new BlobObjectId("test");

		// Act & Assert
		await Assert.That(id.Equals("test")).IsFalse();
		await Assert.That(id.Equals(null)).IsFalse();
	}

	[Test]
	public async Task GetHashCode_SameValues_ReturnsSameHashCode()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var guidId1 = new BlobObjectId(guid);
		var guidId2 = new BlobObjectId(guid);

		var strId1 = new BlobObjectId("test");
		var strId2 = new BlobObjectId("test");

		var longId1 = new BlobObjectId(12345L);
		var longId2 = new BlobObjectId(12345L);

		// Act & Assert
		await Assert.That(guidId2.GetHashCode()).IsEqualTo(guidId1.GetHashCode());
		await Assert.That(strId2.GetHashCode()).IsEqualTo(strId1.GetHashCode());
		await Assert.That(longId2.GetHashCode()).IsEqualTo(longId1.GetHashCode());
	}

	#endregion

	#region Conversion Operator Tests

	[Test]
	public async Task ImplicitOperator_FromGuid_CreatesBlobObjectId()
	{
		// Arrange
		var guid = Guid.NewGuid();

		// Act
		BlobObjectId id = guid;

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Guid);
		await Assert.That(id.GuidId).IsEqualTo(guid);
	}

	[Test]
	public async Task ImplicitOperator_FromString_CreatesBlobObjectId()
	{
		// Arrange
		string str = "test-string";

		// Act
		BlobObjectId id = str;

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.String);
		await Assert.That(id.StringId).IsEqualTo(str);
	}

	[Test]
	public async Task ImplicitOperator_FromLong_CreatesBlobObjectId()
	{
		// Arrange
		long value = 12345;

		// Act
		BlobObjectId id = value;

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Int);
		await Assert.That(id.LongId).IsEqualTo(value);
	}

	[Test]
	public async Task ExplicitOperator_ToGuid_WhenTypeIsGuid_ReturnsGuid()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var id = new BlobObjectId(guid);

		// Act
		var result = (Guid)id;

		// Assert
		await Assert.That(result).IsEqualTo(guid);
	}

	[Test]
	public void ExplicitOperator_ToGuid_WhenTypeIsNotGuid_ThrowsException()
	{
		// Arrange
		var id = new BlobObjectId("test");

		// Act & Assert
		Assert.Throws<InvalidCastException>(() => { _ = (Guid)id; });
	}

	[Test]
	public async Task ExplicitOperator_ToString_WhenTypeIsString_ReturnsString()
	{
		// Arrange
		var str = "test-string";
		var id = new BlobObjectId(str);

		// Act
		var result = (string)id;

		// Assert
		await Assert.That(result).IsEqualTo(str);
	}

	[Test]
	public void ExplicitOperator_ToString_WhenTypeIsNotString_ThrowsException()
	{
		// Arrange
		var id = new BlobObjectId(12345L);

		// Act & Assert
		Assert.Throws<InvalidCastException>(() => { _ = (string)id; });
	}

	[Test]
	public async Task ExplicitOperator_ToLong_WhenTypeIsLong_ReturnsLong()
	{
		// Arrange
		long value = 12345;
		var id = new BlobObjectId(value);

		// Act
		var result = (long)id;

		// Assert
		await Assert.That(result).IsEqualTo(value);
	}

	[Test]
	public void ExplicitOperator_ToLong_WhenTypeIsNotLong_ThrowsException()
	{
		// Arrange
		var id = new BlobObjectId(Guid.NewGuid());

		// Act & Assert
		Assert.Throws<InvalidCastException>(() => { _ = (long)id; });
	}

	#endregion

	#region Parse and FromObject Tests

	[Test]
	public async Task Parse_WithGuidString_ReturnsGuidBlobObjectId()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var guidString = guid.ToString();

		// Act
		var id = BlobObjectId.Parse(guidString);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Guid);
		await Assert.That(id.GuidId).IsEqualTo(guid);
	}

	[Test]
	public async Task Parse_WithLongString_ReturnsLongBlobObjectId()
	{
		// Arrange
		long value = 12345;
		var longString = value.ToString();

		// Act
		var id = BlobObjectId.Parse(longString);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Int);
		await Assert.That(id.LongId).IsEqualTo(value);
	}

	[Test]
	public async Task Parse_WithNonGuidNonLongString_ReturnsStringBlobObjectId()
	{
		// Arrange
		var str = "test-string-id";

		// Act
		var id = BlobObjectId.Parse(str);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.String);
		await Assert.That(id.StringId).IsEqualTo(str);
	}

	[Test]
	public async Task FromObject_WithGuid_ReturnsGuidBlobObjectId()
	{
		// Arrange
		var guid = Guid.NewGuid();

		// Act
		var id = BlobObjectId.FromObject(guid);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Guid);
		await Assert.That(id.GuidId).IsEqualTo(guid);
	}

	[Test]
	public async Task FromObject_WithLong_ReturnsLongBlobObjectId()
	{
		// Arrange
		long value = 12345;

		// Act
		var id = BlobObjectId.FromObject(value);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.Int);
		await Assert.That(id.LongId).IsEqualTo(value);
	}

	[Test]
	public async Task FromObject_WithString_ReturnsStringBlobObjectId()
	{
		// Arrange
		var str = "test-string-id";

		// Act
		var id = BlobObjectId.FromObject(str);

		// Assert
		await Assert.That(id.Type).IsEqualTo(BlobObjectIdType.String);
		await Assert.That(id.StringId).IsEqualTo(str);
	}

	[Test]
	public void FromObject_WithUnsupportedType_ThrowsArgumentException()
	{
		// Arrange
		var obj = new object();

		// Act & Assert
		Assert.Throws<ArgumentException>(() => BlobObjectId.FromObject(obj));
	}

	#endregion
}