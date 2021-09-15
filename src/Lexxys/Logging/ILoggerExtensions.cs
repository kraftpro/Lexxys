using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

using Lexxys.Logging;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	public static class ILoggerExtensions
	{
		public static bool IsEnabled(this ILogger logger, LogType logType)
		{
			return logger is ILogging log ? log.IsEnabled(logType) : logger.IsEnabled(Type2Level(logType));
		}

		public static void Log(this ILogger logger, LogType logType, string source, string message, Exception exception, IDictionary args)
		{
			if (logger is ILogging log)
				log.Log(logType, source, message, exception, args);
			else
				logger.Log(Type2Level(logType), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static IDisposable? Enter(this ILogger logger, LogType logType, string sectionName, IDictionary? args = null)
		{
			return logger is ILogging log ? log.Enter(logType, sectionName, args) : logger.BeginScope(sectionName);
		}

		public static IDisposable? Timing(this ILogger logger, LogType logType, string description, TimeSpan threshold)
		{
			return logger is ILogging log ? log.Timing(logType, description, threshold) : logger.BeginScope(description);
		}

		/// <summary>
		/// True, if Direct messages will be logged (Write(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WriteEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Critical);

		/// <summary>
		/// True, if Error messages will be logged (Error(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ErrorEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Error);
		/// <summary>
		/// True, if Warning messages will be logged (Warning(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WarningEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Warning);
		/// <summary>
		/// True, if Information messages will be logged (Info(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool InfoEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Information);
		/// <summary>
		/// True, if Debug messages will be logged (Debug(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DebugEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Debug);
		/// <summary>
		/// True, if Trace messages will be logged (Trace(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TraceEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Trace);

		#region Trace

		public static void Trace(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Trace(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Trace), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Trace(source, exception);
			else
				logger.Log(LogLevel.Trace, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Trace(message, args);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Trace(message);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Trace(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Trace(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Trace(message, args);
			else
				logger.Log(LogLevel.Trace, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? TraceEnter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return
				logger is ILogging log ? log.TraceEnter(message, args):
				logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? TraceEnter(this ILogger logger, string? message, params object[] args)
		{
			return
				logger is ILogging log ? log.TraceEnter(message, args):
				logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? TraceTiming(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.TraceTiming(description, threshold): null;
		}

		#endregion

		#region Debug

		public static void Debug(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Debug(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Debug), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Debug(source, exception);
			else
				logger.Log(LogLevel.Debug, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Debug(message, args);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Debug(message);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Debug(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Debug(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Debug(message, args);
			else
				logger.Log(LogLevel.Debug, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? DebugEnter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return
				logger is ILogging log ? log.DebugEnter(message, args):
				logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? DebugEnter(this ILogger logger, string? message, params object[] args)
		{
			return
				logger is ILogging log ? log.DebugEnter(message, args):
				logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? DebugTiming(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.DebugTiming(description, threshold): null;
		}

		#endregion

		#region Information

		public static void Info(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Info(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Information), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Info(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Info(source, exception);
			else
				logger.Log(LogLevel.Information, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Info(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Info(message);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Info(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Info(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Info(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Info(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Info(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? InfoEnter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return
				logger is ILogging log ? log.InfoEnter(message, args):
				logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? InfoEnter(this ILogger logger, string? message, params object[] args)
		{
			return
				logger is ILogging log ? log.InfoEnter(message, args):
				logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? InfoTiming(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.InfoTiming(description, threshold): null;
		}

		#endregion

		#region Warning

		public static void Warning(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Warning(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Warning), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Warning(source, exception);
			else
				logger.Log(LogLevel.Warning, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Warning(message, args);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Warning(message);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Warning(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Warning(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Warning(message, args);
			else
				logger.Log(LogLevel.Warning, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? WarningEnter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return
				logger is ILogging log ? log.WarningEnter(message, args):
				logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? WarningEnter(this ILogger logger, string? message, params object[] args)
		{
			return
				logger is ILogging log ? log.WarningEnter(message, args):
				logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? WarningTiming(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.WarningTiming(description, threshold): null;
		}

		#endregion

		#region Error

		public static void Error(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Error(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Error), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Error(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Error(source, exception);
			else
				logger.Log(LogLevel.Error, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Error(message, args);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Error(message);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Error(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Error(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Error(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Error(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Error(message, args);
			else
				logger.Log(LogLevel.Error, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? ErrorEnter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return
				logger is ILogging log ? log.ErrorEnter(message, args):
				logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new TState(null, message, args)): null;
		}

		public static IDisposable? ErrorEnter(this ILogger logger, string? message, params object[] args)
		{
			return
				logger is ILogging log ? log.ErrorEnter(message, args):
				logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new TState(null, message, LogRecord.Args(args))): null;
		}

		public static IDisposable? ErrorTiming(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.ErrorTiming(description, threshold): null;
		}

		#endregion

		#region Write

		public static void Write(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is ILogging log)
				log.Write(source, message, exception, args);
			else
				logger.Log(Type2Level(LogType.Information), 0, new TState(source, message, args), exception, TState.Formatter);
		}

		public static void Write(this ILogger logger, string? source, Exception exception)
		{
			if (logger is ILogging log)
				log.Write(source, exception);
			else
				logger.Log(LogLevel.Information, 0, new TState(source, null, null), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, IDictionary args)
		{
			if (logger is ILogging log)
				log.Write(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, args), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message)
		{
			if (logger is ILogging log)
				log.Write(message);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, null), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value)
		{
			if (logger is ILogging log)
				log.Write(message, arg1Name, arg1Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(1) { { arg1Name, arg1Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if (logger is ILogging log)
				log.Write(message, arg1Name, arg1Value, arg2Name, arg2Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if (logger is ILogging log)
				log.Write(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, new OrderedBag<string, object>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, TState.Formatter);
		}

		public static void Write(this ILogger logger, string message, params object[] args)
		{
			if (logger is ILogging log)
				log.Write(message, args);
			else
				logger.Log(LogLevel.Information, 0, new TState(null, message, LogRecord.Args(args)), null, TState.Formatter);
		}

		public static IDisposable? Enter(this ILogger logger, string? message, IDictionary? args = null)
		{
			return logger is ILogging log ? log.TraceEnter(message, args): logger.BeginScope(new TState(null, message, args));
		}

		public static IDisposable? Enter(this ILogger logger, string? message, params object[] args)
		{
			return logger is ILogging log ? log.TraceEnter(message, args): logger.BeginScope(new TState(null, message, LogRecord.Args(args)));
		}

		public static IDisposable? Timing(this ILogger logger, string? description, TimeSpan threshold = default)
		{
			return logger is ILogging log ? log.Timing(description, threshold) : null;
		}

		#endregion

		#region Implementation

		class TState
		{
			string? Source { get; }
			string? Message { get; }
			IDictionary? Args { get; }

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
					if (value.Message == null)
						text.AppendLine(value.Source);
					else
						text.Append(value.Source).Append(": ").AppendLine(value.Message);
				else if (value.Message != null)
					text.AppendLine(value.Message);
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
					text.Append(tab).Append("Stack: ").AppendLine(exception.StackTrace);
					tab += "\t";
					exception = exception.InnerException;
				}
				return text.ToString();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
