// Lexxys Infrastructural library.
// file: IBlobStorageProvider.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Provides access to the abstract file storage.
/// </summary>
public interface IBlobStorageService: IDisposable
{
	/// <summary>
	/// Retrieves metadata information for the blob at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The path of the blob to retrieve information for. This value cannot be null or empty.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>An object containing metadata about the specified blob.</returns>
	IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens a readable stream for the blob located at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The path of the blob to read. Cannot be null or empty.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the read operation.</param>
	/// <returns>A stream for reading the contents of the specified blob. The caller is responsible for disposing the returned
	/// stream.</returns>
	Stream Read(string location, CancellationToken cancellationToken = default);

	/// <summary>
	/// Writes the contents of the specified <paramref name="stream"/> to a blob at the given <paramref name="location"/>, using the specified write <paramref name="mode"/>.
	/// </summary>
	/// <param name="location">The destination path or identifier where the blob will be written. Cannot be null or empty.</param>
	/// <param name="stream">The stream containing the data to write to the blob.</param>
	/// <param name="mode">The write mode to use when writing the blob. Determines whether the destination is overwritten, appended to, or if
	/// the operation fails when the destination exists. The default is <see cref="BlobWriteMode.Overwrite"/>.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
	void Write(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Copies a blob from the specified <paramref name="source"/> location to the specified <paramref name="destination"/> location, using the given write <paramref name="mode"/>.
	/// </summary>
	/// <param name="source">The path of the source blob to copy. Cannot be null or empty.</param>
	/// <param name="destination">The path of the destination where the blob will be copied. Cannot be null or empty.</param>
	/// <param name="mode">The write mode to use when copying the blob. Determines whether the destination is overwritten, appended to, or if
	/// the operation fails when the destination exists. The default is <see cref="BlobWriteMode.Create"/>.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is signaled.</param>
	void Copy(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Moves a blob from the specified <paramref name="source"/> location to the specified <paramref name="destination"/> location, using the given write <paramref name="mode"/>.
	/// </summary>
	/// <param name="source">The path of the source blob to move. Cannot be null or empty.</param>
	/// <param name="destination">The path of the destination where the blob will be moved. Cannot be null or empty.</param>
	/// <param name="mode">The write mode to use when moving the blob. Determines whether the destination is overwritten, appended to, or if
	/// the operation fails when the destination exists. The default is <see cref="BlobWriteMode.Create"/>.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is signaled.</param>
	void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the blob at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The path of the blob to delete. Cannot be null or empty.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
	void Delete(string location, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="GetBlobInfo(string, CancellationToken)"/>
	Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="Read(string, CancellationToken)"/>
	Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="Write(string, Stream, BlobWriteMode, CancellationToken)"/>
	Task WriteAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="Copy(string, string, BlobWriteMode, CancellationToken)"/>
	Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="Move(string, string, BlobWriteMode, CancellationToken)"/>
	Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default);
	
	/// <inheritdoc cref="Delete(string, CancellationToken)"/>
	Task DeleteAsync(string location, CancellationToken cancellationToken = default);
}

public interface IBlobStorageService<T>: IBlobStorageService
{
}
