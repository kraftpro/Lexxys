using System;
using System.Collections;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys
{
	public enum LogType
	{
		Output = 0,
		Error = 1,
		Warning = 2,
		Information = 3,
		Trace = 4,
		Debug = 5,
		MaxValue = Debug
	}

	public interface ILogging: ILogger
	{
		string Source { get; }
		bool IsEnabled(LogType logType);
		void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args);
		IDisposable? Enter(LogType logType, string? section, IDictionary? args);
		IDisposable? Timing(LogType logType, string? section, TimeSpan threshold);

#if NET5_0_OR_GREATER
		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
			=> LoggingTools.Log(this, logLevel, eventId, state, exception, formatter);

		bool ILogger.IsEnabled(LogLevel logLevel)
			=> LoggingTools.IsEnabled(this, logLevel);

		IDisposable ILogger.BeginScope<TState>(TState state)
			=> LoggingTools.BeginScope(this, state);
#endif

	}

	public interface ILogging<out T>: ILogging, ILogger<T>
	{
	}
}
