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
		private const int DefaultMaxQueueSize = 256 * 1024;
		private const int DefaultLogoffTimeout = 5000;

		internal const int LogWatcherSleep = 500;

		internal const int LogTypeCount = (int)LogType.MaxValue + 1;

		private static readonly LoggingConfiguration[] DefaultLoggingConfiguration = new [] {
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
			}};

		internal const int FlushBoundMultiplier = 1024;

		private static readonly List<Logger> _loggers = new List<Logger>(64);
		private static volatile bool _enabled = true;
		private static volatile int _lockDepth;
		private static volatile bool _initialized;
		private static volatile bool _stopped;
		private static readonly object SyncRoot = new object();

		internal static int MaxQueueSize { get; private set; }
		internal static int LogoffTimeout { get; private set; }

		private static void Initialize()
		{
			if (_stopped || _initialized)
				return;

			lock (SyncRoot)
			{
				if (_stopped || _initialized)
					return;

				ApplyConfiguration();
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
				ApplyConfiguration();
				_initialized = true;
				foreach (var logger in _loggers)
				{
					logger.SetListeners(GetListeners(logger.Source));
				}
				OnChanged();
			}
		}

		private static void ApplyConfiguration()
		{
			var config = Config.GetList<LoggingConfiguration>("logging");
			if (config == null)
			{
				Config.LogConfigurationEvent("Lexxys.LoggingContext", SR.LoggingConfidurationMissing());
				config = DefaultLoggingConfiguration;
			}

			LoggingRule.GlobalExclude = String.Join(",", config.Select(o => o.Exclude.TrimToNull()).Where(o => o != null)).TrimToNull();

			MaxQueueSize = Value(config.Select(o => o?.MaxQueueSize).FirstOrDefault(o => o != null), 0, int.MaxValue, DefaultMaxQueueSize);
			LogoffTimeout = Value((int?)(config.Select(o => o?.LogoffTimeout).FirstOrDefault(o => o != null)?.Ticks / TimeSpan.TicksPerMillisecond), 0, int.MaxValue, DefaultLogoffTimeout);

			LogRecordsListener.Initialize(GetLogWriters(config));

			static int Value(int? value, int min, int max, int def)
				=> value == null ? def : Math.Max(min, Math.Min(max, value.GetValueOrDefault()));
		}

		private static IEnumerable<ILogWriter> GetLogWriters(IEnumerable<LoggingConfiguration> config)
		{
			var writers = config.SelectMany(o => o.LogItems).Select(o => LogWriter.FromXml(o)).Where(o => o != null).ToList();
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
						if (IsInitialized)
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

		private class LoggingConfiguration
		{
			public TimeSpan? LogoffTimeout;
			public string Exclude;
			public int? MaxQueueSize;
			public List<XmlLiteNode> LogItems;

			public static LoggingConfiguration FromXml(XmlLiteNode config)
			{
				if (config == null || config.IsEmpty)
					return null;

				return new LoggingConfiguration
				{
					Exclude = XmlTools.GetString(config["exclude"], null),
					LogoffTimeout = XmlTools.GetTimeSpan(config["logoffTimeout"], null),
					MaxQueueSize = XmlTools.GetInt32(config["maxQueueSize"], null),
					LogItems = config.Where("logger").Where(o => !o.IsEmpty).ToList(),
				};
			}
		}
	}
}
