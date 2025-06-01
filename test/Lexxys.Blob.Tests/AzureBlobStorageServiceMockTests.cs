using System.Text;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Moq;

namespace Lexxys.Blob.Tests;

public class AzureBlobStorageServiceMockTests: IDisposable
{
	private readonly string _connectionString = "UseDevelopmentStorage=true";
	private Mock<BlobServiceClient> _mockBlobServiceClient;
	private Mock<BlobContainerClient> _mockBlobContainerClient;
	private Mock<BlobClient> _mockBlobClient;
	private AzureBlobStorageService _service;

	public AzureBlobStorageServiceMockTests()
	{
		// Set up mocks
		_mockBlobClient = new Mock<BlobClient>();
		_mockBlobContainerClient = new Mock<BlobContainerClient>();
		_mockBlobServiceClient = new Mock<BlobServiceClient>();

		// Create service with mock setup
		_service = new AzureBlobStorageServiceWrapper(_connectionString,
			_mockBlobServiceClient.Object,
			_mockBlobContainerClient.Object,
			_mockBlobClient.Object);
	}

	public void Dispose()
	{
		_service.Dispose();
	}

	#region SupportedSchemes Tests

	[Test]
	public async Task SupportedSchemes_ReturnsHttpAndHttps()
	{
		// Act
		var schemes = _service.SupportedSchemes;

		// Assert
		await Assert.That(schemes).Contains("http");
		await Assert.That(schemes).Contains("https");
		await Assert.That(schemes.Count).IsEqualTo(2);
	}

	#endregion

	#region CanOpen Tests

	[Test]
	public async Task CanOpen_WithHttpUrl_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("http://test.blob.core.windows.net/container/blob.txt");

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task CanOpen_WithHttpsUrl_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task CanOpen_WithFileUrl_ReturnsFalse()
	{
		// Arrange
		var uri = new Uri("file:///C:/test.txt");

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsFalse();
	}

	#endregion

	#region GetFileInfo Tests

	[Test]
	public async Task GetFileInfo_ReturnsBlobInfo()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var properties = BlobsModelFactory.BlobProperties(
			lastModified: DateTimeOffset.UtcNow,
			contentLength: 100);

		_mockBlobClient.Setup(x => x.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(properties, Mock.Of<Response>()));

		// Act
		var info = _service.GetFileInfo(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(100);
		await Assert.That(info.Path).IsEqualTo(uri.ToString());
		await Assert.That(info.LastModified).IsNotNull();
	}

	#endregion

	#region GetFileInfoAsync Tests

	[Test]
	public async Task GetFileInfoAsync_ReturnsBlobInfo()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var properties = BlobsModelFactory.BlobProperties(
			lastModified: DateTimeOffset.UtcNow,
			contentLength: 100);

		_mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(properties, Mock.Of<Response>()));

		// Act
		var info = await _service.GetFileInfoAsync(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(100);
		await Assert.That(info.Path).IsEqualTo(uri.ToString());
		await Assert.That(info.LastModified).IsNotNull();
	}

	#endregion

	#region WriteFile Tests

	[Test]
	public void WriteFile_UploadsBlob()
	{
		// Arrange  
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var content = "Test content";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var uploadResponse = BlobsModelFactory.BlobContentInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null, null, null, 0);

		_mockBlobClient.Setup(x => x.Upload(
			It.IsAny<Stream>(),
			It.IsAny<bool>(),
			It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(uploadResponse, Mock.Of<Response>()));

		// Act - Should not throw  
		_service.WriteFile(uri, stream, true);

		// Assert  
		_mockBlobClient.Verify(x => x.Upload(
			It.IsAny<Stream>(),
			true,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region WriteFileAsync Tests

	[Test]
	public async Task WriteFileAsync_UploadsBlob()
	{
		// Arrange  
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var content = "Test content";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var uploadResponse = BlobsModelFactory.BlobContentInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null, null, null, 0);

		_mockBlobClient.Setup(x => x.UploadAsync(
			It.IsAny<Stream>(),
			It.IsAny<bool>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(uploadResponse, Mock.Of<Response>()));

		// Act - Should not throw  
		await _service.WriteFileAsync(uri, stream, true);

		// Assert  
		_mockBlobClient.Verify(x => x.UploadAsync(
			It.IsAny<Stream>(),
			true,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region CopyFile Tests

	[Test]
	public void CopyFile_CopiesBlob()
	{
		// Arrange
		var sourceUri = new Uri("https://test.blob.core.windows.net/container/source.txt");
		var destinationUri = new Uri("https://test.blob.core.windows.net/container/destination.txt");
		var copyResponse = BlobsModelFactory.BlobCopyInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null,
			"copyId",
			CopyStatus.Success);

		_mockBlobClient.Setup(x => x.SyncCopyFromUri(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(copyResponse, Mock.Of<Response>()));

		// Act - Should not throw
		_service.CopyFile(sourceUri, destinationUri);

		// Assert
		_mockBlobClient.Verify(x => x.SyncCopyFromUri(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region CopyFileAsync Tests

	[Test]
	public async Task CopyFileAsync_CopiesBlob()
	{
		// Arrange
		var sourceUri = new Uri("https://test.blob.core.windows.net/container/source.txt");
		var destinationUri = new Uri("https://test.blob.core.windows.net/container/destination.txt");
		var copyResponse = BlobsModelFactory.BlobCopyInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null, "copyId", CopyStatus.Success);

		_mockBlobClient.Setup(x => x.SyncCopyFromUriAsync(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(copyResponse, Mock.Of<Response>()));

		// Act - Should not throw
		await _service.CopyFileAsync(sourceUri, destinationUri);

		// Assert
		_mockBlobClient.Verify(x => x.SyncCopyFromUriAsync(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region MoveFile Tests

	[Test]
	public void MoveFile_CopiesAndDeletesBlob()
	{
		// Arrange
		var sourceUri = new Uri("https://test.blob.core.windows.net/container/source.txt");
		var destinationUri = new Uri("https://test.blob.core.windows.net/container/destination.txt");
		var copyResponse = BlobsModelFactory.BlobCopyInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null, "copyId", CopyStatus.Success);
		var deleteResponse = Response.FromValue(true, Mock.Of<Response>());

		_mockBlobClient.Setup(x => x.SyncCopyFromUri(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(copyResponse, Mock.Of<Response>()));

		_mockBlobClient.Setup(x => x.DeleteIfExists(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(true, Mock.Of<Response>()));

		// Act - Should not throw
		_service.MoveFile(sourceUri, destinationUri);

		// Assert
		_mockBlobClient.Verify(x => x.SyncCopyFromUri(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()), Times.Once);

		_mockBlobClient.Verify(x => x.DeleteIfExists(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region MoveFileAsync Tests

	[Test]
	public async Task MoveFileAsync_CopiesAndDeletesBlob()
	{
		// Arrange
		var sourceUri = new Uri("https://test.blob.core.windows.net/container/source.txt");
		var destinationUri = new Uri("https://test.blob.core.windows.net/container/destination.txt");
		var copyResponse = BlobsModelFactory.BlobCopyInfo(
			new ETag("etag"),
			DateTimeOffset.UtcNow,
			null, "copyId", CopyStatus.Success);

		_mockBlobClient.Setup(x => x.SyncCopyFromUriAsync(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(copyResponse, Mock.Of<Response>()));

		_mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

		// Act - Should not throw
		await _service.MoveFileAsync(sourceUri, destinationUri);

		// Assert
		_mockBlobClient.Verify(x => x.SyncCopyFromUriAsync(
			It.IsAny<Uri>(),
			It.IsAny<BlobCopyFromUriOptions>(),
			It.IsAny<CancellationToken>()), Times.Once);

		_mockBlobClient.Verify(x => x.DeleteIfExistsAsync(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region DeleteFile Tests

	[Test]
	public void DeleteFile_DeletesBlob()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");

		_mockBlobClient.Setup(x => x.DeleteIfExists(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(true, Mock.Of<Response>()));

		// Act - Should not throw
		_service.DeleteFile(uri);

		// Assert
		_mockBlobClient.Verify(x => x.DeleteIfExists(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region DeleteFileAsync Tests

	[Test]
	public async Task DeleteFileAsync_DeletesBlob()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");

		_mockBlobClient.Setup(x => x.DeleteIfExistsAsync(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

		// Act - Should not throw
		await _service.DeleteFileAsync(uri);

		// Assert
		_mockBlobClient.Verify(x => x.DeleteIfExistsAsync(
			It.IsAny<DeleteSnapshotsOption>(),
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region AzureBlobInfo Tests

	[Test]
	public async Task AzureBlobInfo_Properties_AreCorrect()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var lastModified = DateTimeOffset.UtcNow;
		var properties = BlobsModelFactory.BlobProperties(
			lastModified: lastModified,
			contentLength: 100);

		_mockBlobClient.Setup(x => x.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(properties, Mock.Of<Response>()));

		// Act
		var info = _service.GetFileInfo(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(100);
		await Assert.That(info.Path).IsEqualTo(uri.ToString());
		await Assert.That(info.LastModified).IsEqualTo(lastModified);
	}

	[Test]
	public async Task AzureBlobInfo_OpenReadStream_ReturnsStream()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var properties = BlobsModelFactory.BlobProperties(
			lastModified: DateTimeOffset.UtcNow,
			contentLength: 100);

		_mockBlobClient.Setup(x => x.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
			.Returns(Response.FromValue(properties, Mock.Of<Response>()));

		var mockStream = new MemoryStream();
		_mockBlobClient.Setup(x => x.OpenRead(
			It.IsAny<long>(), null,
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.Returns(mockStream);

		// Act
		var info = _service.GetFileInfo(uri);
		var stream = info.OpenReadStream();

		// Assert
		await Assert.That(stream).IsNotNull();
	}

	[Test]
	public async Task AzureBlobInfo_OpenReadStreamAsync_ReturnsStream()
	{
		// Arrange
		var uri = new Uri("https://test.blob.core.windows.net/container/blob.txt");
		var properties = BlobsModelFactory.BlobProperties(
			lastModified: DateTimeOffset.UtcNow,
			contentLength: 100);

		_mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Response.FromValue(properties, Mock.Of<Response>()));

		var mockStream = new MemoryStream();
		_mockBlobClient.Setup(x => x.OpenReadAsync(
			It.IsAny<long>(), null,
			It.IsAny<BlobRequestConditions>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(mockStream);

		// Act
		var info = await _service.GetFileInfoAsync(uri);
		var stream = await info.OpenReadStreamAsync();

		// Assert
		await Assert.That(stream).IsNotNull();
	}

	#endregion
}

// Helper class to facilitate testing with mocks
internal class AzureBlobStorageServiceWrapper: AzureBlobStorageService
{
	private readonly BlobServiceClient _blobServiceClient;
	private readonly BlobContainerClient _blobContainerClient;
	private readonly BlobClient _blobClient;

	public AzureBlobStorageServiceWrapper(
		string connectionString,
		BlobServiceClient blobServiceClient,
		BlobContainerClient blobContainerClient,
		BlobClient blobClient)
		: base(connectionString)
	{
		_blobServiceClient = blobServiceClient;
		_blobContainerClient = blobContainerClient;
		_blobClient = blobClient;
	}

	protected override BlobClient GetBlobClient(Uri location)
	{
		return _blobClient;
	}
}