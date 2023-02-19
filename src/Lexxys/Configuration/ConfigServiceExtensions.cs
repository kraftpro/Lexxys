using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys
{
	using Configuration;

	using Lexxys;

	public static class ConfigServiceExtensions
	{
		public static IServiceCollection AddConfigService(this IServiceCollection services, Action<IConfigService>? config = default)
		{
			if (services is null)
				throw new ArgumentNullException(nameof(services));
			if (services.Any(o => o.ServiceType == typeof(IConfigService)))
				return services;

			var provider = new ConfigProvidersCollection();
			services.AddSingleton<IConfigService>(provider);
			services.AddSingleton<IConfigSource>(provider);
			services.AddSingleton<IConfigLogger>(provider);
			services.AddSingleton(typeof(IConfigSection), typeof(ConfigSection));

			config?.Invoke(provider);

			return services;
		}

		public static IConfigService AddConfiguration(this IConfigService service, string path, IReadOnlyCollection<string>? parameters = null, bool tail = false)
		{
			if (service is null)
				throw new ArgumentNullException(nameof(service));
			if (path is null or { Length: 0 })
				throw new ArgumentNullException(nameof(path));

			service.AddConfiguration(new Uri(path, UriKind.RelativeOrAbsolute), parameters, tail);
			return service;
		}
	}
}
