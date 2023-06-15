// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Represents a factory of <see cref="IBlobStorageProvider"/>.
/// </summary>
public interface IBlobStorage
{
	/// <summary>
	/// Returns <see cref="IBlobStorageProvider"/> for the specified <paramref name="location"/> or null.
	/// </summary>
	/// <param name="location">Blob location</param>
	/// <returns></returns>
	IBlobStorageProvider? TryGetProvider(Uri location);

	/// <summary>
	/// Registers a blob storage provider.
	/// </summary>
	/// <param name="provider">Blob storage provider</param>
	void Register(IBlobStorageProvider provider);
}
