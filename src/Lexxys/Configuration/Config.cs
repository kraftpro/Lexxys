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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexxys
{
	using Configuration;
	using Logging;

	public class ConfigurationEventArgs: EventArgs
	{
	}

	public interface IConfiguration
	{
		event EventHandler<ConfigurationEventArgs> Changed;
		IValue<T> GetSection<T>(string name, Func<T> defaultValue);
		IOptions<T> GetOptions<T>(string node, Func<T> defaultValue) where T : class, new();
	}

	public static class ConfigurationExtenstions
	{
		public static IValue<T> GetSection<T>(this IConfiguration config, string node)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetSection<T>(node, () => default);
		}

		public static IValue<T> GetSection<T>(this IConfiguration config, string node, T defaultValue)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetSection<T>(node, () => defaultValue);
		}

		public static IOptions<T> GetOptions<T>(this IConfiguration config, string node) where T : class, new()
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetOptions<T>(node, () => default);
		}

		public static IOptions<T> GetOptions<T>(this IConfiguration config, string node, T defaultValue) where T : class, new()
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetOptions<T>(node, () => defaultValue);
		}
	}

	public static class Config
	{
		private const bool CachingDefault = false;

		private const string LogSource = "Lexxys.Configuration";
		private const string ConfigCachingOption = "lexxys.config:cacheValues";
		private const string InitialLocationKey = "configuration";
		private const string ConfigurationDerectoryKey = "configurationDirectory";
		private static readonly string[] Extensions = { ".config.xml", ".config.txt", ".config.ini", ".config.json" };

		private static IReadOnlyList<string> _configurationDirectory;
		private static bool _initialized;
		private static bool _initializing;
		private static bool _cacheValues;
		private static ILogging __log;
		private static List<EventEntry> __messages = new List<EventEntry>();
		private volatile static int __version;

		private static readonly object SyncRoot = new object();
		private static readonly List<ConfigurationLocator> Locations = new List<ConfigurationLocator>();
		private static readonly List<IConfigurationProvider> Providers = new List<IConfigurationProvider>();
		private static readonly ConcurrentDictionary<string, object> Cache = new ConcurrentDictionary<string, object>();

		static Config()
		{
			Factory.AssemblyLoad += Factory_AssemblyLoad;
		}

		static void Factory_AssemblyLoad(object sender, AssemblyLoadEventArgs e)
		{
			if (e != null)
			{
				ConfigurationLocator locator = GetConfigurationLocator(e.LoadedAssembly);
				if (locator != null)
					AddConfiguration(locator, null);
			}
		}

		public static IReadOnlyList<string> ConfigurationDirectories
		{
			get
			{
				var cc = _configurationDirectory;
				if (cc == null)
					Interlocked.CompareExchange(ref _configurationDirectory, null, (cc = GetConfigurationDerectories()));
				return cc;
			}
		}

		public static bool IsInitialized => _initialized;

		public static T GetValue<T>(string node)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			return GetValue(node, typeof(T)) is T value ? value: default;
		}

		public static T GetValue<T>(string node, T defaultValue)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			return GetValue(node, typeof(T)) is T value ? value: defaultValue;
		}

		public static T GetValue<T>(string node, Func<T> defaultValue)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));
			return GetValue(node, typeof(T)) is T value ? value : defaultValue();
		}

		public static T GetValue<T>(string node, T minValue, T maxValue, T defaultValue)
			where T: IComparable<T>
		{
			return GetValue(node, typeof(T)) is not T value ? defaultValue:
				value.CompareTo(minValue) <= 0 ? minValue:
				value.CompareTo(maxValue) >= 0 ? maxValue: value;
		}


		public static IValue<T> GetSection<T>(string node)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			var custom = new Custom<T>(node, () => default);
			return new Out<T>(() => custom.Value);
		}

		public static IValue<T> GetSection<T>(string node, T defaultValue)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			var custom = new Custom<T>(node, () => defaultValue);
			return new Out<T>(() => custom.Value);
		}

		public static IValue<T> GetSection<T>(string node, Func<T> defaultValue)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));
			var custom = new Custom<T>(node, defaultValue);
			return new Out<T>(() => custom.Value);
		}

		public static IOptions<T> GetOptions<T>(string node) where T : class, new()
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			var custom = new Custom<T>(node, () => default);
			return new OptOut<T>(() => custom.Value);
		}

		public static IOptions<T> GetOptions<T>(string node, T defaultValue) where T : class, new()
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			var custom = new Custom<T>(node, () => defaultValue);
			return new OptOut<T>(() => custom.Value);
		}

		public static IOptions<T> GetOptions<T>(string node, Func<T> defaultValue) where T : class, new()
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));
			var custom = new Custom<T>(node, defaultValue);
			return new OptOut<T>(() => custom.Value);
		}

		private class Custom<T>: IValue<T>
		{
			private readonly Func<T> _default;
			private readonly string _section;
			volatile private VersionValue _item;

			public Custom(string section, Func<T> @default)
			{
				if (section == null || section.Length <= 0)
					throw new ArgumentNullException(nameof(section));

				_section = section;
				_default = @default;
				_item = VersionValue.Default;
			}

			public T Value
			{
				get
				{
					for (;;)
					{
						var current = _item;
						var version = __version;
						if (current.Version == version)
							return current.Value;
						try
						{
							var value = Config.GetValue(_section, _default);
							var updated = new VersionValue(version, value);
							Interlocked.CompareExchange(ref _item, updated, current);
						}
						catch (Exception flaw)
						{
							flaw.Add("Section", _section);
							throw;
						}
					}
				}
				set { throw new NotSupportedException(); }
			}

			object IValue.Value => Value;

			class VersionValue
			{
				public static readonly VersionValue Default = new VersionValue(-1, default);

				public T Value { get; }
				public int Version { get; }

				public VersionValue(int version, T value)
				{
					Value = value;
					Version = version;
				}
			}
		}


		public static IReadOnlyList<T> GetList<T>(string node, bool suppressNull = false)
		{
			if (node == null || node.Length <= 0)
				throw new ArgumentNullException(nameof(node));
			if (!_initialized)
				Initialize();

			string key = node + "`List`" + typeof(T).ToString();
			if (_cacheValues && Cache.TryGetValue(key, out object value))
				return value is IReadOnlyList<T> tvalue ? tvalue: suppressNull ? EmptyArray<T>.Value : null;

			lock (SyncRoot)
			{
				if (_cacheValues && Cache.TryGetValue(key, out value))
					return value is IReadOnlyList<T> tvalue ? tvalue : suppressNull ? EmptyArray<T>.Value : null;

				List<T> temp = null;
				bool copy = false;
				for (int i = Providers.Count - 1; i >= 0; --i)
				{
					List<T> x = Providers[i].GetList<T>(node);
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
				IReadOnlyList<T> result = temp == null || temp.Count == 0 ? null: ReadOnly.Wrap(temp);
				if (_cacheValues)
					Cache[key] = result;
				return result == null && suppressNull ? EmptyArray<T>.Value: result;
			}
		}

		public static string ExpandParameters(string value)
		{
			return __parametersRex.Replace(value, m => GetValue(m.Groups[1].Value, () => m.Groups[1].Value == "global" ? Lxx.GlobalConfigurationDirectory: ""));
		}
		private static readonly Regex __parametersRex = new Regex(@"\${(.*?)}");

		public static event EventHandler<ConfigurationEventArgs> Changed;

		public static void OnChanged()
		{
			OnChanged(null, new ConfigurationEventArgs());
		}

		public static IConfigurationProvider GetConfiguration(string location)
		{
			if (location == null || location.Length <= 0)
				throw new ArgumentNullException(nameof(location));
			return GetConfiguration(new ConfigurationLocator(location));
		}

		public static IConfigurationProvider GetConfiguration(ConfigurationLocator location)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			FindProvider(null, ref location, null, out IConfigurationProvider provider);
			return provider;
		}

		public static IConfigurationProvider AddConfiguration(string location)
		{
			if (location == null || location.Length <= 0)
				throw new ArgumentNullException(nameof(location));
			var (value, parameters) = SplitOptions(location);
			return AddConfiguration(new ConfigurationLocator(value), parameters);
		}

		// TODO: optional
		public static IConfigurationProvider AddConfiguration(ConfigurationLocator location, IReadOnlyCollection<string> parameters) // = null)
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			lock (SyncRoot)
			{
				int n = Providers.Count;
				IConfigurationProvider provider = AddConfiguration(location, parameters, null, 0);
				if (provider == null)
					return null;
				if (n < Providers.Count)
					OnChanged();
				return provider;
			}
		}

		private static IConfigurationProvider AddConfiguration(ConfigurationLocator location, IReadOnlyCollection<string> parameters, string currentDirectory, int position)
		{
			try
			{
				Logger.WriteDebugMessage("Find Configuration", location.ToString());
				if (!FindProvider(currentDirectory, ref location, parameters, out IConfigurationProvider provider))
					return provider;

				if (position >= Locations.Count || position < 0)
				{
					Locations.Add(location);
					Providers.Add(provider);
					position = Locations.Count;
				}
				else
				{
					Locations.Insert(position, location);
					Providers.Insert(position, provider);
					++position;
				}
				LogConfigurationEvent(LogSource, SR.ConfigurationLoaded(location, position));
				ScanConfigurationFile(provider, location, position);
				provider.Changed += OnChanged;
				return provider;
			}
			catch (Exception flaw)
			{
				flaw = flaw.Unwrap();
				flaw.Add("locaion", location);
				Logger.WriteErrorMessage($"ERROR: Cannot load configuration {location?.ToString() ?? "(null)"}.", flaw);
				if (__log != null && __log.IsEnabled(LogType.Error))
					__log.Log(new LogRecord(LogType.Error, "AddConfiguration", flaw));
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

		private static (string Location, IReadOnlyCollection<string> Parameters) SplitOptions(string value)
		{
			if (String.IsNullOrWhiteSpace(value) || value.IndexOf(' ') < 0)
				return (value, null);
			var xx = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
			return (xx[0], xx);
		}
		private static readonly char[] SpaceSeparator = new[] { ' ' };

		private static bool FindProvider(string currentDirectory, ref ConfigurationLocator location, IReadOnlyCollection<string> parameters, out IConfigurationProvider provider)
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
				if (provider.IsEmpty)
				{
					provider = null;
					return false;
				}
				return true;
			}
		}

		private static void OnChanged(object sender, ConfigurationEventArgs e)
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
			EventHandler<ConfigurationEventArgs> ch = Changed;
			ch?.Invoke(sender, e);
			__log = null;
		}


		internal static void OnLoggerInitialized()
		{
			if (_initialized && __log == null && Logger.Initialized)
			{
				lock (SyncRoot)
				{
					TryCreateLogger();
				}
			}
		}

		private static void TryCreateLogger()
		{
			if (_initialized && __log == null && Logger.Initialized)
			{
				__log = new Logger(LogSource);
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

		private class EventEntry
		{
			public Exception Exception;
			public Func<string> Message;
			public string Source;
		}

		private static void LogEventEntry(EventEntry item)
		{
			if (item.Exception != null)
			{
				if (__log.IsEnabled(LogType.Error))
					__log.Log(new LogRecord(LogType.Error, item.Source, item.Message?.Invoke(), item.Exception, null));
			}
			else if (item.Message != null)
			{
				if (__log.IsEnabled(LogType.Trace))
					__log.Log(new LogRecord(LogType.Trace, item.Source, item.Message(), null));
			}
		}

		private static void LogConfigurationItem(EventEntry item)
		{
			if (__log == null)
			{
				lock (SyncRoot)
				{
					TryCreateLogger();
					if (__log == null)
					{
						__messages.Add(item);
						return;
					}
				}
			}
			LogEventEntry(item);
		}

		public static void LogConfigurationError(string logSource, Exception exception)
		{
			if (exception != null)
				LogConfigurationItem(new EventEntry { Exception = exception, Source = logSource });
		}

		public static void LogConfigurationEvent(string logSource, Func<string> message)
		{
			if (message != null)
				LogConfigurationItem(new EventEntry { Message = message, Source = logSource });
		}

		private static object GetValue(string node, Type objectType)
		{
			if (objectType == null)
				throw EX.ArgumentNull(nameof(objectType));
			if (node == null || node.Length <= 0)
				throw EX.ArgumentNull(nameof(node));
			if (!_initialized)
				Initialize();

			string key = node + "`" + objectType.ToString();
			if (_cacheValues && Cache.TryGetValue(key, out object value))
				return value;

			lock (SyncRoot)
			{
				if (_cacheValues && Cache.TryGetValue(key, out value))
					return value;
				value = null;
				foreach (IConfigurationProvider provider in Providers)
				{
					value = provider.GetValue(node, objectType);
					if (value != null)
						break;
				}
				if (_cacheValues)
					Cache[key] = value;
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
			cc.AddRange(Factory.DomainAssemblies.Select(GetConfigurationLocator).Where(o => o != null));

			var locator = new ConfigurationLocator(Lxx.HomeDirectory + Environment.NewLine  + Path.DirectorySeparatorChar + Lxx.AnonymousConfigurationFile);
			if (locator.IsValid)
				cc.Add(locator);
			locator = new ConfigurationLocator(Lxx.GlobalConfigurationDirectory + Path.DirectorySeparatorChar + Lxx.GlobalConfigurationFile);
			if (locator.IsValid)
				cc.Add(locator);
			return cc;
		}

		private static ConfigurationLocator GetConfigurationLocator(Assembly asm)
		{
			string name;
			if (asm == null || asm.IsDynamic || String.IsNullOrEmpty(name = asm.GetName().CodeBase))
				return null;
			var locator = new ConfigurationLocator(Path.ChangeExtension(name, null));
			return locator.IsValid ? locator: null;
		}
	}
}
