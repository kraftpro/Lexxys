using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Lexxys.Configuration
{
	internal class ConfigProvidersCollection: IConfigService, IConfigLogger
	{
		public static readonly IConfigService Instance = new ConfigProvidersCollection();

		private const string LogSource = "Lexxys.Configuration";
#if NETFRAMEWORK
		private const string InitialLocationKey = "configuration";
		private const string ConfigurationDirectoryKey = "configurationDirectory";
#endif

		private bool _initializing;
		private bool _initialized;
		private ILogger? _log;
		private ConcurrentQueue<EventEntry>? _messages;
		private volatile int _version;
		private int _top;
		private volatile List<IConfigProvider> _providers = new List<IConfigProvider>();
		private readonly object SyncObj = new Object();

		public ConfigProvidersCollection()
		{
			Factory.AssemblyLoad += Factory_AssemblyLoad;

			void Factory_AssemblyLoad(object? sender, AssemblyLoadEventArgs e)
			{
				var assembly = e?.LoadedAssembly;
				if (assembly == null)
					return;
				var location = GetConfigurationLocation(assembly);
				if (location != null)
					AddConfiguration(location);
			}
		}

		public ConfigProvidersCollection(IEnumerable<IConfigProvider> providers) : this()
		{
			if (providers == null)
				throw new ArgumentNullException(nameof(providers));
			foreach (var provider in providers)
			{
				AddConfiguration(provider);
			}
		}

		private IReadOnlyList<string>? ConfigurationDirectories => _configDirectories.Value;
		private readonly Lazy<IReadOnlyList<string>?> _configDirectories = new Lazy<IReadOnlyList<string>?>(GetConfigurationDirectories, true);

		#region IConfigService

		public int Version => _version;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		public object? GetValue(string key, Type objectType)
		{
			if (objectType is null)
				throw new ArgumentNullException(nameof(objectType));
			if (key is null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));

			if (!_initialized)
				Initialize();

			object? value = null;
			var providers = _providers;
			for (int i = 0; i < providers.Count; ++i)
			{
				value = providers[i].GetValue(key, objectType);
				if (value != null)
					break;
			}
			return value;
		}

		public IReadOnlyList<T> GetList<T>(string key)
		{
			if (key is null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));

			if (!_initialized)
				Initialize();

			IReadOnlyList<T>? temp = null;
			List<T>? list = null;
			var providers = _providers;
			for (int i = 0; i < providers.Count; ++i)
			{
				IReadOnlyList<T> x = providers[i].GetList<T>(key);
				if (x.Count <= 0) continue;
				if (temp == null)
					temp = x;
				else
					(list ??= new List<T>(temp)).AddRange(x);
			}
			IReadOnlyList<T> result =
				temp == null ? Array.Empty<T>() :
				list == null ? temp : ReadOnly.Wrap(list)!;
			return result;
		}

		public bool AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null, bool tail = false)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			if (!_initialized)
				Initialize();

			var added = AddConfiguration(location, parameters, tail ? int.MaxValue : _top);
			if ((added & Found.Added) != 0)
				OnChanged();
			return added != 0;
		}

		public bool AddConfiguration(IConfigProvider provider, bool tail = false)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (!_initialized)
				Initialize();

			return AddConfiguration(provider, tail ? int.MaxValue : _top) >= 0;
		}

		#endregion

		#region IConfigLogger

		public void LogConfigurationError(string logSource, Exception exception)
		{
			if (logSource is null)
				throw new ArgumentNullException(nameof(logSource));
			if (exception is null)
				throw new ArgumentNullException(nameof(exception));
			LogConfigurationItem(new EventEntry(exception, logSource));
		}

		public void LogConfigurationEvent(string logSource, string message)
		{
			if (logSource is null)
				throw new ArgumentNullException(nameof(logSource));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			LogConfigurationItem(new EventEntry(message, logSource));
		}

		public void SetLogger(ILogger? logger = null)
		{
			logger ??= Statics.TryGetLogger(LogSource);
			if (_log == logger || logger == null)
				return;
			_log = logger;

			if (_messages == null)
				return;
			var messages = Interlocked.Exchange(ref _messages, null);
			if (messages == null)
				return;

			while (!messages.IsEmpty)
			{
				if (messages.TryDequeue(out var message))
					LogEventEntry(logger, message);
			}
		}

		#endregion

		private void OnChanged() => OnChanged(null, new ConfigurationEventArgs());

		private int AddConfiguration(IConfigProvider provider, int position)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			int i = AddProvider(provider, position);
			if (i >= 0)
			{
				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(provider.Location, i));
				ScanConfigurationFile(provider, i);
				provider.Changed += OnChanged;
				OnChanged();
			}
			return i;
		}

		[Flags]
		enum Found
		{
			None = 0,
			Found = 1,
			Added = 2
		}

		private Found AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters, int position)
		{
			try
			{
				Found found = default;
				foreach (var provider in CreateProviders(location, parameters))
				{
					if (provider == null)
					{
						found |= Found.Found;
						continue;
					}
					int i = AddConfiguration(provider, position);
					found |= i >= 0 ? Found.Added: Found.Found;
					if (i >= 0)
						position = i;
				}
				return found;
			}
			#pragma warning disable CA1031 // Ignore any errors
			catch (Exception flaw)
			{
				flaw = flaw.Unwrap()
					.Add(nameof(location), location);
				if (parameters != null)
					flaw.Add(nameof(parameters), parameters.ToArray());
				if (_log == null)
					SystemLog.WriteErrorMessage($"ERROR: Cannot load configuration {location}.", flaw);
				else
					_log.Error(nameof(AddConfiguration), flaw);
				return Found.None;
			}
			#pragma warning restore CA1031 // Do not catch general exception types
		}

		private List<IConfigProvider?> CreateProviders(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));
			if (!_initialized)
				Initialize();

			var result = new List<IConfigProvider?>();
			var files = !location.IsAbsoluteUri || location.IsFile ? Config.GetLocalFiles(location.IsAbsoluteUri ? location.LocalPath: location.OriginalString, ConfigurationDirectories): new[] { location };
			foreach (var file in files)
			{
				try
				{
					int i = _providers.FindIndex(o => o.Location == file);
					if (i >= 0)
					{
						result.Add(null);
						continue;
					}
					var provider = TryCreateProvider(file, parameters);
					if (provider == null)
						continue;
					i = _providers.FindIndex(o => o.Location == provider.Location);
					result.Add(i < 0 ? provider: null);
				}
				#pragma warning disable CA1031 // Ignore any errors
				catch (Exception flaw)
				{
					flaw = flaw.Unwrap()
						.Add(nameof(location), location)
						.Add(nameof(file), file);
					if (parameters != null)
						flaw.Add(nameof(parameters), parameters.ToArray());
					if (_log == null)
						SystemLog.WriteErrorMessage($"ERROR: Cannot create configuration from file {file}.", flaw);
					else
						_log.Error(nameof(CreateProviders), flaw);
				}
				#pragma warning restore CA1031 // Do not catch general exception types
			}
			return result;
		}

		private static IConfigProvider? TryCreateProvider(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			var arguments = new object?[] { location, parameters };

			foreach (var constructor in Factory.Constructors(typeof(IConfigProvider), "TryCreate", __locationType2))
			{
				try
				{
					var obj = constructor.Invoke(null, arguments);
					if (obj is IConfigProvider source)
						return source;
				}
#pragma warning disable CA1031 // Ignore all the errors.
				catch (Exception flaw)
				{
					Config.LogConfigurationError($"{nameof(TryCreateProvider)} from {location}", flaw);
				}
#pragma warning restore CA1031 // Do not catch general exception types
			}
			return null;
		}
		private static readonly Type[] __locationType2 = { typeof(Uri), typeof(IReadOnlyCollection<string>) };

		private int AddProvider(IConfigProvider provider, int position)
		{
			List<IConfigProvider> providers;
			List<IConfigProvider> updated;
			int inserted;
			do
			{
				providers = _providers;
				var i = providers.FindIndex(o => o.Location == provider.Location);
				if (i >= 0)
					return ~i;
				updated = new(providers);
				if (position >= updated.Count)
				{
					updated.Add(provider);
					inserted = updated.Count;
				}
				else
				{
					updated.Insert(position, provider);
					inserted = position + 1;
				}
			} while (Interlocked.CompareExchange(ref _providers, updated, providers) != providers);
			return inserted;
		}

		private void ScanConfigurationFile(IConfigProvider provider, int position)
		{
			if (provider.GetValue("applicationDirectory", typeof(string)) is string home)
			{
				var dir = home.Trim().TrimEnd('/', '\\').TrimToNull();
				if (dir != null)
					Lxx.HomeDirectory = dir;
			}

			foreach (string s in provider.GetList<string>("include"))
			{
				int k = _providers.Count;
				var (value, parameters) = SplitOptions(s);
				AddConfiguration(new Uri(value, UriKind.RelativeOrAbsolute), parameters, position);
				position += _providers.Count - k;
			}

			static (string Location, IReadOnlyCollection<string>? Parameters) SplitOptions(string value)
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				value = value.Trim();
				var xx = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
				if (xx.Length == 1)
					return (value, null);
				if (xx.Length == 2)
					return (xx[0], new[] { xx[1] });
				return (xx[0], xx.Skip(1).ToList());
			}
		}
		private static readonly char[] SpaceSeparator = new[] { ' ' };

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			if (!_initialized)
				return;
			Interlocked.Increment(ref _version);
			if (sender != null)
			{
				var cs = sender as IXmlConfigurationSource;
				if (cs != null)
					LogConfigurationEvent(LogSource, SR.ConfigurationChanged(cs));
			}
			Changed?.Invoke(sender, e);
			_log = null;
		}

		private class EventEntry
		{
			public readonly Exception? Exception;
			public readonly string? Message;
			public readonly string Source;

			public EventEntry(Exception exception, string source)
			{
				Exception = exception ?? throw new ArgumentNullException(nameof(exception));
				Source = source ?? throw new ArgumentNullException(nameof(source));
			}

			public EventEntry(string message, string source)
			{
				Message = message ?? throw new ArgumentNullException(nameof(message));
				Source = source ?? throw new ArgumentNullException(nameof(source));
			}
		}

		private static void LogEventEntry(ILogger logger, EventEntry item)
		{
			if (item.Exception != null)
			{
				if (logger.IsEnabled(LogType.Error))
					logger.Error(item.Source, item.Exception);
			}
			else if (item.Message != null)
			{
				if (logger.IsEnabled(LogType.Trace))
					logger.Error(item.Source, item.Message, null, null);
			}
		}

		private void LogConfigurationItem(EventEntry item)
		{
			ILogger? logger = _log;
			if (logger != null)
				LogEventEntry(logger, item);
			else
				(_messages ??= new ConcurrentQueue<EventEntry>()).Enqueue(item);
		}

		private void Initialize()
		{
			if (_initialized || _initializing)
				return;
			lock (SyncObj)
			{
				if (_initialized || _initializing)
					return;

				_initializing = true;
				AddSystem(new Uri("system:environment"), new EnvironmentConfigurationProvider(), 0);
				_top = _providers.Count;
#if NETFRAMEWORK
				AddSystem(new Uri("system:configuration"), new SystemConfigurationProvider(), _top);
#endif
				foreach (var item in Directory.EnumerateFiles(".", "Lexxys.config.*"))
				{
					AddConfiguration(new Uri("file:///" + Path.GetFullPath(item)), null, true);
				}
				IEnumerable<Uri> cc0 = GetInitialConfigurationsLocations();
				foreach (var c in cc0)
				{
					AddConfiguration(c, null, true);
				}
				_initialized = true;
				_initializing = false;
				Lxx.OnConfigurationInitialized(this, new ConfigurationEventArgs());
				OnChanged();
			}
		}

		private void AddSystem(Uri locator, IConfigProvider provider, int position)
		{
			position = AddProvider(provider, position);
			if (position < 0)
				return;
			LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(locator, position));
			ScanConfigurationFile(provider, position);
		}

		private static IReadOnlyList<string> GetConfigurationDirectories()
		{
#if NETFRAMEWORK
			string[]? directories = System.Configuration.ConfigurationManager.AppSettings.GetValues(ConfigurationDirectoryKey);
			if (directories == null)
				return new [] { Lxx.HomeDirectory };
			
			var configurationDirectory = new List<string> { Lxx.HomeDirectory };
			foreach (var entry in directories)
			{
				foreach (var item in entry.Split(__separators, StringSplitOptions.RemoveEmptyEntries))
				{
					var value = item.Trim().TrimEnd(Path.DirectorySeparatorChar);
					if (value.Length > 0 && configurationDirectory.FindIndex(o => String.Equals(o, value, StringComparison.OrdinalIgnoreCase)) < 0)
						configurationDirectory.Add(value);
				}
			}
			return configurationDirectory;
#else
			return new [] { Lxx.HomeDirectory };
#endif
		}
#if NETFRAMEWORK
		private static readonly char[] __separators = { ',', ';', ' ', '\t', '\n', '\r' };
#endif

		private static Uri? GetConfigurationLocation(Assembly? asm)
		{
			string? name;
			if (asm == null || asm.IsDynamic || String.IsNullOrEmpty(name = asm.Location))
				return null;
			return new Uri(Path.ChangeExtension(name, null));
		}

		private static IEnumerable<Uri> GetInitialConfigurationsLocations()
		{
			var cc = new OrderedSet<Uri>();
#if NETFRAMEWORK
			string[]? ss = System.Configuration.ConfigurationManager.AppSettings.GetValues(InitialLocationKey);
			if (ss != null)
			{
				foreach (string s in ss)
				{
					var c = new Uri(s, UriKind.RelativeOrAbsolute);
					if (c.IsAbsoluteUri)
						cc.Add(c);
				}
			}
#endif
			cc.AddRange(Factory.DomainAssemblies.Select(GetConfigurationLocation).Where(o => o != null)!);

			var locator = new Uri("file:///" + Lxx.HomeDirectory + Path.DirectorySeparatorChar + Lxx.AnonymousConfigurationFile);
			cc.Add(locator);
			return cc;
		}
	}
}
