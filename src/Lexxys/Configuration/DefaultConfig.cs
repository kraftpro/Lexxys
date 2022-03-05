// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys.Configuration
{
	using Logging;

	internal class DefaultConfig
	{
		private const bool CachingDefault = false;
		private const int UserConfigPosition = 1;

		private const string LogSource = "Lexxys.Configuration";
		private const string ConfigCachingOption = "lexxys.config:cacheValues";
		private const string InitialLocationKey = "configuration";
		private const string ConfigurationDerectoryKey = "configurationDirectory";
		private static readonly string[] Extensions = { ".config.xml", ".config.txt", ".config.ini", ".config.json" };

		private static readonly object SyncObject = new object();
		private static IReadOnlyList<string>? _configurationDirectory;
		private static bool _initialized;
		private static bool _initializing;
		private static bool _cacheValues;
		private static ILogger? __log;
		private static ConcurrentQueue<EventEntry>? __messages;
		private volatile static int __version;

		private static volatile List<IConfigurationProvider> Providers = new List<IConfigurationProvider>();
		private static readonly ConcurrentDictionary<string, object?> Cache = new ConcurrentDictionary<string, object?>();

		static DefaultConfig()
		{
			Factory.AssemblyLoad += Factory_AssemblyLoad;
		}

		public static int Version => __version;

		private static void Factory_AssemblyLoad(object? sender, AssemblyLoadEventArgs e)
		{
			if (e != null)
			{
				Uri? locator = GetConfigurationLocator(e.LoadedAssembly);
				if (locator != null)
					AddConfiguration(locator, null);
			}
		}

		private static IReadOnlyList<string> ConfigurationDirectories
		{
			get
			{
				var cc = _configurationDirectory;
				if (cc == null)
					Interlocked.CompareExchange(ref _configurationDirectory, null, (cc = GetConfigurationDerectories()));
				return cc;
			}
		}

		internal static bool IsInitialized => _initialized;

		public static IReadOnlyList<T> GetList<T>(string key)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (!_initialized)
				Initialize();

			string cacheKey = key + "$*$" + typeof(T).ToString();
			if (_cacheValues && Cache.TryGetValue(cacheKey, out object? value))
				return value as IReadOnlyList<T> ?? EmptyArray<T>.Value;

			if (_cacheValues && Cache.TryGetValue(cacheKey, out value))
				return value as IReadOnlyList<T> ?? EmptyArray<T>.Value;

			List<T>? temp = null;
			bool copy = false;
			var providers = Providers;
			for (int i = providers.Count - 1; i >= 0; --i)
			{
				List<T> x = providers[i].GetList<T>(key);
				if (x != null && x.Count > 0)
				{
					if (temp == null)
					{
						temp = x;
					}
					else
					{
						if (!copy)
						{
							temp = new List<T>(temp);
							copy = true;
						}
						temp.AddRange(x);
					}
				}
			}
			IReadOnlyList<T>? result = temp == null || temp.Count == 0 ? null: ReadOnly.Wrap(temp);
			if (_cacheValues)
				Cache[cacheKey] = result;
			return result ?? Array.Empty<T>();
		}

		public static object? GetValue(string key, Type objectType)
		{
			if (objectType == null)
				throw EX.ArgumentNull(nameof(objectType));
			if (key == null || key.Length <= 0)
				throw EX.ArgumentNull(nameof(key));
			if (!_initialized)
				Initialize();

			string cacheKey = key + "$" + objectType.ToString();
			if (_cacheValues && Cache.TryGetValue(cacheKey, out object? value))
				return value;

			if (_cacheValues && Cache.TryGetValue(cacheKey, out value))
				return value;
			value = null;
			var providers = Providers;
			foreach (IConfigurationProvider provider in providers)
			{
				value = provider.GetValue(key, objectType);
				if (value != null)
					break;
			}
			if (_cacheValues)
				Cache[cacheKey] = value;
			return value;
		}

		public static event EventHandler<ConfigurationEventArgs>? Changed;

		private static void OnChanged()
		{
			OnChanged(null, new ConfigurationEventArgs());
		}

		public static IConfigurationProvider? AddConfiguration(string location)
		{
			if (location == null || location.Length <= 0)
				throw new ArgumentNullException(nameof(location));
			var (value, parameters) = SplitOptions(location);
			return AddConfiguration(new Uri(value, UriKind.RelativeOrAbsolute), parameters);
		}

		// TODO: optional
		public static IConfigurationProvider? AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters) // = null)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			var (provider, created) = AddConfiguration(location, parameters, UserConfigPosition);
			if (provider == null)
				return null;
			if (created)
				OnChanged();
			return provider;
		}

		private static int AddProvider(IConfigurationProvider provider, int position)
		{
			List<IConfigurationProvider> providers;
			List<IConfigurationProvider> updated;
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

		private static (IConfigurationProvider?, bool) AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters, int position)
		{
			try
			{
				//Logger.WriteDebugMessage("Find Configuration", location.ToString());
				if (!CreateProvider(ref location, parameters, out var provider))
					return (provider, false);

				Debug.Assert(provider != null);
				Debug.Assert(location != null);

				position = AddProvider(provider!, position);
				if (position < 0)
					return (provider, false);

				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(location, position));
				ScanConfigurationFile(provider!, location!, position);
				provider!.Changed += OnChanged;
				return (provider, true);
			}
			catch (Exception flaw)
			{
				flaw = flaw.Unwrap();
				flaw.Add("locaion", location);
				if (__log == null)
					Logger.WriteErrorMessage($"ERROR: Cannot load configuration {location?.ToString() ?? "(null)"}.", flaw);
				else
					__log.Error(nameof(AddConfiguration), flaw.Add(nameof(location), location));
				return (null, false);
			}
		}

		private static void ScanConfigurationFile(IConfigurationProvider provider, Uri location, int position)
		{
			if (provider.GetValue("applicationDirectory", typeof(string)) is string home)
				Lxx.HomeDirectory = home.Trim().TrimEnd(Path.DirectorySeparatorChar).TrimToNull();

			List<string> ss = provider.GetList<string>("include");
			if (ss != null)
			{
				foreach (string s in ss)
				{
					int k = Providers.Count;
					var (value, parameters) = SplitOptions(s);
					AddConfiguration(new Uri(value, UriKind.RelativeOrAbsolute), parameters, position);
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

		private static bool CreateProvider(ref Uri location, IReadOnlyCollection<string>? parameters, out IConfigurationProvider? provider)
		{
			if (!_initialized)
				Initialize();

			var path = FindLocalFile(location.IsAbsoluteUri ? location.LocalPath: location.OriginalString, ConfigurationDirectories, Extensions);
			if (path != null)
				location = new Uri(path);

			var providers = Providers;
			var l = location;
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
			i = providers.IndexOf(provider);
			if (i >= 0)
			{
				provider = providers[i];
				return false;
			}
			return true;
		}

		private static string? FindLocalFile(string path, IEnumerable<string> directory, IReadOnlyCollection<string> extension)
		{
			bool extFound = false;
			foreach (string ext in extension)
			{
				int i = ext.LastIndexOf('.');
				string ending = i > 0 ? ext.Substring(i): ext;
				if (path.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
				{
					extFound = true;
					break;
				}
			}

			string? file;
			if (Path.IsPathRooted(path))
				file = 
					extFound ? File.Exists(path) ? path: null:
					extension
                        .Select(ext => Path.ChangeExtension(path, ext))
                        .FirstOrDefault(File.Exists);
			else if (extFound)
				file = File.Exists(path) ? path:
					directory
						.Select(o => Path.Combine(o, path))
						.FirstOrDefault(File.Exists);
			else
				file = extension.FirstOrDefault(File.Exists) ??
					directory
						.SelectMany(_ => extension, (o, e) => Path.Combine(o, Path.ChangeExtension(path, e)))
						.FirstOrDefault(File.Exists);

			return file == null ? null: Path.GetFullPath(file);
		}

		private static void OnChanged(object? sender, ConfigurationEventArgs e)
		{
			Interlocked.Increment(ref __version);
			if (!_initialized)
				return;
			if (sender != null)
			{
				var cs = sender as IXmlConfigurationSource;
				LogConfigurationEvent(LogSource, SR.ConfigurationChanged(cs));
			}
			Cache.Clear();
			Changed?.Invoke(sender, e);
			__log = null;
		}


		internal static void OnLoggerInitialized()
		{
			if (__log == null)
				SetLogger(new Logger(LogSource));
		}

		public static void SetLogger(ILogger logger)
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

		private static void LogEventEntry(EventEntry item)
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

		private static void LogConfigurationItem(EventEntry item)
		{
			if (__log == null)
				(__messages ??= new ConcurrentQueue<EventEntry>()).Enqueue(item);
			else
				LogEventEntry(item);
		}

		public static void LogConfigurationError(string logSource, Exception exception)
		{
			if (exception != null)
				LogConfigurationItem(new EventEntry(exception, logSource));
		}

		public static void LogConfigurationEvent(string logSource, Func<string> message)
		{
			if (message != null)
				LogConfigurationItem(new EventEntry(message, logSource));
		}

		private static void Initialize()
		{
			if (_initialized || (_initializing && !Factory.AssembliesImported))
				return;
			lock (SyncObject)
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
						AddConfiguration(c, null, -1);
					}
					_initialized = true;
					_initializing = false;
					_cacheValues = GetValue(ConfigCachingOption, typeof(bool)) is bool b ? b : CachingDefault;
					Lxx.OnConfigurationInitialized(null, new ConfigurationEventArgs());
					OnChanged();
				}
			}
		}

		private static void AddSystem(Uri locator, IConfigurationProvider provider)
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
			string[] directories = ConfigurationManager.AppSettings.GetValues(ConfigurationDerectoryKey);
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

		private static IEnumerable<Uri> GetInitialConfigurationsLocations()
		{
			var cc = new OrderedSet<Uri>();
#if !NETCOREAPP
			string[] ss = ConfigurationManager.AppSettings.GetValues(InitialLocationKey);
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
			cc.AddRange(Factory.DomainAssemblies.Select(GetConfigurationLocator).Where(o => o != null)!);

			var locator = new Uri("file:///" + Lxx.HomeDirectory + Path.DirectorySeparatorChar + Lxx.AnonymousConfigurationFile);
			cc.Add(locator);
			locator = new Uri("file:///" + Lxx.GlobalConfigurationDirectory + Path.DirectorySeparatorChar + Lxx.GlobalConfigurationFile);
			cc.Add(locator);
			return cc;
		}

		private static Uri? GetConfigurationLocator(Assembly? asm)
		{
			string? name;
			if (asm == null || asm.IsDynamic || String.IsNullOrEmpty(name = asm.GetName().CodeBase))
				return null;
			return new Uri(name);
		}

		#region IConfigSource
		
		public static readonly IConfigSource ConfigSourceInstance = new DefaultConfigSource();

		class DefaultConfigSource: IConfigSource
		{
			public int Version => DefaultConfig.Version;

			public event EventHandler<ConfigurationEventArgs>? Changed
			{
				add => DefaultConfig.Changed += value;
				remove => DefaultConfig.Changed -= value;
			}

			public IReadOnlyList<T> GetList<T>(string key) => DefaultConfig.GetList<T>(key);

			public object? GetValue(string key, Type objectType) => DefaultConfig.GetValue(key, objectType);
		}

		#endregion
	}
}
