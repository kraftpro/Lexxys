using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys
{
	public static class SystemLog
	{
		public const string EventSource = "Lexxys";
		public const int MaxEventLogMessage = 30000;

		private static readonly bool UseSystemEventLog = TestEventLog(EventSource, "Application");


		//private static readonly LogRecordTextFormatter __eventLogFormatter = new LogRecordTextFormatter(EventLogLogWriter.Defaults);

#pragma warning disable CA1416 // Validate platform compatibility

		internal static bool TestEventLog(string eventSource, string logName)
		{
			try
			{
				if (EventLog.SourceExists(eventSource))
					return true;
			}
			catch
			{
				// ignored
			}
			try
			{
				EventLog.CreateEventSource(eventSource, logName);
				return EventLog.SourceExists(eventSource);
			}
			catch
			{
				// ignored
			}
			return false;
		}

		internal static EventLogEntryType LogEntryType(LogType logType)
		{
			return !UseSystemEventLog ? 0 : logType switch
			{
				LogType.Output or LogType.Error => EventLogEntryType.Error,
				LogType.Warning => EventLogEntryType.Warning,
				_ => EventLogEntryType.Information,
			};
		}

		/// <summary>
		/// Write a message to the Debugger console
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		/// <param name="arguments">Optional parameters</param>
		public static void WriteDebugMessage(string source, string message, IDictionary? arguments = null)
		{
			if (Debugger.IsLogging())
				Debugger.Log(5, EventSource, Format(LogType.Debug, source, message, arguments));
		}

		/// <summary>
		/// Write an error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		/// <param name="arguments">Optional parameters</param>
		public static void WriteErrorMessage(string? source, string message, IDictionary? arguments = null)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			WriteMessage(LogType.Error, Format(LogType.Error, source, message, arguments));
		}

		/// <summary>
		/// Write an error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="exception">Exception info</param>
		/// <param name="arguments">Optional parameters</param>
		public static void WriteErrorMessage(string? source, Exception exception, IDictionary? arguments = null)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			WriteMessage(LogType.Error, Format(LogType.Error, source, exception, arguments));
		}

		/// <summary>
		/// Write an informational message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		/// <param name="arguments">Optional arguments</param>
		public static void WriteEventLogMessage(string? source, string? message, IDictionary? arguments = null)
		{
			WriteMessage(LogType.Information, Format(LogType.Information, source, message, arguments));
		}

		private static void WriteMessage(LogType type, string message)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			if (Debugger.IsLogging())
				Debugger.Log(5, EventSource, message);
			if (message.Length > MaxEventLogMessage)
				message = message.Substring(0, MaxEventLogMessage);
			if (UseSystemEventLog)
				EventLog.WriteEntry(EventSource, message, LogEntryType(type));
		}

		private static string Format(LogType type, string? source, string? message, IDictionary? arguments)
		{
			var dump = new DumpStringWriter();
			dump.Text('[').Text(type.ToString().ToUpperInvariant()).Text(']');
			if (source != null)
				if (message != null)
					dump.Text(source).Text(": ").Text(message).Text('\n');
				else
					dump.Text(source).Text(":\n");
			else if (message != null)
				dump.Text(message).Text('\n');
			else
				dump.Text('\n');

			if (arguments != null)
			{
				foreach (DictionaryEntry item in arguments)
				{
					dump.Text(item.Key?.ToString() ?? "(null)").Text('=').Dump(item.Value).Text('\n');
				}
			}
			return dump.ToString();
		}

		private static string Format(LogType type, string? source, Exception exception, IDictionary? arguments)
		{
			var dump = new DumpStringWriter();
			dump.Text('[').Text(type.ToString().ToUpperInvariant()).Text(']');
			Format(dump, source, exception);
			if (arguments != null)
			{
				foreach (DictionaryEntry item in arguments)
				{
					dump.Text(item.Key?.ToString() ?? "(null)").Text('=').Dump(item.Value).Text('\n');
				}
			}
			return dump.ToString();
		}

		private static void Format(DumpWriter dump, string? source, Exception exception)
		{
			source = source ?? exception.Source;
			var message = exception.Message;
			if (source != null)
				if (message != null)
					dump.Text(source).Text(": ").Text(message).Text('\n');
				else
					dump.Text(source).Text(":\n");
			else if (message != null)
				dump.Text(message).Text('\n');
			if (exception.StackTrace != null)
			{
				dump.Text(exception.StackTrace).Text('\n');
			}
			if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
			{
				foreach (var item in aggregate.InnerExceptions)
				{
					Format(dump, null, item);
				}
			}
			else if (exception.InnerException != null)
			{
				Format(dump, null, exception.InnerException);
			}
		}
	}
}
