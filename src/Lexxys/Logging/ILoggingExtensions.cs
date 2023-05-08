using System.Collections;

namespace Lexxys
{
	public static class ILoggingExtensions
	{
		public static void Log(this ILogging logger, LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger is null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(logType))
				logger.Log(logType, 0, source ?? logger.Source, message, exception, args);
		}
		
#if NET6_0_OR_GREATER

		public static void Log(this ILogging logger, LogType logType, int eventId, string? source, [InterpolatedStringHandlerArgument("logger", "logType")] ref LoggingInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger is null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(logType))
				logger.Log(logType, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}
		
		public static void Log(this ILogging logger, LogType logType, string? source, [InterpolatedStringHandlerArgument("logger", "logType")] ref LoggingInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger is null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(logType))
				logger.Log(logType, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

#endif		
		
		/// <summary>
		/// True, if Direct messages will be logged (Write(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WriteEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Output);

		/// <summary>
		/// True, if Error messages will be logged (Error(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ErrorEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Error);
		/// <summary>
		/// True, if Warning messages will be logged (Warning(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool WarningEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Warning);
		/// <summary>
		/// True, if Information messages will be logged (Info(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool InfoEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Information);
		/// <summary>
		/// True, if Debug messages will be logged (Debug(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DebugEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Debug);
		/// <summary>
		/// True, if Trace messages will be logged (Trace(...) methods)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TraceEnabled(this ILogging logger) => (logger ?? throw new ArgumentNullException(nameof(logger))).IsEnabled(LogType.Trace);

		#region Trace
		//.?

		public static void Trace(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Trace(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Trace(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null, args);
		}

		public static void Trace(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null, null);
		}

		public static void Trace<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Trace<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Trace<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Trace(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Trace, section, args);
		}

		public static IDisposable? TraceEnter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Trace, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? TraceTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Trace, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Trace(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Trace(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Trace(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Trace(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Trace<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Trace<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Trace<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Trace))
				logger.Log(LogType.Trace, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? TraceEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Trace, section.ToStringAndClear(), args);
		}

		public static IDisposable? TraceEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Trace, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? TraceTiming(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingTraceInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Trace, description.ToStringAndClear(), threshold);
		}

		#endif

		//.?$X = above("LogType.Trace", "TraceTiming", "TraceEnter", "Trace");
		#endregion

		#region Debug
		//.#back($X, "LogType.Debug", "DebugTiming", "DebugEnter", "Debug")

		public static void Debug(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Debug(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Debug(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null, args);
		}

		public static void Debug(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null, null);
		}

		public static void Debug<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Debug<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Debug<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Debug(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Debug, section, args);
		}

		public static IDisposable? DebugEnter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Debug, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? DebugTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Debug, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Debug(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Debug(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Debug(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Debug(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Debug<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Debug<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Debug<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Debug))
				logger.Log(LogType.Debug, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? DebugEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Debug, section.ToStringAndClear(), args);
		}

		public static IDisposable? DebugEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Debug, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? DebugTiming(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingDebugInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Debug, description.ToStringAndClear(), threshold);
		}

		#endif

		//.=cut
		#endregion

		#region Info
		//.#back($X, "LogType.Information", "InfoTiming", "InfoEnter", "Info")

		public static void Info(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Info(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Info(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null, args);
		}

		public static void Info(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null, null);
		}

		public static void Info<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Info<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Info<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Info(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Information, section, args);
		}

		public static IDisposable? InfoEnter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Information, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? InfoTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Information, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Info(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Info(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Info(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Info(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Info<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Info<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Info<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Information))
				logger.Log(LogType.Information, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? InfoEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Information, section.ToStringAndClear(), args);
		}

		public static IDisposable? InfoEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Information, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? InfoTiming(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingInfoInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Information, description.ToStringAndClear(), threshold);
		}

		#endif

		//.=cut
		#endregion

		#region Warning
		//.#back($X, "LogType.Warning", "WarningTiming", "WarningEnter", "Warning")

		public static void Warning(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Warning(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Warning(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null, args);
		}

		public static void Warning(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null, null);
		}

		public static void Warning<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Warning<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Warning<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Warning(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Warning, section, args);
		}

		public static IDisposable? WarningEnter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Warning, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? WarningTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Warning, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Warning(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Warning(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Warning(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Warning(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Warning<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Warning<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Warning<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Warning))
				logger.Log(LogType.Warning, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? WarningEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Warning, section.ToStringAndClear(), args);
		}

		public static IDisposable? WarningEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Warning, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? WarningTiming(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWarningInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Warning, description.ToStringAndClear(), threshold);
		}

		#endif

		//.=cut
		#endregion

		#region Error
		//.#back($X, "LogType.Error", "ErrorTiming", "ErrorEnter", "Error")

		public static void Error(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Error(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Error(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null, args);
		}

		public static void Error(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null, null);
		}

		public static void Error<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Error<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Error<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Error(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Error, section, args);
		}

		public static IDisposable? ErrorEnter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Error, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? ErrorTiming(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Error, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Error(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Error(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Error(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Error(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Error<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Error<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Error<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Error))
				logger.Log(LogType.Error, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? ErrorEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Error, section.ToStringAndClear(), args);
		}

		public static IDisposable? ErrorEnter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Error, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? ErrorTiming(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingErrorInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Error, description.ToStringAndClear(), threshold);
		}

		#endif

		//.=cut
		#endregion

		#region Write
		//.#back($X, "LogType.Output", "Timing", "Enter", "Write")

		public static void Write(this ILogging logger, string? source, string? message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source ?? logger.Source, message, exception, args);
		}

		public static void Write(this ILogging logger, string? source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source ?? logger.Source, null, exception, null);
		}

		public static void Write(this ILogging logger, string message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null, args);
		}

		public static void Write(this ILogging logger, string message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null, null);
		}

		public static void Write<T1>(this ILogging logger, string message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Write<T1, T2>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Write<T1, T2, T3>(this ILogging logger, string message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message, null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static void Write(this ILogging logger, Func<string> message, Func<IDictionary>? args = null)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message(), null, args?.Invoke());
		}

		public static IDisposable? Enter(this ILogging logger, string? section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Output, section, args);
		}

		public static IDisposable? Enter(this ILogging logger, string? section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Output, section, ILoggerExtensions.Args(args));
		}

		public static IDisposable? Timing(this ILogging logger, string? description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Output, description, threshold);
		}

		#if NET6_0_OR_GREATER

		public static void Write(this ILogging logger, string? source, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, Exception? exception, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source ?? logger.Source, message.ToStringAndClear(), exception, args);
		}

		public static void Write(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler source, Exception exception)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, source.ToStringAndClear(), null, exception, null);
		}

		public static void Write(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, IDictionary? args)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message.ToStringAndClear(), null, args);
		}

		public static void Write(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message.ToStringAndClear(), null, null);
		}

		public static void Write<T1>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(1) { { arg1Name, arg1Value } });
		}

		public static void Write<T1, T2>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(2) { { arg1Name, arg1Value }, { arg2Name, arg2Value } });
		}

		public static void Write<T1, T2, T3>(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler message, string arg1Name, T1 arg1Value, string arg2Name, T2 arg2Value, string arg3Name, T3 arg3Value)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (logger.IsEnabled(LogType.Output))
				logger.Log(LogType.Output, 0, logger.Source, message.ToStringAndClear(), null,
					args: new OrderedBag<string, object?>(3) { { arg1Name, arg1Value }, { arg2Name, arg2Value }, { arg3Name, arg3Value } });
		}

		public static IDisposable? Enter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler section, IDictionary? args = null)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Output, section.ToStringAndClear(), args);
		}

		public static IDisposable? Enter(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler section, params object[] args)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Enter(LogType.Output, section.ToStringAndClear(), ILoggerExtensions.Args(args));
		}

		public static IDisposable? Timing(this ILogging logger, [InterpolatedStringHandlerArgument("logger")] ref LoggingWriteInterpolatedStringHandler description, TimeSpan threshold = default)
		{
			return (logger ?? throw new ArgumentNullException(nameof(logger))).Timing(LogType.Output, description.ToStringAndClear(), threshold);
		}

		#endif

		//.=cut
		#endregion
	}
}
