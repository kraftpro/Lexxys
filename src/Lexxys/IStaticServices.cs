using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public interface IStaticServices
{
	bool IsInitialized { get; }
	IServiceProvider ServiceProvider { get; }
	bool ContainsService(Type? serviceType);
	bool AddService(ServiceDescriptor? service, bool unique = false);
}

public static partial class StaticServicesExtensions
{
	extension(IStaticServices staticServices)
	{
		public bool AddServices(IEnumerable<ServiceDescriptor>? services, bool unique = false)
		{
			if (services == null)
				return false;

			bool added = false;
			foreach (var item in services)
			{
				added |= staticServices.AddService(item, unique);
			}
			return added;
		}
	}

	public static bool ContainsService<T>(this IStaticServices staticServices) where T: class
	{
		if (staticServices == null)
			throw new ArgumentNullException(nameof(staticServices));
		return staticServices.ContainsService(typeof(T));
	}
}
