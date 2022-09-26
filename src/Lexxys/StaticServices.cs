using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexxys
{

	public sealed class StaticServices: IStaticServices
	{
		private readonly IServiceCollection _collection = new ServiceCollection();
		private ServiceProvider? _provider;

		internal StaticServices()
		{
		}

		public bool ServiceInitialized => _provider != null;

		public IServiceProvider ServiceProvider => _provider ??= _collection.BuildServiceProvider();

		public void AppendServices(IEnumerable<ServiceDescriptor>? services, bool safe = false)
		{
			if (_provider != null)
				if (safe)
					return;
				else
					throw new InvalidOperationException("The service provider has been already initialized.");

			if (services == null)
				return;

			foreach (var item in services)
			{
				if (item.Lifetime == ServiceLifetime.Scoped)
					continue;
				if (safe)
					_collection.TryAdd(item);
				else
					_collection.Add(item);
			}
		}
	}
}
