using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Lexxys.Configuration;

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
	private volatile List<IConfigSource> _providers = new List<IConfigSource>();
	private readonly object _syncObj = new Object();

	public ConfigProvidersCollection()
	{
		_version = 1;
		Factory.AssemblyLoad += FactoryAssemblyLoad;

		void FactoryAssemblyLoad(object? sender, AssemblyLoadEventArgs e)
		{
			var assembly = e?.LoadedAssembly;
			if (assembly == null)
				return;
			var location = GetConfigurationLocation(assembly);
			if (location != null)
				AddConfiguration(location);
		}
	}

	public ConfigProvidersCollection(IEnumerable<IConfigSource> providers) : this()
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
		foreach (IConfigSource item in providers)
		{
			value = item.GetValue(key, objectType);
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
		foreach (IConfigSource item in providers)
		{
			var x = item.GetList<T>(key);
			if (x.Count <= 0) continue;
			if (temp == null)
				temp = x;
			else
				(list ??= new List<T>(temp)).AddRange(x);
		}
		IReadOnlyList<T> result =
			temp == null ? Array.Empty<T>():
			list == null ? temp: ReadOnly.Wrap(list)!;
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

	public bool AddConfiguration(IConfigSource provider, bool tail = false)
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

	bool IEquatable<IConfigSource>.Equals(IConfigSource? other) => ReferenceEquals(this, other);

	private void OnChanged() => OnChanged(null, ConfigurationEventArgs.Default);

	public int AddConfiguration(IConfigSource provider, int position)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		int i = AddProvider(provider, position);
		if (i >= 0)
		{
			LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(provider.ToString(), i));
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
			throw;
		}
	}

	private List<IConfigSource?> CreateProviders(Uri location, IReadOnlyCollection<string>? parameters)
	{
		if (location is null)
			throw new ArgumentNullException(nameof(location));
		if (!_initialized)
			Initialize();

		var result = new List<IConfigSource?>();
		var files = !location.IsAbsoluteUri || location.IsFile ? Config.GetLocalFiles(location.IsAbsoluteUri ? location.LocalPath: location.OriginalString, ConfigurationDirectories): [location];
		foreach (var file in files)
		{
			try
			{
				var provider = TryCreateProvider(file, parameters);
				if (provider == null)
					continue;
				result.Add(_providers.Any(o => o.Equals(provider)) ? null: provider);
			}
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
				throw;
			}
		}
		return result;
	}

	internal static IConfigSource? TryCreateProvider(Uri location, IReadOnlyCollection<string>? parameters)
		=> TryCreateInstance<IConfigSource>(__methodNames, __locationType2, [location, parameters]);
	private static readonly Type[] __locationType2 = [typeof(Uri), typeof(IReadOnlyCollection<string>)];
	private static readonly string[] __methodNames = ["Create", "TryCreate"];


	private static T? TryCreateInstance<T>(string[] methods, Type[] parametersType, object?[]? parameters)
	{
		foreach (var method in GetConstructors(methods, parametersType))
		{
			try
			{
				var obj = method.Invoke(null, parameters);
				if (obj is T result)
					return result;
			}
			catch (Exception flaw)
			{
				Config.LogConfigurationError($"{nameof(TryCreateInstance)}, {String.Join(", ", methods)}({DumpWriter.Create().Dump(parameters)})", flaw);
			}
		}
		return default;

		static List<MethodInfo> GetConstructors(string[] methods, Type[] parametersType)
			=> MethodsCache<T>.Cache ??= Factory.Types(typeof(T))
				.SelectMany(t => methods
					.Select(m => t.GetMethod(m, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parametersType, null)!)
					.Where(o => o != null && typeof(T).IsAssignableFrom(o.ReturnType)))
				.ToList();
	}

	private static class MethodsCache<T>
	{
		public static List<MethodInfo>? Cache;
	}

	private int AddProvider(IConfigSource provider, int position)
	{
		List<IConfigSource> providers;
		List<IConfigSource> updated;
		int inserted;
		do
		{
			providers = _providers;
			var i = providers.FindIndex(o => o.Equals(provider));
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

	private void ScanConfigurationFile(IConfigSource provider, int position)
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
			if (value is null) throw new ArgumentNullException(nameof(value));
			
			value = value.Trim();
			var xx = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
			return xx.Length switch
			{
				1 => (value, null),
				2 => (xx[0], new[] { xx[1] }),
				_ => (xx[0], xx.Skip(1).ToList())
			};
		}
	}
	private static readonly char[] SpaceSeparator = [' '];

	private void OnChanged(object? sender, ConfigurationEventArgs e)
	{
		if (!_initialized) return;

		Interlocked.Increment(ref _version);
		if (sender is IXmlConfigurationSource cs)
			LogConfigurationEvent(LogSource, SR.ConfigurationChanged(cs));
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
		lock (_syncObj)
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
			Lxx.OnConfigurationInitialized(this, ConfigurationEventArgs.Default);
			OnChanged();
		}
	}

	private void AddSystem(Uri locator, IConfigSource provider, int position)
	{
		position = AddProvider(provider, position);
		if (position < 0)
			return;
		LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(provider.ToString(), position));
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
	private static readonly char[] __separators = [',', ';', ' ', '\t', '\n', '\r'];
#endif

	private static Uri? GetConfigurationLocation(Assembly? assembly)
	{
		if (assembly is null || assembly.IsDynamic || String.IsNullOrEmpty(assembly.Location)) return null;
		if (!TryConfigFile(assembly, out var name) && !TryMetadata(assembly, out name)) return null;

		var path = String.IsNullOrEmpty(name) ?
			Path.ChangeExtension(assembly.Location, null):
			Path.Combine(Path.GetDirectoryName(assembly.Location) ?? String.Empty, name);
		return new Uri(path);

		static bool TryConfigFile(Assembly assembly, out string? name)
		{
			if (!assembly.IsDefined(typeof(ConfigFileAttribute), false))
			{
				name = null;
				return false;
			}
			var a = assembly.GetCustomAttributes(typeof(ConfigFileAttribute), false)
				.OfType<ConfigFileAttribute>().FirstOrDefault();
			name = a?.Name;
			return a != null;
		}

		static bool TryMetadata(Assembly assembly, out string? name)
		{
			var a = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
				.OfType<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "ConfigFile");
			name = a?.Value;
			return a != null;
		}
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
		cc.AddRange(AppDomain.CurrentDomain.GetAssemblies().Select(GetConfigurationLocation).Where(o => o != null)!);

		var locator = new Uri("file:///" + Lxx.HomeDirectory + Path.DirectorySeparatorChar + Lxx.AnonymousConfigurationFile);
		cc.Add(locator);
		return cc;
	}
}
