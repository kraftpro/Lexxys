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

	public class Logger: ILogger, ILoggerEx
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

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Trace => TraceEnabled,
				LogLevel.Debug => DebugEnabled,
				LogLevel.Information => InfoEnabled,
				LogLevel.Warning => WarningEnabled,
				LogLevel.Error => ErrorEnabled,
				LogLevel.Critical => WriteEnabled,
				_ => false
			};
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState? state, Exception? exception, Func<TState?, Exception?, string?>? formatter)
		{
			if (!IsEnabled(logLevel))
				return;
			IDictionary? args = state as IDictionary;
			string? message = null;
			if (formatter != null)
				message = formatter(state, exception);
			if (message == null && args == null)
				message = state?.ToString();
			var logType = LogTypeFromLogLevel(logLevel);
			if (exception == null)
			{
				Log(new LogRecord(logType, Source, message, args));
			}
			else
			{
				Log(new LogRecord(logType, Source, message, exception, args));
				InnerExceptions(logType, Source, exception);
			}

			static LogType LogTypeFromLogLevel(LogLevel logLevel)
			{
				{
					return logLevel switch
					{
						LogLevel.Trace => LogType.Trace,
						LogLevel.Debug => LogType.Debug,
						LogLevel.Information => LogType.Information,
						LogLevel.Warning => LogType.Warning,
						LogLevel.Error => LogType.Error,
						LogLevel.Critical => LogType.Output,
						_ => LogType.MaxValue
					};
				}
			}
		}

		public IDisposable? BeginScope<TState>(TState state)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			var section = state?.ToString() ?? typeof(TState).Name;
			return Entry.Create(this, section, LogType.Output, 0, null);
		}

		#endregion

		#region ILogger, ILoggerEx extensions

		#region Trace

		public void Trace(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!TraceEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Trace, source ?? Source, exception);
			}
		}

		public void Trace(string? source, Exception exception)
		{
			if (TraceEnabled)
				Exception(LogType.Trace, source, exception);
		}

		public void Trace(string message, IDictionary args)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message, args));
		}

		public void Trace(string message)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message, null));
		}

		public void Trace(string message, string arg1Name, object arg1Value)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Trace(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Trace(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Trace(string message, params object[] args)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message, LogRecord.Args(args)));
		}

		public void Trace(Func<string> message, Func<IDictionary>? args = null)
		{
			if (TraceEnabled)
				Stream(LogType.Trace)?.Write(new LogRecord(LogType.Trace, Source, message(), args?.Invoke()));
		}

		public IDisposable? TraceEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Trace, section, args);
		}

		public IDisposable? TraceEnter(string? section, params object[] args)
		{
			return Enter(LogType.Trace, section, LogRecord.Args(args));
		}

		public IDisposable? TraceTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Trace, description, threshold);
		}

		#endregion

		#region Debug

		public void Debug(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!DebugEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Debug, source ?? Source, exception);
			}
		}

		public void Debug(string? source, Exception exception)
		{
			if (DebugEnabled)
				Exception(LogType.Debug, source, exception);
		}

		public void Debug(string message, IDictionary args)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message, args));
		}

		public void Debug(string message)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message, null));
		}

		public void Debug(string message, string arg1Name, object arg1Value)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Debug(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Debug(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Debug(string message, params object[] args)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message, LogRecord.Args(args)));
		}

		public void Debug(Func<string> message, Func<IDictionary>? args = null)
		{
			if (DebugEnabled)
				Stream(LogType.Debug)?.Write(new LogRecord(LogType.Debug, Source, message(), args?.Invoke()));
		}

		public IDisposable? DebugEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Debug, section, args);
		}

		public IDisposable? DebugEnter(string? section, params object[] args)
		{
			return Enter(LogType.Debug, section, LogRecord.Args(args));
		}

		public IDisposable? DebugTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Debug, description, threshold);
		}

		#endregion

		#region Info

		public void Info(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!InfoEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Information, source ?? Source, exception);
			}
		}

		public void Info(string? source, Exception exception)
		{
			if (InfoEnabled)
				Exception(LogType.Information, source, exception);
		}

		public void Info(string message, IDictionary args)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message, args));
		}

		public void Info(string message)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message, null));
		}

		public void Info(string message, string arg1Name, object arg1Value)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Info(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Info(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Info(string message, params object[] args)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message, LogRecord.Args(args)));
		}

		public void Info(Func<string> message, Func<IDictionary>? args = null)
		{
			if (InfoEnabled)
				Stream(LogType.Information)?.Write(new LogRecord(LogType.Information, Source, message(), args?.Invoke()));
		}

		public IDisposable? InfoEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Information, section, args);
		}

		public IDisposable? InfoEnter(string? section, params object[] args)
		{
			return Enter(LogType.Information, section, LogRecord.Args(args));
		}

		public IDisposable? InfoTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Information, description, threshold);
		}

		#endregion

		#region Warning

		public void Warning(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!WarningEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Warning, source ?? Source, exception);
			}
		}

		public void Warning(string? source, Exception exception)
		{
			if (WarningEnabled)
				Exception(LogType.Warning, source, exception);
		}

		public void Warning(string message, IDictionary args)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message, args));
		}

		public void Warning(string message)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message, null));
		}

		public void Warning(string message, string arg1Name, object arg1Value)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Warning(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Warning(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Warning(string message, params object[] args)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message, LogRecord.Args(args)));
		}

		public void Warning(Func<string> message, Func<IDictionary>? args = null)
		{
			if (WarningEnabled)
				Stream(LogType.Warning)?.Write(new LogRecord(LogType.Warning, Source, message(), args?.Invoke()));
		}

		public IDisposable? WarningEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Warning, section, args);
		}

		public IDisposable? WarningEnter(string? section, params object[] args)
		{
			return Enter(LogType.Warning, section, LogRecord.Args(args));
		}

		public IDisposable? WarningTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Warning, description, threshold);
		}

		#endregion

		#region Error

		public void Error(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!ErrorEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Error, source ?? Source, exception);
			}
		}

		public void Error(string? source, Exception exception)
		{
			if (ErrorEnabled)
				Exception(LogType.Error, source, exception);
		}

		public void Error(string message, IDictionary args)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message, args));
		}

		public void Error(string message)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message, null));
		}

		public void Error(string message, string arg1Name, object arg1Value)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Error(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Error(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Error(string message, params object[] args)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message, LogRecord.Args(args)));
		}

		public void Error(Func<string> message, Func<IDictionary>? args = null)
		{
			if (ErrorEnabled)
				Stream(LogType.Error)?.Write(new LogRecord(LogType.Error, Source, message(), args?.Invoke()));
		}

		public IDisposable? ErrorEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Error, section, args);
		}

		public IDisposable? ErrorEnter(string? section, params object[] args)
		{
			return Enter(LogType.Error, section, LogRecord.Args(args));
		}

		public IDisposable? ErrorTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Error, description, threshold);
		}

		#endregion

		#region Write

		public void Write(string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!WriteEnabled)
				return;
			if (exception == null)
			{
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, source ?? Source, message, args));
			}
			else
			{
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, source ?? Source, message, exception, args));
				InnerExceptions(LogType.Output, source ?? Source, exception);
			}
		}

		public void Write(string? source, Exception exception)
		{
			if (WriteEnabled)
				Exception(LogType.Output, source, exception);
		}

		public void Write(string message, IDictionary args)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message, args));
		}

		public void Write(string message)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message, null));
		}

		public void Write(string message, string arg1Name, object arg1Value)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public void Write(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public void Write(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public void Write(string message, params object[] args)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message, LogRecord.Args(args)));
		}

		public void Write(Func<string> message, Func<IDictionary>? args = null)
		{
			if (WriteEnabled)
				Stream(LogType.Output)?.Write(new LogRecord(LogType.Output, Source, message(), args?.Invoke()));
		}

		public IDisposable? WriteEnter(string? section, IDictionary? args)
		{
			return Enter(LogType.Output, section, args);
		}

		public IDisposable? WriteEnter(string? section, params object[] args)
		{
			return Enter(LogType.Output, section, LogRecord.Args(args));
		}

		public IDisposable? WriteTiming(string? description, TimeSpan threshold = default)
		{
			return Timing(LogType.Output, description, threshold);
		}

		#endregion

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Exception(LogType logType, string? source, Exception exception)
		{
			Stream(logType)?.Write(new LogRecord(logType, source, exception));
			InnerExceptions(logType, source, exception);
		}

		private void InnerExceptions(LogType logType, string? source, Exception exception)
		{
			int i = 0;
			while (exception.InnerException != null)
			{
				exception = exception.InnerException;
				Stream(logType)?.Write(new LogRecord(logType, source, exception, ++i));
			}
		}

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
