using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public interface IStaticServices
{
	bool IsInitialized { get; }
	IServiceProvider ServiceProvider { get; }

	bool AddServices(IEnumerable<ServiceDescriptor>? services, bool unique = false);
}