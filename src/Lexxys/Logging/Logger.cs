// Lexxys Infrastructural library.
// file: Logger.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	using Logging;

	public class Logger<T>: Logger, ILogger<T>
	{
		public Logger(): base(typeof(T).Name)
		{
		}
	}	

	public class Logger: ILogging
	{
		private LogRecordsListener[] _listeners;
		private LogTypeMask _levels;

		public Logger(string source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			Source = source;
			_listeners = Array.Empty<LogRecordsListener>();
			_levels = LogTypeMask.None;
			LoggingContext.Register(this);
		}

		public static Logger? TryCreate(string source)
		{
			return LoggingContext.IsInitialized ? new Logger(source): null;
		}

		internal void SetListeners(LogRecordsListener[] listeners)
		{
			_listeners = listeners;
			_levels = LoggingContext.IsEnabled ? SetToOn(_listeners): LogTypeMask.None;
		}

		private static LogTypeMask SetToOn(LogRecordsListener[] listeners)
		{
			int levels = 0;
			for (int i = 0, m = 1; i < listeners.Length; ++i, m <<= 1)
			{
				if (listeners[i] != null)
					levels |= m;
			}
			return (LogTypeMask)levels;
		}

		/// <summary>
		/// Get logger source (usually class name)
		/// </summary>
		public string Source { get; }

		/// <summary>
		/// True, if Direct messages will be logged (Write(...) methods)
		/// </summary>
		public bool WriteEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Output) != 0;
		}

		/// <summary>
		/// True, if Error messages will be logged (Error(...) methods)
		/// </summary>
		public bool ErrorEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Error) != 0;
		}
		/// <summary>
		/// True, if Warning messages will be logged (Warning(...) methods)
		/// </summary>
		public bool WarningEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Warning) != 0;
		}
		/// <summary>
		/// True, if Information messages will be logged (Info(...) methods)
		/// </summary>
		public bool InfoEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Information) != 0;
		}
		/// <summary>
		/// True, if Debug messages will be logged (Debug(...) methods)
		/// </summary>
		public bool DebugEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Debug) != 0;
		}
		/// <summary>
		/// True, if Trace messages will be logged (Trace(...) methods)
		/// </summary>
		public bool TraceEnabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_levels & LogTypeMask.Trace) != 0;
		}

		/// <summary>
		/// Write the <paramref name="record"/> into log
		/// </summary>
		/// <param name="record">The log record to be writen</param>
		public void Log(LogRecord record)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));
			_listeners[(int)record.LogType]?.Write(record);
		}

		public IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args)
		{
			if (!IsEnabled(logType))
				return null;
			return Entry.Create(this, sectionName, logType, 0, args);
		}

		public IDisposable? Timing(LogType logType, string? description, TimeSpan threshold)
		{
			if (!IsEnabled(logType))
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, logType, n == 0 ? -1: n, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsEnabled(LogType logType) => (_levels & (LogTypeMask)(1 << (int)logType)) != 0;

		public void TurnOn()
		{
			_levels = SetToOn(_listeners);
		}

		public void TurnOff()
		{
			_levels = 0;
		}

		#region Global methods and static helpers

		public static OrderedBag<string, object?>? Args(params object?[] args)
		{
			return LogRecord.Args(args);
		}

		public static bool Initialized => LoggingContext.IsInitialized;

		public static void Flush()
		{
			LoggingContext.FlushBuffers();
		}

		public static void Close()
		{
			LoggingContext.Stop();
		}

		/// <summary>
		/// Write a message to the Debugger console
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteDebugMessage(string source, string message)
		{
			LogWriter.WriteDebugMessage(source, message);
		}

		/// <summary>
		/// Write a message to the Debugger console
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteDebugMessage(string source, Func<string> message)
		{
			LogWriter.WriteDebugMessage(source, message);
		}

		/// <summary>
		/// Write informational message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteEventLogMessage(string source, string message)
		{
			LogWriter.WriteEventLogMessage(new LogRecord(LogType.Information, source, message, null));
		}

		/// <summary>
		/// Write informational message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteEventLogMessage(string source, Func<string> message)
		{
			LogWriter.WriteEventLogMessage(new LogRecord(LogType.Information, source, message(), null));
		}

		/// <summary>
		/// Write error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteErrorMessage(string source, string message)
		{
			LogWriter.WriteErrorMessage(source, message, null);
		}

		/// <summary>
		/// Write error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteErrorMessage(string source, Func<string> message)
		{
			LogWriter.WriteErrorMessage(source, message(), null);
		}

		/// <summary>
		/// Write exception info to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="exception">The exception</param>
		public static void WriteErrorMessage(string source, Exception exception)
		{
			LogWriter.WriteErrorMessage(source, exception);
		}

		public static int LockLogging() => LoggingContext.LockLogging();
		public static int UnlockLogging() => LoggingContext.LockLogging();

		#endregion

		#region ILogger

		bool ILogger.IsEnabled(LogLevel logLevel)
			=> LoggingTools.IsEnabled(this, logLevel);

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
			=> LoggingTools.Log(this, logLevel, eventId, state, exception, formatter);

		IDisposable? ILogger.BeginScope<TState>(TState state)
			=> LoggingTools.BeginScope(this, state);

		#endregion

		private class Entry: IDisposable
		{
			public static IDisposable Empty = new NotEntry();

			private Logger _log;
			private readonly string _endMessage;
			private readonly long _stamp;
			private readonly long _threshold;
			private readonly LogType _logType;
			private readonly IDictionary? _arg;

			public Entry(Logger log, string? endMessage, LogType logType, int threshold, IDictionary? arg)
			{
				_log = log;
				_endMessage = endMessage ?? "exiting";
				_threshold = threshold * WatchTimer.TicksPerMillisecond;
				_logType = logType;
				_arg = arg;
				_stamp = WatchTimer.Start();
			}

			public static Entry Create(Logger log, string? sectionName, LogType logType, int threshold, IDictionary? arg)
			{
				if (threshold == 0)
				{
					var rec = new LogRecord(LogGroupingType.BeginGroup, logType, log.Source, (sectionName == null ? SR.LOG_BeginSection() : SR.LOG_BeginSection(sectionName)), arg);
					log.Log(rec);
				}
				return new Entry(log, (sectionName == null ? SR.LOG_EndSection() : SR.LOG_EndSection(sectionName)), logType, threshold, threshold == 0 ? null : arg);
			}

			public static Entry Create(Logger log, string? startMessage, string? endMessage, LogType logType, int threshold, IDictionary? arg)
			{
				if (threshold == 0)
				{
					var rec = new LogRecord(LogGroupingType.BeginGroup, logType, log.Source, startMessage ?? SR.LOG_BeginGroup(), arg);
					log.Log(rec);
				}
				return new Entry(log, (endMessage ?? SR.LOG_EndGroup()), logType, threshold, threshold == 0 ? null : arg);
			}

			void IDisposable.Dispose()
			{
				if (!_disposed)
				{
					_disposed = true;
					long time = WatchTimer.Stop(_stamp);
					if (_threshold == 0 || _threshold <= time)
					{
						_log.Log(
							new LogRecord(_threshold == 0 ? LogGroupingType.EndGroup : LogGroupingType.Message,
								_logType, _log.Source, _endMessage + " (" + WatchTimer.ToString(time, false) + ")", _arg)
							);
					}
				}
			}
			private bool _disposed;

			private class NotEntry: IDisposable
			{
				public void Dispose()
				{
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private LogRecordsListener Stream(LogType logType) => _listeners[(int)logType];
	}

}
