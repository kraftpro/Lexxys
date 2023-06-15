// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public static class BlobStorageExtensions
{
	/// <summary>
	/// Returns <see cref="IBlobStorageProvider"/> for the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="factory">The blob storage factory</param>
	/// <param name="location">The blob location</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException">Factory does not contain provider for the specified <paramref name="location"/>.</exception>
	public static IBlobStorageProvider GetProvider(this IBlobStorage factory, Uri location)
	{
		if (factory is null)
			throw new ArgumentNullException(nameof(factory));
		return factory.TryGetProvider(location) ?? throw new ArgumentOutOfRangeException(nameof(location), location, $"Cannot find {nameof(IBlobStorageProvider)} for {location}.");
	}

	/// <summary>
	/// Registers <see cref="IBlobStorage"/> factory as a singleton service.
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void AddBlobStorage(this IServiceCollection services)
	{
		if (services is null)
			throw new ArgumentNullException(nameof(services));
		services.AddSingleton<IBlobStorage>(new BlobStorage());
	}
}