// Lexxys Infrastructural library.
// file: Lxx.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Configuration;
using System.IO;

namespace Lexxys
{
	public static class Lxx
	{
		private static Logger _log;
		private static FileVersionInfo _appVersion;
		private static string _productName;
		private static string _appDirectory;
		private static string _configurationFile;
		private static bool _initialized;

		public static event EventHandler<ConfigurationEventArgs> ConfigurationChanged;
		public static event EventHandler<ConfigurationEventArgs> ConfigurationInitialized;
		public static event EventHandler<ThreadExceptionEventArgs> UnhandledException;

		public static ILogging Log
		{
			get
			{
				if (_log == null)
					Interlocked.CompareExchange(ref _log, new Logger("Lexxys.Lxx"), null);
				return _log;
			}
		}

#if NETCOREAPP
		public static string AnonymousConfigurationFile => "application";
#else
		public static string AnonymousConfigurationFile => ConfigurationManager.AppSettings["AnonymousConfigurationFile"] ?? "application";
#endif

#if NETCOREAPP
		public static string GlobalConfigurationFile => "global";
#else
		public static string GlobalConfigurationFile => ConfigurationManager.AppSettings["GlobalConfigurationFile"] ?? "global";
#endif

#if NETCOREAPP
		public static string GlobalConfigurationDirectory => @"C:\Application\Config";
#else
		public static string GlobalConfigurationDirectory => ConfigurationManager.AppSettings["ConfigurationDirectory"] ?? @"C:\Application\Config";
#endif

		public static string ConfigurationFile
		{
			get
			{
				if (_configurationFile == null)
				{
					string cf = GetEntry().Location;
					if (cf.Length == 0)
					{
						cf = AnonymousConfigurationFile;
					}
					else
					{
						int i = cf.LastIndexOf('\\');
						int j = cf.LastIndexOf('/');
						if (i < j)
							i = j;
						if (i >= 0)
							cf = cf.Substring(i + 1);
						i = cf.LastIndexOf('.');
						if (i > 0)
							cf = cf.Substring(0, i);
					}
					Interlocked.CompareExchange(ref _configurationFile, cf, null);
				}
				return _configurationFile;
			}
		}

		public static string ProductName
		{
			get
			{
				if (_productName == null)
				{
					string temp = Config.Current.GetValue("ProductVersion:Name", () => AppVersion.ProductName).Value;
					if (!Configuration.DefaultConfig.IsInitialized)
						return temp;
					Interlocked.CompareExchange(ref _productName, temp, null);
				}
				return _productName;
			}
		}

		private static FileVersionInfo AppVersion
		{
			[System.Security.SecuritySafeCritical]
			get
			{
				if (_appVersion == null)
				{
					lock (SyncRoot)
					{
						_appVersion ??= FileVersionInfo.GetVersionInfo(GetEntry().Location);
					}
				}
				return _appVersion;
			}
		}

		private static Assembly GetEntry()
		{
			var a = Assembly.GetEntryAssembly();
			if (a == null || a.IsDynamic)
			{
				StackFrame[] sf = new StackTrace().GetFrames();
				if (sf.Length > 0)
				{
					for (int i = sf.Length - 1; i >= 0; --i)
					{
						a = sf[i].GetMethod()?.DeclaringType?.Assembly;
						if (a != null && !a.IsDynamic)
							break;
					}
				}
				if (a == null || a.IsDynamic)
					a = Assembly.GetCallingAssembly();
				if (a.IsDynamic)
					a = Assembly.GetExecutingAssembly();
			}
			Debug.Assert(!a.IsDynamic);
			return a;
		}

		public static string HomeDirectory
		{
			get
			{
				if (_appDirectory == null)
					Interlocked.CompareExchange(ref _appDirectory, AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), null);
				return _appDirectory;
			}
			set
			{
				Interlocked.Exchange(ref _appDirectory, value?.TrimEnd(Path.DirectorySeparatorChar));
			}
		}

		internal static void OnConfigurationInitialized(object sender, ConfigurationEventArgs e)
		{
			if (!_initialized)
			{
				lock (SyncRoot)
				{
					if (!_initialized)
					{
						_initialized = true;
						Config.Current.Changed += OnConfigChanged;
						AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
						Type type = Factory.GetType("System.Windows.Forms.Application", Factory.SystemAssemblies);
						if (type != null)
						{
							EventInfo te = type.GetEvent("ThreadException");
							if (te != null)
								te.AddEventHandler(null, new ThreadExceptionEventHandler(OnGuiUnhandedException));
						}
					}
				}
			}
			ConfigurationInitialized?.Invoke(sender, e);
		}

		private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			EventHandler<ThreadExceptionEventArgs> handler = UnhandledException;
			if (handler != null && e.ExceptionObject is Exception x && !x.IsCriticalException())
				handler(sender, new ThreadExceptionEventArgs(x));
		}

		private static void OnGuiUnhandedException(object sender, ThreadExceptionEventArgs e)
		{
			UnhandledException?.Invoke(sender, e);
		}

		private static void OnConfigChanged(object sender, ConfigurationEventArgs e)
		{
			ConfigurationChanged?.Invoke(sender, e);
		}

		private static readonly object SyncRoot = new object();
	}
}



