// Lexxys Infrastructural library.
// file: Lxx.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Diagnostics;
using System.Reflection;

using Microsoft.Extensions.Logging;

namespace Lexxys;

using Configuration;

public static class Lxx
{
	private static bool _initialized;

	public const string Framework =
#if NETFRAMEWORK
		"framework";
#elif NETSTANDARD
		"standard";
#elif NET8_0_OR_GREATER
		"net8";
#elif NET7_0_OR_GREATER
		"net7";
#elif NET6_0_OR_GREATER
		"net6";
#elif NET5_0_OR_GREATER
		"net5";
#else
		"unknown";
#endif

	public static event EventHandler<ConfigurationEventArgs>? ConfigurationChanged;
	public static event EventHandler<ConfigurationEventArgs>? ConfigurationInitialized;
	public static event EventHandler<ThreadExceptionEventArgs>? UnhandledException;

	public static ILogger? Log
	{
		get
		{
			if (_log == null)
				Interlocked.CompareExchange(ref _log, Statics.TryGetLogger("Lexxys"), null);
			return _log;
		}
	}
	private static ILogger? _log;

	public static string AnonymousConfigurationFile => "application";

	public static string GlobalConfigurationFile => "global";

	public static string? ProductName => __productName.Value;
	private static readonly Lazy<string?> __productName = new Lazy<string?>(() => FileVersionInfo.GetVersionInfo(AssemblyLocation).ProductName, true);

	private static string AssemblyLocation => __assemblyName.Value;
	private static readonly Lazy<string> __assemblyName = new Lazy<string>(() => GetEntry().Location, true);

	private static Assembly GetEntry()
	{
		var a = Assembly.GetEntryAssembly();
		if (a == null || a.IsDynamic)
		{
			var sf = new StackTrace().GetFrames();
			if (sf is { Length: >0 })
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
					if (sender is IConfigService service)
						service.Changed += OnConfigChanged;
					AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
					Type? type = Factory.GetType("System.Windows.Forms.Application", Factory.SystemAssemblies);
					if (type != null)
					{
						EventInfo? te = type.GetEvent("ThreadException");
						if (te != null)
							te.AddEventHandler(null, OnGuiUnhandedException);
					}
					_initialized = true;
				}
			}
		}
		ConfigurationInitialized?.Invoke(sender, e);
	}

	private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
	{
		var handler = UnhandledException;
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



