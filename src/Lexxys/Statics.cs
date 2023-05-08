using Lexxys.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexxys
{
	public static class Statics
	{
		public static readonly IStaticServices Instance = new StaticServices();

		public static IServiceProvider Services => Instance.ServiceProvider;

		public static object? TryGetService(Type serviceType)
		{
			if (serviceType is null)
				throw new ArgumentNullException(nameof(serviceType));

			return Instance.ServiceProvider.GetService(serviceType);
		}

		public static object GetService(Type serviceType)
		{
			if (serviceType is null)
				throw new ArgumentNullException(nameof(serviceType));

			return Instance.ServiceProvider.GetService(serviceType) ?? throw new InvalidOperationException($"Cannot create service of type {serviceType.FullName}.");
		}

		public static T? TryGetService<T>() where T : class => Instance.ServiceProvider.GetService<T>();

		public static T GetService<T>() where T : class => Instance.ServiceProvider.GetService<T>() ?? throw new InvalidOperationException($"Cannot create service of type {typeof(T).FullName}.");

		public static void AddServices(IEnumerable<ServiceDescriptor> services) => Instance.AppendServices(services, false);

		public static void TryAddServices(IEnumerable<ServiceDescriptor> services) => Instance.AppendServices(services, true);

		public static IServiceCollection AddServices(Func<IServiceCollection, IServiceCollection>? settings = null)
		{
			IServiceCollection sc = new ServiceCollection();
			sc = settings?.Invoke(sc) ?? sc;
			Instance.AppendServices(sc, true);
			return sc;
		}

		#region Configuration

		public static IConfigSection Config => _config ??= Statics.GetService<IConfigSection>();
		private static IConfigSection? _config;

		#endregion

		#region ILogger

		public static ILogger? TryGetLogger(string source)
		{
			var logger = TryGetService<ILogger>();
			if (logger is ILogging logging)
				logging.Source = source;
			return logger;
		}

		public static ILogger? TryGetLogger<T>() => TryGetService<ILogger<T>>();

		public static ILogger GetLogger(string source) => TryGetLogger(source) ?? throw new InvalidOperationException($"Cannot create logger for {source}.");

		public static ILogger GetLogger<T>() => TryGetLogger<T>() ?? throw new InvalidOperationException($"Cannot create logger for {typeof(T).GetTypeName()}.");

		#endregion
	}

	public static class StaticServicesExtensions
	{
		public static void AddStatics(this IServiceCollection services)
		{
			if (services is null)
				throw new ArgumentNullException(nameof(services));
			Statics.AddServices(services);
		}
	}

}
