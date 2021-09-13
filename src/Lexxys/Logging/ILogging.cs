using System;
using System.Collections;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	using Logging;

	public interface ILogging: ILogger
	{
		string Source { get; }
		bool IsEnabled(LogType logType);
		void Log(LogRecord record);
		IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args);
		IDisposable? Timing(LogType logType, string? description, TimeSpan threshold);

#if NET5_0_OR_GREATER
		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
			=> LoggingTools.Log(this, logLevel, eventId, state, exception, formatter);

		bool ILogger.IsEnabled(LogLevel logLevel)
			=> LoggingTools.IsEnabled(this, logLevel);

		IDisposable? ILogger.BeginScope<TState>(TState state)
			=> LoggingTools.BeginScope(this, state);
#endif

	}
}
