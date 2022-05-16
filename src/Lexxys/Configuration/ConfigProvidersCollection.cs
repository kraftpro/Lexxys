using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys.Configuration
{
	using Logging;

	internal class ConfigProvidersCollection
	{
		private const int UserConfigPosition = 1;

		private const string LogSource = "Lexxys.Configuration";
		private const string ConfigCachingOption = "lexxys.config:cacheValues";
		private const string InitialLocationKey = "configuration";
		private const string ConfigurationDerectoryKey = "configurationDirectory";
		private static readonly string[] Extensions = { ".config.xml", ".config.txt", ".config.ini", ".config.json" };

		private bool _initialized;
		private bool _initializing;
		private ILogger? __log;
		private ConcurrentQueue<EventEntry>? __messages;
		private volatile int __version;

		private volatile List<IConfigProvider> Providers = new List<IConfigProvider>();
		private readonly ConcurrentDictionary<string, object?> Cache = new ConcurrentDictionary<string, object?>();

		ConfigProvidersCollection()
		{
			Factory.AssemblyLoad += Factory_AssemblyLoad;
		}

		public int Version => __version;

		private void Factory_AssemblyLoad(object? sender, AssemblyLoadEventArgs e)
			=> AddConfigLocation(e?.LoadedAssembly);

		public void AddConfigLocation(Assembly? assembly)
		{
			var location = GetConfigurationLocation(assembly);
			if (location != null)
				AddConfiguration(location, null);
		}

		private IReadOnlyList<string> ConfigurationDirectories => __configDirectories ??= GetConfigurationDerectories();
		private IReadOnlyList<string>? __configDirectories;

		internal bool IsInitialized => _initialized;

		public event EventHandler<ConfigurationEventArgs>? Changed;

		private void OnChanged()
		{
			OnChanged(null, new ConfigurationEventArgs());
		}

		public IConfigProvider? AddConfiguration(string location)
		{
			if (location == null || location.Length <= 0)
				throw new ArgumentNullException(nameof(location));
			var (value, parameters) = SplitOptions(location);
			return AddConfiguration(new Uri(value, UriKind.RelativeOrAbsolute), parameters);
		}

		public IConfigProvider? AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			if (AddConfiguration(location, parameters, UserConfigPosition, out var provider))
				OnChanged();
			return provider;
		}

		private bool AddConfiguration1(Uri location, IReadOnlyCollection<string>? parameters, int position, out IConfigProvider? provider)
		{
			try
			{
				if (!CreateProvider1(location, parameters, out provider))
					return false;

				System.Diagnostics.Contracts.Contract.Assume(parameters != null);
				System.Diagnostics.Contracts.Contract.Assert(parameters != null);
				//Debug.Assert(provider != null);
				//Debug.Assert(location != null);

				position = AddProvider(provider!, position);
				if (position < 0)
					return false;

				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(location, position));
				ScanConfigurationFile(provider!, location, position);
				provider!.Changed += OnChanged;
				return true;
			}
			catch (Exception flaw)
			{
				provider = null;
				flaw = flaw.Unwrap();
				flaw.Add("locaion", location);
				if (__log == null)
					Logger.WriteErrorMessage($"ERROR: Cannot load configuration {location?.ToString() ?? "(null)"}.", flaw);
				else
					__log.Error(nameof(AddConfiguration), flaw.Add(nameof(location), location));
				return false;
			}
		}

		private bool CreateProvider1(Uri location, IReadOnlyCollection<string>? parameters, [NotNullWhen(true)] out IConfigProvider? provider)
		{
			var path = location.IsAbsoluteUri ?
				File.Exists(location.LocalPath) ? Path.GetFullPath(location.LocalPath): null:
				File.Exists(location.OriginalString) ? Path.GetFullPath(location.OriginalString): null;
			if (path == null)
			{
				provider = null;
				return false;
			}

			provider = null;
			return false;
		}

		private bool AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters, int position, out IConfigProvider? provider)
		{
			try
			{
				if (!CreateProvider(ref location, parameters, out provider))
					return false;

				Debug.Assert(provider != null);
				Debug.Assert(location != null);

				position = AddProvider(provider!, position);
				if (position < 0)
					return false;

				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(location, position));
				ScanConfigurationFile(provider!, location!, position);
				provider!.Changed += OnChanged;
				return true;
			}
			catch (Exception flaw)
			{
				provider = null;
				flaw = flaw.Unwrap();
				flaw.Add("locaion", location);
				if (__log == null)
					Logger.WriteErrorMessage($"ERROR: Cannot load configuration {location?.ToString() ?? "(null)"}.", flaw);
				else
					__log.Error(nameof(AddConfiguration), flaw.Add(nameof(location), location));
				return false;
			}
		}

		private bool CreateProvider(ref Uri location, IReadOnlyCollection<string>? parameters, [NotNullWhen(true)] out IConfigProvider? provider)
		{
			if (!_initialized)
				Initialize();

			var path = FindLocalFile(location.IsAbsoluteUri ? location.LocalPath : location.OriginalString, ConfigurationDirectories, Extensions);
			if (path == null)
			{
				provider = null;
				return false;
			}

			var l = location = new Uri(path);
			var providers = Providers;
			int i = providers.FindIndex(o => o.Location == l);
			if (i >= 0)
			{
				provider = providers[i];
				return false;
			}
			provider = ConfigurationFactory.TryCreateProvider(location, parameters);
			if (provider == null)
				return false;

			providers = Providers;
			var p = provider;
			i = providers.FindIndex(o => o.Location == p.Location);
			if (i >= 0)
			{
				provider = providers[i];
				return false;
			}
			return true;
		}

		private int AddProvider(IConfigProvider provider, int position)
		{
			List<IConfigProvider> providers;
			List<IConfigProvider> updated;
			int inserted;
			do
			{
				providers = Providers;
				var i = providers.FindIndex(o => o.Location == provider.Location);
				if (i >= 0)
					return ~i;
				updated = new(providers);
				if (position >= providers.Count || position < 0)
				{
					updated.Add(provider);
					inserted = updated.Count;
				}
				else
				{
					updated.Insert(position, provider);
					inserted = position + 1;
				}
				Interlocked.CompareExchange(ref Providers, updated, providers);
			} while (Interlocked.CompareExchange(ref Providers, updated, providers) == providers);
			return inserted;
		}

		private void ScanConfigurationFile(IConfigProvider provider, Uri location, int position)
		{
			if (provider.GetValue("applicationDirectory", typeof(string)) is string home)
				Lxx.HomeDirectory = home.Trim().TrimEnd(Path.DirectorySeparatorChar).TrimToNull();

			var ss = provider.GetList<string>("include");
			if (ss != null)
			{
				foreach (string s in ss)
				{
					int k = Providers.Count;
					var (value, parameters) = SplitOptions(s);
					AddConfiguration(new Uri(value, UriKind.RelativeOrAbsolute), parameters, position, out var _);
					position += Providers.Count - k;
				}
			}
		}

		private static (string Location, IReadOnlyCollection<string>? Parameters) SplitOptions(string value)
		{
			if (String.IsNullOrWhiteSpace(value) || value.IndexOf(' ') < 0)
				return (value, null);
			var xx = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
			return (xx[0], xx);
		}
		private static readonly char[] SpaceSeparator = new[] { ' ' };

		private string? FindLocalFile(string path, IEnumerable<string> directory, IReadOnlyCollection<string> extension)
		{
			bool extFound = false;
			foreach (string ext in extension)
			{
				int i = ext.LastIndexOf('.');
				string ending = i > 0 ? ext.Substring(i) : ext;
				if (path.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
				{
					extFound = true;
					break;
				}
			}

			string? file;
			if (Path.IsPathRooted(path))
				file =
					extFound ? File.Exists(path) ? path : null :
					extension
						.Select(ext => Path.ChangeExtension(path, ext))
						.FirstOrDefault(File.Exists);
			else if (extFound)
				file = File.Exists(path) ? path :
					directory
						.Select(o => Path.Combine(o, path))
						.FirstOrDefault(File.Exists);
			else
				file = extension.FirstOrDefault(File.Exists) ??
					directory
						.SelectMany(_ => extension, (o, e) => Path.Combine(o, Path.ChangeExtension(path, e)))
						.FirstOrDefault(File.Exists);

			return file == null ? null : Path.GetFullPath(file);
		}

		private void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			if (!_initialized)
				return;
			Interlocked.Increment(ref __version);
			if (sender != null)
			{
				var cs = sender as IXmlConfigurationSource;
				LogConfigurationEvent(LogSource, SR.ConfigurationChanged(cs));
			}
			Cache.Clear();
			Changed?.Invoke(sender, e);
			__log = null;
		}


		internal void OnLoggerInitialized()
		{
			if (__log == null)
				SetLogger(new Logger(LogSource));
		}

		public void SetLogger(ILogger logger)
		{
			if (__log == logger || logger == null)
				return;
			__log = logger;
			if (__messages == null)
				return;

			var messages = Interlocked.Exchange(ref __messages, null);
			if (messages == null)
				return;

			while (!messages.IsEmpty)
			{
				if (messages.TryDequeue(out var message))
					LogEventEntry(message);
			}
		}

		private class EventEntry
		{
			public Exception? Exception;
			public Func<string>? Message;
			public string Source;

			public EventEntry(Exception exception, string source)
			{
				Exception = exception ?? throw new ArgumentNullException(nameof(exception));
				Source = source ?? throw new ArgumentNullException(nameof(source));
			}

			public EventEntry(Func<string> message, string source)
			{
				Message = message ?? throw new ArgumentNullException(nameof(message));
				Source = source ?? throw new ArgumentNullException(nameof(source));
			}
		}

		private void LogEventEntry(EventEntry item)
		{
			if (item.Exception != null)
			{
				if (__log!.IsEnabled(LogType.Error))
					__log!.Error(item.Source, item.Exception);
			}
			else if (item.Message != null)
			{
				if (__log!.IsEnabled(LogType.Trace))
					__log!.Error(item.Source, item.Message(), null, null);
			}
		}

		private void LogConfigurationItem(EventEntry item)
		{
			if (__log == null)
				(__messages ??= new ConcurrentQueue<EventEntry>()).Enqueue(item);
			else
				LogEventEntry(item);
		}

		public void LogConfigurationError(string logSource, Exception exception)
		{
			if (exception != null)
				LogConfigurationItem(new EventEntry(exception, logSource));
		}

		public void LogConfigurationEvent(string logSource, Func<string> message)
		{
			if (message != null)
				LogConfigurationItem(new EventEntry(message, logSource));
		}

		private void Initialize()
		{
			if (!_initialized && !_initializing)
			{
				if (!_initialized && !_initializing)
				{
					_initializing = true;
					AddSystem(new Uri("system:environment"), new EnvironmentConfigurationProvider());
#if !NETCOREAPP
					AddSystem(new Uri("system:configuration"), new SystemConfigurationProvider());
#endif

					IEnumerable<Uri> cc0 = GetInitialConfigurationsLocations();
					foreach (var c in cc0)
					{
						AddConfiguration(c, null, -1, out var _);
					}
					_initialized = true;
					_initializing = false;
					Lxx.OnConfigurationInitialized(null, new ConfigurationEventArgs());
					OnChanged();
				}
			}
		}

		private void AddSystem(Uri locator, IConfigProvider provider)
		{
			if (AddProvider(provider, -1) >= 0)
			{
				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(locator, Providers.Count));
				ScanConfigurationFile(provider, locator, Providers.Count);
			}
		}

		private static IReadOnlyList<string> GetConfigurationDerectories()
		{
			var configurationDirectory = new List<string> { Lxx.HomeDirectory };
#if !NETCOREAPP
			string[] directories = System.Configuration.ConfigurationManager.AppSettings.GetValues(ConfigurationDerectoryKey);
			if (directories != null)
			{
				foreach (var entry in directories)
				{
					foreach (var item in entry.Split(__separators, StringSplitOptions.RemoveEmptyEntries))
					{
						string value = item.Trim().TrimEnd(Path.DirectorySeparatorChar);
						if (value.Length > 0 && configurationDirectory.FindIndex(o => String.Equals(o, value, StringComparison.OrdinalIgnoreCase)) < 0)
							configurationDirectory.Add(value);
					}
				}
			}
#endif
			if (configurationDirectory.FindIndex(o => String.Equals(o, Lxx.GlobalConfigurationDirectory, StringComparison.OrdinalIgnoreCase)) < 0)
				configurationDirectory.Add(Lxx.GlobalConfigurationDirectory);
			return ReadOnly.Wrap(configurationDirectory);
		}
		private static readonly char[] __separators = { ',', ';', ' ', '\t', '\n', '\r' };

		private static Uri? GetConfigurationLocation(Assembly? asm)
		{
			string? name;
			if (asm == null || asm.IsDynamic || String.IsNullOrEmpty(name = asm.GetName().CodeBase))
				return null;
			return new Uri(name);
		}

		private static IEnumerable<Uri> GetInitialConfigurationsLocations()
		{
			var cc = new OrderedSet<Uri>();
#if !NETCOREAPP
			string[] ss = System.Configuration.ConfigurationManager.AppSettings.GetValues(InitialLocationKey);
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
			locator = new Uri("file:///" + Lxx.GlobalConfigurationDirectory + Path.DirectorySeparatorChar + Lxx.GlobalConfigurationFile);
			cc.Add(locator);
			return cc;
		}
	}
}
