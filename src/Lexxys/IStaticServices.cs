using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Lexxys
{
	public interface IStaticServices
	{
		bool ServiceInitialized { get; }
		IServiceProvider ServiceProvider { get; }

		void AppendServices(IEnumerable<ServiceDescriptor>? services, bool safe = false);
	}
}