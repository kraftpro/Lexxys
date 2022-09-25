using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexxys
{
	public sealed class Statics
	{
		public static readonly Statics Instance = new Statics();

		private readonly IServiceCollection _collection = new ServiceCollection();
		private ServiceProvider? _provider;

		public IServiceProvider ServiceProvider => _provider ??= _collection.BuildServiceProvider();

		public void AppendServices(IEnumerable<ServiceDescriptor> services)
		{
			if (_provider != null)
				throw new InvalidOperationException();

			foreach (var item in services)
			{
				if (item.Lifetime != ServiceLifetime.Scoped)
					_collection.Add(item);
			}
		}

		public static IServiceProvider Services => Instance.ServiceProvider;

		public static object? GetService(Type serviceType) => Instance.ServiceProvider.GetService(serviceType);

		public static T? GetService<T>() => Instance.ServiceProvider.GetService<T>();

		public static void AddServices(IEnumerable<ServiceDescriptor> services) => Instance.AppendServices(services);

		public static ILogger? GetLogger(string source)
		{
			var logger = GetService<ILogger>();
			if (logger is ILogging logging)
				logging.Source = source;
			return logger;
		}

		public static ILogger? GetLogger<T>()
		{
			return GetService<ILogger<T>>();
		}

		//private class NullLogger: ILogging
		//{
		//	public static readonly ILogging Instance = new NullLogger();

		//	protected NullLogger()
		//	{
		//	}

		//	public string Source { get => "Null"; set { } }

		//	public IDisposable BeginScope<TState>(TState state) => default!;

		//	public IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args) => default;

		//	public bool IsEnabled(LogType logType) => false;

		//	public bool IsEnabled(LogLevel logLevel) => false;

		//	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

		//	public void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
		//	{
		//	}

		//	public IDisposable? Timing(LogType logType, string? description, TimeSpan threshold) => default;
		//}

	}
}
