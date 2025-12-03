using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Lexxys;

using Configuration;

public static class ConfigServiceExtensions
{
	public static IServiceCollection AddConfigService(this IServiceCollection services, Action<IConfigService>? config = default)
	{
		if (services is null) throw new ArgumentNullException(nameof(services));
		if (services.Any(o => o.ServiceType == typeof(IConfigService))) return services;

		var service = new ConfigService();
		services.AddSingleton<IConfigService>(service);
		services.AddSingleton<IConfigSource>(service);
		services.AddSingleton<IConfigLogger>(service);
		services.AddSingleton<IConfigSection, ConfigSection>();

		config?.Invoke(service);

		return services;
	}

	public static IConfigService AddConfiguration(this IConfigService service, string path, IReadOnlyCollection<string>? parameters = null, bool tail = false)
	{
		if (service is null) throw new ArgumentNullException(nameof(service));
		if (path is null or { Length: 0 }) throw new ArgumentNullException(nameof(path));

		Uri location = new Uri(path, UriKind.RelativeOrAbsolute);
		if (!location.IsAbsoluteUri)
		{
			var fullPath = Path.GetFullPath(path);
			location = new Uri("file:///" + fullPath, UriKind.RelativeOrAbsolute);
			if (!location.IsAbsoluteUri)
				return service;
		}

		service.AddConfiguration(location, parameters, tail);
		return service;
	}

#warning REWRITE
	public static bool AddConfiguration(this IConfigService service, Uri path, IReadOnlyCollection<string>? parameters = null, bool tail = false)
	{
		if (service is null) throw new ArgumentNullException(nameof(service));
		if (path is null) throw new ArgumentNullException(nameof(path));

		return path.Scheme switch
		{
			"file" => AddConfig(service, LocalFileConfigurationSource.TryCreate(path, parameters)),
			"string" => AddConfig(service, StringConfigurationSource.TryCreate(path, parameters)),
			"http" or "https" => AddConfig(service, HttpConfigurationSource.TryCreate(path, parameters)),
			_ => false
		};

		static bool AddConfig(IConfigService service, IXmlConfigurationSource? source)
		{
			if (source == null)
				return false;
			service.AddConfiguration(new XmlConfigurationProvider(source));
			return true;
		}
	}
}
