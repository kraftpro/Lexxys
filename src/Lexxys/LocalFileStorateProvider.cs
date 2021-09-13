// Lexxys Infrastructural library.
// file: LocalFileStorateProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public class LocalFileStorageProvider: IBlobStorageProvider
	{
		private static readonly IReadOnlyCollection<string> _schemes = ReadOnly.Wrap(new[] { "file" });
		public IReadOnlyCollection<string> SupportedSchemes => _schemes;

		public IBlobInfo GetFileInfo(string uri)
		{
			if (!CanOpen(uri))
				return null;
			var p = BlobStorage.SplitSchemeAndPath(uri);
			return IsMe(p.Scheme) ? new LocalFileInfo(p.Path) : null;
		}

		public virtual bool CanOpen(string uri)
		{
			return uri != null && IsMe(BlobStorage.SplitSchemeAndPath(uri).Scheme);
		}

		private static bool IsMe(string scheme) => scheme == "file" || String.IsNullOrEmpty(scheme);

		public void SaveFile(string uri, Stream stream, bool overwrite)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(uri))
				throw new ArgumentOutOfRangeException(nameof(uri), uri, null);

			CreateDirectory(uri);
			using var file = File.Open(uri, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
			stream.CopyTo(file);
		}

		private static void CreateDirectory(string uri)
		{
			var dir = Path.GetDirectoryName(uri);
			if (!String.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);
		}

		public async Task SaveFileAsync(string uri, Stream stream, bool overwrite)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(uri))
				throw new ArgumentOutOfRangeException(nameof(uri), uri, null);

			CreateDirectory(uri);
			using var file = File.Open(uri, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
			await stream.CopyToAsync(file).ConfigureAwait(false);
		}

		public void MoveFile(string source, string destination)
		{
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			CreateDirectory(destination);
			File.Move(source, destination);
		}

		public void DeleteFile(string uri)
		{
			if (!CanOpen(uri))
				throw new ArgumentOutOfRangeException(nameof(uri), uri, null);
			if (File.Exists(uri))
				File.Delete(uri);
		}

		private class LocalFileInfo: IBlobInfo
		{
			private readonly FileInfo _fileInfo;
			public LocalFileInfo(string filename)
			{
				_fileInfo = new FileInfo(filename);
			}

			public bool Exists => _fileInfo.Exists;

			public long Length => _fileInfo.Length;

			public string Path => _fileInfo.FullName;

			public DateTimeOffset LastModified => _fileInfo.LastAccessTimeUtc;

			public Stream CreateReadStream(bool async = false)
			{
				return !_fileInfo.Exists ? Stream.Null: new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, async);
			}

			public Task<Stream> CreateReadStreamAsync(bool async = false)
			{
				return Task.FromResult(!_fileInfo.Exists ? Stream.Null: new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, async));
			}
		}

		#region IDisposable Support
		private bool _disposed;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
				_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
