using System;
using System.Collections;

using Lexxys.Logging;

#nullable enable

namespace Lexxys
{
	public static class ILoggingExtensions
	{
		public static void Log(this ILogging logger, LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(logType))
				logger.Log(new LogRecord(logType, source ?? logger.Source, message, exception, args));
		}

		#region Trace

		public static void Trace(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, source ?? logger.Source, message, exception, args));
		}

		public static void Trace(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, source ?? logger.Source, null, exception, null));
		}

		public static void Trace(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, args));
		}

		public static void Trace(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, null));
		}

		public static void Trace(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Trace(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Trace(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Trace(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Trace(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(new LogRecord(LogType.Trace, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, IDictionary? args)
		{
			return logger.Enter(LogType.Trace, section, args);
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Trace, section, LogRecord.Args(args));
		}

		public static IDisposable? TraceTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Trace, description, threshold);
		}

		#endregion

		#region Debug

		public static void Debug(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, source ?? logger.Source, message, exception, args));
		}

		public static void Debug(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, source ?? logger.Source, null, exception, null));
		}

		public static void Debug(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, args));
		}

		public static void Debug(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, null));
		}

		public static void Debug(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Debug(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Debug(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Debug(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Debug(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(new LogRecord(LogType.Debug, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? message, IDictionary? args)
		{
			return logger.Enter(LogType.Debug, message, args);
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? message, params object[] args)
		{
			return logger.Enter(LogType.Debug, message, LogRecord.Args(args));
		}

		public static IDisposable? DebugTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Debug, description, threshold);
		}

		#endregion

		#region Info

		public static void Info(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, source ?? logger.Source, message, exception, args));
		}

		public static void Info(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, source ?? logger.Source, null, exception, null));
		}

		public static void Info(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, args));
		}

		public static void Info(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, null));
		}

		public static void Info(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Info(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Info(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Info(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Info(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(new LogRecord(LogType.Information, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? message, IDictionary? args)
		{
			return logger.Enter(LogType.Information, message, args);
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? message, params object[] args)
		{
			return logger.Enter(LogType.Information, message, LogRecord.Args(args));
		}

		public static IDisposable? InfoTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Information, description, threshold);
		}

		#endregion

		#region Warning

		public static void Warning(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, source ?? logger.Source, message, exception, args));
		}

		public static void Warning(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, source ?? logger.Source, null, exception, null));
		}

		public static void Warning(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, args));
		}

		public static void Warning(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, null));
		}

		public static void Warning(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Warning(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Warning(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Warning(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Warning(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(new LogRecord(LogType.Warning, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? message, IDictionary? args)
		{
			return logger.Enter(LogType.Warning, message, args);
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? message, params object[] args)
		{
			return logger.Enter(LogType.Warning, message, LogRecord.Args(args));
		}

		public static IDisposable? WarningTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Warning, description, threshold);
		}

		#endregion

		#region Error

		public static void Error(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, source ?? logger.Source, message, exception, args));
		}

		public static void Error(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, source ?? logger.Source, null, exception, null));
		}

		public static void Error(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, args));
		}

		public static void Error(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, null));
		}

		public static void Error(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Error(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Error(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Error(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Error(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(new LogRecord(LogType.Error, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? message, IDictionary? args)
		{
			return logger.Enter(LogType.Error, message, args);
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? message, params object[] args)
		{
			return logger.Enter(LogType.Error, message, LogRecord.Args(args));
		}

		public static IDisposable? ErrorTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Error, description, threshold);
		}

		#endregion

		#region Write

		public static void Write(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, source ?? logger.Source, message, exception, args));
		}

		public static void Write(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, source ?? logger.Source, null, exception, null));
		}

		public static void Write(this ILogging logger, string message, IDictionary args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, args));
		}

		public static void Write(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, null));
		}

		public static void Write(this ILogging logger, string message, string arg1Name, object arg1Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }));
		}

		public static void Write(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }));
		}

		public static void Write(this ILogging logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message,
					args: new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }));
		}

		public static void Write(this ILogging logger, string message, params object[] args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message, LogRecord.Args(args)));
		}

		public static void Write(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(new LogRecord(LogType.Output, logger.Source, message(), args?.Invoke()));
		}

		public static IDisposable? Enter(this ILogging logger, string? message, IDictionary? args)
		{
			return logger.Enter(LogType.Output, message, args);
		}

		public static IDisposable? Enter(this ILogging logger, string? message, params object[] args)
		{
			return logger.Enter(LogType.Output, message, LogRecord.Args(args));
		}

		public static IDisposable? Timing(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Output, description, threshold);
		}

		#endregion
	}
}
