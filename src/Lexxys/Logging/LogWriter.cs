// Lexxys Infrastructural library.
// file: LogWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Lexxys.Xml;

#nullable enable

namespace Lexxys.Logging
{
	public abstract class LogWriter: ILogWriter
	{
		public const string EventSource = "Lexxys";
		public const int MaxEventLogMessage = 30000;

		private static readonly bool UseSystemEventLog = TestEventLog(EventSource, "Application");

		public static readonly TextFormatSetting TextFormatDefaults = new TextFormatSetting(
			"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff} {IndentMark}{Source}: {Message}",
			"  ",
			". ");

		private readonly LoggingRule _rule;

		public LogWriter(string name, XmlLiteNode config, ILogRecordFormatter? formatter = null)
		{
			if (config == null)
				config = XmlLiteNode.Empty;

			Name = name ?? this.GetType().Name;
			Formatter = CreateFormatter(config["formatter"], config) ?? formatter ?? new LogRecordTextFormatter(TextFormatDefaults.Join(config));
			_rule = LoggingRule.Create(config);
		}

		/// <summary>
		/// Get name of the <see cref="LogWriter"/>
		/// </summary>
		public string Name { get; }

		public abstract string Target { get; }

		protected internal ILogRecordFormatter Formatter { get; }

		public static ILogWriter? FromXml(XmlLiteNode node)
		{
			if (node == null || node.IsEmpty)
				return null;

			string? name = node["name"].AsString(null);
			string? className = node["class"].AsString(null);
			if (className != null)
				return CreateLogWriter(className, name, node);

			LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name));
			return null;
		}

		private static LogWriter? CreateLogWriter(string className, string? name, XmlLiteNode node)
		{
			if (String.IsNullOrEmpty(className))
				return null;
			LogWriter? writer = null;
			try
			{
				Type? type = Factory.GetType(className) ??
					(className.IndexOf('.') < 0 ? Factory.GetType("Lexxys.Logging." + className) : null);
				if (type != null && type.IsSubclassOf(typeof(LogWriter)))
					writer = Factory.TryGetConstructor(type, false, new[] { typeof(string), typeof(XmlLiteNode) })?
						.Invoke(new object?[] { name, node }) as LogWriter;
				if (writer == null)
					LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className));
			}
			catch (Exception e)
			{
				LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className, e));
			}
			return writer;
		}

		private static ILogRecordFormatter? CreateFormatter(string className, XmlLiteNode node)
		{
			if (String.IsNullOrEmpty(className))
				return null;
			ILogRecordFormatter? formatter = null;
			try
			{
				Type? type = Factory.GetType(className) ??
					(className.IndexOf('.') < 0 ? Factory.GetType("Lexxys.Logging." + className) : null);
				if (type != null)
					formatter = Factory.TryGetConstructor(type, false, new[] { typeof(XmlLiteNode) })?
						.Invoke(new object[] { node }) as ILogRecordFormatter;
				if (formatter == null)
					LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogFormatter(className));
			}
			catch (Exception e)
			{
				LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogFormatter(className, e));
			}
			return formatter;
		}

		/// <summary>
		/// Open the <see cref="LogWriter"/> object for logging
		/// </summary>
		public virtual void Open()
		{
		}

		/// <summary>
		/// Close <see cref="LogWriter"/> and free all resources used.
		/// </summary>
		public virtual void Close()
		{
		}

		public bool Accepts(string? source, LogType type) => _rule.Contains(source, type);

		/// <summary>
		/// Log the <paramref name="records"/> to the log
		/// </summary>
		/// <param name="records">The <see cref="LogRecord"/>s to be logged.</param>
		public abstract void Write(IEnumerable<LogRecord> records);

		#region EventLog tools

		private static readonly LogRecordTextFormatter __eventLogFormatter = new LogRecordTextFormatter(EventLogLogWriter.Defaults);

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

		protected static EventLogEntryType LogEntryType(LogType logType)
		{
			return !UseSystemEventLog ? 0 : logType switch
			{
				LogType.Output or LogType.Error => EventLogEntryType.Error,
				LogType.Warning => EventLogEntryType.Warning,
				_ => EventLogEntryType.Information,
			};
		}

		protected internal static void WriteDebugMessage(string source, string message)
		{
			if (Debugger.IsLogging())
				WriteDebugMessage(new LogRecord(LogType.Debug, source, message, null));
		}

		protected internal static void WriteDebugMessage(string source, Func<string> message)
		{
			if (Debugger.IsLogging())
				WriteDebugMessage(new LogRecord(LogType.Debug, source, message(), null));
		}

		protected internal static void WriteDebugMessage(LogRecord record)
		{
			if (Debugger.IsLogging())
			{
				string message = __eventLogFormatter.Format(record);
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message : message + Environment.NewLine);
			}
		}


		protected internal static void WriteErrorMessage(string source, string message)
		{
			WriteErrorMessage(new LogRecord(LogType.Error, source, message, null));
		}

		protected internal static void WriteErrorMessage(string source, string message, IDictionary? argument)
		{
			WriteErrorMessage(new LogRecord(LogType.Error, source, message, argument));
		}

		protected internal static void WriteErrorMessage(string source, Exception exception)
		{
			WriteErrorMessage(new LogRecord(LogType.Error, source, exception));
		}

		protected internal static void WriteErrorMessage(LogRecord record)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			string message = __eventLogFormatter.Format(record);
			if (Debugger.IsLogging())
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message : message + Environment.NewLine);
			if (message.Length > MaxEventLogMessage)
				message = message.Substring(0, MaxEventLogMessage);
			if (UseSystemEventLog)
				EventLog.WriteEntry(EventSource, message, EventLogEntryType.Error);
		}


		protected internal static void WriteEventLogMessage(string source, string message, IDictionary? argument)
		{
			WriteEventLogMessage(new LogRecord(LogType.Information, source, message, argument));
		}

		protected internal static void WriteEventLogMessage(LogRecord record)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			string message = __eventLogFormatter.Format(record);
			if (Debugger.IsLogging())
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message : message + Environment.NewLine);
			if (message.Length > MaxEventLogMessage)
				message = message.Substring(0, MaxEventLogMessage);
			if (UseSystemEventLog)
				EventLog.WriteEntry(EventSource, message, LogEntryType(record.LogType));
		}

		#endregion
	}
}
