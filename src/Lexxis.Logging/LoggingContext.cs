// Lexxys Infrastructural library.
// file: LoggingContext.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lexxys.Configuration;
using Lexxys.Xml;

namespace Lexxys.Logging;

internal static class LoggingContext
{
	private const int DefaultMaxQueueSize = 256 * 1024;
	private const int DefaultLogoffTimeout = 5000;

	internal const int LogTypeCount = (int)LogType.MaxValue + 1;
	internal const int LogWatcherSleep = 500;
	internal const int FlushTimeout = 1000;
	internal const int FlushBoundMultiplier = 1024;

	private static readonly LoggingConfiguration[] DefaultLoggingConfiguration = new [] {
		new LoggingConfiguration(null, null, null, 
			new List<XmlLiteNode>
			{
				new XmlLiteBuilder(true)
					.Element("logger")
						.Attrib("name", "TextFile")
						.Attrib("class", "Lexxys.Logging.TextFileLogWriter")
						.Attrib("file", "{YMD}-XX.log")
						.Element("rule")
							.Attrib("level", "TRACE")
						.End()
					.End().GetFirstNode()
			})
		};

	private static volatile bool _initialized;
	private static readonly object SyncRoot = new object();

	internal static int MaxQueueSize { get; private set; }
	internal static int LogoffTimeout { get; private set; }

	internal static bool Initialize()
	{
		if (_initialized)
			return false;

		lock (SyncRoot)
		{
			if (_initialized)
				return false;

			ApplyConfiguration();
			_initialized = true;
			StaticServices.TryCreate<IConfigLogger>()?.SetLogger();
			// DefaultConfig.OnLoggerInitialized();
		}

		bool registered = false;
		try
		{
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_Exit;
			registered = true;
		}
		catch
		{
			// ignored
		}
		try
		{
			AppDomain.CurrentDomain.DomainUnload += CurrentDomain_Exit;
			registered = true;
		}
		catch
		{
			// ignored
		}
		Lxx.UnhandledException += Lxx_UnhandledException;
		if (!registered)
		{
			var watcher = new Thread(LogWatcher) { Name = "LOG Watcher", Priority = ThreadPriority.Lowest };
			watcher.Start(Thread.CurrentThread);
		}

		Lxx.ConfigurationChanged += Lxx_ConfigurationChanged;

		return true;
	}

	private static void Lxx_ConfigurationChanged(object? sender, ConfigurationEventArgs e)
	{
		if (!_initialized || !LogRecordsService.IsStarted)
			return;
		lock (SyncRoot)
		{
			if (!_initialized || !LogRecordsService.IsStarted)
				return;
			ApplyConfiguration();
		}
	}

	private static void ApplyConfiguration()
	{
		var config = Config.Current.GetCollection<LoggingConfiguration>("logging").Value;
		if (config.Count == 0)
		{
			Config.LogConfigurationEvent("Lexxys.LoggingContext", SR.LoggingConfidurationMissing());
			config = DefaultLoggingConfiguration;
		}

		LoggingRule.GlobalExclude = String.Join(",", config.Select(o => o.Exclude.TrimToNull()).Where(o => o != null)).TrimToNull();

		MaxQueueSize = Value(config.Select(o => o?.MaxQueueSize).FirstOrDefault(o => o != null), 0, int.MaxValue, DefaultMaxQueueSize);
		LogoffTimeout = Value((int?)(config.Select(o => o?.LogoffTimeout).FirstOrDefault(o => o != null)?.Ticks / TimeSpan.TicksPerMillisecond), 0, int.MaxValue, DefaultLogoffTimeout);

		LogRecordsService.Start(GetLogWriters(config));

		static int Value(int? value, int min, int max, int def)
			=> value == null ? def : Math.Max(min, Math.Min(max, value.GetValueOrDefault()));
	}

	private static IEnumerable<ILogWriter> GetLogWriters(IEnumerable<LoggingConfiguration> config)
	{
		List<ILogWriter> writers = config.SelectMany(o => o.Loggers).Select(o => LogWriter.FromXml(o)).Where(o => o != null).ToList()!;
		if (System.Diagnostics.Debugger.IsLogging())
		{
			foreach (var item in writers)
			{
				SystemLog.WriteDebugMessage("Lexxys.LoggingContext", "Writer: " + item.Name + " -> " + item.Target);
			}
		}
		return writers;
	}

	private static void CurrentDomain_Exit(object? sender, EventArgs e) => LogRecordsService.Stop();

	private static void Lxx_UnhandledException(object? sender, ThreadExceptionEventArgs e) => LogRecordsService.Stop();

	private static void LogWatcher(object? caller)
	{
		if (caller is not Thread main)
			return;
		while (main.IsAlive)
		{
			Thread.Sleep(LogWatcherSleep);
		}
		LogRecordsService.Stop();
	}

	private class LoggingConfiguration
	{
		public TimeSpan? LogoffTimeout { get; }
		public string? Exclude { get; }
		public int? MaxQueueSize { get; }
		public List<XmlLiteNode> Loggers { get; }

		public LoggingConfiguration(TimeSpan? logoffTimeout, string? exclude, int? maxQueueSize, List<XmlLiteNode> loggers)
		{
			LogoffTimeout = logoffTimeout;
			Exclude = exclude;
			MaxQueueSize = maxQueueSize;
			Loggers = loggers ?? new List<XmlLiteNode>();
		}

		public static LoggingConfiguration? FromXml(XmlLiteNode config)
		{
			if (config == null || config.IsEmpty)
				return null;

			return new LoggingConfiguration(
				exclude: XmlTools.GetString(config["exclude"], null),
				logoffTimeout: XmlTools.GetTimeSpan(config["logoffTimeout"], null),
				maxQueueSize: XmlTools.GetInt32(config["maxQueueSize"], null),
				loggers: config.Where("logger").Where(o => !o.IsEmpty).ToList());
		}
	}
}
