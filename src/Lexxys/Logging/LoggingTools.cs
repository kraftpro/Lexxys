﻿using System;
using System.Collections;
using System.Runtime.CompilerServices;

using Lexxys.Logging;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	public static class LoggingTools
	{
		public static void Log<TState>(ILogging log, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
		{
			var logType = LogTypeFromLogLevel(logLevel);
			if (!log.IsEnabled(logType))
				return;
			IDictionary? args = state as IDictionary;
			string? message = null;
			if (formatter != null)
				message = formatter(state, exception);
			if (message == null && args == null)
				message = state?.ToString();
			log.Log(new LogRecord(logType, log.Source, message, exception, args));
		}

		public static bool IsEnabled(ILogging log, LogLevel logLevel)
			=> log.IsEnabled(LogTypeFromLogLevel(logLevel));

		public static IDisposable? BeginScope<TState>(ILogging log, TState state)
		{
			var section = state?.ToString() ?? typeof(TState).Name;
			return log.Enter(LogType.Output, section, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static LogType LogTypeFromLogLevel(LogLevel logLevel) => logLevel switch
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