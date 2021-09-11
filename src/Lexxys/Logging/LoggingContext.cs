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

namespace Lexxys.Logging
{
	public static class LoggingContext
	{
		internal const int LogWatcherSleep = 500;

		internal const int LogTypeCount = (int)LogType.MaxValue + 1;

		private static readonly IReadOnlyList<LoggingConfiguration> DefaultLoggingConfiguration = new []
		{
			new LoggingConfiguration
			{
				LogItems = new List<XmlLiteNode>
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
				}
			}
		};

		internal const int FlushBoundMultiplier = 1024;

		private static readonly List<Logger> _loggers = new List<Logger>(64);
		private static volatile bool _enabled = true;
		private static volatile int _lockDepth;
		private static volatile bool _initialized;
		private static volatile bool _stopped;
		private static readonly object SyncRoot = new object();

		internal static TimeSpan LogoffTimeout { get; private set; }
		internal static TimeSpan ListenerTurnSleep { get; private set; }
		internal static TimeSpan ListenerPulseInterval { get; private set; }
		internal static int DefaultBatchSize { get; private set; }
		internal static int DefaultFlushBound { get; private set; }

		private static void Initialize()
		{
			if (_stopped || _initialized)
				return;

			lock (SyncRoot)
			{
				if (_stopped || _initialized)
					return;

				LogRecordsListener.Initialize(LoadConfiguration());
				_initialized = true;
				Config.OnLoggerInitialized();
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
		}

		private static void Lxx_ConfigurationChanged(object sender, ConfigurationEventArgs e)
		{
			if (_stopped || !_initialized)
				return;
			lock (SyncRoot)
			{
				if (_stopped || !_initialized)
					return;
				_initialized = false;
				LogRecordsListener.Initialize(LoadConfiguration());
				_initialized = true;
				foreach (var logger in _loggers)
				{
					logger.SetListeners(GetListeners(logger.Source));
				}
				OnChanged();
			}
		}

		private static List<LogWriter> LoadConfiguration()
		{
			var config = Config.GetList<LoggingConfiguration>("logging");
			if (config == null || config.Count == 0)
			{
				Config.LogConfigurationEvent("Lexxys.LoggingContext", SR.LoggingConfidurationMissing());
				config = DefaultLoggingConfiguration;
			}
			DefaultBatchSize = -1;
			DefaultFlushBound = -1;
			foreach (var item in config)
			{
				if (item.BatchSize != BatchSizeDefault && item.BatchSize > DefaultBatchSize)
					DefaultBatchSize = item.BatchSize;
				if (item.FlushBound != FlushBoundDefault && item.FlushBound > DefaultFlushBound)
					DefaultFlushBound = item.FlushBound;
			}
			if (DefaultBatchSize == -1)
				DefaultBatchSize = BatchSizeDefault;
			if (DefaultFlushBound == -1)
				DefaultFlushBound = FlushBoundDefault;

			LoggingRule.GlobalExclude = String.Join(",", config.Select(o => o.Exclude.TrimToNull()).Where(o => o != null)).TrimToNull();
			LogoffTimeout = config.Max(o => o.LogoffTimeout);
			ListenerTurnSleep = config.Max(o => o.ListenerTurnSleep);
			ListenerPulseInterval = config.Max(o => o.ListenerPulseInterval);

			var writers = config.SelectMany(o => o.LogItems).Select(o => XmlTools.GetValue(o, LogWriter.Empty)).Where(o => o != null && !o.Rule.IsEmpty).ToList();
			if (System.Diagnostics.Debugger.IsLogging())
			{
				foreach (var item in writers)
				{
					Logger.WriteDebugMessage("Lexxys.LoggingContext", "Writer: " + item.Name + " -> " + item.Target);
				}
			}
			return writers;
		}

		private static void CurrentDomain_Exit(object sender, EventArgs e)
		{
			Stop();
		}

		private static void Lxx_UnhandledException(object sender, ThreadExceptionEventArgs e)
		{
			Stop();
		}

		private static void LogWatcher(object caller)
		{
			if (caller is not Thread main)
				return;
			while (!_stopped && main.IsAlive)
			{
				Thread.Sleep(LogWatcherSleep);
			}
			Stop();
		}

		public static event EventHandler<EventArgs> Changed;

		public static void ClearBuffers()
		{
			if (_initialized)
				LogRecordsListener.ClearBuffers();
		}

		public static void FlushBuffers()
		{
			if (_initialized)
				LogRecordsListener.FlushBuffers();
		}

		public static void Stop(bool force = false)
		{
			if (_stopped || !_initialized)
				return;
			lock (SyncRoot)
			{
				if (_stopped || !_initialized)
					return;
				Disable();
				LogRecordsListener.StopAll(force);
				_initialized = true;
				_stopped = true;
			}
		}

		//public static void Start()
		//{
		//	if (!_stopped)
		//		return;
		//	lock (SyncRoot)
		//	{
		//		if (!_stopped)
		//			return;

		//		_stopped = false;
		//		_initialized = false;
		//		Initialize();
		//		Enable();
		//	}
		//}

		public static void Enable()
		{
			if (_enabled || _stopped || !IsInitialized)
				return;
			lock (SyncRoot)
			{
				if (_enabled || _stopped)
					return;
				_enabled = true;
				foreach (Logger t in _loggers)
				{
					t.TurnOn();
				}
			}
		}

		public static void Disable()
		{
			if (_enabled)
			{
				lock (SyncRoot)
				{
					if (_enabled)
					{
						_enabled = false;
						if (_initialized)
						{
							foreach (Logger t in _loggers)
							{
								t.TurnOff();
							}
						}
					}
				}
			}
		}

		public static int LockLogging() => Interlocked.Increment(ref _lockDepth);

		public static int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

		public static bool IsEnabled => _enabled && !_stopped && _lockDepth <= 0;

		public static bool IsInitialized => _initialized;

		internal static void Register(Logger logger)
		{
			if (_stopped)
				return;
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (!_initialized)
				Initialize();

			lock (SyncRoot)
			{
				if (!_loggers.Contains(logger))
				{
					logger.SetListeners(GetListeners(logger.Source));
					_loggers.Add(logger);
				}
			}
		}

		internal static LogRecordsListener[] GetListeners(string source)
		{
			if (_stopped)
				return LogRecordsListener.SelectEmpty();
			lock (SyncRoot)
			{
				if (_stopped)
					return LogRecordsListener.SelectEmpty();

				if (!_initialized)
					Initialize();
				return LogRecordsListener.Collect(source);
			}
		}

		private static void OnChanged()
		{
			if (_stopped)
				return;
			EventHandler<EventArgs> ch = Changed;
			ch?.Invoke(null, EventArgs.Empty);
		}

		public static readonly TimeSpan LogoffTimeoutMin = new TimeSpan(0, 0, 1);
		public static readonly TimeSpan LogoffTimeoutMax = new TimeSpan(0, 0, 30);
		public static readonly TimeSpan LogoffTimeoutDefault = new TimeSpan(0, 0, 5);
		public static readonly TimeSpan ListenerTurnSleepMin = TimeSpan.Zero;
		public static readonly TimeSpan ListenerTurnSleepMax = TimeSpan.FromTicks(200 * TimeSpan.TicksPerMillisecond);
		public static readonly TimeSpan ListenerTurnSleepDefault = TimeSpan.Zero;
		public static readonly TimeSpan ListenerPulseIntervalMin = TimeSpan.FromTicks(500 * TimeSpan.TicksPerMillisecond);
		public static readonly TimeSpan ListenerPulseIntervalMax = new TimeSpan(0, 5, 0);
		public static readonly TimeSpan ListenerPulseIntervalDefault = new TimeSpan(0, 0, 10);
		
		public const int BatchSizeMin = 1;
		public const int BatchSizeMax = 1024;
		public const int BatchSizeDefault = 4;
		public const int FlushBoundMin = 1;
		public const int FlushBoundMax = 1024;
		public const int FlushBoundDefault = 64;

		private class LoggingConfiguration
		{
			public TimeSpan LogoffTimeout;
			public TimeSpan ListenerTurnSleep;
			public TimeSpan ListenerPulseInterval;
			public string Exclude;
			public int BatchSize;
			public int FlushBound;
			public List<XmlLiteNode> LogItems;

			public static LoggingConfiguration FromXml(XmlLiteNode config)
			{
				if (config == null || config.IsEmpty)
					return null;

				return new LoggingConfiguration
					{
						Exclude = XmlTools.GetString(config["exclude"], null),
						LogoffTimeout = XmlTools.GetTimeSpan(config["logoffTimeout"], LogoffTimeoutDefault, LogoffTimeoutMin, LogoffTimeoutMax),
						ListenerTurnSleep = XmlTools.GetTimeSpan(config["turnSleep"], ListenerTurnSleepDefault, ListenerTurnSleepMin, ListenerTurnSleepMax),
						ListenerPulseInterval = XmlTools.GetTimeSpan(config["turnSleep"], ListenerPulseIntervalDefault, ListenerPulseIntervalMin, ListenerPulseIntervalMax),
						BatchSize = XmlTools.GetInt32(config["batchSize"], BatchSizeDefault, BatchSizeMin, BatchSizeMax),
						FlushBound = XmlTools.GetInt32(config["flushBound"], FlushBoundDefault, FlushBoundMin, FlushBoundMax),
						LogItems = config.Where("logger").Where(o => o != null && !o.IsEmpty).ToList(),
					};
			}
		}
	}
}
