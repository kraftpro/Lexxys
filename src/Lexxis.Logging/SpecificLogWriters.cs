// Lexxys Infrastructural library.
// file: ConcreteLogWriters.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Logging;
using Xml;

public class FileLogWriter: LogWriter
{
	private const string LogSource = "Lexxys.Logging.FileLogWriter";

	private readonly TimeSpan _timeout = DefaultTimeout;
	private readonly string _file;
	private bool _truncate;
	private bool _errorLogged;

	public const string DefaultLogFileMask = @"{YMD}.log";
	public const int DefaultBatchSize = 4096/200;
	public const int DefaultFlushBound = 4096/200;

	public static readonly TimeSpan DefaultTimeout = new TimeSpan(TimeSpan.TicksPerSecond * 2);
	public static readonly TimeSpan MaxTimeout = new TimeSpan(TimeSpan.TicksPerSecond * 10);
	public static readonly TimeSpan MinTimeout = new TimeSpan(TimeSpan.TicksPerMillisecond * 100);

	/// <summary>
	/// Creates a new <see cref="LogWriter"/> to write logs to the text file.
	/// </summary>
	/// <param name="parameters"></param>
	public FileLogWriter(LoggingFileParameters parameters): base(parameters)
	{
		_file = parameters.Path ?? DefaultLogFileMask;
		_timeout = Range(parameters.Timeout, DefaultTimeout, MinTimeout, MaxTimeout);
		_truncate = parameters.Overwrite;

		TimeSpan Range(TimeSpan? value, TimeSpan def, TimeSpan min, TimeSpan max)
			=> value == null ? def: value.GetValueOrDefault() < min ? min: value.GetValueOrDefault() > max ? max: value.GetValueOrDefault();
	}

	public override string Target => _file;

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (records == null)
			return;

		using (var o = OpenLog())
		{
			if (o != null)
			{
				foreach (var record in records)
				{
					if (record == null)
						continue;

					Formatter.Format(o, record);
					o.WriteLine();
				}
				return;
			}
		}

		if (!_errorLogged)
		{
			SystemLog.WriteErrorMessage(LogSource, SR.LOG_CannotOpenLogFile(_file), null);
			_errorLogged = true;
		}
		foreach (var record in records)
		{
			SystemLog.WriteEventLogMessage(record.Source, record.Message, record.Data);
		}
	}

	private StreamWriter? OpenLog()
	{
		try
		{
			StreamWriter? o = OpenLogStream(FileMaskToName(Target), _timeout, _truncate);
			for (int i = 0; o == null && i < 5; ++i)
			{
				string name = FileMaskToName(Target);
				int k = name.LastIndexOf('.');
				name = k < 0 ?
					name + "." + SixBitsCoder.Thirty((ulong)(WatchTimer.Query(0) % WatchTimer.TicksPerMinute)).PadLeft(6, '0') + ".":
#if NET6_0_OR_GREATER
					String.Concat(name.AsSpan(0, k), ".", SixBitsCoder.Thirty((ulong)(WatchTimer.Query(0) % WatchTimer.TicksPerMinute)).PadLeft(6, '0'), name.AsSpan(k));
#else
					name.Substring(0, k) + "." + SixBitsCoder.Thirty((ulong)(WatchTimer.Query(0) % WatchTimer.TicksPerMinute)).PadLeft(6, '0') + name.Substring(k);
#endif
				o = OpenLogStream(name, TimeSpan.Zero, _truncate);
			}
			_truncate = false;
			return o;
		}
		catch
		{
			_truncate = false;
			return null;
		}
	}

	private static unsafe string FileMaskToName(string logFileMask)
	{
		if (logFileMask == null)
			return String.Empty;
		if (logFileMask.IndexOf('{') < 0)
			return logFileMask;

		DateTime tm = DateTime.Now;
		var digits = stackalloc char[4];

		return __fileMaskRe.Replace(logFileMask, match =>
		{
			string s = match.Value;
			var r = new StringBuilder();
			for (int i = 1; i < s.Length-1; ++i)
			{
				switch (s[i])
				{
					case 'Y':
						r.Append(D4(tm.Year), 4);
						break;
					case 'y':
						r.Append(D2(tm.Year % 100), 2);
						break;
					case 'M':
						r.Append(D2(tm.Month), 2);
						break;
					case 'D':
						r.Append(D2(tm.Day), 2);
						break;
					case 'd':
						r.Append(D3(tm.DayOfYear), 3);
						break;
					case 'H':
						r.Append(D2(tm.Hour), 2);
						break;
					case 'm':
						r.Append(D2(tm.Minute), 2);
						break;
					case 'W':
						var calendar = new GregorianCalendar();
						var day = calendar.GetWeekOfYear(tm, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
						r.Append(D2(day), 2);
						break;
					default:
						r.Append(s[i]);
						break;
				}
			}
			return r.ToString();
		});

		char* D2(int value)
		{
			digits[1] = (char)('0' + value % 10);
			value /= 10;
			digits[0] = (char)('0' + value % 10);
			return digits;
		}

		char* D3(int value)
		{
			digits[2] = (char)('0' + value % 10);
			value /= 10;
			digits[1] = (char)('0' + value % 10);
			value /= 10;
			digits[0] = (char)('0' + value % 10);
			return digits;
		}

		char* D4(int value)
		{
			digits[3] = (char)('0' + value % 10);
			value /= 10;
			digits[2] = (char)('0' + value % 10);
			value /= 10;
			digits[1] = (char)('0' + value % 10);
			value /= 10;
			digits[0] = (char)('0' + value % 10);
			return digits;
		}
	}
	private static readonly Regex __fileMaskRe = new Regex(@"\{[^\}]*\}");

	private static StreamWriter? OpenLogStream(string? fileName, TimeSpan timeout, bool truncate)
	{
		if (fileName == null || fileName.Length == 0)
			return null;

		const int ErrorSharingViolation = 32;
		const int ErrorLockViolation = 33;

		fileName = Environment.ExpandEnvironmentVariables(fileName);
		var directory = Path.GetDirectoryName(fileName);
		if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		Random? r = null;
		TimeSpan delay = TimeSpan.Zero;
		int bound = 128;
		StreamWriter? o = null;
		do
		{
			try
			{
				o = new StreamWriter(fileName, !truncate);
			}
			catch (IOException e)
			{
				int errorId = Marshal.GetHRForException(e) & 0xFFFF;
				if (errorId != ErrorSharingViolation && errorId != ErrorLockViolation)
					return null;
				if (delay >= timeout)
					return null;
				int sleep = (r ??= new Random()).Next(bound);
				delay += TimeSpan.FromMilliseconds(sleep);
				bound += bound;
				System.Threading.Thread.Sleep(sleep);
			}
		} while (o == null);

		return o;
	}
}


public class NullLogWriter: LogWriter
{
	private static NullLogWriter Instance = new NullLogWriter();

	public override string Target => "Null";

	public NullLogWriter(): base(Parameters.Instance)
	{
	}

	public override void Write(IEnumerable<LogRecord> records)
	{
	}

	class NullLogRecordFormatter: ILogRecordFormatter
	{
		public static readonly ILogRecordFormatter Instance = new NullLogRecordFormatter();

		public void Format(TextWriter writer, LogRecord record) { }
	}

	private class Parameters: ILogWriterParameters
	{
		public static readonly ILogWriterParameters Instance = new Parameters();

		public string? Name { get => "Null"; set { } }
		public string? Include { get => null; set { } }
		public string? Exclude { get => null; set { } }
		public LogType? LogLevel { get => LogType.Output; set { } }
		public int? MaxQueueSize { get => null; set { } }
		public TimeSpan? FlushTimeout { get => null; set { } }
		public ILogRecordFormatterParameters? Formatter { get => null; set { } }
		public ICollection<LogWriterFilter>? Rules { get => null; set { } }

		public ILogWriter CreateWriter() => NullLogWriter.Instance;
	}
}


public class ConsoleLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{Type:xxxx} {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	private readonly bool _isEnabled;

	public ConsoleLogWriter(LoggingConsoleParameters parameters): base(parameters)
	{
		_isEnabled = IsEnabled();
	}

	private static bool IsEnabled()
	{
		try
		{
			return Console.BufferHeight > 0 || Console.IsOutputRedirected || Console.IsErrorRedirected;
			//if (Console.IsOutputRedirected)
			//{
			//	if (Console.Out != TextWriter.Null)
			//		return true;
			//	if (Console.IsErrorRedirected)
			//		return Console.Error != TextWriter.Null;
			//}
			//else if (Console.IsErrorRedirected)
			//{
			//	if (Console.Error != TextWriter.Null)
			//		return true;
			//}
			//return Console.BufferHeight > 0;
		}
		catch { return false; }
	}

	public override string Target => "Console";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (!_isEnabled || records == null)
			return;
		foreach (var record in records)
		{
			if (record == null)
				continue;
			TextWriter writer;
			//bool redirected;
			if (record.LogType == LogType.Error)
			{
				writer = Console.Error;
			//	redirected = Console.IsErrorRedirected;
			}
			else
			{
				writer = Console.Out;
			//	redirected = Console.IsOutputRedirected;
			}

			Formatter.Format(writer, record);
			writer.WriteLine();
		}
	}
}


public class TraceLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	public TraceLogWriter(ILogWriterParameters parameters): base(parameters)
	{
	}

	public override string Target => "Trace";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (records == null)
			return;
		foreach (var record in records)
		{
			if (record != null)
			{
				if (record.Indent != Trace.IndentLevel)
					Trace.IndentLevel = record.Indent;
				Trace.WriteLine(Formatter.Format(record));
			}
		}
	}
}


public class DebuggerLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	private readonly bool _isLogging;

	public DebuggerLogWriter(ILogWriterParameters parameters): base(parameters)
	{
		_isLogging = Debugger.IsLogging();
	}

	public override string Target => "Debugger";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (!_isLogging || records == null)
			return;
		foreach (var record in records)
		{
			if (record == null)
				continue;
			string message = Formatter.Format(record);
			Debugger.Log(record.Priority, record.Source, message.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? message: message + Environment.NewLine);
		}
	}
}


public class EventLogLogWriter: LogWriter
{
	public static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	private readonly string? _eventSource;

	public EventLogLogWriter(LoggingEventParameters parameters) : base(parameters)
	{
		_eventSource = GetEventSource(parameters.EventSource, parameters.LogName);
	}

	private string? GetEventSource(string? eventSource, string? logName)
	{
		eventSource ??= SystemLog.EventSource;
		logName ??= "Application";
		if (eventSource.Length > 254)
			eventSource = eventSource.Substring(0, 254);
		return SystemLog.TestEventLog(eventSource, logName) ? eventSource: null;
	}

	public override string Target => "EventLog";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (_eventSource == null || records == null)
			return;

		foreach (var record in records)
		{
			if (record == null)
				continue;
			string message = Formatter.Format(record);
			if (message.Length > SystemLog.MaxEventLogMessage)
				message = message.Substring(0, SystemLog.MaxEventLogMessage);
			#pragma warning disable CA1416 // Validate platform compatibility
			EventLog.WriteEntry(_eventSource, message, SystemLog.LogEntryType(record.LogType));
		}
	}
}
