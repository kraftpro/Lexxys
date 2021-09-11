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

using Lexxys.Xml;

namespace Lexxys.Logging
{
	public abstract class LogWriter
	{
		private string _name;
		private LoggingRule _rule;
		private LogRecordTextFormatter _formatter;
		private int _batchSize;
		private int _flushBound;

		private static readonly string EventSource = "Lexxys";
		private static readonly bool UseSystemEventLog = TestEventLog();
		private static readonly TextFormatSetting Defaults = new TextFormatSetting(
			"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff} {IndentMark}{Source}: {Message}",
			"  ",
			". ");
		private static readonly TextFormatSetting EventLogDefaults = new TextFormatSetting(
			"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff} {IndentMark}{Source}: {Message}",
			"  ",
			". ");

		/// <summary>
		/// Get name of the <see cref="LogWriter"/>
		/// </summary>
		public string Name => _name;

		internal LoggingRule Rule => _rule;

		public abstract string Target { get; }

		public int BatchSize { get => _batchSize; protected set => _batchSize = value; }

		public int FlushBound { get => _flushBound; protected set => _flushBound = value; }

		public virtual bool IsReady => true;

		protected internal LogRecordTextFormatter Formatter => _formatter;

		protected string Format(LogRecord record) => _formatter.Format(record);

		protected TextWriter Format(TextWriter writer, LogRecord record) => _formatter.Format(writer, record);

		protected virtual TextFormatSetting FormattingDefaults => Defaults;

		protected static string DefaultLogEventSource => EventSource;

		/// <summary>
		/// Initialize the LogWriter from XML.
		/// </summary>
		/// <param name="name">Name of the <see cref="LogWriter"/>.</param>
		/// <param name="config">Not used</param>
		protected virtual void Initialize(string name, XmlLiteNode config)
		{
			if (config == null)
				config = XmlLiteNode.Empty;

			_name = name ?? this.GetType().Name;
			_batchSize = config["batchSize"].AsInt32(LoggingContext.DefaultBatchSize);
			_flushBound = config["flushBound"].AsInt32(LoggingContext.DefaultFlushBound);
			_formatter = new LogRecordTextFormatter(FormattingDefaults.Join(config));
			_rule = LoggingRule.Create(config);
		}

		public static LogWriter Empty { get; } = new EmptyWriter();

		public static LogWriter FromXml(XmlLiteNode node)
		{
			if (node == null || node.IsEmpty)
				return null;

			string name = node["name"].AsString(null);
			string className = node["class"].AsString(null);
			if (className == null)
			{
				LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, null, null));
				return Empty;
			}
			try
			{
				var writer = Factory.TryConstruct(Factory.GetType(className), false) as LogWriter ??
					Factory.TryConstruct(Factory.GetType("Lexxys.Logging." + className), false) as LogWriter;
				if (writer != null)
				{
					writer.Initialize(name, node);
					return writer.Rule.IsEmpty ? Empty: writer;
				}
				LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className, null));
			}
			catch (Exception e)
			{
				LogWriter.WriteErrorMessage("Lexxys.Logging.LoggingContext", SR.LOG_CannotCreateLogWriter(name, className, e));
			}
			return Empty;
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
		/// <summary>
		/// Log the <paramref name="record"/> to the log
		/// </summary>
		/// <param name="record">The <see cref="LogRecord"/> to be logged.</param>
		public abstract void Write(LogRecord record);

		public virtual void Write(IEnumerable<LogRecord> records)
		{
			if (records == null)
				return;

			foreach (LogRecord record in records)
			{
				if (record != null)
					Write(record);
			}
		}

		protected static string LogEventSource => EventSource;


		#pragma warning disable CA1416 // Validate platform compatibility

		private static readonly LogRecordTextFormatter __eventLogFormatter = new LogRecordTextFormatter(EventLogDefaults);

		private static bool TestEventLog()
		{
			try
			{
				if (EventLog.SourceExists(EventSource))
					return true;
			}
			catch
			{
				// ignored
			}
			try
			{
				EventLog.CreateEventSource(EventSource, "Application");
				return EventLog.SourceExists(EventSource);
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
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message: message + Environment.NewLine);
			}
		}


		protected internal static void WriteErrorMessage(string source, string message)
		{
			WriteErrorMessage(new LogRecord(LogType.Error, source, message, null));
		}

		protected internal static void WriteErrorMessage(string source, string message, IDictionary argument)
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
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message: message + Environment.NewLine);
			if (message.Length > 32765)
				message = message.Substring(0, 32765);
			if (UseSystemEventLog)
				EventLog.WriteEntry(EventSource, message, EventLogEntryType.Error);
		}


		protected internal static void WriteEventLogMessage(string source, string message, IDictionary argument)
		{
			WriteEventLogMessage(new LogRecord(LogType.Information, source, message, argument));
		}

		protected internal static void WriteEventLogMessage(LogRecord record)
		{
			if (!UseSystemEventLog && !Debugger.IsLogging())
				return;
			string message = __eventLogFormatter.Format(record);
			if (Debugger.IsLogging())
				Debugger.Log(5, EventSource, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message: message + Environment.NewLine);
			if (message.Length > 32765)
				message = message.Substring(0, 32765);
			if (UseSystemEventLog)
				EventLog.WriteEntry(EventSource, message, LogEntryType(record.LogType));
		}

		class EmptyWriter: LogWriter
		{
			public EmptyWriter()
			{
				_name = "Empty";
				_formatter = new LogRecordTextFormatter(Defaults);
				_rule = LoggingRule.Empty;
			}

			public override string Target => "(null)";

			public override void Write(LogRecord record)
			{
			}
		}
	}
}
