using System.Collections;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Lexxys;

public static class ILoggerExtensions
{
	public static bool IsEnabled(this ILogger logger, LogType logType)
	{
		if (logger is ILogging log)
			return log.IsEnabled(logType);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else
			return logger.IsEnabled(LoggingTools.ToLogLevel(logType));
	}

	public static void Log(this ILogger logger, LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Log(logType, source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else
			logger.Log(LoggingTools.ToLogLevel(logType), 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Log(this ILogger logger, LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Log(logType, source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else
			logger.Log(LoggingTools.ToLogLevel(logType), eventId, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static IDisposable? Enter(this ILogger logger, LogType logType, string sectionName, IDictionary? args = null)
	{
		if (logger is ILogging log)
			return log.Enter(logType, sectionName, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else
			return logger.BeginScope(sectionName);
	}

	public static IDisposable? Timing(this ILogger logger, LogType logType, string description, TimeSpan threshold)
	{
		if (logger is ILogging log)
			return log.Timing(logType, description, threshold);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else
			return logger.BeginScope(description);
	}

	/// <summary>
	/// True, if Direct messages will be logged (Write(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool WriteEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Critical);

	/// <summary>
	/// True, if Error messages will be logged (Error(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ErrorEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Error);
	/// <summary>
	/// True, if Warning messages will be logged (Warning(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool WarningEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Warning);
	/// <summary>
	/// True, if Information messages will be logged (Info(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InfoEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Information);
	/// <summary>
	/// True, if Debug messages will be logged (Debug(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool DebugEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Debug);
	/// <summary>
	/// True, if Trace messages will be logged (Trace(...) methods)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TraceEnabled(this ILogger logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogLevel.Trace);

	#region Trace
	//.?

	public static void Trace(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Trace(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Trace(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Trace(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Trace(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Trace<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Trace(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Trace(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Trace(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Trace))
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? TraceEnter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.TraceEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? TraceEnter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.TraceEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Trace) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? TraceTiming(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.TraceTiming(description, threshold): null;
	}

	//.?$X = above("LogLevel.Trace", "TraceTiming", "TraceEnter", "Trace");
	#endregion

	#region Debug
	//.#back($X, "LogLevel.Debug", "DebugTiming", "DebugEnter", "Debug")

	public static void Debug(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Debug(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Debug(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Debug(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Debug(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Debug<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Debug(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Debug(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Debug(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Debug))
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? DebugEnter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.DebugEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? DebugEnter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.DebugEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Debug) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? DebugTiming(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.DebugTiming(description, threshold): null;
	}

	//.=cut
	#endregion

	#region Info
	//.#back($X, "LogLevel.Information", "InfoTiming", "InfoEnter", "Info")

	public static void Info(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Info(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Info(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Info(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Info(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Info<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Info(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Info(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Info(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Info(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? InfoEnter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.InfoEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? InfoEnter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.InfoEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? InfoTiming(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.InfoTiming(description, threshold): null;
	}

	//.=cut
	#endregion

	#region Warning
	//.#back($X, "LogLevel.Warning", "WarningTiming", "WarningEnter", "Warning")

	public static void Warning(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Warning(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Warning(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Warning(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Warning(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Warning<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Warning(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Warning(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Warning(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Warning))
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? WarningEnter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.WarningEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? WarningEnter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.WarningEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Warning) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? WarningTiming(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.WarningTiming(description, threshold): null;
	}

	//.=cut
	#endregion

	#region Error
	//.#back($X, "LogLevel.Error", "ErrorTiming", "ErrorEnter", "Error")

	public static void Error(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Error(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Error(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Error(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Error(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Error<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Error(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Error(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Error(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Error(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Error))
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? ErrorEnter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.ErrorEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? ErrorEnter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.ErrorEnter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Error) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? ErrorTiming(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.ErrorTiming(description, threshold): null;
	}

	//.=cut
	#endregion

	#region Write
	//.#back($X, "LogLevel.Information", "Timing", "Enter", "Write")

	public static void Write(this ILogger logger, string? source, string? message, Exception? exception, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Write(source, message, exception, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string? source, Exception exception)
	{
		if (logger is ILogging log)
			log.Write(source, exception);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(source, null, null), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string message, IDictionary? args)
	{
		if (logger is ILogging log)
			log.Write(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string message)
	{
		if (logger is ILogging log)
			log.Write(message);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Write<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is ILogging log)
			log.Write(message, arg1Name, arg1Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } }), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is ILogging log)
			log.Write(message, arg1Name, arg1Value, arg2Name, arg2Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } }), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is ILogging log)
			log.Write(message, arg1Name, arg1Value, arg2Name, arg2Value, arg3Name, arg3Value);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } }), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, Func<string> message, Func<IDictionary>? args = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));
		if (logger is ILogging log)
			log.Write(message, args);
		else if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		else if (logger.IsEnabled(LogLevel.Information))
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? Enter(this ILogger logger, string? message, IDictionary? args = null)
	{
		return
			logger is ILogging log ? log.Enter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)):
			logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new LoggerState(null, message, args)): null;
	}

	public static IDisposable? Enter(this ILogger logger, string? message, params object[] args)
	{
		return
			logger is ILogging log ? log.Enter(message, args):
			logger is null ? throw new ArgumentNullException(nameof(logger)) :
			logger.IsEnabled(LogLevel.Information) ? logger.BeginScope(new LoggerState(null, message, Args(args))): null;
	}

	public static IDisposable? Timing(this ILogger logger, string? description, TimeSpan threshold = default)
	{
		return logger is ILogging log ? log.Timing(description, threshold): null;
	}

	//.=cut
	#endregion

	#region Implementation

	class LoggerState
	{
		private readonly string? _source;
		private readonly string? _message;
		private readonly IDictionary? _args;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LoggerState(string? source, string? message, IDictionary? args)
		{
			_source = source;
			_message = message;
			_args = args;
		}

		public override string ToString()
		{
			return Formatter(this, null);
		}

		public static string Formatter(LoggerState value, Exception? exception)
		{
			var text = new StringBuilder();
			if (value._source != null)
				if (value._message == null)
					text.AppendLine(value._source);
				else
					text.Append(value._source).Append(": ").AppendLine(value._message);
			else if (value._message != null)
				text.AppendLine(value._message);
			if (value._args is { Count: >0 })
			{
				foreach (DictionaryEntry item in value._args)
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

	#endregion

	/// <summary>
	/// Converts an array of arguments into a dictionary of [string, value] parameters for logging.
	/// </summary>
	/// <param name="args">Array in form [string, value]*</param>
	/// <returns></returns>
	public static IDictionary? Args(params object?[]? args)
	{
		if (args is not { Length: >0 })
			return null;
		System.Diagnostics.Debug.Assert(args != null);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var arg = new OrderedBag<string, object?>((args.Length + 1) / 2);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		int count = args.Length & ~1;
		for (int i = 0; i < count; i += 2)
		{
			arg.Add(args[i]?.ToString() ?? "null", args[i + 1]);
		}
		if (count >= args.Length)
			return arg;

		var name = args[count]?.ToString();
		if (name != null)
			arg.Add(name, null);
		return arg;
	}
}
