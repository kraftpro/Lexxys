using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexxys;

public sealed class StaticServices: IStaticServices
{
	private readonly IServiceCollection _collection = new ServiceCollection();
	private ServiceProvider? _provider;

	internal StaticServices()
	{
	}

	public bool IsInitialized => _provider != null || _collection.Count > 0;

	public IServiceProvider ServiceProvider => _provider ??= _collection.BuildServiceProvider();

	public bool ContainsService(Type? serviceType)
		=> serviceType != null && _collection.Any(s => s.ServiceType == serviceType);

	public bool AddService(ServiceDescriptor? service, bool unique = false)
	{
		if (_provider != null)
			throw new InvalidOperationException("The service provider has been already initialized.");

		if (service == null || service.Lifetime == ServiceLifetime.Scoped)
			return false;

		int n = _collection.Count;
		if (unique)
			_collection.TryAdd(service);
		else
			_collection.Add(service);
		return _collection.Count > n;
	}
}
