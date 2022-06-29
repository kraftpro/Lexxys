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
using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	public static class Lxx
	{
		private static bool _initialized;

		public static event EventHandler<ConfigurationEventArgs>? ConfigurationChanged;
		public static event EventHandler<ConfigurationEventArgs>? ConfigurationInitialized;
		public static event EventHandler<ThreadExceptionEventArgs>? UnhandledException;

		public static ILogging? Log
		{
			get
			{
				if (_log == null)
					Interlocked.CompareExchange(ref _log, StaticServices.TryCreate<ILogging>("Lexxys.Lxx"), null);
				return _log;
			}
		}
		private static ILogging? _log;

		public static string AnonymousConfigurationFile => "application";

		public static string GlobalConfigurationFile => "global";

#if NETCOREAPP
		public static string GlobalConfigurationDirectory => @"C:\Application\Config";
#else
		public static string GlobalConfigurationDirectory => ConfigurationManager.AppSettings["ConfigurationDirectory"] ?? @"C:\Application\Config";
#endif

		public static string ConfigurationFile => __configurationFile.Value;
		private static Lazy<string> __configurationFile = new Lazy<string>(() => GetConfigurationFile(), true);

		private static string GetConfigurationFile()
		{
			string configFile = AssemblyLocation;
			if (configFile.Length == 0)
			{
				configFile = AnonymousConfigurationFile;
			}
			else
			{
				int i = configFile.LastIndexOf('\\');
				int j = configFile.LastIndexOf('/');
				if (i < j)
					i = j;
				if (i >= 0)
					configFile = configFile.Substring(i + 1);
				i = configFile.LastIndexOf('.');
				if (i > 0)
					configFile = configFile.Substring(0, i);
			}
			return configFile;
		}

		public static string? ProductName => __productName.Value;
		private static Lazy<string?> __productName = new Lazy<string?>(() => FileVersionInfo.GetVersionInfo(AssemblyLocation).ProductName, true);

		private static string AssemblyLocation => __assemblyName.Value;
		private static Lazy<string> __assemblyName = new Lazy<string>(() => GetEntry().Location, true);

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

		public static string HomeDirectory { get => __appDirectory.Value; set => __appDirectory = new Lazy<string>(() => value); }
		private static Lazy<string> __appDirectory = new Lazy<string>(() => (AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory).TrimEnd('/', '\\'));

		internal static void OnConfigurationInitialized(object? sender, ConfigurationEventArgs e)
		{
			if (!_initialized)
			{
				lock (SyncRoot)
				{
					if (!_initialized)
					{
						Config.Current.Changed += OnConfigChanged;
						AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
						Type type = Factory.GetType("System.Windows.Forms.Application", Factory.SystemAssemblies);
						if (type != null)
						{
							EventInfo? te = type.GetEvent("ThreadException");
							if (te != null)
								te.AddEventHandler(null, new ThreadExceptionEventHandler(OnGuiUnhandedException));
						}
						_initialized = true;
					}
				}
			}
			ConfigurationInitialized?.Invoke(sender, e);
		}

		private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
		{
			EventHandler<ThreadExceptionEventArgs>? handler = UnhandledException;
			if (handler != null && e.ExceptionObject is Exception x && !x.IsCriticalException())
				handler(sender, new ThreadExceptionEventArgs(x));
		}

		private static void OnGuiUnhandedException(object? sender, ThreadExceptionEventArgs e)
		{
			UnhandledException?.Invoke(sender, e);
		}

		private static void OnConfigChanged(object? sender, ConfigurationEventArgs e)
		{
			ConfigurationChanged?.Invoke(sender, e);
		}

		private static readonly object SyncRoot = new object();
	}
}



