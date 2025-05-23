// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

/// <summary>
/// Represents a factory of <see cref="IBlobStorageService"/>.
/// </summary>
public interface IBlobStorageFactory
{
	/// <summary>
	/// Returns <see cref="IBlobStorageService"/> for the specified <paramref name="location"/> or null.
	/// </summary>
	/// <param name="location">Blob location</param>
	/// <returns></returns>
	IBlobStorageService? TryGetService(Uri location);

	/// <summary>
	/// Registers a blob storage service.
	/// </summary>
	/// <param name="service">Blob storage service</param>
	void Register(IBlobStorageService service);
}
