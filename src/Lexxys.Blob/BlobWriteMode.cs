// Lexxys Infrastructural library.
// file: IBlobStorageProvider.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Specifies the available modes for writing data to a blob.
/// </summary>
/// <remarks>Use this enumeration to control how data is written to a blob, such as whether to create a new blob,
/// overwrite an existing one, or append data. The selected mode determines the behavior when the target blob already
/// exists.</remarks>
public enum BlobWriteMode
{
	/// <summary>
	/// Specifies the default mode for the operation.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Creates a new blob. If the blob already exists, an exception is thrown.
	/// </summary>
	Create = 1,

	/// <summary>
	/// Overwrite the blob or create a new blob if it does not exist.
	/// </summary>
	Overwrite = 2,

	/// <summary>
	/// Appends data to the end of the existing blob or creates a new blob if it does not exist.
	/// </summary>
	Append = 3,
}
