using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.Extensions.DependencyInjection;

namespace Lexxys
{
	using Configuration;

	public static class IConfigServiceExtensions
	{
		public static IServiceCollection AddConfigService(this IServiceCollection services)
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

			return services;
		}

	}
}
