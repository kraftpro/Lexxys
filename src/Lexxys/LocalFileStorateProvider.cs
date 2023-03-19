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
		private static readonly IReadOnlyCollection<string> _schemes = ReadOnly.Wrap(new[] { Uri.UriSchemeFile, "" })!;

		/// <inheritdoc />
		public IReadOnlyCollection<string> SupportedSchemes => _schemes;

		/// <inheritdoc />
		public IBlobInfo? GetFileInfo(Uri location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			return CanOpen(location) ? new LocalFileInfo(GetPath(location)): null;
		}

		private static string GetPath(Uri location) => location.IsAbsoluteUri ? location.LocalPath: location.OriginalString;

		/// <inheritdoc />
		public Task<IBlobInfo?> GetFileInfoAsync(Uri location, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(GetFileInfo(location));
		}

		/// <inheritdoc />
		public virtual bool CanOpen(Uri location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			return !location.IsAbsoluteUri || location.IsFile;
		}

		/// <inheritdoc />
		public void SaveFile(Uri location, Stream stream, bool overwrite)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			var path = GetPath(location);
			CreateDirectory(path);
			using var file = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.SequentialScan);
			stream.CopyTo(file);
		}

		/// <inheritdoc />
		public async Task SaveFileAsync(Uri location, Stream stream, bool overwrite,
			CancellationToken cancellationToken = default)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			var path = GetPath(location);
			CreateDirectory(path);
#if NET6_0_OR_GREATER
			await
#endif
			using var file = File.Open(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
			int bufferSize = stream.CanSeek ? (int)Math.Min(DefaultBufferSize, stream.Length): DefaultBufferSize;
			await stream.CopyToAsync(file, bufferSize, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public void CopyFile(Uri source, Uri destination)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			var path1 = GetPath(source);
			var path2 = GetPath(destination);
			CreateDirectory(path2);
			File.Copy(path1, path2, true);
		}

		/// <inheritdoc/>
		public async Task CopyFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			var path1 = GetPath(source);
			var path2 = GetPath(destination);
			CreateDirectory(path2);

#if NET6_0_OR_GREATER
			await
#endif
			using var sourceStream = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
#if NET6_0_OR_GREATER
			await
#endif
			using var destinationStream = new FileStream(path2, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
			int bufferSize = sourceStream.CanSeek ? (int)Math.Min(DefaultBufferSize, sourceStream.Length): DefaultBufferSize;
			await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
		}

		private static void CreateDirectory(string path)
		{
			var dir = Path.GetDirectoryName(path);
			if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		/// <inheritdoc />
		public void MoveFile(Uri source, Uri destination)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (destination is null)
				throw new ArgumentNullException(nameof(destination));
			if (!CanOpen(source))
				throw new ArgumentOutOfRangeException(nameof(source), source, null);
			if (!CanOpen(destination))
				throw new ArgumentOutOfRangeException(nameof(destination), destination, null);

			var path1 = GetPath(source);
			var path2 = GetPath(destination);
			CreateDirectory(path2);
			File.Move(path1, path2);
		}

		/// <inheritdoc/>
		public Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
		{
			MoveFile(source, destination);
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public void DeleteFile(Uri location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (!CanOpen(location))
				throw new ArgumentOutOfRangeException(nameof(location), location, null);

			var path = GetPath(location);
			if (File.Exists(path))
				File.Delete(path);
		}

		/// <inheritdoc/>
		public Task DeleteFileAsync(Uri location, CancellationToken cancellationToken = default)
		{
			DeleteFile(location);
			return Task.CompletedTask;
		}

		private class LocalFileInfo: IBlobInfo
		{
			private readonly FileInfo _fileInfo;
			public LocalFileInfo(string filename) => _fileInfo = new FileInfo(filename);

			public bool Exists => _fileInfo.Exists;

			public long Length => _fileInfo.Length;

			public string Path => _fileInfo.FullName;

			public DateTimeOffset LastModified => _fileInfo.LastWriteTimeUtc;

			public Stream CreateReadStream() => _fileInfo.Exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan): Stream.Null;

			public Task<Stream> CreateReadStreamAsync() => Task.FromResult(_fileInfo.Exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan): Stream.Null);
		}

		#region IDisposable Support

		/// <summary>
		/// Actual dispose.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
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
