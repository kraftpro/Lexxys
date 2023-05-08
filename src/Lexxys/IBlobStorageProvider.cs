// Lexxys Infrastructural library.
// file: IBlobStorageProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

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
	/// <returns></returns>
	bool CanOpen(Uri location);
	IBlobInfo? GetFileInfo(Uri location);
	Task<IBlobInfo?> GetFileInfoAsync(Uri location, CancellationToken cancellationToken = default);
	void SaveFile(Uri location, Stream stream, bool overwrite);
	Task SaveFileAsync(Uri location, Stream stream, bool overwrite, CancellationToken cancellationToken = default);
	void CopyFile(Uri source, Uri destination);
	Task CopyFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default);
	void MoveFile(Uri source, Uri destination);
	Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default);
	void DeleteFile(Uri location);
	Task DeleteFileAsync(Uri location, CancellationToken cancellationToken = default);
}

