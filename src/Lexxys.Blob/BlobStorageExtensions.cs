// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexxys;

public static class BlobStorageExtensions
{
	/// <summary>
	/// Returns <see cref="IBlobStorageService"/> for the specified <paramref name="domain"/>.
	/// </summary>
	/// <param name="factory">The blob storage factory</param>
	/// <param name="domain">Name of the domain</param>
	/// <returns>Blob storage service to access blobs in the specified domain.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException">Factory does not contain provider for the specified <paramref name="domain"/>.</exception>
	public static IBlobStorageService GetStorage(this IBlobStorageFactory factory, string domain)
	{
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		if (domain is null) throw new ArgumentNullException(nameof(domain));

		return factory.TryGetStorage(domain) ?? throw new ArgumentOutOfRangeException(nameof(domain), domain, $"Cannot find {nameof(IBlobStorageService)} for {domain}.");
	}

	/// <summary>
	/// Registers <see cref="IBlobStorageFactory"/> factory as a singleton service.
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="configure"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddBlobStorageFactory(this IServiceCollection services, Action<IServiceProvider, IBlobStorageFactory>? configure = null)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));

		services.AddSingleton<IBlobStorageFactory>(sp =>
		{
			var factory = new BlobStorageFactory();
			configure?.Invoke(sp, factory);
			return factory;
		});
		return services;
	}

	public static IServiceCollection AddBlobStorage(this IServiceCollection services, string domain, IBlobStorageService service)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));
		if (domain is null) throw new ArgumentNullException(nameof(domain));
		if (service is null) throw new ArgumentNullException(nameof(service));

		IBlobStorageFactory factory;
		var factoryDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IBlobStorageFactory));
		if (factoryDescriptor is null)
		{
			factory = new BlobStorageFactory();
			services.AddSingleton(factory);
		}
		else
		{
			factory = (factoryDescriptor.ImplementationInstance as IBlobStorageFactory)!;
		}
		factory.RegisterStorage(domain, service);
		services.AddKeyedSingleton(domain, service);
		return services;
	}

	public static IServiceCollection AddBlobStorage<T>(this IServiceCollection services, IBlobStorageService service)
	{
		services.AddSingleton<IBlobStorageService<T>>(_ => new BlobService<T>(service));
		return services;
	}

	public static IServiceCollection AddBlobStorage<T>(this IServiceCollection services, Func<IServiceProvider, IBlobStorageService> construct)
	{
		services.AddSingleton<IBlobStorageService<T>>(o => new BlobService<T>(construct(o)));
		return services;
	}

	public static IBlobStorageService With(this IBlobStorageService service, Func<string, string> formatter)
	{
		if (service is null) throw new ArgumentNullException(nameof(service));
		if (formatter is null) throw new ArgumentNullException(nameof(formatter));
		return new BlobService(service, formatter);
	}

	public static IBlobStorageService With(this IBlobStorageService service, Func<string?, string?, string> formatter)
	{
		if (service is null) throw new ArgumentNullException(nameof(service));
		if (formatter is null) throw new ArgumentNullException(nameof(formatter));
		return new BlobService(service, o =>
		{
			int i = o.LastIndexOf('.');
			return i switch
			{
				-1 => formatter(o, null),
				0 => formatter(null, o),
				_ => formatter(o[..i], o[i..]),
			};
		});
	}

	public static IBlobStorageService With(this IBlobStorageService service, IBlobNameFormatter formatter)
	{
		if (service is null) throw new ArgumentNullException(nameof(service));
		if (formatter is null) throw new ArgumentNullException(nameof(formatter));
		return With(service, (n, e) => formatter.CreateName(n, e));
	}
}