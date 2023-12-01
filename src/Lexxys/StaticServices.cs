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

	public bool IsInitialized => _provider != null;

	public IServiceProvider ServiceProvider => _provider ??= _collection.BuildServiceProvider();

	public bool AddServices(IEnumerable<ServiceDescriptor>? services, bool unique = false)
	{
		if (_provider != null)
			throw new InvalidOperationException("The service provider has been already initialized.");

		if (services == null)
			return false;

		bool added = false;
		foreach (var item in services)
		{
			if (item.Lifetime == ServiceLifetime.Scoped)
				continue;
			if (unique)
			{
				int n = _collection.Count;
				_collection.TryAdd(item);
				added |= _collection.Count > n;
			}
			else
			{
				_collection.Add(item);
				added = true;
			}
		}
		return added;
	}
}
