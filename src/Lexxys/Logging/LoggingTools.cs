using System;
using System.Collections;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace Lexxys
{

	public static class LoggingTools
	{
		public static void Log<TState>(ILogging log, LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
		{
			var logType = ToLogType(logLevel);
			if (log == null || !log.IsEnabled(logType))
				return;
			IDictionary? args = state as IDictionary;
			string? message = null;
			if (formatter != null)
				message = formatter(state, exception);
			if (message == null && args == null)
				message = state?.ToString();
			log.Log(logType, eventId.Id, log.Source, message, exception, args);
		}

		public static bool IsEnabled(ILogging log, LogLevel logLevel)
			=> log != null && log.IsEnabled(ToLogType(logLevel));

		public static IDisposable BeginScope<TState>(ILogging log, TState state)
		{
			var section = state?.ToString() ?? typeof(TState).Name;
			return log?.Enter(LogType.Output, section, null) ?? NullDisposable.Value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LogType ToLogType(LogLevel logLevel) => logLevel switch
		{
			LogLevel.Trace => LogType.Trace,
			LogLevel.Debug => LogType.Debug,
			LogLevel.Information => LogType.Information,
			LogLevel.Warning => LogType.Warning,
			LogLevel.Error => LogType.Error,
			LogLevel.Critical => LogType.Output,
			_ => LogType.MaxValue
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static LogLevel ToLogLevel(LogType logType) => logType switch
		{
			LogType.Trace => LogLevel.Trace,
			LogType.Debug => LogLevel.Debug,
			LogType.Information => LogLevel.Information,
			LogType.Warning => LogLevel.Warning,
			LogType.Error => LogLevel.Error,
			LogType.Output => LogLevel.Critical,
			_ => LogLevel.None
		};

		public static IDisposable Disposable => NullDisposable.Value;

		sealed class NullDisposable: IDisposable
		{
			public static readonly NullDisposable Value = new NullDisposable();

			private NullDisposable()
			{
			}

			void IDisposable.Dispose()
			{
			}
		}
	}
}
