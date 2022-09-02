using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Lexxys
{
	public static class ILoggingExtensions
	{
		#pragma warning disable CA1062 // Validate arguments of public methods

		public static void Log(this ILogging logger, LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(logType))
				logger.Log(logType, 0, source ?? logger.Source, message, exception, args);
		}

		/// <summary>
		/// True, if Direct messages will be logged (Write(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WriteEnabled(this ILogging logger) => logger.IsEnabled(LogType.Output);

		/// <summary>
		/// True, if Error messages will be logged (Error(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ErrorEnabled(this ILogging logger) => logger.IsEnabled(LogType.Error);
		/// <summary>
		/// True, if Warning messages will be logged (Warning(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WarningEnabled(this ILogging logger) => logger.IsEnabled(LogType.Warning);
		/// <summary>
		/// True, if Information messages will be logged (Info(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool InfoEnabled(this ILogging logger) => logger.IsEnabled(LogType.Information);
		/// <summary>
		/// True, if Debug messages will be logged (Debug(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DebugEnabled(this ILogging logger) => logger.IsEnabled(LogType.Debug);
		/// <summary>
		/// True, if Trace messages will be logged (Trace(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TraceEnabled(this ILogging logger) => logger.IsEnabled(LogType.Trace);

		#region Trace
		//.?

		public static void Trace(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Trace(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Trace(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null, args);
		}

		public static void Trace(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null, null);
		}

		public static void Trace<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Trace<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Trace<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Trace(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Trace, section, args);
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Trace, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? TraceTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Trace, description, threshold);
		}

		//.?$X = above("LogType.Trace", "TraceTiming", "TraceEnter", "Trace");
		#endregion

		#region Debug
		//.#back($X, "LogType.Debug", "DebugTiming", "DebugEnter", "Debug")

		public static void Debug(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Debug(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Debug(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null, args);
		}

		public static void Debug(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null, null);
		}

		public static void Debug<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Debug<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Debug<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Debug(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Debug, section, args);
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Debug, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? DebugTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Debug, description, threshold);
		}

		//.=cut
		#endregion

		#region Info
		//.#back($X, "LogType.Information", "InfoTiming", "InfoEnter", "Info")

		public static void Info(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Info(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Info(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null, args);
		}

		public static void Info(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null, null);
		}

		public static void Info<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Info<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Info<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Info(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Information, section, args);
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Information, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? InfoTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Information, description, threshold);
		}

		//.=cut
		#endregion

		#region Warning
		//.#back($X, "LogType.Warning", "WarningTiming", "WarningEnter", "Warning")

		public static void Warning(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Warning(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Warning(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null, args);
		}

		public static void Warning(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null, null);
		}

		public static void Warning<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Warning<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Warning<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Warning(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Warning, section, args);
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Warning, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? WarningTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Warning, description, threshold);
		}

		//.=cut
		#endregion

		#region Error
		//.#back($X, "LogType.Error", "ErrorTiming", "ErrorEnter", "Error")

		public static void Error(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Error(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Error(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null, args);
		}

		public static void Error(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null, null);
		}

		public static void Error<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Error<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Error<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Error(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Error, section, args);
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Error, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? ErrorTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Error, description, threshold);
		}

		//.=cut
		#endregion

		#region Write
		//.#back($X, "LogType.Output", "Timing", "Enter", "Write")

		public static void Write(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Write(this ILogging logger, string? source, Exception exception)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Write(this ILogging logger, string message, IDictionary? args)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null, args);
		}

		public static void Write(this ILogging logger, string message)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null, null);
		}

		public static void Write<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Write<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Write<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Write(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? Enter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return logger.Enter(LogType.Output, section, args);
		}

		public static IDisposable? Enter(this ILogging logger, string? section, params object[] args)
		{
			return logger.Enter(LogType.Output, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? Timing(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return logger.Timing(LogType.Output, description, threshold);
		}

		//.=cut
		#endregion
	}
}
