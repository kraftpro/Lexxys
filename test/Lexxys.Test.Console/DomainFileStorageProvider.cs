using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lexxys;

namespace Lexxys.Test.Con
{
	public static class FileTest
	{
		public static void Go()
		{
			var cc = Array.Empty<int>().FirstOrDefault();

			var p = new DomainFileStorageProbider("Documents", "./Docs", new LocalFileStorageProvider());
			using (var file = File.OpenRead(@"application.config.txt"))
			{
				var s = p.CreateFileAsync(12345, "ttt", file).Result;
				Console.WriteLine(s);
			}
			string temp = null;
			using (var file = File.OpenRead(@"application.config.txt"))
			{
				temp = p.CreateFileAsync(null, null, file).Result;
				Console.WriteLine(temp);
			}
			var perm = p.CreateFromTemporary(temp, 22345, true);
			Console.WriteLine(perm);
			p.DeleteFile(temp);
		}
	}
}

namespace Lexxys.Test.Con
{
	public class DomainFileStorageProbider
	{
		private static Logger Log => __log ??= new Logger("DomainFileStorageProbider");
		private static Logger __log;

		public const int MaxTryCount = 12;

		private readonly string _domain;
		private readonly string _localpath;
		private readonly IBlobStorageProvider _provider;
		private readonly DirectoryStorageConfig _directoryConfig;

		public DomainFileStorageProbider(string domain, string localpath, IBlobStorageProvider provider, DirectoryStorageConfig directoryConfig = null)
		{
			_domain = domain?.Replace("\\", "").Replace("/", "").TrimToNull() ?? throw new ArgumentNullException(nameof(domain));
			_localpath = localpath.TrimToNull() ?? throw new ArgumentNullException(nameof(_localpath));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_directoryConfig = directoryConfig ?? DirectoryStorageConfig.Default;
			_localpath += _directoryConfig.PathSeparator;
		}

		public async Task<string> CreateFileAsync(int? entityId, string extension, Stream stream)
		{
			if (entityId <= 0)
				throw new ArgumentOutOfRangeException(nameof(entityId), entityId, null);
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (extension == null)
				extension = "";
			else if (extension.Length > 0 && extension[0] != '.')
				extension = "." + extension;
			var salt = BlobStorage.InitSalt();

			int tryCount = 0;
			for (; ; )
			{
				// {domain}/01/02/3120201-salt.doc
				var fileName = BlobStorage.MakePath(entityId, salt, extension, _directoryConfig);
				try
				{
					await _provider.SaveFileAsync(_localpath + fileName, stream, false);
					return fileName;
				}
				catch (Exception flaw)
				{
					if (++tryCount >= MaxTryCount)
						throw new InvalidOperationException($"Cannot create file {_localpath + fileName} for {_domain}.{entityId}.", flaw);
					Log.Warning($"SaveAsync: {tryCount}: Cannot create file {_localpath + fileName} ({flaw.Message}). Next try...");
				}
				salt = BlobStorage.NextSalt(salt);
			}
		}

		public string CreateFromTemporary(string tempFileName, int entityId, bool copy)
		{
			if (tempFileName == null)
				throw new ArgumentNullException(nameof(tempFileName));
			if (Path.IsPathRooted(tempFileName) || tempFileName.Contains(".."))
				throw new ArgumentOutOfRangeException(nameof(tempFileName), tempFileName, null);
			if (entityId <= 0)
				throw new ArgumentOutOfRangeException(nameof(entityId), entityId, null);

			string extension = Path.GetExtension(tempFileName);
			var salt = BlobStorage.InitSalt();

			var fileName = BlobStorage.MakePath(entityId, salt, extension);
			int i = tempFileName.IndexOf('-');
			if (i > 0 && fileName.IndexOf('-') == i && tempFileName.StartsWith(fileName.Substring(0, i)))
				return tempFileName;

			int tryCount = 0;
			for (; ; )
			{
				// /01/02/3120201-salt.doc
				try
				{
					if (copy)
					{
						using (var stream = _provider.GetFileInfo(_localpath + tempFileName).CreateReadStream())
						{
							_provider.SaveFileAsync(_localpath + fileName, stream, false);
						}
					}
					else
					{
						_provider.MoveFile(_localpath + tempFileName, _localpath + fileName);
					}
					return fileName;
				}
				catch (Exception flaw)
				{
					if (++tryCount >= MaxTryCount)
						throw new InvalidOperationException($"Cannot move file {_localpath + tempFileName} to {_localpath + fileName} for {_domain}.{entityId}.", flaw);
					Log.Warning($"SaveAsync: {tryCount}: Cannot move file {_localpath + tempFileName} to {_localpath + fileName} ({flaw.Message}). Next try...");
				}
				salt = BlobStorage.NextSalt(salt);
				fileName = BlobStorage.MakePath(entityId, salt, extension, _directoryConfig);
			}
		}

		public void DeleteFile(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));
			if (Path.IsPathRooted(fileName) || fileName.Contains(".."))
				throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
			_provider.DeleteFile(_localpath + fileName);
		}

		public async Task UpdateFileAsync(string fileName, Stream stream)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (fileName.Contains(".."))
				throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
			if (!_provider.GetFileInfo(_localpath + fileName).Exists)
				throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
			await _provider.SaveFileAsync(_localpath + fileName, stream, true);
		}

		public Stream OpenReadStream(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));
			if (fileName.Contains(".."))
				throw new ArgumentOutOfRangeException(nameof(fileName), fileName, null);
			return _provider.GetFileInfo(_localpath + fileName).CreateReadStream();
		}

		public bool Esists(string fileName)
		{
			return fileName != null && !fileName.Contains("..") && _provider.GetFileInfo(_localpath + fileName).Exists;
		}
	}
}