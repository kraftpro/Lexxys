// Lexxys Infrastructural library.
// file: IBlobInfo.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Represents information about a blob in the storage.
/// </summary>
public interface IBlobInfo
{
	/// <summary>
	/// Indicates whether a resource exists. Returns true if it exists, otherwise false.
	/// </summary>
	bool Exists { get; }

	/// <summary>
	/// Gets the length of a blob. Returns a long value representing the total number of bytes.
	/// </summary>
	long Length { get; }

	/// <summary>
	/// Represents the blob path as a string. It provides the location of a blob.
	/// </summary>
	string Path { get; }

	/// <summary>
	/// Represents the UTC date and time when the item was last modified.
	/// </summary>
	DateTime? LastModified { get; }

	/// <summary>
	/// Opens a stream for reading data. This allows for reading the contents of a blob.
	/// </summary>
	/// <returns>Returns a stream that can be used to read data.</returns>
	Stream OpenReadStream();

	/// <summary>
	/// Asynchronously opens a stream for reading data. It allows for non-blocking access to the blob.
	/// </summary>
	/// <returns>Returns a Task that represents the asynchronous operation, with a Stream as the result.</returns>
	Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default);
}
