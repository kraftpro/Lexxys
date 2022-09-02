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
	private readonly TimeSpan _timeout = DefaultTimeout;
	private readonly string _file;
	private bool _truncate;
	private bool _errorLogged;

	public const string DefaultLogFileMask = @"{YMD}.log";
	public const int DefaultBatchSize = 4096/200;
	public const int DefaultFlushBound = 4096/200;

	public static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 0, 2);
	public static readonly TimeSpan MaxTimeout = new TimeSpan(0, 0, 10);
	public static readonly TimeSpan MinTimeout = new TimeSpan(0, 0, 0, 100);

	/// <summary>
	/// Creates a new <see cref="LogWriter"/> to write logs to the text file.
	/// </summary>
	/// <param name="name">Name of the <see cref="LogWriter"/> for future reference.</param>
	/// <param name="config">
	///	Initialization XML containing 'parameters' element with attributes:
	///		format setting <see cref="TextFormatSetting.Join(XmlLiteNode)"/>
	///		file		- mask of file name. default: %TEMP%\Logs\{YMD}-LL.log
	///		timeout		- timeout while opening file in milliseconds. default: 2000
	/// </param>
	/// <param name="formatter"></param>
	/// 
	/// 
	public FileLogWriter(string name, XmlLiteNode? config, ILogRecordFormatter? formatter):
		base(name, config, formatter)
	{
		config ??= XmlLiteNode.Empty;
		_file = XmlTools.GetString(config["file"], DefaultLogFileMask)!;
		_timeout = XmlTools.GetTimeSpan(config["timeout"], DefaultTimeout, MinTimeout, MaxTimeout);
		XmlLiteNode? overwrite = config.FirstOrDefault("overwrite");
		_truncate = overwrite != null && overwrite.Value.AsBoolean(true);
	}

	public FileLogWriter(string name, XmlLiteNode? config): this(name, config, null)
	{
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
					if (record != null)
						o.Write(record, Formatter).WriteLine();
				}
				return;
			}
		}

		if (!_errorLogged)
		{
			SystemLog.WriteErrorMessage("TextFileLogWriter", SR.LOG_CannotOpenLogFile(_file), null);
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

	private static string FileMaskToName(string logFileMask)
	{
		if (logFileMask == null)
			return String.Empty;

		if (logFileMask.IndexOf('{') < 0)
			return logFileMask;

		DateTime tm = DateTime.Now;

		return __fileMaskRe.Replace(logFileMask, match =>
		{
			string s = match.Value;
			var r = new StringBuilder();
			for (int i = 1; i < s.Length-1; ++i)
			{
				switch (s[i])
				{
					case 'Y':
						r.Append(tm.Year.ToString(CultureInfo.InvariantCulture));
						break;
					case 'y':
						r.Append((tm.Year % 100).ToString("D2", CultureInfo.InvariantCulture));
						break;
					case 'M':
						r.Append(tm.Month.ToString("D2", CultureInfo.InvariantCulture));
						break;
					case 'D':
						r.Append(tm.Day.ToString("D2", CultureInfo.InvariantCulture));
						break;
					case 'd':
						r.Append(tm.DayOfYear.ToString("D3", CultureInfo.InvariantCulture));
						break;
					case 'H':
						r.Append(tm.Hour.ToString("D2", CultureInfo.InvariantCulture));
						break;
					case 'm':
						r.Append(tm.Minute.ToString("D2", CultureInfo.InvariantCulture));
						break;
					default:
						r.Append(s[i]);
						break;
				}
			}
			return r.ToString();
		});
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

public class TextFileLogWriter: FileLogWriter
{
	public TextFileLogWriter(string name, XmlLiteNode config): base(name, config, formatter: new LogRecordTextFormatter(TextFormatDefaults.Join(config)))
	{
	}
}

public class JsonFileLogWriter: FileLogWriter
{
	public JsonFileLogWriter(string name, XmlLiteNode config): base(name, config, formatter: new LogRecordJsonFormatter(config))
	{
	}
}


public class NullLogWriter: LogWriter
{

	public override string Target => "Null";

	public NullLogWriter(string name, XmlLiteNode? config): base(name, config, NullLogRecordFormatter.Instance)
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
}

public class ConsoleLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{Type} {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	public ConsoleLogWriter(string name, XmlLiteNode? config): base(name, config, new LogRecordTextFormatter(Defaults))
	{
	}

	public override string Target => "Console";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (records == null)
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
		}
	}
}


public class TraceLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	public TraceLogWriter(string name, XmlLiteNode? config): base(name, config, new LogRecordTextFormatter(Defaults))
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
				Trace.WriteLine(Formatter.Format(record));
		}
	}
}


public class DebuggerLogWriter: LogWriter
{
	private static readonly TextFormatSetting Defaults = new TextFormatSetting(
		"{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:HH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}: {Message}",
		"  ",
		". ");

	public DebuggerLogWriter(string name, XmlLiteNode? config): base(name, config, new LogRecordTextFormatter(Defaults))
	{
	}

	public override string Target => "Debugger";

	public override void Write(IEnumerable<LogRecord> records)
	{
		if (!Debugger.IsLogging() || records == null)
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

	public EventLogLogWriter(string name, XmlLiteNode? config): base(name, config, new LogRecordTextFormatter(Defaults))
	{
		config ??= XmlLiteNode.Empty;
		var eventSource = XmlTools.GetString(config["eventSource"], SystemLog.EventSource)!;
		if (eventSource.Length > 254)
			eventSource = eventSource.Substring(0, 254);
		var logName = XmlTools.GetString(config["logName"], "Application")!;
		if (SystemLog.TestEventLog(eventSource, logName))
			_eventSource = eventSource;
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
