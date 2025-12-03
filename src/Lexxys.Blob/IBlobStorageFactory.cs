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
	/// Returns <see cref="IBlobStorageService"/> for the specified <paramref name="domain"/> or null.
	/// </summary>
	/// <param name="domain">Blob storage identifier</param>
	/// <returns></returns>
	IBlobStorageService? TryGetStorage(string domain);

	/// <summary>
	/// Registers a blob storage service.
	/// </summary>
	/// <param name="domain">Blob storage identifier</param>
	/// <param name="service">Blob storage service</param>
	IBlobStorageService RegisterStorage(string domain, IBlobStorageService service);
}
