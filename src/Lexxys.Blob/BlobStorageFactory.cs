// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license

using System.Collections.Immutable;

namespace Lexxys;

/// <summary>
/// Provides methods to retrieve and register blob storage services based on their URI schemes.
/// It allows for service lookup and management.
/// </summary>
public class BlobStorageFactory: IBlobStorageFactory
{
	private ImmutableDictionary<string, IBlobStorageService> _schemes = ImmutableDictionary<string, IBlobStorageService>.Empty;

	/// <summary>
	/// Get <see cref="IBlobStorageService"/> for the specified <paramref name="domain"/>.
	/// </summary>
	/// <param name="domain">Name of the domain</param>
	/// <returns>Blob storage service to access blobs in the specified domain or null if no service is registered for the domain.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public IBlobStorageService? TryGetStorage(string domain)
	{
		if (domain is null) throw new ArgumentNullException(nameof(domain));

		_schemes.TryGetValue(domain, out var service);
		return service;
	}

	/// <summary>
	/// Registers a blob storage service.
	/// </summary>
	/// <param name="service">Blob storage service</param>
	/// <param name="domain">Name of the domain</param>
	/// <exception cref="ArgumentNullException"><paramref name="service"/> is null</exception>
	public IBlobStorageService RegisterStorage(string domain, IBlobStorageService service)
	{
		if (domain is null) throw new ArgumentNullException(nameof(domain));
		if (service == null) throw new ArgumentNullException(nameof(service));

		ImmutableDictionary<string, IBlobStorageService> oldSchemes;
		ImmutableDictionary<string, IBlobStorageService> newSchemes;

		do
		{
			oldSchemes = _schemes;
			newSchemes = oldSchemes.SetItem(domain, service);
		} while (Interlocked.CompareExchange(ref _schemes, newSchemes, oldSchemes) != oldSchemes);

		return service;
	}
}


