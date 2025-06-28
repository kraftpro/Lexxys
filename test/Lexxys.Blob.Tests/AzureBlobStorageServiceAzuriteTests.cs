using System.Text;

using Azure.Storage.Blobs;


namespace Lexxys.Blob.Tests;

public class AzureBlobStorageServiceAzuriteTests: IDisposable
{
	// Azurite connection string
	private const string ConnectionString = "UseDevelopmentStorage=true";
	private const string TestContainerName = "testcontainer";
	private AzureBlobStorageService _service;
	private BlobServiceClient _blobServiceClient;
	private BlobContainerClient _containerClient;

	public AzureBlobStorageServiceAzuriteTests()
	{
		// Create service with Azurite connection string
		_service = new AzureBlobStorageService(ConnectionString, TestContainerName);
		_blobServiceClient = new BlobServiceClient(ConnectionString);
		try
		{
			_blobServiceClient.CreateBlobContainer(TestContainerName);
		}
		catch { }

		// Create test container
		_containerClient = _blobServiceClient.GetBlobContainerClient(TestContainerName);
		_containerClient.CreateIfNotExists();
	}

	public void Dispose()
	{
		// Clean up any test blobs
		_containerClient.DeleteIfExists();
		_service.Dispose();
	}

	#region Helper Methods

	private Uri CreateBlobUri(string blobName)
	{
		// Azurite uses the special host "127.0.0.1" for local testing
		return new Uri($"http://127.0.0.1:10000/{TestContainerName}/{blobName}");
	}

	private async Task UploadTestBlobAsync(string blobName, string content)
	{
		var blobClient = _containerClient.GetBlobClient(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		await blobClient.UploadAsync(stream, overwrite: true);
	}

	private async Task<string> DownloadBlobContentAsync(string blobName)
	{
		var blobClient = _containerClient.GetBlobClient(blobName);
		using var stream = new MemoryStream();
		await blobClient.DownloadToAsync(stream);
		stream.Position = 0;
		using var reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	private bool BlobExists(string blobName)
	{
		var blobClient = _containerClient.GetBlobClient(blobName);
		return blobClient.Exists();
	}

	#endregion

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
		string blobName = "getfileinfo-test.txt";
		string content = "Test content for GetFileInfo";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);

		// Act
		var info = _service.GetFileInfo(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(content.Length);
		await Assert.That(info.LastModified).IsNotNull();
	}

	#endregion

	#region GetFileInfoAsync Tests

	[Test]
	public async Task GetFileInfoAsync_ReturnsBlobInfo()
	{
		// Arrange
		string blobName = "getfileinfoasync-test.txt";
		string content = "Test content for GetFileInfoAsync";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);

		// Act
		var info = await _service.GetFileInfoAsync(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(content.Length);
		await Assert.That(info.LastModified).IsNotNull();
	}

	[Test]
	public async Task GetFileInfoAsync_WithCancellation_ThrowsOperationCanceledException()
	{
		// Arrange
		string blobName = "getfileinfoasync-cancel-test.txt";
		string content = "Test content for canceled GetFileInfoAsync";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await _service.GetFileInfoAsync(uri, cts.Token));
	}

	#endregion

	#region WriteFile Tests

	[Test]
	public async Task WriteFile_CreatesNewBlob()
	{
		// Arrange
		string blobName = "writefile-test.txt";
		string content = "Test content for WriteFile";
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		_service.WriteFile(uri, stream, false);

		// Assert
		await Assert.That(BlobExists(blobName)).IsTrue();
		string downloadedContent = await DownloadBlobContentAsync(blobName);
		await Assert.That(downloadedContent).IsEqualTo(content);
	}

	[Test]
	public async Task WriteFile_WithOverwriteTrue_OverwritesExistingBlob()
	{
		// Arrange
		string blobName = "writefile-overwrite-test.txt";
		string initialContent = "Initial content";
		string newContent = "New overwritten content";
		await UploadTestBlobAsync(blobName, initialContent);
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));

		// Act
		_service.WriteFile(uri, stream, true);

		// Assert
		await Assert.That(BlobExists(blobName)).IsTrue();
		string downloadedContent = await DownloadBlobContentAsync(blobName);
		await Assert.That(downloadedContent).IsEqualTo(newContent);
	}

	[Test]
	public async Task WriteFile_WithOverwriteFalse_ThrowsIfBlobExists()
	{
		// Arrange
		string blobName = "writefile-no-overwrite-test.txt";
		string initialContent = "Initial content";
		string newContent = "New content that should not be written";
		await UploadTestBlobAsync(blobName, initialContent);
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));

		// Act - should throw
		Assert.Throws<Azure.RequestFailedException>(() =>
			_service.WriteFile(uri, stream, false));
	}

	#endregion

	#region WriteFileAsync Tests

	[Test]
	public async Task WriteFileAsync_CreatesNewBlob()
	{
		// Arrange
		string blobName = "writefileasync-test.txt";
		string content = "Test content for WriteFileAsync";
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		await _service.WriteFileAsync(uri, stream, false);

		// Assert
		await Assert.That(BlobExists(blobName)).IsTrue();
		string downloadedContent = await DownloadBlobContentAsync(blobName);
		await Assert.That(downloadedContent).IsEqualTo(content);
	}

	[Test]
	public async Task WriteFileAsync_WithOverwriteTrue_OverwritesExistingBlob()
	{
		// Arrange
		string blobName = "writefileasync-overwrite-test.txt";
		string initialContent = "Initial content";
		string newContent = "New overwritten content async";
		await UploadTestBlobAsync(blobName, initialContent);
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));

		// Act
		await _service.WriteFileAsync(uri, stream, true);

		// Assert
		await Assert.That(BlobExists(blobName)).IsTrue();
		string downloadedContent = await DownloadBlobContentAsync(blobName);
		await Assert.That(downloadedContent).IsEqualTo(newContent);
	}

	[Test]
	public async Task WriteFileAsync_WithCancellation_ThrowsOperationCanceledException()
	{
		// Arrange
		string blobName = "writefileasync-cancel-test.txt";
		string content = "Test content that should not be written due to cancellation";
		var uri = CreateBlobUri(blobName);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await _service.WriteFileAsync(uri, stream, false, cts.Token));
	}

	#endregion

	#region CopyFile Tests

	[Test]
	public async Task CopyFile_CopiesBlobCorrectly()
	{
		// Arrange
		string sourceBlobName = "copyfile-source-test.txt";
		string destBlobName = "copyfile-dest-test.txt";
		string content = "Test content for CopyFile";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		_service.CopyFile(sourceUri, destUri);

		// Assert
		await Assert.That(BlobExists(sourceBlobName)).IsTrue(); // Source should still exist
		await Assert.That(BlobExists(destBlobName)).IsTrue(); // Destination should now exist
		string sourceContent = await DownloadBlobContentAsync(sourceBlobName);
		string destContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(destContent).IsEqualTo(sourceContent);
	}

	[Test]
	public async Task CopyFile_OverwritesExistingDestination()
	{
		// Arrange
		string sourceBlobName = "copyfile-source-overwrite-test.txt";
		string destBlobName = "copyfile-dest-overwrite-test.txt";
		string sourceContent = "Source content for CopyFile overwrite test";
		string destContent = "Initial destination content that should be overwritten";
		await UploadTestBlobAsync(sourceBlobName, sourceContent);
		await UploadTestBlobAsync(destBlobName, destContent);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		_service.CopyFile(sourceUri, destUri);

		// Assert
		string newDestContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(newDestContent).IsEqualTo(sourceContent);
	}

	#endregion

	#region CopyFileAsync Tests

	[Test]
	public async Task CopyFileAsync_CopiesBlobCorrectly()
	{
		// Arrange
		string sourceBlobName = "copyfileasync-source-test.txt";
		string destBlobName = "copyfileasync-dest-test.txt";
		string content = "Test content for CopyFileAsync";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		await _service.CopyFileAsync(sourceUri, destUri);

		// Assert
		await Assert.That(BlobExists(sourceBlobName)).IsTrue(); // Source should still exist
		await Assert.That(BlobExists(destBlobName)).IsTrue(); // Destination should now exist
		string sourceContent = await DownloadBlobContentAsync(sourceBlobName);
		string destContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(destContent).IsEqualTo(sourceContent);
	}

	[Test]
	public async Task CopyFileAsync_WithCancellation_ThrowsOperationCanceledException()
	{
		// Arrange
		string sourceBlobName = "copyfileasync-cancel-source-test.txt";
		string destBlobName = "copyfileasync-cancel-dest-test.txt";
		string content = "Test content for canceled CopyFileAsync";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);
		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await _service.CopyFileAsync(sourceUri, destUri, cts.Token));
	}

	#endregion

	#region MoveFile Tests

	[Test]
	public async Task MoveFile_MovesBlobCorrectly()
	{
		// Arrange
		string sourceBlobName = "movefile-source-test.txt";
		string destBlobName = "movefile-dest-test.txt";
		string content = "Test content for MoveFile";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		_service.MoveFile(sourceUri, destUri);

		// Assert
		await Assert.That(BlobExists(sourceBlobName)).IsFalse(); // Source should no longer exist
		await Assert.That(BlobExists(destBlobName)).IsTrue(); // Destination should now exist
		string destContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(destContent).IsEqualTo(content);
	}

	[Test]
	public async Task MoveFile_OverwritesExistingDestination()
	{
		// Arrange
		string sourceBlobName = "movefile-source-overwrite-test.txt";
		string destBlobName = "movefile-dest-overwrite-test.txt";
		string sourceContent = "Source content for MoveFile overwrite test";
		string destContent = "Initial destination content that should be overwritten";
		await UploadTestBlobAsync(sourceBlobName, sourceContent);
		await UploadTestBlobAsync(destBlobName, destContent);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		_service.MoveFile(sourceUri, destUri);

		// Assert
		await Assert.That(BlobExists(sourceBlobName)).IsFalse(); // Source should no longer exist
		string newDestContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(newDestContent).IsEqualTo(sourceContent);
	}

	#endregion

	#region MoveFileAsync Tests

	[Test]
	public async Task MoveFileAsync_MovesBlobCorrectly()
	{
		// Arrange
		string sourceBlobName = "movefileasync-source-test.txt";
		string destBlobName = "movefileasync-dest-test.txt";
		string content = "Test content for MoveFileAsync";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);

		// Act
		await _service.MoveFileAsync(sourceUri, destUri);

		// Assert
		await Assert.That(BlobExists(sourceBlobName)).IsFalse(); // Source should no longer exist
		await Assert.That(BlobExists(destBlobName)).IsTrue(); // Destination should now exist
		string destContent = await DownloadBlobContentAsync(destBlobName);
		await Assert.That(destContent).IsEqualTo(content);
	}

	[Test]
	public async Task MoveFileAsync_WithCancellation_ThrowsOperationCanceledException()
	{
		// Arrange
		string sourceBlobName = "movefileasync-cancel-source-test.txt";
		string destBlobName = "movefileasync-cancel-dest-test.txt";
		string content = "Test content for canceled MoveFileAsync";
		await UploadTestBlobAsync(sourceBlobName, content);
		var sourceUri = CreateBlobUri(sourceBlobName);
		var destUri = CreateBlobUri(destBlobName);
		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await _service.MoveFileAsync(sourceUri, destUri, cts.Token));
	}

	#endregion

	#region DeleteFile Tests

	[Test]
	public async Task DeleteFile_DeletesBlob()
	{
		// Arrange
		string blobName = "deletefile-test.txt";
		string content = "Test content for DeleteFile";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		await Assert.That(BlobExists(blobName)).IsTrue(); // Verify blob exists before test

		// Act
		_service.DeleteFile(uri);

		// Assert
		await Assert.That(BlobExists(blobName)).IsFalse();
	}

	[Test]
	public async Task DeleteFile_DoesNotThrowForNonExistentBlob()
	{
		// Arrange
		string blobName = "deletefile-nonexistent-test.txt";
		var uri = CreateBlobUri(blobName);
		await Assert.That(BlobExists(blobName)).IsFalse(); // Verify blob doesn't exist before test

		// Act & Assert - should not throw
		_service.DeleteFile(uri);
	}

	#endregion

	#region DeleteFileAsync Tests

	[Test]
	public async Task DeleteFileAsync_DeletesBlob()
	{
		// Arrange
		string blobName = "deletefileasync-test.txt";
		string content = "Test content for DeleteFileAsync";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		await Assert.That(BlobExists(blobName)).IsTrue(); // Verify blob exists before test

		// Act
		await _service.DeleteFileAsync(uri);

		// Assert
		await Assert.That(BlobExists(blobName)).IsFalse();
	}

	[Test]
	public async Task DeleteFileAsync_WithCancellation_ThrowsOperationCanceledException()
	{
		// Arrange
		string blobName = "deletefileasync-cancel-test.txt";
		string content = "Test content for canceled DeleteFileAsync";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () =>
			await _service.DeleteFileAsync(uri, cts.Token));
	}

	#endregion

	#region AzureBlobInfo Tests

	[Test]
	public async Task AzureBlobInfo_OpenReadStream_ReturnsCorrectContent()
	{
		// Arrange
		string blobName = "openreadstream-test.txt";
		string content = "Test content for OpenReadStream";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		var info = _service.GetFileInfo(uri);

		// Act
		using var stream = info.OpenReadStream();
		using var reader = new StreamReader(stream);
		string readContent = reader.ReadToEnd();

		// Assert
		await Assert.That(readContent).IsEqualTo(content);
	}

	[Test]
	public async Task AzureBlobInfo_OpenReadStreamAsync_ReturnsCorrectContent()
	{
		// Arrange
		string blobName = "openreadstreamasync-test.txt";
		string content = "Test content for OpenReadStreamAsync";
		await UploadTestBlobAsync(blobName, content);
		var uri = CreateBlobUri(blobName);
		var info = await _service.GetFileInfoAsync(uri);

		// Act
		using var stream = await info.OpenReadStreamAsync();
		using var reader = new StreamReader(stream);
		string readContent = await reader.ReadToEndAsync();

		// Assert
		await Assert.That(readContent).IsEqualTo(content);
	}

	#endregion
}