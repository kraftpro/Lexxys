using System.Collections;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Lexxys;

public static class ILoggerExtensions
{
	public static bool IsEnabled(this ILogger logger, LogType logType)
	{
		return
			logger is ILogging log ? log.IsEnabled(logType):
			logger is not null ? logger.IsEnabled(LoggingTools.ToLogLevel(logType)):
			throw new ArgumentNullException(nameof(logger));
	}

	public static void Log(this ILogger logger, LogType logType, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is ILogging log)
			log.Log(logType, 0, source, message, exception, args);
		else if (logger is not null)
			logger.Log(LoggingTools.ToLogLevel(logType), 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
		else
			throw new ArgumentNullException(nameof(logger));
	}

	public static void Log(this ILogger logger, LogType logType, int eventId, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is ILogging log)
			log.Log(logType, 0, source, message, exception, args);
		else if (logger is not null)
			logger.Log(LoggingTools.ToLogLevel(logType), eventId, new LoggerState(source, message, args), exception, LoggerState.Formatter);
		else
			throw new ArgumentNullException(nameof(logger));
	}

	public static IDisposable? Enter(this ILogger logger, LogType logType, string sectionName, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		return
			logger is ILogging log ? log.Enter(logType, sectionName, args):
			logger is not null ? logger.BeginScope(sectionName): throw new ArgumentNullException(nameof(logger));
	}

	public static IDisposable? Timing(this ILogger logger, LogType logType, string description, TimeSpan threshold)
	{
		return
			logger is ILogging log ? log.Timing(logType, description, threshold):
			logger is not null ? logger.BeginScope(description):
			throw new ArgumentNullException(nameof(logger));
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

	public static void Trace(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Trace<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? TraceEnter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Trace, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? TraceTiming(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Trace, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Trace(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Trace(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Trace<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Trace<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Trace))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Trace, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Trace, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

#endif

	//.?$X = above("\tif (!logger.IsEnabled(LogLevel.Trace))\n\t\treturn;\n", "LogType.Trace", "LogLevel.Trace", "TraceTiming", "TraceEnter", "Trace");
	#endregion

	#region Debug
	//.#back($X, "\tif (!logger.IsEnabled(LogLevel.Debug))\n\t\treturn;\n", "LogType.Debug", "LogLevel.Debug", "DebugTiming", "DebugEnter", "Debug")

	public static void Debug(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Debug<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? DebugEnter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Debug, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? DebugTiming(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Debug, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Debug(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Debug(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Debug<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Debug<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Debug))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Debug, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Debug, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	#endif

	//.=cut
	#endregion

	#region Info
	//.#back($X, "\tif (!logger.IsEnabled(LogLevel.Information))\n\t\treturn;\n", "LogType.Information", "LogLevel.Information", "InfoTiming", "InfoEnter", "Info")

	public static void Info(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Info<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? InfoEnter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Information, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? InfoTiming(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Information, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Info(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Info(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Info<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Info<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Information, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	#endif

	//.=cut
	#endregion

	#region Warning
	//.#back($X, "\tif (!logger.IsEnabled(LogLevel.Warning))\n\t\treturn;\n", "LogType.Warning", "LogLevel.Warning", "WarningTiming", "WarningEnter", "Warning")

	public static void Warning(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Warning<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? WarningEnter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Warning, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? WarningTiming(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Warning, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Warning(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Warning(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Warning<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Warning<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Warning))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Warning, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Warning, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	#endif

	//.=cut
	#endregion

	#region Error
	//.#back($X, "\tif (!logger.IsEnabled(LogLevel.Error))\n\t\treturn;\n", "LogType.Error", "LogLevel.Error", "ErrorTiming", "ErrorEnter", "Error")

	public static void Error(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Error<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? ErrorEnter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Error, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? ErrorTiming(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Error, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Error(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Error(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Error<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Error<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Error))
			return;
		if (logger is ILogging log)
			log.Log(LogType.Error, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Error, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	#endif

	//.=cut
	#endregion

	#region Write
	//.#back($X, "", "LogType.Output", "LogLevel.Information", "Timing", "Enter", "Write")

	public static void Write(this ILogger logger, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, source ?? log.Source, message, exception, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message, args), exception, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string? source, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, source ?? log.Source, null, exception, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, null, null), exception, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message, null, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, args), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, string message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message, null, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, null), null, LoggerState.Formatter);
	}

	public static void Write<T1>(this ILogger logger, string message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message, null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2, T3>(this ILogger logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message, null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, Func<string> message, Func<IEnumerable<NameValueTuple<string, object?>>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message?.Invoke(), null, args?.Invoke());
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message?.Invoke(), args?.Invoke()), null, LoggerState.Formatter);
	}

	public static IDisposable? Enter(this ILogger logger, string section, IEnumerable<NameValueTuple<string, object?>>? args = null)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return null;
		return logger is ILogging log ?
			log.Enter(LogType.Output, section, args):
			logger.BeginScope(new LoggerState(null, section, args));
	}

	public static IDisposable? Timing(this ILogger logger, string section, TimeSpan threshold = default)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (!logger.IsEnabled(LogLevel.Information))
			return null;
		return logger is ILogging log ? log.Timing(LogType.Output, section, threshold): null;
	}

	#if NET6_0_OR_GREATER

	public static void Write(this ILogger logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, source ?? log.Source, message.ToStringAndClear(), exception, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(source, message.ToStringAndClear(), args), exception, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message.ToStringAndClear(), null, args);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), args), null, LoggerState.Formatter);
	}

	public static void Write(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message.ToStringAndClear(), null, null);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), null), null, LoggerState.Formatter);
	}

	public static void Write<T1>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value)]), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value)]), null, LoggerState.Formatter);
	}

	public static void Write<T1, T2, T3>(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger is ILogging log)
			log.Log(LogType.Output, 0, log.Source, message.ToStringAndClear(), null, [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]);
		else
			logger.Log(LogLevel.Information, 0, new LoggerState(null, message.ToStringAndClear(), [(arg1Name, arg1Value), (arg2Name, arg2Value), (arg3Name, arg3Value)]), null, LoggerState.Formatter);
	}

	#endif

	//.=cut
	#endregion

	#region Implementation

	private class LoggerState
	{
		private readonly string? _source;
		private readonly string? _message;
		private readonly IReadOnlyCollection<NameValueTuple<string, object?>>? _args;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LoggerState(string? source, string? message, IEnumerable<NameValueTuple<string, object?>>? args)
		{
			_source = source;
			_message = message;
			_args = args?.ToIReadOnlyCollection();
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
				foreach (var item in value._args)
				{
					text.Append('\t').Append(item.Name).Append(": ").Append(item.Value).AppendLine();
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
	public static IEnumerable<NameValueTuple<string, object?>>? Args(params object?[]? args)
	{
		if (args is not { Length: >0 })
			return null;
		var arg = new List<NameValueTuple<string, object?>>((args.Length + 1) / 2);
		int count = args.Length & ~1;
		for (int i = 0; i < count; i += 2)
		{
			arg.Add((args[i]?.ToString() ?? "null", args[i + 1]));
		}
		if (count >= args.Length)
			return arg;

		string? name = args[count]?.ToString();
		if (name != null)
			arg.Add((name, null));
		return arg;
	}
}
