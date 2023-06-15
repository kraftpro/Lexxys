// Lexxys Infrastructural library.
// file: IBlobStorageProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Provides access to the abstract blob storage.
/// </summary>
public interface IBlobStorageProvider: IDisposable
{
	/// <summary>
	/// Collection of supported schemes
	/// </summary>
	IReadOnlyCollection<string> SupportedSchemes { get; }

	/// <summary>
	/// Determines if this provider can open a blob at the specified <paramref name="location"/>. 
	/// </summary>
	/// <param name="location">Blob location</param>
	/// <returns>True if the specified blob exists.</returns>
	bool CanOpen(Uri location);
	/// <summary>
	/// Returns a <see cref="IBlobInfo"/> for the specified <paramref name="location"/> or null if the blob does not exist.
	/// </summary>
	/// <param name="location">The blob location.</param>
	/// <returns></returns>
	IBlobInfo? GetFileInfo(Uri location);
	/// <summary>
	/// Returns a <see cref="IBlobInfo"/> for the specified <paramref name="location"/> or null if the blob does not exist.
	/// </summary>
	/// <param name="location">The blob location.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns></returns>
	Task<IBlobInfo?> GetFileInfoAsync(Uri location, CancellationToken cancellationToken = default);
	/// <summary>
	/// Saves the specified <paramref name="stream"/> to the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The blob location.</param>
	/// <param name="stream">The stream to save.</param>
	/// <param name="overwrite">If true, the existing blob will be overwritten.</param>
	void SaveFile(Uri location, Stream stream, bool overwrite);
	/// <summary>
	/// Saves the specified <paramref name="stream"/> to the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">The blob location.</param>
	/// <param name="stream">The stream to save.</param>
	/// <param name="overwrite">If true, the existing blob will be overwritten.</param>
	/// <param name="cancellationToken">Cancelation token.</param>
	Task SaveFileAsync(Uri location, Stream stream, bool overwrite, CancellationToken cancellationToken = default);
	/// <summary>
	/// Copies the blob from <paramref name="source"/> to <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source blob location.</param>
	/// <param name="destination">Destination blob location.</param>
	void CopyFile(Uri source, Uri destination);
	/// <summary>
	/// Copies the blob from <paramref name="source"/> to <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source blob location.</param>
	/// <param name="destination">Destination blob location.</param>
	/// <param name="cancellationToken">Cancelation token.</param>
	Task CopyFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default);
	/// <summary>
	/// Moves the blob from <paramref name="source"/> to <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source blob location.</param>
	/// <param name="destination">Destination blob location.</param>
	void MoveFile(Uri source, Uri destination);
	/// <summary>
	/// Moves the blob from <paramref name="source"/> to <paramref name="destination"/>.
	/// </summary>
	/// <param name="source">Source blob location.</param>
	/// <param name="destination">Destination blob location.</param>
	/// <param name="cancellationToken">Cancelation token.</param>
	Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default);
	/// <summary>
	/// Deletes the blob at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">Blob location to delete.</param>
	void DeleteFile(Uri location);
	/// <summary>
	/// Deletes the blob at the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">Blob location to delete.</param>
	/// <param name="cancellationToken">Cancelation token.</param>
	Task DeleteFileAsync(Uri location, CancellationToken cancellationToken = default);
}

