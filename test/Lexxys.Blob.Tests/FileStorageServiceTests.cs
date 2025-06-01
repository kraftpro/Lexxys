using System.Text;

namespace Lexxys.Blob.Tests;

public class FileStorageServiceTests: IDisposable
{
	private string _testDirectory;
	private readonly FileStorageService _service;

	public FileStorageServiceTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"FileStorageTests_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);
		_service = new FileStorageService();
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDirectory))
		{
			try
			{
				Directory.Delete(_testDirectory, true);
			}
			catch
			{
				// Ignore cleanup errors in tests
			}
		}
		_service.Dispose();
	}

	#region SupportedSchemes Tests

	[Test]
	public async Task SupportedSchemes_ReturnsExpectedSchemes()
	{
		// Act
		var schemes = _service.SupportedSchemes;

		// Assert
		await Assert.That(schemes).IsNotNull();
		await Assert.That(schemes.Count).IsGreaterThan(0);
		await Assert.That(schemes).Contains("file");
		await Assert.That(schemes).Contains("");
		await Assert.That(schemes.Count).IsEqualTo(2);
	}

	#endregion

	#region CanOpen Tests

	[Test]
	public async Task CanOpen_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act
		await Assert.ThrowsAsync<ArgumentNullException>(() => 
			Task.FromResult(_service.CanOpen(null!)));
	}

	[Test]
	public async Task CanOpen_WithRelativeUri_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("test.txt", UriKind.Relative);

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task CanOpen_WithFileUri_ReturnsTrue()
	{
		// Arrange
		var uri = new Uri("file:///C:/test.txt");

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task CanOpen_WithHttpUri_ReturnsFalse()
	{
		// Arrange
		var uri = new Uri("http://example.com/test.txt");

		// Act
		var result = _service.CanOpen(uri);

		// Assert
		await Assert.That(result).IsFalse();
	}

	#endregion

	#region GetFileInfo Tests

	[Test]
	public void GetFileInfo_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act
		Assert.Throws<ArgumentNullException>(() => 
			_service.GetFileInfo(null!));
	}

	[Test]
	public async Task GetFileInfo_WithNonExistentFile_ReturnsInfoWithExistsFalse()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "nonexistent.txt");
		var uri = new Uri(filePath);

		// Act
		var info = _service.GetFileInfo(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsFalse();
		await Assert.That(info.Length).IsEqualTo(0);
		await Assert.That(info.Path).IsEqualTo(filePath);
	}

	[Test]
	public async Task GetFileInfo_WithExistingFile_ReturnsCorrectInfo()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "test.txt");
		var content = "Test content";
		File.WriteAllText(filePath, content);
		var uri = new Uri(filePath);

		// Act
		var info = _service.GetFileInfo(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(content.Length);
		await Assert.That(info.Path).IsEqualTo(filePath);
		await Assert.That(info.LastModified).IsNotNull();
	}

	#endregion

	#region GetFileInfoAsync Tests

	[Test]
	public async Task GetFileInfoAsync_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.GetFileInfoAsync(null!));
	}

	[Test]
	public async Task GetFileInfoAsync_WithExistingFile_ReturnsCorrectInfo()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "test-async.txt");
		var content = "Test content for async";
		File.WriteAllText(filePath, content);
		var uri = new Uri(filePath);

		// Act
		var info = await _service.GetFileInfoAsync(uri);

		// Assert
		await Assert.That(info).IsNotNull();
		await Assert.That(info.Exists).IsTrue();
		await Assert.That(info.Length).IsEqualTo(content.Length);
		await Assert.That(info.Path).IsEqualTo(filePath);
		await Assert.That(info.LastModified).IsNotNull();
	}

	#endregion

	#region WriteFile Tests

	[Test]
	public void WriteFile_WithNullLocation_ThrowsArgumentNullException()
	{
		// Arrange
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.WriteFile(null!, stream, false));
	}

	[Test]
	public void WriteFile_WithNullStream_ThrowsArgumentNullException()
	{
		// Arrange
		var uri = new Uri(Path.Combine(_testDirectory, "test.txt"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.WriteFile(uri, null!, false));
	}

	[Test]
	public void WriteFile_WithInvalidLocation_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
		var uri = new Uri("http://example.com/test.txt");

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.WriteFile(uri, stream, false));
	}

	[Test]
	public async Task WriteFile_CreatesNewFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "write-test.txt");
		var uri = new Uri(filePath);
		var content = "Test content for write";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		_service.WriteFile(uri, stream, false);

		// Assert
		await Assert.That(File.Exists(filePath)).IsTrue();
		await Assert.That(new FileInfo(filePath).Length).IsEqualTo(content.Length);
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo(content);
	}

	[Test]
	public async Task WriteFile_WithOverwriteFalse_ThrowsIfFileExists()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "write-exists.txt");
		File.WriteAllText(filePath, "Initial content");
		var uri = new Uri(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("New content"));

		// Act & Assert
		Assert.Throws<IOException>(() =>
			_service.WriteFile(uri, stream, false));
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo("Initial content");
	}

	[Test]
	public async Task WriteFile_WithOverwriteTrue_OverwritesExistingFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "write-overwrite.txt");
		File.WriteAllText(filePath, "Initial content");
		var uri = new Uri(filePath);
		var newContent = "New overwritten content";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));

		// Act
		_service.WriteFile(uri, stream, true);

		// Assert
		await Assert.That(File.Exists(filePath)).IsTrue();
		await Assert.That(new FileInfo(filePath).Length).IsEqualTo(newContent.Length);
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo(newContent);
	}

	[Test]
	public async Task WriteFile_CreatesDirectoryIfNotExists()
	{
		// Arrange
		var dirPath = Path.Combine(_testDirectory, "subdir");
		var filePath = Path.Combine(dirPath, "nested-file.txt");
		var uri = new Uri(filePath);
		var content = "Nested file content";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		_service.WriteFile(uri, stream, false);

		// Assert
		await Assert.That(Directory.Exists(dirPath)).IsTrue();
		await Assert.That(File.Exists(filePath)).IsTrue();
		await Assert.That(new FileInfo(filePath).Length).IsEqualTo(content.Length);
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo(content);
	}

	#endregion

	#region WriteFileAsync Tests

	[Test]
	public async Task WriteFileAsync_WithNullLocation_ThrowsArgumentNullException()
	{
		// Arrange
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.WriteFileAsync(null!, stream, false));
	}

	[Test]
	public async Task WriteFileAsync_WithNullStream_ThrowsArgumentNullException()
	{
		// Arrange
		var uri = new Uri(Path.Combine(_testDirectory, "test.txt"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.WriteFileAsync(uri, null!, false));
	}

	[Test]
	public async Task WriteFileAsync_CreatesNewFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "write-async.txt");
		var uri = new Uri(filePath);
		var content = "Test content for async write";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		await _service.WriteFileAsync(uri, stream, false);

		// Assert
		await Assert.That(File.Exists(filePath)).IsTrue();
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo(content);
	}

	[Test]
	public async Task WriteFileAsync_WithOverwriteTrue_OverwritesExistingFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "write-async-overwrite.txt");
		File.WriteAllText(filePath, "Initial content");
		var uri = new Uri(filePath);
		var newContent = "New overwritten content async";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));

		// Act
		await _service.WriteFileAsync(uri, stream, true);

		// Assert
		await Assert.That(File.Exists(filePath)).IsTrue();
		await Assert.That(File.ReadAllText(filePath)).IsEqualTo(newContent);
	}

	#endregion

	#region CopyFile Tests

	[Test]
	public void CopyFile_WithNullSource_ThrowsArgumentNullException()
	{
		// Arrange
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.CopyFile(null!, destUri));
	}

	[Test]
	public void CopyFile_WithNullDestination_ThrowsArgumentNullException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.CopyFile(sourceUri, null!));
	}

	[Test]
	public void CopyFile_WithInvalidSource_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var sourceUri = new Uri("http://example.com/test.txt");
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.CopyFile(sourceUri, destUri));
	}

	[Test]
	public void CopyFile_WithInvalidDestination_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));
		var destUri = new Uri("http://example.com/test.txt");

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.CopyFile(sourceUri, destUri));
	}

	[Test]
	public async Task CopyFile_CopiesFileCorrectly()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "copy-source.txt");
		var destPath = Path.Combine(_testDirectory, "copy-dest.txt");
		var content = "Test content for copy";
		File.WriteAllText(sourcePath, content);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		_service.CopyFile(sourceUri, destUri);

		// Assert
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(content);
		await Assert.That(File.ReadAllText(sourcePath)).IsEqualTo(content); // Source unchanged
	}

	[Test]
	public async Task CopyFile_OverwritesExistingDestination()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "copy-source-overwrite.txt");
		var destPath = Path.Combine(_testDirectory, "copy-dest-overwrite.txt");
		var sourceContent = "Source content";
		var destContent = "Destination content";
		File.WriteAllText(sourcePath, sourceContent);
		File.WriteAllText(destPath, destContent);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		_service.CopyFile(sourceUri, destUri);

		// Assert
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(sourceContent); // Destination has source content
	}

	#endregion

	#region CopyFileAsync Tests

	[Test]
	public async Task CopyFileAsync_WithNullSource_ThrowsArgumentNullException()
	{
		// Arrange
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.CopyFileAsync(null!, destUri));
	}

	[Test]
	public async Task CopyFileAsync_WithNullDestination_ThrowsArgumentNullException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.CopyFileAsync(sourceUri, null!));
	}

	[Test]
	public async Task CopyFileAsync_CopiesFileCorrectly()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "copy-source-async.txt");
		var destPath = Path.Combine(_testDirectory, "copy-dest-async.txt");
		var content = "Test content for async copy";
		File.WriteAllText(sourcePath, content);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		await _service.CopyFileAsync(sourceUri, destUri);

		// Assert
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(content);
		await Assert.That(File.ReadAllText(sourcePath)).IsEqualTo(content); // Source unchanged
	}

	#endregion

	#region MoveFile Tests

	[Test]
	public void MoveFile_WithNullSource_ThrowsArgumentNullException()
	{
		// Arrange
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.MoveFile(null!, destUri));
	}

	[Test]
	public void MoveFile_WithNullDestination_ThrowsArgumentNullException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.MoveFile(sourceUri, null!));
	}

	[Test]
	public void MoveFile_WithInvalidSource_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var sourceUri = new Uri("http://example.com/test.txt");
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.MoveFile(sourceUri, destUri));
	}

	[Test]
	public void MoveFile_WithInvalidDestination_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));
		var destUri = new Uri("http://example.com/test.txt");

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.MoveFile(sourceUri, destUri));
	}

	[Test]
	public async Task MoveFile_MovesFileCorrectly()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "move-source.txt");
		var destPath = Path.Combine(_testDirectory, "move-dest.txt");
		var content = "Test content for move";
		File.WriteAllText(sourcePath, content);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		_service.MoveFile(sourceUri, destUri);

		// Assert
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.Exists(sourcePath)).IsFalse(); // Source should be gone
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(content);
	}

	[Test]
	public async Task MoveFile_CreatesDirectoriesIfNeeded()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "move-source-dir.txt");
		var destDirPath = Path.Combine(_testDirectory, "nested", "move");
		var destPath = Path.Combine(destDirPath, "move-dest-dir.txt");
		var content = "Test content for move with directory creation";
		File.WriteAllText(sourcePath, content);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		_service.MoveFile(sourceUri, destUri);

		// Assert
		await Assert.That(Directory.Exists(destDirPath)).IsTrue();
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.Exists(sourcePath)).IsFalse();
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(content);
	}

	#endregion

	#region MoveFileAsync Tests

	[Test]
	public async Task MoveFileAsync_WithNullSource_ThrowsArgumentNullException()
	{
		// Arrange
		var destUri = new Uri(Path.Combine(_testDirectory, "dest.txt"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.MoveFileAsync(null!, destUri));
	}

	[Test]
	public async Task MoveFileAsync_WithNullDestination_ThrowsArgumentNullException()
	{
		// Arrange
		var sourceUri = new Uri(Path.Combine(_testDirectory, "source.txt"));

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.MoveFileAsync(sourceUri, null!));
	}

	[Test]
	public async Task MoveFileAsync_MovesFileCorrectly()
	{
		// Arrange
		var sourcePath = Path.Combine(_testDirectory, "move-source-async.txt");
		var destPath = Path.Combine(_testDirectory, "move-dest-async.txt");
		var content = "Test content for async move";
		File.WriteAllText(sourcePath, content);
		var sourceUri = new Uri(sourcePath);
		var destUri = new Uri(destPath);

		// Act
		await _service.MoveFileAsync(sourceUri, destUri);

		// Assert
		await Assert.That(File.Exists(destPath)).IsTrue();
		await Assert.That(File.Exists(sourcePath)).IsFalse(); // Source should be gone
		await Assert.That(File.ReadAllText(destPath)).IsEqualTo(content);
	}

	#endregion

	#region DeleteFile Tests

	[Test]
	public void DeleteFile_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			_service.DeleteFile(null!));
	}

	[Test]
	public void DeleteFile_WithInvalidLocation_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var uri = new Uri("http://example.com/test.txt");

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			_service.DeleteFile(uri));
	}

	[Test]
	public async Task DeleteFile_DeletesExistingFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "delete-test.txt");
		File.WriteAllText(filePath, "Delete me");
		var uri = new Uri(filePath);

		// Act
		_service.DeleteFile(uri);

		// Assert
		await Assert.That(File.Exists(filePath)).IsFalse();
	}

	[Test]
	public async Task DeleteFile_DoesNotThrowForNonExistentFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "nonexistent-delete.txt");
		var uri = new Uri(filePath);

		// Act & Assert (should not throw)
		_service.DeleteFile(uri);
		await Assert.That(File.Exists(filePath)).IsFalse();
	}

	#endregion

	#region DeleteFileAsync Tests

	[Test]
	public async Task DeleteFileAsync_WithNullLocation_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _service.DeleteFileAsync(null!));
	}

	[Test]
	public async Task DeleteFileAsync_WithInvalidLocation_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		var uri = new Uri("http://example.com/test.txt");

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			await _service.DeleteFileAsync(uri));
	}

	[Test]
	public async Task DeleteFileAsync_DeletesExistingFile()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "delete-test-async.txt");
		File.WriteAllText(filePath, "Delete me async");
		var uri = new Uri(filePath);

		// Act
		await _service.DeleteFileAsync(uri);

		// Assert
		await Assert.That(File.Exists(filePath)).IsFalse();
	}

	#endregion

	#region OpenReadStream Tests

	[Test]
	public async Task OpenReadStream_WithExistingFile_ReturnsReadableStream()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "read-test.txt");
		var content = "Test content for read";
		File.WriteAllText(filePath, content);
		var uri = new Uri(filePath);
		var info = _service.GetFileInfo(uri);

		// Act
		using var stream = info.OpenReadStream();
		using var reader = new StreamReader(stream);
		var readContent = reader.ReadToEnd();

		// Assert
		await Assert.That(readContent).IsEqualTo(content);
	}

	[Test]
	public async Task OpenReadStream_WithNonExistentFile_ReturnsNullStream()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "nonexistent-read.txt");
		var uri = new Uri(filePath);
		var info = _service.GetFileInfo(uri);

		// Act
		using var stream = info.OpenReadStream();

		// Assert
		await Assert.That(stream).IsEqualTo(Stream.Null);
		await Assert.That(stream.Length).IsEqualTo(0);
	}

	[Test]
	public async Task OpenReadStreamAsync_WithExistingFile_ReturnsReadableStream()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "read-test-async.txt");
		var content = "Test content for async read";
		File.WriteAllText(filePath, content);
		var uri = new Uri(filePath);
		var info = await _service.GetFileInfoAsync(uri);

		// Act
		using var stream = await info.OpenReadStreamAsync();
		using var reader = new StreamReader(stream);
		var readContent = await reader.ReadToEndAsync();

		// Assert
		await Assert.That(readContent).IsEqualTo(content);
	}

	[Test]
	public async Task OpenReadStreamAsync_WithNonExistentFile_ReturnsNullStream()
	{
		// Arrange
		var filePath = Path.Combine(_testDirectory, "nonexistent-read-async.txt");
		var uri = new Uri(filePath);
		var info = await _service.GetFileInfoAsync(uri);

		// Act
		using var stream = await info.OpenReadStreamAsync();

		// Assert
		await Assert.That(stream).IsEqualTo(Stream.Null);
		await Assert.That(stream.Length).IsEqualTo(0);
	}

	#endregion

	#region Dispose Tests

	[Test]
	public void Dispose_CanBeCalledMultipleTimes()
	{
		// Arrange
		var service = new FileStorageService();

		// Act & Assert (should not throw)
		service.Dispose();
		service.Dispose(); // Second call should not throw
	}

	#endregion
}