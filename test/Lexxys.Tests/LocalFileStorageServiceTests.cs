using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexxys.Tests
{
	[TestClass]
	public class LocalFileStorageServiceTests
	{
		private FileStorageService _provider;
		private Uri _testFileUri;
		private Uri _testFileUri2;
		private string _testFilePath;
		private string _testFilePath2;

		[TestInitialize]
		public void Setup()
		{
			_provider = new FileStorageService();
			_testFilePath = Path.Combine(Path.GetTempPath(), "testfile.txt");
			_testFilePath2 = Path.Combine(Path.GetTempPath(), "testfile2.txt");
			_testFileUri = new Uri(_testFilePath);
			_testFileUri2 = new Uri(_testFilePath2);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (File.Exists(_testFilePath))
				File.Delete(_testFilePath);
			if (File.Exists(_testFilePath2))
				File.Delete(_testFilePath2);
		}

		[TestMethod]
		public void TestCanOpen()
		{
			Assert.IsTrue(_provider.CanOpen(_testFileUri));
			Assert.IsFalse(_provider.CanOpen(new Uri("http://example.com")));
		}

		[TestMethod]
		public void TestGetFileInfo()
		{
			File.WriteAllText(_testFilePath, "test content");
			var info = _provider.GetFileInfo(_testFileUri);
			Assert.IsNotNull(info);
			Assert.IsTrue(info.Exists);
			Assert.AreEqual("testfile.txt", Path.GetFileName(info.Path));
		}

		[TestMethod]
		public async Task TestGetFileInfoAsync()
		{
			File.WriteAllText(_testFilePath, "test content");
			var info = await _provider.GetFileInfoAsync(_testFileUri);
			Assert.IsNotNull(info);
			Assert.IsTrue(info.Exists);
			Assert.AreEqual("testfile.txt", Path.GetFileName(info.Path));
		}

		[TestMethod]
		public void TestSaveFile()
		{
			using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
			_provider.WriteFile(_testFileUri, stream, true);
			Assert.IsTrue(File.Exists(_testFilePath));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath));
		}

		[TestMethod]
		public async Task TestSaveFileAsync()
		{
			using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
			await _provider.WriteFileAsync(_testFileUri, stream, true);
			Assert.IsTrue(File.Exists(_testFilePath));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath));
		}

		[TestMethod]
		public void TestCopyFile()
		{
			File.WriteAllText(_testFilePath, "test content");
			_provider.CopyFile(_testFileUri, _testFileUri2);
			Assert.IsTrue(File.Exists(_testFilePath2));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath2));
		}

		[TestMethod]
		public async Task TestCopyFileAsync()
		{
			File.WriteAllText(_testFilePath, "test content");
			await _provider.CopyFileAsync(_testFileUri, _testFileUri2);
			Assert.IsTrue(File.Exists(_testFilePath2));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath2));
		}

		[TestMethod]
		public void TestMoveFile()
		{
			File.WriteAllText(_testFilePath, "test content");
			_provider.MoveFile(_testFileUri, _testFileUri2);
			Assert.IsFalse(File.Exists(_testFilePath));
			Assert.IsTrue(File.Exists(_testFilePath2));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath2));
		}

		[TestMethod]
		public async Task TestMoveFileAsync()
		{
			File.WriteAllText(_testFilePath, "test content");
			await _provider.MoveFileAsync(_testFileUri, _testFileUri2);
			Assert.IsFalse(File.Exists(_testFilePath));
			Assert.IsTrue(File.Exists(_testFilePath2));
			Assert.AreEqual("test content", File.ReadAllText(_testFilePath2));
		}

		[TestMethod]
		public void TestDeleteFile()
		{
			File.WriteAllText(_testFilePath, "test content");
			_provider.DeleteFile(_testFileUri);
			Assert.IsFalse(File.Exists(_testFilePath));
		}

		[TestMethod]
		public async Task TestDeleteFileAsync()
		{
			File.WriteAllText(_testFilePath, "test content");
			await _provider.DeleteFileAsync(_testFileUri);
			Assert.IsFalse(File.Exists(_testFilePath));
		}
	}
}
