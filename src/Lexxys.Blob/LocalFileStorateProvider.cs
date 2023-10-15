// Lexxys Infrastructural library.
// file: LocalFileStorageProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Implements <see cref="IBlobStorageProvider"/> for local file system.
/// </summary>
public class LocalFileStorageProvider: IBlobStorageProvider
{
	private const int DefaultBufferSize = 81920;
	private static readonly IReadOnlyCollection<string> _schemes = ReadOnly.Wrap(new[] { Uri.UriSchemeFile, "" })!;

	/// <summary>
	/// Collection of supported schemes ("file:" or empty).
	/// </summary>
	public IReadOnlyCollection<string> SupportedSchemes => _schemes;

	/// <summary>
	/// Determines whether the specified <paramref name="location"/> can be opened by this provider.
	/// </summary>
	/// <param name="location">File location</param>
	/// <returns>True if the specified file exists.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> is null.</exception>
	public virtual bool CanOpen(Uri location)
	{
		if (location is null)
			throw new ArgumentNullException(nameof(location));
		return !location.IsAbsoluteUri || location.IsFile;
	}

	/// <summary>
	/// Returns a <see cref="IBlobInfo"/> for the specified <paramref name="location"/> or null if the blob does not exist.
	/// </summary>
	/// <param name="location">The file location</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> is null.</exception>
	public IBlobInfo? GetFileInfo(Uri location)
	{
		if (location is null)
			throw new ArgumentNullException(nameof(location));
		return CanOpen(location) ? new LocalFileInfo(GetPath(location)): null;
	}

	private static string GetPath(Uri location) => location.IsAbsoluteUri ? location.LocalPath: location.OriginalString;

	/// <summary>
	/// Returns a <see cref="IBlobInfo"/> for the specified <paramref name="location"/> or null if the blob does not exist.
	/// </summary>
	/// <param name="location">The file location.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> is null.</exception>
	public Task<IBlobInfo?> GetFileInfoAsync(Uri location, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(GetFileInfo(location));
	}

	/// <summary>
	/// Saves the specified <paramref name="stream"/> to the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The file location.</param>
	/// <param name="stream">The stream to save.</param>
	/// <param name="overwrite">If true, the existing blob will be overwritten.</param>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> or <paramref name="stream"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="location"/> is not a valid file location.</exception>
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

	/// <summary>
	/// Saves the specified <paramref name="stream"/> to the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The file location.</param>
	/// <param name="stream">The stream to save.</param>
	/// <param name="overwrite">If true, the existing blob will be overwritten.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ArgumentNullException">location or stream is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">location is not a valid file location.</exception>
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
		#if !NETFRAMEWORK
		await 
		#endif
		using var file = File.Open(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
		int bufferSize = stream.CanSeek ? (int)Math.Min(DefaultBufferSize, stream.Length): DefaultBufferSize;
		await stream.CopyToAsync(file, bufferSize, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Copies a file from the specified <paramref name="source"/> to the selected <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source file location.</param>
	/// <param name="destination">Destination file location.</param>
	/// <exception cref="ArgumentNullException">The source or destination is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The source or destination is not a valid file location.</exception>
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

	/// <summary>
	/// Copies a file from the specified <paramref name="source"/> to the selected <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source file location.</param>
	/// <param name="destination">Destination file location.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ArgumentNullException">The <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="source"/> or <paramref name="destination"/> is not a valid file location.</exception>
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

		#if !NETFRAMEWORK
		await 
		#endif
		using var sourceStream = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
		#if !NETFRAMEWORK
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

	/// <summary>
	/// Moves a file the specified <paramref name="source"/> to the selected <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source file location.</param>
	/// <param name="destination">Destination file location.</param>
	/// <exception cref="ArgumentNullException">The <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="source"/> or <paramref name="destination"/> is not a valid file location.</exception>
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

	/// <summary>
	/// Moves a file the specified <paramref name="source"/> to the selected <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source file location.</param>
	/// <param name="destination">Destination file location.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ArgumentNullException">The <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="source"/> or <paramref name="destination"/> is not a valid file location.</exception>
	public Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
	{
		MoveFile(source, destination);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Deletes a file at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">A file location</param>
	/// <exception cref="ArgumentNullException">The <paramref name="location"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="location"/> is not a valid file location.</exception>
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

	/// <summary>
	/// Deletes a file at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">A file location</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="ArgumentNullException">The <paramref name="location"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">The <paramref name="location"/> is not a valid file location.</exception>
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
