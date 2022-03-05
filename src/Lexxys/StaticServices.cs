using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


#nullable enable

namespace Lexxys
{
	public static class StaticServices
	{
		public interface IFactory
		{
			bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result);
		}

		public interface IFactory<T>: IFactory where T: class
		{
		}

		private volatile static IFactory[] __facrotyCollection = new [] { LoggerFactory.Instance, ConfigFactory.Instance };

		public static void AddFactory(IFactory? factory)
		{
			if (factory == null)
				return;
			IFactory[] local, next;
			do
			{
				local = __facrotyCollection;
				next = new IFactory[local.Length + 1];
				next[0] = factory;
				Array.Copy(local, 0, next, 1, local.Length);
			} while (Interlocked.CompareExchange(ref __facrotyCollection, next, local) != local);
		}

		public static bool TryCreate(Type serviceType, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		{
			if (serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));
			foreach (var item in __facrotyCollection)
			{
				if (item.TryCreate(serviceType, arguments, out result))
					return true;
			}
			result = null!;
			return false;
		}

		public static object Create(Type serviceType, params object?[]? arguments)
			=> TryCreate(serviceType, arguments, out var result) ? result : throw new InvalidOperationException($"Cannot create service of type {serviceType.FullName}.");

		#region Specific services

		public static void AddFactory(ILoggerFactory? factory) => AddFactory(MsLoggerFactory.CreateFactory(factory));

		public static void AddLoggerFactory(IServiceProvider serviceProvider) => AddFactory((ILoggerFactory?)serviceProvider.GetService(typeof(ILoggerFactory)));

		public static void AddConfigurationFactory(IConfiguration configuration) => AddFactory(MsConfigFactory.CreateFactory(configuration));

		public static T Create<T>(params object?[] arguments) where T: class => (T)Create(typeof(T), arguments);

		#endregion

		#region IFactory implementation for the specific services

		class LoggerFactory : IFactory
		{
			public static readonly IFactory Instance = new LoggerFactory();

			private LoggerFactory()
			{
			}

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type.IsAssignableFrom(typeof(Logger)))
				{
					result = new Logger(arguments?.Length > 0 ? arguments[0]?.ToString() ?? "Log": "Log");
					return true;
				}
				if (type.IsGenericType)
				{
					var pp = type.GetGenericArguments();
					if (pp.Length == 1)
					{
						var lt = typeof(Logger<>).MakeGenericType(pp[0]);
						if (type.IsAssignableFrom(lt))
						{
							result = Activator.CreateInstance(lt);
							return result != null;
						}
					}
				}
				result = null;
				return false;
			}
		}

		class ConfigFactory: IFactory
		{
			public static readonly IFactory Instance = new ConfigFactory();

			private ConfigFactory()
			{
			}

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type.IsAssignableFrom(typeof(Configuration.IConfigSection)))
				{
					result = Config.Current;
					return true;
				}
				result = null;
				return false;
			}
		}

		class MsLoggerFactory: IFactory
		{
			private readonly ILoggerFactory _factory;

			public MsLoggerFactory(ILoggerFactory factory)
			{
				_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			}

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type.IsAssignableFrom(typeof(ILogger)))
				{
					result = _factory.CreateLogger((arguments?.Length > 0 ? arguments[0]?.ToString() : null) ?? type.GetTypeName(true));
					return result != null;
				}
				if (!type.IsGenericType)
				{
					result = null!;
					return false;
				}
				var generic = type.GetGenericTypeDefinition();
				if (generic != typeof(ILogger<>))
				{
					result = null!;
					return false;
				}

				var loggerType = typeof(Logger<>).MakeGenericType(type.GetGenericArguments());
				var constructor = loggerType.GetConstructor(new Type[] { typeof(ILoggerFactory) });
#if NET6_0_OR_GREATER
				result = constructor?.Invoke(System.Reflection.BindingFlags.DoNotWrapExceptions, null, new[] { _factory }, null);
#else
				result = constructor?.Invoke(new[] { _factory })!;
#endif
				return result != null;
			}

			public static IFactory? CreateFactory(ILoggerFactory? factory) => factory == null ? null: new MsLoggerFactory(factory);
		}

		class MsConfigFactory: IFactory
		{
			private readonly IConfiguration _configuration;

			public MsConfigFactory(IConfiguration configuration)
			{
				_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			}

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type.IsAssignableFrom(typeof(Configuration.IConfigSource)))
				{
					result = new MsConfig(_configuration);
					return true;
				}
				result = null!;
				return false;
			}

			public static IFactory? CreateFactory(IConfiguration? configuration) => configuration == null ? null : new MsConfigFactory(configuration);

			sealed class MsConfig: Configuration.IConfigSource, IDisposable
			{
				private readonly IConfiguration _configuration;
				private readonly IDisposable _changeCollback;
				private volatile int _version;

				public MsConfig(IConfiguration configuration)
				{
					_configuration = configuration;
					var token = _configuration.GetReloadToken();
					_changeCollback = token.RegisterChangeCallback(o =>
					{
						if (o is MsConfig c)
						{
							Interlocked.Increment(ref c._version);
							c.Changed?.Invoke(c, ConfigurationEventArgs.Empty);
						}
					}, this);
				}

				public int Version => _version;

				public event EventHandler<ConfigurationEventArgs>? Changed;

				public IReadOnlyList<T> GetList<T>(string key)
				{
					var section = String.IsNullOrEmpty(key) ? _configuration: _configuration.GetSection(key);
					return (IReadOnlyList<T>)section.Get<List<T>>() ?? Array.Empty<T>();
				}

				public object? GetValue(string key, Type objectType)
				{
					var section = String.IsNullOrEmpty(key) ? _configuration: _configuration.GetSection(key);
					return section.Get(objectType);
				}

				private bool _disposed;
				public void Dispose()
				{
					if (!_disposed)
					{
						_changeCollback?.Dispose();
						_disposed = true;
					}
				}
			}
		}

		#endregion
	}

	public static class StaticServiceFactoryExtensions
	{
		public static bool TryCreate<T>(this StaticServices.IFactory<T> factory, object?[]? arguments, [MaybeNullWhen(false)] out T result) where T: class
		{
			if (factory.TryCreate(typeof(T), arguments, out var obj))
			{
				result = (T)obj;
				return true;
			}
			result = null!;
			return false;
		}
	}
}
