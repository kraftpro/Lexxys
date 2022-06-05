using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
			IReadOnlyCollection<Type>? SupportedTypes { get; }
			bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result);
		}

		private volatile static Dictionary<Type, IFactory[]> __factoryMap = new Dictionary<Type, IFactory[]>();

		//static StaticServices()
		//{
		//	IFactory factory = new DummyConfigFactory();
		//	foreach (var type in factory.SupportedTypes!)
		//	{
		//		var mapType = type is null ? typeof(void): type.IsGenericType ? type.GetGenericTypeDefinition(): type;
		//		__factoryMap.Add(mapType, new IFactory[] { factory });
		//	}
		//	factory = new DummyLoggingFactory();
		//	foreach (var type in factory.SupportedTypes!)
		//	{
		//		var mapType = type is null ? typeof(void): type.IsGenericType ? type.GetGenericTypeDefinition(): type;
		//		__factoryMap.Add(mapType, new IFactory[] { factory });
		//	}
		//}

		public static void AddFactory(IFactory? factory)
		{
			if (factory == null)
				return;
			Dictionary<Type, IFactory[]> local, next;
			var supportedTypes = factory.SupportedTypes ?? __void;
			do
			{
				local = __factoryMap;
				next = new Dictionary<Type, IFactory[]>(local);
				foreach (var type in supportedTypes)
				{
					var mapType = type is null ? typeof(void): type.IsGenericType ? type.GetGenericTypeDefinition(): type;
					if (next.TryGetValue(mapType, out var list))
					{
						var tmp = new IFactory[list.Length + 1];
						tmp[0] = factory;
						Array.Copy(list, 0, tmp, 1, list.Length);
						next[mapType] = tmp;
					}
					else
					{
						next.Add(mapType, new IFactory[] { factory });
					}
				}
			} while (Interlocked.CompareExchange(ref __factoryMap, next, local) != local);
		}
		private static Type[] __void = new[] { typeof(void) };

		public static bool TryCreate(Type serviceType, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		{
			if (serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));
			if (serviceType == typeof(void))
				throw new ArgumentOutOfRangeException(nameof(serviceType));

			var mapType = serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition(): serviceType;
			if (__factoryMap.TryGetValue(mapType, out var list))
			{
				foreach (var item in list)
				{
					if (item.TryCreate(serviceType, arguments, out result))
						return true;
				}
			}
			if (__factoryMap.TryGetValue(typeof(void), out list))
			{
				foreach (var item in list)
				{
					if (item.TryCreate(serviceType, arguments, out result))
						return true;
				}
			}
			result = null!;
			return false;
		}

		public static bool TryCreate<T>(object?[]? arguments, [MaybeNullWhen(false)] out T result) where T: class
		{
			if (TryCreate(typeof(T), arguments, out var obj))
			{
				result = (T)obj;
				return true;
			}
			result = default;
			return false;
		}

		public static T? TryCreate<T>(params object?[]? arguments) where T: class
			=> TryCreate<T>(arguments, out var result) ? result: default;

		public static object? TryCreate(Type serviceType, params object?[] arguments)
			=> TryCreate(serviceType, arguments, out var obj) ? obj: null;

		public static object Create(Type serviceType, params object?[]? arguments)
			=> TryCreate(serviceType, arguments, out var result) ? result : throw new InvalidOperationException($"Cannot create service of type {serviceType.FullName}.");


		public static IConfigService ConfigFactory => ConfigService.Default;

		public static IConfigSection Config => Create<IConfigSection>();

		public static IConfigService GetConfigFactory() => Create<IConfigService>();

		public static T Create<T>(params object?[] arguments) where T : class => (T)Create(typeof(T), arguments);

		#region Specific services

		public static LoggingFactoryFacade Logger => default;

		public static ILogging GetLogger(string name)
			=> Create<ILogging>(name);

		public static ILogging GetLogger<T>()
			=> Create<ILogging<T>>();

		public static IConfigSection GetConfig(string? name = null)
			=> Create<IConfigSection>(name);

		public static void AddFactory(ILoggerFactory? factory) => AddFactory(MsLoggerFactory.CreateFactory(factory));

		public static void AddLoggerFactory(IServiceProvider serviceProvider) => AddFactory((ILoggerFactory?)serviceProvider.GetService(typeof(ILoggerFactory)));

		public static void AddConfigurationFactory(IConfiguration configuration) => AddFactory(MsConfigFactory.CreateFactory(configuration));

		public struct LoggingFactoryFacade
		{
			public ILogging Create(string name) => StaticServices.Create<ILogging>(name);
			public ILogging? TryCreate(string name) => StaticServices.TryCreate<ILogging>(name);
			public ILogging<T> Create<T>() => StaticServices.Create<ILogging<T>>();
			public ILogging? TryCreate<T>() => StaticServices.TryCreate<ILogging<T>>();
		}

		#endregion

		#region IFactory implementation for the specific services

		//class InternalLoggerFactory : IFactory
		//{
		//	public static readonly IFactory Instance = new InternalLoggerFactory();

		//	private InternalLoggerFactory()
		//	{
		//	}

		//	public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
		//	private readonly Type[] _supportedTypes = new Type[] { typeof(Logger), typeof(Logger<>) };

		//	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		//	{
		//		if (type.IsAssignableFrom(typeof(Logger)))
		//		{
		//			result = new Logger(arguments?.Length > 0 ? arguments[0]?.ToString() ?? "Log": "Log");
		//			return true;
		//		}
		//		if (type.IsGenericType)
		//		{
		//			var pp = type.GetGenericArguments();
		//			if (pp.Length == 1)
		//			{
		//				var lt = typeof(Logger<>).MakeGenericType(pp[0]);
		//				if (type.IsAssignableFrom(lt))
		//				{
		//					result = Activator.CreateInstance(lt);
		//					return result != null;
		//				}
		//			}
		//		}
		//		result = null;
		//		return false;
		//	}
		//}

		//class ConfigFactory: IFactory
		//{
		//	public static readonly IFactory Instance = new ConfigFactory();

		//	private ConfigFactory()
		//	{
		//	}

		//	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		//	{
		//		if (type.IsAssignableFrom(typeof(Configuration.IConfigSection)))
		//		{
		//			result = Config.Current;
		//			return true;
		//		}
		//		result = null;
		//		return false;
		//	}
		//}

		class MsLoggerFactory: IFactory
		{
			private readonly ILoggerFactory _factory;

			public MsLoggerFactory(ILoggerFactory factory)
			{
				_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			}

			public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
			private readonly Type[] _supportedTypes = new Type[] { typeof(ILogger), typeof(ILogger<>) };

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type == typeof(ILogger))
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

				var loggerType = typeof(Microsoft.Extensions.Logging.Logger<>).MakeGenericType(type.GetGenericArguments());
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


			public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
			private readonly Type[] _supportedTypes = new Type[] { typeof(IConfigSource), typeof(IConfigSection) };

			public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
			{
				if (type == typeof(IConfigSource))
					result = new MsConfig(_configuration);
				else if (type == typeof(IConfigSection))
					result = new ConfigSection(new MsConfig(_configuration));
				else
					result = null!;
				return result != null;
			}

			public static IFactory? CreateFactory(IConfiguration? configuration) => configuration == null ? null : new MsConfigFactory(configuration);

			sealed class MsConfig: IConfigSource, IDisposable
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

		#region Logging and Config stub

		//private class DummyLoggingFactory: IFactory
		//{
		//	public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
		//	private readonly Type[] _supportedTypes = new Type[] { typeof(ILogger), typeof(ILogger<>), typeof(ILogging), typeof(ILogging<>) };

		//	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		//	{
		//		if (type.IsAssignableFrom(typeof(ILogging)))
		//		{
		//			result = DummyLogging.Instance;
		//			return true;
		//		}
		//		if (!type.IsGenericType)
		//		{
		//			result = null;
		//			return false;
		//		}
		//		var generic = type.GetGenericTypeDefinition();
		//		if (type == typeof(ILogging<>) || type == typeof(ILogger<>))
		//			result = type.GetField("Instance", System.Reflection.BindingFlags.Static)?.GetValue(null);
		//		else
		//			result = null;
		//		return result != null;
		//	}
		//}

		//private class DummyConfigFactory: IFactory
		//{
		//	public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
		//	private readonly Type[] _supportedTypes = new Type[] { typeof(IConfigSection) };

		//	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		//	{
		//		if (type.IsAssignableFrom(typeof(IConfigSection)))
		//			result = DummyConfigSection.Instance;
		//		else
		//			result = null;
		//		return result != null;
		//	}
		//}

		//private class DummyLogging<T>: DummyLogging, ILogging<T>
		//{
		//	public static new readonly ILogging<T> Instance = new DummyLogging<T>();

		//	private DummyLogging()
		//	{
		//	}
		//}

		//private class DummyLogging: ILogging
		//{
		//	public static readonly ILogging Instance = new DummyLogging();

		//	protected DummyLogging()
		//	{
		//	}

		//	public string Source => "Dummy";

		//	public IDisposable BeginScope<TState>(TState state) => default!;

		//	public IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args) => default;

		//	public bool IsEnabled(LogType logType) => false;

		//	public bool IsEnabled(LogLevel logLevel) => false;

		//	public void Log(LogRecord record) { }

		//	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

		//	public IDisposable? Timing(LogType logType, string? description, TimeSpan threshold) => default;
		//}

		//private class DummyConfigSection: IConfigSection
		//{
		//	public static readonly IConfigSection Instance = new DummyConfigSection();

		//	private DummyConfigSection()
		//	{
		//	}

		//	public int Version => 0;

		//	public event EventHandler<ConfigurationEventArgs>? Changed;

		//	public IValue<IReadOnlyList<T>> GetCollection<T>(string? key) => (Ref<IReadOnlyList<T>>)Array.Empty<T>();

		//	public IConfigSection GetSection(string? key) => this;

		//	public IValue<T> GetValue<T>(string? key, Func<T>? defaultValue = null) => (Ref<T>)(defaultValue is null ? default!: defaultValue());

		//	public void MapPath(string key, string value) { }

		//	public void SetCollection<T>(string? key, IReadOnlyList<T> value) { }

		//	public void SetValue<T>(string? key, T value) { }
		//}

		#endregion
	}
}
