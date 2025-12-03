// Lexxys Infrastructural library.
// file: FileStorageProvider.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public class FileStorageService: IBlobStorageService
{
	private const int DefaultBufferSize = 65536;
	private string _directory;

	public FileStorageService()
	{
		_directory = Path.GetFullPath(".");
	}

	public FileStorageService(string directory)
	{
		_directory = Path.GetFullPath(directory);
	}

	public void Initialize(string directory)
	{
		_directory = Path.GetFullPath(directory);
	}

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		return new LocalFileInfo(GetPath(location));
	}

	public Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		return Task.FromResult(GetBlobInfo(location));
	}


	public Stream Read(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		var path = GetPath(location);
		return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
	}

	public Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default) => Task.FromResult(Read(location));

	public void Write(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		if (stream == null) throw new ArgumentNullException(nameof(stream));

		var path = CreatePath(location);
		var fileMode = MapWriteMode(mode, FileMode.Create);

		using var file = new FileStream(path, fileMode, FileAccess.Write, FileShare.None, 8192, FileOptions.SequentialScan);
		stream.CopyTo(file);
	}

	public async Task WriteAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		if (stream == null) throw new ArgumentNullException(nameof(stream));

		var path = CreatePath(location);
		var fileMode = MapWriteMode(mode, FileMode.Create);

#if NET
		await
#endif
		using var file = File.Open(path, fileMode, FileAccess.Write);
#if NET
		await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
#else
		int bufferSize = stream.CanSeek ? (int)Math.Min(DefaultBufferSize, stream.Length): DefaultBufferSize;
		await stream.CopyToAsync(file, bufferSize, cancellationToken).ConfigureAwait(false);
#endif
	}

	public void Copy(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));

		var path1 = GetPath(source);
		var path2 = CreatePath(destination);
		switch (mode)
		{
			case BlobWriteMode.Create:
			case BlobWriteMode.Default:
				File.Copy(path1, path2, false);
				break;

			case BlobWriteMode.Overwrite:
				File.Copy(path1, path2, true);
				break;

			case BlobWriteMode.Append:
				{
					using var file1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.SequentialScan);
					using var file2 = new FileStream(path2, FileMode.Append, FileAccess.Write, FileShare.None, DefaultBufferSize, FileOptions.SequentialScan);
					file1.CopyTo(file2);
					break;
				}

			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
		}
	}

	public async Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));

		var path1 = GetPath(source);
		var path2 = CreatePath(destination);

#if NET
		await
#endif
		using var sourceStream = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
		FileMode destMode = MapWriteMode(mode, FileMode.CreateNew);
#if NET
		await
#endif
		using var destinationStream = new FileStream(path2, destMode, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);

#if NET
		await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
#else
		int bufferSize = sourceStream.CanSeek ? (int)Math.Min(DefaultBufferSize, sourceStream.Length): DefaultBufferSize;
		await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
#endif
	}

	public void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));

		var path1 = GetPath(source);
		var path2 = CreatePath(destination);
		switch (mode)
		{
			case BlobWriteMode.Default:
			case BlobWriteMode.Create:
				File.Move(path1, path2);
				break;

			case BlobWriteMode.Overwrite:
#if NET
				File.Move(path1, path2, true);
#else
				if (File.Exists(path2))
					File.Delete(path2);
				File.Move(path1, path2);
#endif
				break;

			case BlobWriteMode.Append:
				{
					if (File.Exists(path2))
						AppendFile(path1, path2);
					else
						File.Move(path1, path2);
					break;
				}
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
		}

		static void AppendFile(string path1, string path2)
		{
			using (var file1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.SequentialScan))
			using (var file2 = new FileStream(path2, FileMode.Append, FileAccess.Write, FileShare.None, DefaultBufferSize, FileOptions.SequentialScan))
			{
				file1.CopyTo(file2);
			}
			File.Delete(path1);
		}
	}


	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">The <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="source"/> or <paramref name="destination"/> is not a valid file location.</exception>
	public Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));

		var path1 = GetPath(source);
		var path2 = CreatePath(destination);

		switch (mode)
		{
			case BlobWriteMode.Default:
			case BlobWriteMode.Create:
				File.Move(path1, path2);
				break;

			case BlobWriteMode.Overwrite:
#if NET
				File.Move(path1, path2, true);
#else
				if (File.Exists(path2))
					File.Delete(path2);
				File.Move(path1, path2);
#endif
				break;

			case BlobWriteMode.Append:
				if (File.Exists(path2))
					return AppendFile(path1, path2, cancellationToken);

				File.Move(path1, path2);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
		}
		return Task.CompletedTask;

		static async Task AppendFile(string path1, string path2, CancellationToken cancellationToken)
		{
			{
#if NET
				await
#endif
				using var file1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, FileOptions.SequentialScan);
#if NET
				await
#endif
				using var file2 = new FileStream(path2, FileMode.Append, FileAccess.Write, FileShare.None, DefaultBufferSize, FileOptions.SequentialScan);
#if NET
				await file1.CopyToAsync(file2, cancellationToken).ConfigureAwait(false);
#else
				int bufferSize = file1.CanSeek ? (int)Math.Min(DefaultBufferSize, file1.Length): DefaultBufferSize;
				await file1.CopyToAsync(file2, bufferSize, cancellationToken).ConfigureAwait(false);
#endif
			}
			File.Delete(path1);
		}
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException">The <paramref name="location"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="location"/> is not a valid file location.</exception>
	public void Delete(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		var path = GetPath(location);
		if (File.Exists(path))
			File.Delete(path);
	}

	/// <summary>
	/// Deletes a file at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">A file location</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ArgumentNullException">The <paramref name="location"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="location"/> is not a valid file location.</exception>
	public Task DeleteAsync(string location, CancellationToken cancellationToken = default)
	{
		Delete(location);
		return Task.CompletedTask;
	}

	private string GetPath(string location)
	{
		var path = Path.Combine(_directory, location);
		if (!path.StartsWith(_directory, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentOutOfRangeException(nameof(location), location, null);
		return path;
	}

	private string CreatePath(string location)
	{
		var path = GetPath(location);
		var dir = Path.GetDirectoryName(path);
		if (!String.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
		return path;
	}

	private static FileMode MapWriteMode(BlobWriteMode mode, FileMode defaultMode) => mode switch
	{
		BlobWriteMode.Default => defaultMode,
		BlobWriteMode.Create => FileMode.CreateNew,
		BlobWriteMode.Overwrite => FileMode.Create,
		BlobWriteMode.Append => FileMode.Append,
		_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
	};

	private class LocalFileInfo: IBlobInfo
	{
		private readonly FileInfo _fileInfo;
		private readonly bool _exists;

		public LocalFileInfo(string filename)
		{
			_fileInfo = new(filename);
			_exists = _fileInfo.Exists;
		}


		public bool Exists => _exists;

		public long Length => _exists ? _fileInfo.Length: 0;

		public string Path => _fileInfo.FullName;

		public DateTime? LastModified => _fileInfo.LastWriteTimeUtc;

		public Stream OpenReadStream() => _exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan): Stream.Null;

		public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default) => Task.FromResult(_exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan): Stream.Null);
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
