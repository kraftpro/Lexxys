using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexxys;
using Configuration;

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
		=> TryGetService(serviceType) ?? throw new InvalidOperationException($"Cannot create service of type {serviceType.FullName}.");

	public static object? TryGetKeyedService(Type serviceType, object? serviceKey)
	{
		if (serviceType is null)
			throw new ArgumentNullException(nameof(serviceType));
		return Instance.ServiceProvider is IKeyedServiceProvider ksp ? ksp.GetKeyedService(serviceType, serviceKey): null;
	}

	public static object GetKeyedService(Type serviceType, object? serviceKey)
		=> TryGetKeyedService(serviceType, serviceKey) ?? throw new InvalidOperationException($"Cannot create keyed service of type {serviceType.FullName} and key {serviceKey}.");

	public static T? TryGetService<T>() where T: class => (T?)TryGetService(typeof(T));

	public static T GetService<T>() where T: class => (T)GetService(typeof(T));

	public static T? TryGetKeyedService<T>(object? serviceKey) where T: class => (T?)TryGetKeyedService(typeof(T), serviceKey);

	public static T GetKeyedService<T>(object? serviceKey) where T: class => (T)GetKeyedService(typeof(T), serviceKey);

	public static bool TryAddServices(IEnumerable<ServiceDescriptor> services, bool unique = false) => Instance.IsInitialized ? false: Instance.AddServices(services, unique);

	public static bool AddServices(IEnumerable<ServiceDescriptor> services, bool unique = false) => Instance.AddServices(services, unique);

	public static IServiceCollection AddServices(Func<IServiceCollection, IServiceCollection>? settings = null)
	{
		IServiceCollection sc = new ServiceCollection();
		sc = settings?.Invoke(sc) ?? sc;
		Instance.AddServices(sc, true);
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
	public static IServiceCollection UseStatics(this IServiceCollection services)
	{
		if (services is null)
			throw new ArgumentNullException(nameof(services));
		Statics.AddServices(services);
		return services;
	}
}
