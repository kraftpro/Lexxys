using Microsoft.Extensions.DependencyInjection;

namespace Lexxys;

public interface IStaticServices
{
	bool IsInitialized { get; }
	IServiceProvider ServiceProvider { get; }

	void AddServices(IEnumerable<ServiceDescriptor>? services, bool safe = false);
}