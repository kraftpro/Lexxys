using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lexxys.Logging;

public static class LoggingServicesExtenstions
{
	public static IServiceCollection AddLoggingService(this IServiceCollection services, Action<ILoggingParameters>? configure = default)
	{
		if (services is null)
			throw new ArgumentNullException(nameof(services));
		if (services.Any(o => o.ServiceType == typeof(ILoggingService)))
			return services;

		// TODO: Read configuration and add Loggers into the parameters
		var parameters = new LoggingParameters(services);

		configure?.Invoke(parameters);

		// TODO: Register Logging services
		// services.AddSingleton(typeof(ILogger<>), typeof(Lexxys.Logging.Logger<>));

		services.AddSingleton<ILoggingService>(new LogRecordService(parameters));
		services.AddSingleton(typeof(ILogger<>), typeof(Lexxys.Logging.Logger<>));
		services.AddSingleton(typeof(ILogging<>), typeof(Lexxys.Logging.Logger<>));

		return services;
	}
}
