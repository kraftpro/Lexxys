// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys.Configuration
{
	using Logging;

	internal class DefaultConfig
	{
		private const bool CachingDefault = false;

		private const string LogSource = "Lexxys.Configuration";
		private const string ConfigCachingOption = "lexxys.config:cacheValues";
		private const string InitialLocationKey = "configuration";
		private const string ConfigurationDerectoryKey = "configurationDirectory";
		private static readonly string[] Extensions = { ".config.xml", ".config.txt", ".config.ini", ".config.json" };

		private static IReadOnlyList<string>? _configurationDirectory;
		private static bool _initialized;
		private static bool _initializing;
		private static bool _cacheValues;
		private static ILogger? __log;
		private static List<EventEntry>? __messages;
		private volatile static int __version;

		private static readonly object SyncRoot = new object();
		private static readonly List<ConfigurationLocator> Locations = new List<ConfigurationLocator>();
		private static readonly List<IConfigurationProvider> Providers = new List<IConfigurationProvider>();
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
				ConfigurationLocator? locator = GetConfigurationLocator(e.LoadedAssembly);
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

		private static T GetValue<T>(string key, T defaultValue)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			return GetObjectValue(key, typeof(T)) is T value ? value: defaultValue;
		}

		private static T GetValue<T>(string key, Func<T> defaultValue)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));
			return GetObjectValue(key, typeof(T)) is T value ? value : defaultValue();
		}

		internal static IReadOnlyList<T> GetList<T>(string key)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (!_initialized)
				Initialize();

			string cacheKey = key + "$*$" + typeof(T).ToString();
			if (_cacheValues && Cache.TryGetValue(cacheKey, out object? value))
				return value as IReadOnlyList<T> ?? EmptyArray<T>.Value;

			lock (SyncRoot)
			{
				if (_cacheValues && Cache.TryGetValue(cacheKey, out value))
					return value as IReadOnlyList<T> ?? EmptyArray<T>.Value;

				List<T>? temp = null;
				bool copy = false;
				for (int i = Providers.Count - 1; i >= 0; --i)
				{
					List<T> x = Providers[i].GetList<T>(key);
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
				return result ?? EmptyArray<T>.Value;
			}
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
			return AddConfiguration(new ConfigurationLocator(value), parameters);
		}

		// TODO: optional
		public static IConfigurationProvider? AddConfiguration(ConfigurationLocator location, IReadOnlyCollection<string>? parameters) // = null)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			lock (SyncRoot)
			{
				int n = Providers.Count;
				IConfigurationProvider? provider = AddConfiguration(location, parameters, null, 0);
				if (provider == null)
					return null;
				if (n < Providers.Count)
					OnChanged();
				return provider;
			}
		}

		private static IConfigurationProvider? AddConfiguration(ConfigurationLocator location, IReadOnlyCollection<string>? parameters, string? currentDirectory, int position)
		{
			try
			{
				Logger.WriteDebugMessage("Find Configuration", location.ToString());
				if (!FindProvider(currentDirectory, ref location, parameters, out var provider))
					return provider;

				Debug.Assert(provider != null);
				Debug.Assert(location != null);

				if (position >= Locations.Count || position < 0)
				{
					Locations.Add(location!);
					Providers.Add(provider!);
					position = Locations.Count;
				}
				else
				{
					Locations.Insert(position, location!);
					Providers.Insert(position, provider!);
					++position;
				}
				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(location, position));
				ScanConfigurationFile(provider!, location!, position);
				provider!.Changed += OnChanged;
				return provider;
			}
			catch (Exception flaw)
			{
				flaw = flaw.Unwrap();
				flaw.Add("locaion", location);
				if (__log == null)
					Logger.WriteErrorMessage($"ERROR: Cannot load configuration {location?.ToString() ?? "(null)"}.", flaw);
				else
					__log.Error(nameof(AddConfiguration), flaw.Add(nameof(location), location?.ToString()));
				return null;
			}
		}

		public static void RegisterSource(ITrackedConfiguration source) => source.Changed += OnChanged;

		private static void ScanConfigurationFile(IConfigurationProvider provider, ConfigurationLocator location, int position)
		{
			if (provider.GetValue("applicationDirectory", typeof(string)) is string home)
				Lxx.HomeDirectory = home.Trim().TrimEnd(Path.DirectorySeparatorChar).TrimToNull();

			List<string> ss = provider.GetList<string>("include");
			if (ss != null)
			{
				foreach (string s in ss)
				{
					int k = Locations.Count;
					var (value, parameters) = SplitOptions(s);
					AddConfiguration(new ConfigurationLocator(value), parameters, location.DirectoryName, position);
					position += Locations.Count - k;
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

		private static bool FindProvider(string? currentDirectory, ref ConfigurationLocator location, IReadOnlyCollection<string>? parameters, out IConfigurationProvider? provider)
		{
			if (!_initialized)
				Initialize();
			if (!location.IsLocated && currentDirectory != null)
				location = location.Locate(new[] { currentDirectory }, Extensions);
			if (!location.IsLocated)
				location = location.Locate(ConfigurationDirectories, Extensions);

			lock (SyncRoot)
			{
				int i = Locations.IndexOf(location);
				if (i >= 0)
				{
					provider = Providers[i];
					return false;
				}
				provider = ConfigurationFactory.FindProvider(location, parameters);
				if (provider == null)
				{
					//LogConfigurationEvent(LogSource, SR.ConfigurationProviderNotFound(location));
					return false;
				}
				i = Providers.IndexOf(provider);
				if (i >= 0)
				{
					provider = Providers[i];
					return false;
				}
				return true;
			}
		}

		private static void OnChanged(object? sender, ConfigurationEventArgs e)
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


		internal static void OnLoggerInitialized()
		{
			if (__log == null)
				SetLogger(new Logger(LogSource));
		}

		public static void SetLogger(ILogger? logger)
		{
			if (logger == __log)
				return;
			lock (SyncRoot)
			{
				__log = logger;
				if (__log != null)
				{
					if (__messages != null && __messages.Count > 0)
					{
						foreach (EventEntry item in __messages)
						{
							LogEventEntry(item);
						}
					}
					__messages = null;
				}
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
				(__messages ??= new List<EventEntry>()).Add(item);
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

		public static object? GetObjectValue(string key, Type objectType)
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

			lock (SyncRoot)
			{
				if (_cacheValues && Cache.TryGetValue(cacheKey, out value))
					return value;
				value = null;
				foreach (IConfigurationProvider provider in Providers)
				{
					value = provider.GetValue(key, objectType);
					if (value != null)
						break;
				}
				if (_cacheValues)
					Cache[cacheKey] = value;
				return value;
			}
		}

		private static void Initialize()
		{
			if (!_initialized && !_initializing)
			{
				lock (SyncRoot)
				{
					if (!_initialized && !_initializing)
					{
						_initializing = true;
						AddSystem(new ConfigurationLocator("System.Environment"), new EnvironmentConfigurationProvider());
#if !NETCOREAPP
						AddSystem(new ConfigurationLocator("System.Configuration"), new SystemConfigurationProvider());
#endif

						IEnumerable<ConfigurationLocator> cc0 = GetInitialConfigurationsLocations();
						foreach (var c in cc0)
						{
							AddConfiguration(c, null, null, -1);
						}
						_initialized = true;
						_initializing = false;
						_cacheValues = GetValue(ConfigCachingOption, CachingDefault);
						Lxx.OnConfigurationInitialized(null, new ConfigurationEventArgs());
						OnChanged();
					}
				}
			}
		}

		private static void AddSystem(ConfigurationLocator locator, IConfigurationProvider provider)
		{
			Locations.Add(locator);
			Providers.Add(provider);
			LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(locator, Locations.Count));
			ScanConfigurationFile(provider, locator, Providers.Count);
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

		private static IEnumerable<ConfigurationLocator> GetInitialConfigurationsLocations()
		{
			var cc = new OrderedSet<ConfigurationLocator>();
#if !NETCOREAPP
			string[] ss = ConfigurationManager.AppSettings.GetValues(InitialLocationKey);
			if (ss != null)
			{
				foreach (string s in ss)
				{
					var c = new ConfigurationLocator(s);
					if (c.IsValid)
						cc.Add(c);
				}
			}
#endif
			cc.AddRange(Factory.DomainAssemblies.Select(GetConfigurationLocator).Where(o => o != null)!);

			var locator = new ConfigurationLocator("file:///" + Lxx.HomeDirectory + Path.DirectorySeparatorChar + Lxx.AnonymousConfigurationFile, true);
			if (locator.IsValid)
				cc.Add(locator);
			locator = new ConfigurationLocator(Lxx.GlobalConfigurationDirectory + Path.DirectorySeparatorChar + Lxx.GlobalConfigurationFile);
			if (locator.IsValid)
				cc.Add(locator);
			return cc;
		}

		private static ConfigurationLocator? GetConfigurationLocator(Assembly? asm)
		{
			string? name;
			if (asm == null || asm.IsDynamic || String.IsNullOrEmpty(name = asm.GetName().CodeBase))
				return null;
			var locator = new ConfigurationLocator(Path.ChangeExtension(name, null), true);
			return locator.IsValid ? locator: null;
		}
	}
}
