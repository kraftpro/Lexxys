// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public interface IBlobStorage
{
	IBlobStorageProvider? TryGetProvider(Uri location);
	void Register(IBlobStorageProvider provider);
}

public static class BlobStorageExtensions
{
	public static IBlobStorageProvider GetProvider(this IBlobStorage factory, Uri location)
		=> factory.TryGetProvider(location) ?? throw new InvalidOperationException($"Cannot find {nameof(IBlobStorageProvider)} for {location}.");

	public static void AddBlobStorage(this IServiceCollection services)
	{
		if (services is null)
			throw new ArgumentNullException(nameof(services));
		services.AddSingleton<IBlobStorage>(new BlobStorage());
	}
}