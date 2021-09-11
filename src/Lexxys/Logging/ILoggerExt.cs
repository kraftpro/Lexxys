using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Lexxys.Logging;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	public interface ILoggerEx
	{
		string Source { get; }
		bool IsEnabled(LogType logType);
		void Log(LogRecord record);
		IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args);
		IDisposable? Timing(LogType logType, string? description, TimeSpan threshold);
	}

	public static class ILoggerExExtensions
	{
		public static void Log(this ILoggerEx logger, LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(logType))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(logType, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(logType, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, logType, source ?? logger.Source, exception);
			}
		}

		private static void Exception(ILoggerEx logger, LogType logType, string? source, Exception exception)
		{
			logger.Log(new LogRecord(logType, source ?? logger.Source, exception));
			InnerExceptions(logger, logType, source ?? logger.Source, exception);
		}

		private static void InnerExceptions(ILoggerEx logger, LogType logType, string? source, Exception exception)
		{
			int i = 0;
			while (exception.InnerException != null)
			{
				exception = exception.InnerException;
				var record = new LogRecord(logType, source, exception, ++i);
				logger.Log(record);
			}
		}

		#region Trace

		public static void Trace(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Trace))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Trace, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Trace, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Trace, source ?? logger.Source, exception);
			}
		}

		public static void Trace(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Trace))
				return;
			Exception(logger, LogType.Trace, source, exception);
		}

		public static void Trace(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, args));
		}

		public static void Trace(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, null));
		}

		public static void Trace(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Trace(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Trace(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Trace(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Trace(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? TraceEnter(this ILoggerEx logger, string? section, IDictionary? args)
		{
			return logger.Enter(LogType.Trace, section, args);
		}

		public static IDisposable? TraceEnter(this ILoggerEx logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Trace, section, LogRecord.Args(args));
		}

		public static IDisposable? TraceTiming(this ILoggerEx logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Trace, description, threshold);
		}

		#endregion

		#region Debug

		public static void Debug(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Debug))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Debug, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Debug, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Debug, source ?? logger.Source, exception);
			}
		}

		public static void Debug(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Debug))
				return;
			Exception(logger, LogType.Debug, source, exception);
		}

		public static void Debug(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, args));
		}

		public static void Debug(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, null));
		}

		public static void Debug(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Debug(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Debug(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Debug(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Debug(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? DebugEnter(this ILoggerEx logger, string message, IDictionary? args)
		{
			return logger.Enter(LogType.Debug, message, args);
		}

		public static IDisposable? DebugEnter(this ILoggerEx logger, string message, params object[] args)
		{
			return logger.Enter(LogType.Debug, message, LogRecord.Args(args));
		}

		public static IDisposable? DebugTiming(this ILoggerEx logger, string description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Debug, description, threshold);
		}

		#endregion

		#region Info

		public static void Info(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Information))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Information, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Information, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Information, source ?? logger.Source, exception);
			}
		}

		public static void Info(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Information))
				return;
			Exception(logger, LogType.Information, source, exception);
		}

		public static void Info(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, args));
		}

		public static void Info(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, null));
		}

		public static void Info(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Info(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Info(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Info(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Info(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? InfoEnter(this ILoggerEx logger, string message, IDictionary? args)
		{
			return logger.Enter(LogType.Information, message, args);
		}

		public static IDisposable? InfoEnter(this ILoggerEx logger, string message, params object[] args)
		{
			return logger.Enter(LogType.Information, message, LogRecord.Args(args));
		}

		public static IDisposable? InfoTiming(this ILoggerEx logger, string description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Information, description, threshold);
		}

		#endregion

		#region Warning

		public static void Warning(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Warning))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Warning, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Warning, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Warning, source ?? logger.Source, exception);
			}
		}

		public static void Warning(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Warning))
				return;
			Exception(logger, LogType.Warning, source, exception);
		}

		public static void Warning(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, args));
		}

		public static void Warning(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, null));
		}

		public static void Warning(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Warning(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Warning(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Warning(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Warning(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? WarningEnter(this ILoggerEx logger, string message, IDictionary? args)
		{
			return logger.Enter(LogType.Warning, message, args);
		}

		public static IDisposable? WarningEnter(this ILoggerEx logger, string message, params object[] args)
		{
			return logger.Enter(LogType.Warning, message, LogRecord.Args(args));
		}

		public static IDisposable? WarningTiming(this ILoggerEx logger, string description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Warning, description, threshold);
		}

		#endregion

		#region Error

		public static void Error(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Error))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Error, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Error, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Error, source ?? logger.Source, exception);
			}
		}

		public static void Error(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Error))
				return;
			Exception(logger, LogType.Error, source, exception);
		}

		public static void Error(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, args));
		}

		public static void Error(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, null));
		}

		public static void Error(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Error(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Error(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Error(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Error(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? ErrorEnter(this ILoggerEx logger, string message, IDictionary? args)
		{
			return logger.Enter(LogType.Error, message, args);
		}

		public static IDisposable? ErrorEnter(this ILoggerEx logger, string message, params object[] args)
		{
			return logger.Enter(LogType.Error, message, LogRecord.Args(args));
		}

		public static IDisposable? ErrorTiming(this ILoggerEx logger, string description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Error, description, threshold);
		}

		#endregion

		#region Write

		public static void Write(this ILoggerEx logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (!logger.IsEnabled(LogType.Output))
				return;
			if (exception == null)
			{
				logger.Log(new LogRecord(LogType.Output, source ?? logger.Source, message, args));
			}
			else
			{
				logger.Log(new LogRecord(LogType.Output, source ?? logger.Source, message, exception, args));
				InnerExceptions(logger, LogType.Output, source ?? logger.Source, exception);
			}
		}

		public static void Write(this ILoggerEx logger, string? source, Exception exception)
		{
			if (!logger.IsEnabled(LogType.Output))
				return;
			Exception(logger, LogType.Output, source, exception);
		}

		public static void Write(this ILoggerEx logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, args));
		}

		public static void Write(this ILoggerEx logger, string message)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, null));
		}

		public static void Write(this ILoggerEx logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Write(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Write(this ILoggerEx logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Write(this ILoggerEx logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Write(this ILoggerEx logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? Enter(this ILoggerEx logger, string message, IDictionary? args)
		{
			return logger.Enter(LogType.Output, message, args);
		}

		public static IDisposable? Enter(this ILoggerEx logger, string message, params object[] args)
		{
			return logger.Enter(LogType.Output, message, LogRecord.Args(args));
		}

		public static IDisposable? Timing(this ILoggerEx logger, string description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Output, description, threshold);
		}

		#endregion
	}

	public static class ILoggerExtensions
	{
		public static bool IsEnabled(this ILogger logger, LogType logType)
		{
			return logger is ILoggerEx loggerX ? loggerX.IsEnabled(logType) : logger.IsEnabled(Type2Level(logType));
		}

		public static void Log(this ILogger logger, LogType logType, string source, string message, Exception exception, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Log(logType, source, message, exception, args);
			else
				logger.Log(Type2Level(logType), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static IDisposable? Enter(this ILogger logger, LogType logType, string sectionName, IDictionary? args = null)
		{
			return logger is ILoggerEx loggerX ? loggerX.Enter(logType, sectionName, args) : logger.BeginScope(sectionName);
		}

		public static IDisposable? Timing(this ILogger logger, LogType logType, string description, TimeSpan threshold)
		{
			return logger is ILoggerEx loggerX ? loggerX.Timing(logType, description, threshold) : logger.BeginScope(description);
		}

		#region Trace

		public static void Trace(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Trace), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(source, exception);
			else
				logger.Log(LogLevel.Trace, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(message, args);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Trace(message, args);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? TraceEnter(this ILogger logger, string message, IDictionary? args = null)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.TraceEnter(message, args):
				logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? TraceEnter(this ILogger logger, string message, params object[] args)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.TraceEnter(message, args):
				logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? TraceTiming(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.TraceTiming(description, threshold): null;
		}

		#endregion

		#region Debug

		public static void Debug(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Debug), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(source, exception);
			else
				logger.Log(LogLevel.Debug, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(message, args);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Debug(message, args);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? DebugEnter(this ILogger logger, string message, IDictionary? args = null)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.DebugEnter(message, args):
				logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? DebugEnter(this ILogger logger, string message, params object[] args)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.DebugEnter(message, args):
				logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? DebugTiming(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.DebugTiming(description, threshold): null;
		}

		#endregion

		#region Information

		public static void Info(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Information), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Info(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(source, exception);
			else
				logger.Log(LogLevel.Information, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Info(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? InfoEnter(this ILogger logger, string message, IDictionary? args = null)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.InfoEnter(message, args):
				logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? InfoEnter(this ILogger logger, string message, params object[] args)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.InfoEnter(message, args):
				logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? InfoTiming(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.InfoTiming(description, threshold): null;
		}

		#endregion

		#region Warning

		public static void Warning(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Warning), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(source, exception);
			else
				logger.Log(LogLevel.Warning, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(message, args);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Warning(message, args);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? WarningEnter(this ILogger logger, string message, IDictionary? args = null)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.WarningEnter(message, args):
				logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? WarningEnter(this ILogger logger, string message, params object[] args)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.WarningEnter(message, args):
				logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? WarningTiming(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.WarningTiming(description, threshold): null;
		}

		#endregion

		#region Error

		public static void Error(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Error), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Error(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(source, exception);
			else
				logger.Log(LogLevel.Error, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(message, args);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Error(message, args);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? ErrorEnter(this ILogger logger, string message, IDictionary? args = null)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.ErrorEnter(message, args):
				logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? ErrorEnter(this ILogger logger, string message, params object[] args)
		{
			return
				logger is ILoggerEx loggerX ? loggerX.ErrorEnter(message, args):
				logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? ErrorTiming(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.ErrorTiming(description, threshold): null;
		}

		#endregion

		#region Write

		public static void Write(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Information), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Write(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(source, exception);
			else
				logger.Log(LogLevel.Information, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILoggerEx loggerX)
				loggerX.Write(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? Enter(this ILogger logger, string message, IDictionary? args = null)
		{
			return logger is ILoggerEx loggerX ? loggerX.TraceEnter(message, args): logger.BeginScope(new TState(null, message, args));
		}

		public static IDisposable? Enter(this ILogger logger, string message, params object[] args)
		{
			return logger is ILoggerEx loggerX ? loggerX.TraceEnter(message, args): logger.BeginScope(new TState(null, message, LogRecord.Args(args)));
		}

		public static IDisposable? Timing(this ILogger logger, string description, TimeSpan threshold = default)
		{
			return logger is ILoggerEx loggerX ? loggerX.Timing(description, threshold) : null;
		}

		#endregion

		#region Implementation

		class TState
		{
			string? Source { get; }
			string? Message { get; }
			IDictionary? Args { get; }

			public TState(string? source, string? message, IDictionary? args)
			{
				Source = source;
				Message = message;
				Args = args;
			}

			public override string ToString()
			{
				return Formatter(this, null);
			}

			public static string Formatter(TState value, Exception? exception)
			{
				var text = new StringBuilder();
				if (value.Source != null)
				{
					text.Append(value.Source);
					if (value.Message != null)
						text.Append(": ");
				}
				if (value.Message != null)
					text.Append(value.Message);
				if (text.Length > 0)
					text.AppendLine();
				if (value.Args != null && value.Args.Count > 0)
				{
					foreach (DictionaryEntry item in value.Args)
					{
						text.Append('\t').Append(item.Key).Append(": ").Append(item.Value).AppendLine();
					}
				}
				string tab = "\t";
				while (exception != null)
				{
					text.Append(tab).AppendLine(exception.Message);
					foreach (DictionaryEntry item in exception.Data)
					{
						text.Append(tab).Append(item.Key).Append(": ").Append(item.Value).AppendLine();
					}
					text.Append("Stack: ").AppendLine(exception.StackTrace);
					tab += "\t";
					exception = exception.InnerException;
				}
				return text.ToString();
			}
		}

		private static LogType Level2Type(LogLevel logLevel) =>
			logLevel switch
			{
				LogLevel.Trace => LogType.Trace,
				LogLevel.Debug => LogType.Debug,
				LogLevel.Information => LogType.Information,
				LogLevel.Warning => LogType.Warning,
				LogLevel.Error => LogType.Error,
				_ => LogType.Output
			};

		private static LogLevel Type2Level(LogType logLevel) =>
			logLevel switch
			{
				LogType.Trace => LogLevel.Trace,
				LogType.Debug => LogLevel.Debug,
				LogType.Information => LogLevel.Information,
				LogType.Warning => LogLevel.Warning,
				LogType.Error => LogLevel.Error,
				_ => LogLevel.Critical
			};

		#endregion
	}
}
