// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public static class BlobStorageFactoryExtensions
{
	/// <summary>
	/// Returns <see cref="IBlobStorageService"/> for the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="factory">The blob storage factory</param>
	/// <param name="location">The blob location</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException">Factory does not contain provider for the specified <paramref name="location"/>.</exception>
	public static IBlobStorageService GetProvider(this IBlobStorageFactory factory, Uri location)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));

		return factory.TryGetService(location) ?? throw new ArgumentOutOfRangeException(nameof(location), location, $"Cannot find {nameof(IBlobStorageService)} for {location}.");
	}

	/// <summary>
	/// Registers <see cref="IBlobStorageFactory"/> factory as a singleton service.
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void AddBlobStorage(this IServiceCollection services)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));

		services.AddSingleton<IBlobStorageFactory>(new BlobStorageFactory());
	}
}