// Lexxys Infrastructural library.
// file: Logger.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Lexxys
{
	using System.Threading;
	using Logging;

	public class Logger
	{
		private readonly string _source;
		private bool _includeStackTrace;
		private LogRecordsListener[] _listeners;
		private LogTypeMask _levels;

		public Logger(string source)
		{
			_source = source;
			_listeners = Array.Empty<LogRecordsListener>();
			_levels = LogTypeMask.None;
			LoggingContext.Register(this);
		}

		public static Logger TryCreate(string source)
		{
			return LoggingContext.IsInitialized ? new Logger(source): null;
		}

		internal void Update()
		{
			_listeners = LoggingContext.GetListeners(_source);
			_levels = LoggingContext.IsEnabled ? SetToOn(_listeners): LogTypeMask.None;
		}

		private static LogTypeMask SetToOn(LogRecordsListener[] listeners)
		{
			int levels = 0;
			for (int i = 0, m = 1; i < listeners.Length; ++i, m <<= 1)
			{
				if (listeners[i] != null)
					levels |= m;
			}
			return (LogTypeMask)levels;
		}

		public static bool Initialized => LoggingContext.IsInitialized;

		/// <summary>
		/// True, if Direct messages will be logged (Write(...) methods)
		/// </summary>
		public bool WriteEnabled
		{
			get { return (_levels & LogTypeMask.Output) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Output] != null)
					_levels |= LogTypeMask.Output;
				else
					_levels &= ~LogTypeMask.Output;
			}
		}
		/// <summary>
		/// True, if Error messages will be logged (Error(...) methods)
		/// </summary>
		public bool ErrorEnabled
		{
			get { return (_levels & LogTypeMask.Error) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Error] != null)
					_levels |= LogTypeMask.Error;
				else
					_levels &= ~LogTypeMask.Error;
			}
		}
		/// <summary>
		/// True, if Warning messages will be logged (Warning(...) methods)
		/// </summary>
		public bool WarningEnabled
		{
			get { return (_levels & LogTypeMask.Warning) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Warning] != null)
					_levels |= LogTypeMask.Warning;
				else
					_levels &= ~LogTypeMask.Warning;
			}
		}
		/// <summary>
		/// True, if Information messages will be logged (Info(...) methods)
		/// </summary>
		public bool InfoEnabled
		{
			get { return (_levels & LogTypeMask.Information) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Information] != null)
					_levels |= LogTypeMask.Information;
				else
					_levels &= ~LogTypeMask.Information;
			}
		}
		/// <summary>
		/// True, if Debug messages will be logged (Debug(...) methods)
		/// </summary>
		public bool DebugEnabled
		{
			get { return (_levels & LogTypeMask.Debug) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Debug] != null)
					_levels |= LogTypeMask.Debug;
				else
					_levels &= ~LogTypeMask.Debug;
			}
		}
		/// <summary>
		/// True, if Trace messages will be logged (Trace(...) methods)
		/// </summary>
		public bool TraceEnabled
		{
			get { return (_levels & LogTypeMask.Trace) != 0; }
			set
			{
				if (value && _listeners[(int)LogType.Trace] != null)
					_levels |= LogTypeMask.Trace;
				else
					_levels &= ~LogTypeMask.Trace;
			}
		}
		/// <summary>
		/// Get logger source (usually class name)
		/// </summary>
		public string Source => _source;

		public void TurnOn()
		{
			_levels = SetToOn(_listeners);
		}

		public void TurnOff()
		{
			_levels = 0;
		}

		public static Dictionary<string, object> Args(params object[] args)
		{
			return LogRecord.Args(args);
		}

		public static void Flush()
		{
			LoggingContext.FlushBuffers();
		}

		public static void Close()
		{
			LoggingContext.Stop();
		}

		/// <summary>
		/// True - to include stack trace info into log record
		/// </summary>
		public bool IncludeStackTrace
		{
			get { return _includeStackTrace; }
			set { _includeStackTrace = value; }
		}

		/// <summary>
		/// Write the <paramref name="record"/> into log
		/// </summary>
		/// <param name="record">The log record to be writen</param>
		public void Write(LogRecord record)
		{
			if (record == null)
				throw new ArgumentNullException(nameof(record));
			LogRecordsListener ls = _listeners[(int)record.LogType];
			if (ls != null)
				ls.Write(record);
		}

		/// <summary>
		/// Write a message to the Debugger console
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteDebugMessage(string source, string message)
		{
			LogWriter.WriteDebugMessage(source, message);
		}

		/// <summary>
		/// Write a message to the Debugger console
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteDebugMessage(string source, Func<string> message)
		{
			LogWriter.WriteDebugMessage(source, message);
		}

		/// <summary>
		/// Write informational message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteEventLogMessage(string source, string message)
		{
			LogWriter.WriteEventLogMessage(new LogRecord(source, message, LogType.Information, null));
		}

		/// <summary>
		/// Write informational message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteEventLogMessage(string source, Func<string> message)
		{
			LogWriter.WriteEventLogMessage(new LogRecord(source, message(), LogType.Information, null));
		}

		/// <summary>
		/// Write error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteErrorMessage(string source, string message)
		{
			LogWriter.WriteErrorMessage(source, message, null);
		}

		/// <summary>
		/// Write error message to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="message">The message</param>
		public static void WriteErrorMessage(string source, Func<string> message)
		{
			LogWriter.WriteErrorMessage(source, message(), null);
		}

		/// <summary>
		/// Write exception info to the Windows Event Log
		/// </summary>
		/// <param name="source">Source of message</param>
		/// <param name="exception">The exception</param>
		public static void WriteErrorMessage(string source, Exception exception)
		{
			LogWriter.WriteErrorMessage(source, exception);
		}

		#region Template
		//.?
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Trace(Exception exception)
		{
			Trace(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Trace(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Trace, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Trace, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Trace(string message)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, message, LogType.Trace, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Trace(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, message, LogType.Trace, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Trace(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Trace, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Trace(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Trace, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Trace(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Trace, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Trace(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Trace, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Trace(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, message, LogType.Trace, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Trace(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Trace, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Trace(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Trace, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Trace(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Trace, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void TraceFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Trace, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void TraceFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Trace, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void TraceFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Trace, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void TraceFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Trace) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Trace, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Trace"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable TraceTiming(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Trace, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Trace"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable TraceTiming(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Trace, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Trace"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable TraceTiming(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Trace, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Trace"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable TraceTiming(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Trace, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Trace"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable TraceEnter(string sectionName)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Trace, 0, null);
		}
		public IDisposable TraceEnter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Trace, 0, arg);
		}
		public IDisposable TraceEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Trace, 0, arg);
		}
		public IDisposable TraceEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Trace, 0, arg);
		}
		public IDisposable TraceEnter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Trace, 0, arg);
		}
		public IDisposable TraceEnter(string sectionName, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Trace) == 0)
				return null;

			return Entry.Create(this, sectionName, LogType.Trace, 0, args);
		}
		//.?$X = above("LogType.Trace", "TraceTiming", "TraceEnter", "Trace", "[Conditional(\"TRACE\")]", "#if TRACE", "#else\n\treturn null;\n#endif");
		#endregion
		//.#back($X, "LogType.Debug", "DebugTiming", "DebugEnter", "Debug", "[Conditional(\"DEBUG\")]", "#if DEBUG", "#else\n\treturn null;\n#endif")
		#region sp2.pl
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Debug(Exception exception)
		{
			Debug(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Debug(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Debug, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Debug, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Debug(string message)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, message, LogType.Debug, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Debug(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, message, LogType.Debug, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Debug(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Debug, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Debug(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Debug, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Debug(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Debug, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Debug(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Debug, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Debug(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, message, LogType.Debug, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Debug(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Debug, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Debug(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Debug, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Debug(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Debug, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void DebugFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Debug, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void DebugFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Debug, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void DebugFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Debug, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void DebugFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Debug) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Debug, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Debug"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable DebugTiming(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Debug, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Debug"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable DebugTiming(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Debug, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Debug"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable DebugTiming(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Debug, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Debug"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable DebugTiming(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Debug, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Debug"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable DebugEnter(string sectionName)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Debug, 0, null);
		}
		public IDisposable DebugEnter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Debug, 0, arg);
		}
		public IDisposable DebugEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Debug, 0, arg);
		}
		public IDisposable DebugEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Debug, 0, arg);
		}
		public IDisposable DebugEnter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Debug) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Debug, 0, arg);
		}
		#endregion
		//.=cut
		//~.#back($X, "LogType.Audit", "AuditTiming", "AuditEnter", "Audit")
		//~.=cut
		//.#back($X, "LogType.Information", "InfoTiming", "InfoEnter", "Info")
		#region sp2.pl
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Info(Exception exception)
		{
			Info(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Info(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Information, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Information, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Info(string message)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, message, LogType.Information, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Info(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, message, LogType.Information, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Info(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Information, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Info(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Information, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Info(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Information, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Info(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Information, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Info(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, message, LogType.Information, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Info(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Information, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Info(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Information, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Info(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Information) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Information, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void InfoFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Information, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void InfoFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Information, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void InfoFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Information, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void InfoFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Information) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Information, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Information"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable InfoTiming(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Information, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Information"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable InfoTiming(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Information, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Information"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable InfoTiming(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Information, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Information"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable InfoTiming(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Information, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Information"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable InfoEnter(string sectionName)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Information, 0, null);
		}
		public IDisposable InfoEnter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Information, 0, arg);
		}
		public IDisposable InfoEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Information, 0, arg);
		}
		public IDisposable InfoEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Information, 0, arg);
		}
		public IDisposable InfoEnter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Information) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Information, 0, arg);
		}
		#endregion
		//.=cut
		//.#back($X, "LogType.Warning", "WarningTiming", "WarningEnter", "Warning")
		#region sp2.pl
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Warning(Exception exception)
		{
			Warning(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Warning(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Warning, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Warning, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Warning(string message)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, message, LogType.Warning, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Warning(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, message, LogType.Warning, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Warning(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Warning, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Warning(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Warning, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Warning(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Warning, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Warning(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Warning, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Warning(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, message, LogType.Warning, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Warning(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Warning, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Warning(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Warning, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Warning(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Warning, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void WarningFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Warning, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void WarningFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Warning, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void WarningFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Warning, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void WarningFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Warning) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Warning, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Warning"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable WarningTiming(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Warning, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Warning"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable WarningTiming(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Warning, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Warning"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable WarningTiming(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Warning, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Warning"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable WarningTiming(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Warning, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Warning"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable WarningEnter(string sectionName)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Warning, 0, null);
		}
		public IDisposable WarningEnter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Warning, 0, arg);
		}
		public IDisposable WarningEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Warning, 0, arg);
		}
		public IDisposable WarningEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Warning, 0, arg);
		}
		public IDisposable WarningEnter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Warning) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Warning, 0, arg);
		}
		#endregion
		//.=cut
		//.#back($X, "LogType.Error", "ErrorTiming", "ErrorEnter", "Error")
		#region sp2.pl
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Error(Exception exception)
		{
			Error(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Error(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Error, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Error, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Error(string message)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, message, LogType.Error, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Error(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, message, LogType.Error, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Error(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Error, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Error(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Error, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Error(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Error, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Error(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Error, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Error(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, message, LogType.Error, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Error(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Error, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Error(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Error, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Error(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Error, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void ErrorFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Error, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void ErrorFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Error, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void ErrorFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Error, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void ErrorFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Error, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Error"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable ErrorTiming(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Error, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Error"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable ErrorTiming(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Error, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Error"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable ErrorTiming(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Error, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Error"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable ErrorTiming(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Error, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Error"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable ErrorEnter(string sectionName)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Error, 0, null);
		}
		public IDisposable ErrorEnter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Error, 0, arg);
		}
		public IDisposable ErrorEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Error, 0, arg);
		}
		public IDisposable ErrorEnter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Error, 0, arg);
		}
		public IDisposable ErrorEnter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Error) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Error, 0, arg);
		}
		#endregion
		//.=cut
		//.#back($X, "LogType.Output", "Timing", "Enter", "Write")
		#region sp2.pl
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="exception">Exception to be logged</param>
		public void Write(Exception exception)
		{
			Write(_source, exception);
		}
		/// <summary>
		/// Write the <paramref name="exception"/> to log.
		/// </summary>
		/// <param name="source">Source of the exception</param>
		/// <param name="exception">Exception to be logged</param>
		public void Write(string source, Exception exception)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Output, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Output, exception, ++i);
				}
				Write(record);
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		public void Write(string message)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, message, LogType.Output, null));
		}
		/// <summary>
		/// Write <paramref name="message"/> and arguments to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Write(string message, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, message, LogType.Output, args));
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argValue">Argument value</param>
		public void Write(string message, object argValue)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{"argument", argValue}};
				Write(new LogRecord(_source, message, LogType.Output, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="argName">Argument name</param>
		/// <param name="argValue">Argument value</param>
		public void Write(string message, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				var arg = new Dictionary<string, object>(1) {{argName, argValue}};
				Write(new LogRecord(_source, message, LogType.Output, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		public void Write(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
				Write(new LogRecord(_source, message, LogType.Output, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="arg1Name">1st argument name</param>
		/// <param name="arg1Value">1st arguemnt value</param>
		/// <param name="arg2Name">2nd argument Name</param>
		/// <param name="arg2Value">2nd argument value</param>
		/// <param name="arg3Name">3rd argument name</param>
		/// <param name="arg3Value">3rd argument value</param>
		public void Write(string message, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
				Write(new LogRecord(_source, message, LogType.Output, arg));
			}
		}
		/// <summary>
		/// Write <paramref name="message"/> and argument to log.
		/// </summary>
		/// <param name="message">Message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Write(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, message, LogType.Output, LogRecord.Args(args)));
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		public void Write(Func<string> messageFunction)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Output, null));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">Arguments as Name, Value pairs</param>
		public void Write(Func<string> messageFunction, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Output, args));
			}
		}
		/// <summary>
		/// Write result of <paramref name="messageFunction"/> evaluation to log.
		/// </summary>
		/// <param name="messageFunction">Function that produced a message to be logged</param>
		/// <param name="args">An array contained name/value pair of arguments</param>
		public void Write(Func<string> messageFunction, params object[] args)
		{
			if ((_levels & LogTypeMask.Output) != 0)
			{
				if (messageFunction == null)
					throw new ArgumentNullException(nameof(messageFunction));
				Write(new LogRecord(_source, messageFunction(), LogType.Output, LogRecord.Args(args)));
			}
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The <see cref="System.Object"/> to format</param>
		public void WriteFormat(string message, object arg1)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1), LogType.Output, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		public void WriteFormat(string message, object arg1, object arg2)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2), LogType.Output, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="arg1">The first <see cref="System.Object"/> to format</param>
		/// <param name="arg2">The second <see cref="System.Object"/> to format</param>
		/// <param name="arg3">The third <see cref="System.Object"/> to format</param>
		public void WriteFormat(string message, object arg1, object arg2, object arg3)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, arg1, arg2, arg3), LogType.Output, null));
		}
		/// <summary>
		/// Write formated <paramref name="message"/> to log.
		/// </summary>
		/// <param name="message">A string contained format items</param>
		/// <param name="args">An array contained objects to format</param>
		public void WriteFormat(string message, params object[] args)
		{
			if ((_levels & LogTypeMask.Output) != 0)
				Write(new LogRecord(_source, String.Format(CultureInfo.CurrentCulture, message, args), LogType.Output, null));
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Output"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable Timing(int threshold = 0)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			return Entry.Create(this, "start timer", "stop timer", LogType.Output, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Output"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable Timing(TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, "start timer", "stop timer", LogType.Output, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Output"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold in miliseconds</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable Timing(string description, int threshold = 0)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			return Entry.Create(this, description, description, LogType.Output, threshold == 0 ? -1: threshold, null);
		}
		/// <summary>
		/// Start timer with <see cref="Lexxys.Logging.LogType.Output"/> level and log notification if duration greater or equal to specified <paramref name="threshold"/>. Should be used in the C# using directive.
		/// </summary>
		/// <param name="description">Description</param>
		/// <param name="threshold">Time threshold</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable Timing(string description, TimeSpan threshold)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
			return Entry.Create(this, description, description, LogType.Output, n == 0 ? -1: n, null);
		}
		/// <summary>
		/// Begin named section with <see cref="Lexxys.Logging.LogType.Output"/> level. All records inside the section will indented.
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <returns>Object to be disposed at the end of time calculation</returns>
		public IDisposable Enter(string sectionName)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			return Entry.Create(this, sectionName, LogType.Output, 0, null);
		}
		public IDisposable Enter(string sectionName, string argName, object argValue)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			var arg = new Dictionary<string, object>(1) {{argName, argValue}};
			return Entry.Create(this, sectionName, LogType.Output, 0, arg);
		}
		public IDisposable Enter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			var arg = new Dictionary<string, object>(2) {{arg1Name, arg1Value}, {arg2Name, arg2Value}};
			return Entry.Create(this, sectionName, LogType.Output, 0, arg);
		}
		public IDisposable Enter(string sectionName, string arg1Name, object arg1Value, string arg2Name, object arg2Value, string arg3Name, object arg3Value)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;
			var arg = new Dictionary<string, object>(3) {{arg1Name, arg1Value}, {arg2Name, arg2Value}, {arg3Name, arg3Value}};
			return Entry.Create(this, sectionName, LogType.Output, 0, arg);
		}
		public IDisposable Enter(string sectionName, params object[] args)
		{
			if ((_levels & LogTypeMask.Output) == 0)
				return null;

			var arg = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				arg.Add(args[i - 1] == null ? "(null)": args[i - 1].ToString(), args[i]);
			}
			return Entry.Create(this, sectionName, LogType.Output, 0, arg);
		}
		#endregion
		//.=cut

		public void Error(string source, Exception exception, Dictionary<string, object> args)
		{
			if ((_levels & LogTypeMask.Error) != 0)
			{
				if (exception == null)
					throw new ArgumentNullException(nameof(exception));
				if (source == null)
					source = _source;

				var record = new LogRecord(source, LogType.Error, exception);
				int i = record.Indent;

				while (exception.InnerException != null)
				{
					Write(record);
					exception = exception.InnerException;
					record = new LogRecord(source, LogType.Error, exception, ++i);
				}
				if (args != null)
				{
					foreach (var o in args)
					{
						record.Add(o.Key, o.Value);
					}
				}
				Write(record);
			}
		}


		private class Entry: IDisposable
		{
			private Logger _log;
			private readonly string _endMessage;
			private readonly long _stamp;
			private readonly long _threshold;
			private readonly LogType _logType;
			private readonly Dictionary<string, object> _arg;

			private Entry(Logger log, string endMessage, LogType logType, int threshold, Dictionary<string, object> arg)
			{
				_log = log;
				_endMessage = endMessage ?? "exiting";
				_threshold = threshold * WatchTimer.TicksPerMillisecond;
				_logType = logType;
				_arg = arg;
				_stamp = WatchTimer.Start();
			}

			public static Entry Create(Logger log, string sectionName, LogType logType, int threshold, Dictionary<string, object> arg)
			{
				if (threshold == 0)
				{
					var rec = new LogRecord(LogGroupingType.BeginGroup, log.Source, (sectionName == null ? SR.LOG_BeginSection(): SR.LOG_BeginSection(sectionName)), logType, arg);
					log.Write(rec);
				}
				return new Entry(log, (sectionName == null ? SR.LOG_EndSection(): SR.LOG_EndSection(sectionName)), logType, threshold, threshold == 0 ? null : arg);
			}

			public static Entry Create(Logger log, string startMessage, string endMessage, LogType logType, int threshold, Dictionary<string, object> arg)
			{
				if (threshold == 0)
				{
					var rec = new LogRecord(LogGroupingType.BeginGroup, log.Source, startMessage ?? SR.LOG_BeginGroup(), logType, arg);
					log.Write(rec);
				}
				return new Entry(log, (endMessage ?? SR.LOG_EndGroup()), logType, threshold, threshold == 0 ? null: arg);
			}

			void IDisposable.Dispose()
			{
				if (_log != null)
				{
					long time = WatchTimer.Stop(_stamp);
					if (_threshold == 0 || _threshold <= time)
					{
						_log.Write(
							new LogRecord(_threshold == 0 ? LogGroupingType.EndGroup: LogGroupingType.Message,
								_log.Source, _endMessage + " (" + WatchTimer.ToString(time, false) + ")", _logType, _arg)
							);
					}
					_log = null;
				}
			}
		}

		public static int LockLogging()
		{
			lock (SyncObj)
			{
				if (++__lockDepth == 1)
					LoggingContext.Disable();
				return __lockDepth;
			}
		}

		public static int UnlockLogging()
		{
			lock (SyncObj)
			{
				if (--__lockDepth == 0)
					LoggingContext.Enable();
				return __lockDepth;
			}
		}
		private static int __lockDepth;
		private static readonly object SyncObj = new object();
	}

}
