using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;

namespace Lexxys.Blob.Tests
{
	public class AzureBlobStorageServiceTests: IDisposable
	{
		private Mock<BlobServiceClient> _blobServiceClientMock;
		private Mock<BlobContainerClient> _blobContainerClientMock;
		private Mock<BlobClient> _blobClientMock;
		private AzureBlobStorageService _service;
		private Uri _validUri;
		private Uri _invalidUri;

		public AzureBlobStorageServiceTests()
		{
			_blobServiceClientMock = new Mock<BlobServiceClient>();
			_blobContainerClientMock = new Mock<BlobContainerClient>();
			_blobClientMock = new Mock<BlobClient>();

			_blobServiceClientMock
				.Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
				.Returns(_blobContainerClientMock.Object);

			_blobContainerClientMock
				.Setup(x => x.GetBlobClient(It.IsAny<string>()))
				.Returns(_blobClientMock.Object);

			_service = new AzureBlobStorageService("UseDevelopmentStorage=true", "testcontainer");

			_validUri = new Uri("https://container/blob.txt");
			_invalidUri = new Uri("ftp://container/blob.txt");
		}

		public void Dispose()
		{
			_service.Dispose();
		}

		[Test]
		public async Task CanOpen_ValidScheme_ReturnsTrue()
		{
			await Assert.That(_service.CanOpen(_validUri)).IsTrue();
		}

		[Test]
		public async Task CanOpen_InvalidScheme_ReturnsFalse()
		{
			await Assert.That(_service.CanOpen(_invalidUri)).IsFalse();
		}

		[Test]
		public async Task GetFileInfo_ReturnsBlobInfo()
		{
			var props = BlobsModelFactory.BlobProperties(contentLength: 123, lastModified: DateTimeOffset.UtcNow);
			var responseMock = new Mock<Response<BlobProperties>>();
			responseMock.Setup(x => x.Value).Returns(props);
			_blobClientMock.Setup(x => x.GetProperties(default, default)).Returns(responseMock.Object);

			var info = _service.GetFileInfo(_validUri);

			await Assert.That(info).IsNotNull();
			await Assert.That(info.Exists).IsTrue();
			await Assert.That(info.Length).IsEqualTo(123);
		}

		[Test]
		public async Task GetFileInfoAsync_ReturnsBlobInfo()
		{
			var props = BlobsModelFactory.BlobProperties(contentLength: 456, lastModified: DateTimeOffset.UtcNow);
			var responseMock = new Mock<Response<BlobProperties>>();
			responseMock.Setup(x => x.Value).Returns(props);
			_blobClientMock.Setup(x => x.GetPropertiesAsync(default, default))
				.ReturnsAsync(responseMock.Object);

			var info = await _service.GetFileInfoAsync(_validUri);

			await Assert.That(info).IsNotNull();
			await Assert.That(info.Exists).IsTrue();
			await Assert.That(info.Length).IsEqualTo(456);
		}

		[Test]
		public void WriteFile_CallsUpload()
		{
			using var ms = new MemoryStream();
			_service.WriteFile(_validUri, ms, true);
			_blobClientMock.Verify(x => x.Upload(ms, true, default), Times.Once);
		}

		[Test]
		public async Task WriteFileAsync_CallsUploadAsync()
		{
			using var ms = new MemoryStream();
			_blobClientMock.Setup(x => x.UploadAsync(ms, true, It.IsAny<CancellationToken>()))
				.ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());

			await _service.WriteFileAsync(_validUri, ms, true);

			_blobClientMock.Verify(x => x.UploadAsync(ms, true, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Test]
		public void CopyFile_CallsSyncCopyFromUri()
		{
			_blobClientMock.Setup(x => x.Uri).Returns(_validUri);
			_service.CopyFile(_validUri, _validUri);
			_blobClientMock.Verify(x => x.SyncCopyFromUri(_validUri, default, default), Times.Once);
		}

		[Test]
		public async Task CopyFileAsync_CallsSyncCopyFromUriAsync()
		{
			_blobClientMock.Setup(x => x.Uri).Returns(_validUri);
			_blobClientMock.Setup(x => x.SyncCopyFromUriAsync(_validUri, default, It.IsAny<CancellationToken>()))
				.ReturnsAsync(Mock.Of<Azure.Response<BlobCopyInfo>>());

			await _service.CopyFileAsync(_validUri, _validUri);

			_blobClientMock.Verify(x => x.SyncCopyFromUriAsync(_validUri, default, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Test]
		public void MoveFile_CallsCopyAndDelete()
		{
			_blobClientMock.Setup(x => x.Uri).Returns(_validUri);
			_service.MoveFile(_validUri, _validUri);
			_blobClientMock.Verify(x => x.SyncCopyFromUri(_validUri, default, default), Times.Once);
			_blobClientMock.Verify(x => x.DeleteIfExists(default, default, default), Times.Once);
		}

		[Test]
		public async Task MoveFileAsync_CallsCopyAndDeleteAsync()
		{
			_blobClientMock.Setup(x => x.Uri).Returns(_validUri);
			_blobClientMock.Setup(x => x.SyncCopyFromUriAsync(_validUri, default, It.IsAny<CancellationToken>()))
				.ReturnsAsync(Mock.Of<Azure.Response<BlobCopyInfo>>());
			_blobClientMock.Setup(x => x.DeleteIfExistsAsync(default, default, default))
				.ReturnsAsync(Mock.Of<Azure.Response<bool>>());

			await _service.MoveFileAsync(_validUri, _validUri);

			_blobClientMock.Verify(x => x.SyncCopyFromUriAsync(_validUri, default, default), Times.Once);
			_blobClientMock.Verify(x => x.DeleteIfExistsAsync(default, default, default), Times.Once);
		}

		[Test]
		public void DeleteFile_CallsDeleteIfExists()
		{
			_service.DeleteFile(_validUri);
			_blobClientMock.Verify(x => x.DeleteIfExists(default, default, default), Times.Once);
		}

		[Test]
		public async Task DeleteFileAsync_CallsDeleteIfExistsAsync()
		{
			_blobClientMock.Setup(x => x.DeleteIfExistsAsync(default, default, default))
				.ReturnsAsync(Mock.Of<Azure.Response<bool>>());

			await _service.DeleteFileAsync(_validUri);

			_blobClientMock.Verify(x => x.DeleteIfExistsAsync(default, default, default), Times.Once);
		}

		// Helper subclass to inject mock BlobServiceClient

		private class TestAzureBlobStorageService: AzureBlobStorageService
		{
			public TestAzureBlobStorageService(BlobServiceClient client):
				base("UseDevelopmentStorage=true", "testcontainer")
			{
				var fieldInfo = typeof(AzureBlobStorageService)
					.GetField("_blobServiceClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					?? throw new InvalidOperationException("Field '_blobServiceClient' not found in AzureBlobStorageService.");
				fieldInfo.SetValue(this, client);
			}
		}
	}
}