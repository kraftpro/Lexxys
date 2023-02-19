// Lexxys Infrastructural library.
// file: LocalFileStorageProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexxys
{
	/// <inheritdoc />
	public class LocalFileStorageProvider: IBlobStorageProvider
	{
		private const int DefaultBufferSize = 81920;
		private static readonly IReadOnlyCollection<string> _schemes = ReadOnly.Wrap(new[] { Uri.UriSchemeFile })!;
		public IReadOnlyCollection<string> SupportedSchemes => _schemes;

		/// <inheritdoc />
		public IBlobInfo? GetFileInfo(string location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (!CanOpen(location))
				return null;
			var p = BlobStorage.SplitSchemeAndPath(location);
			return IsMe(p.Scheme) ? new LocalFileInfo(p.Path) : null;
		}

		/// <inheritdoc />
		public Task<IBlobInfo?> GetFileInfoAsync(string location, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(GetFileInfo(location));
		}

		/// <inheritdoc />
		public virtual bool CanOpen(string location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			return IsMe(BlobStorage.SplitSchemeAndPath(location).Scheme);
		}

		private static bool IsMe(string scheme) => scheme == Uri.UriSchemeFile || String.IsNullOrEmpty(scheme);

		/// <inheritdoc />
		public void SaveFile(string location, Stream stream, bool overwrite)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			CreateDirectory(location);
			using var file = new FileStream(location, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.SequentialScan);
			stream.CopyTo(file);
		}

		/// <inheritdoc />
		public async Task SaveFileAsync(string location, Stream stream, bool overwrite, CancellationToken cancellationToken = default)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			CreateDirectory(location);
			using var file = File.Open(location, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
			int bufferSize = stream.CanSeek ? (int)Math.Min(DefaultBufferSize, stream.Length): DefaultBufferSize;
			await stream.CopyToAsync(file, bufferSize, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public void CopyFile(string source, string destination)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			CreateDirectory(destination);
			File.Copy(source, destination, true);
		}

		/// <inheritdoc/>
		public async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken = default)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			CreateDirectory(destination);

			using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
			using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
			int bufferSize = sourceStream.CanSeek ? (int)Math.Min(DefaultBufferSize, sourceStream.Length): DefaultBufferSize;
			await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
		}

		private static void CreateDirectory(string location)
		{
			var dir = Path.GetDirectoryName(location);
			if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		/// <inheritdoc />
		public void MoveFile(string source, string destination)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			CreateDirectory(destination);
			File.Move(source, destination);
		}

		/// <inheritdoc/>
		public Task MoveFileAsync(string source, string destination, CancellationToken cancellationToken = default)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			MoveFile(source, destination);
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public void DeleteFile(string location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);
			if (File.Exists(location))
				File.Delete(location);
		}

		/// <inheritdoc/>
		public Task DeleteFileAsync(string location, CancellationToken cancellationToken = default)
		{
			DeleteFile(location);
			return Task.CompletedTask;
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

			public Stream CreateReadStream() => _fileInfo.Exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan): Stream.Null;

			public Task<Stream> CreateReadStreamAsync() => Task.FromResult(_fileInfo.Exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan): Stream.Null);
		}

		#region IDisposable Support
		private bool _disposed;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
				_disposed = true;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
