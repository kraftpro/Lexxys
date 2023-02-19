// Lexxys Infrastructural library.
// file: Logger.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace Lexxys.Logging;

public class Logger<T>: Logger, ILogging<T>
{
	public new static readonly ILogging<T> Empty = new EmptyLogger();

	public Logger(ILoggingService service): base(service, typeof(T).GetTypeName())
	{
	}

	private class EmptyLogger: ILogging<T>, ILogging
	{
		public string Source { get { return "Empty"; } set { } }

		public IDisposable? BeginScope<TState>(TState state) where TState: notnull => null;

		public IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args) => null;

		public bool IsEnabled(LogType logType) => false;

		public bool IsEnabled(LogLevel logLevel) => false;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
		}

		public void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
		{
		}

		public IDisposable? Timing(LogType logType, string? description, TimeSpan threshold) => null;
	}
}

public class Logger: ILogging
{
	public static readonly ILogging Empty = new EmptyLogger();

	private readonly ILoggingService _service;
	private string _source;
	private ILogRecordWriter? _writer;
	private LogTypeFilter _levels;

	public Logger(ILoggingService service): this(service, "Logger")
	{
	}

	public Logger(ILoggingService service, string source)
	{
		if (service is null)
			throw new ArgumentNullException(nameof(service));
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		_source = source;
		_service = service;
	}

	[Obsolete]
	public Logger(string source)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_service = default!;
	}

	private static LogTypeFilter GetLogTypeMask(ILogRecordWriter writer)
	{
		int levels = 0;
		int mask = 1;
		for (LogType i = 0; i <= LogType.MaxValue; ++i, mask <<= 1)
		{
			if (writer.IsEnabled(i))
				levels |= mask;
		}
		return (LogTypeFilter)levels;
	}

	/// <summary>
	/// Get logger source (usually class name)
	/// </summary>
	public string Source
	{
		get
		{
			return _source;
		}
		set
		{
			_levels = 0;
			_source = value;
			_writer = null;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
	{
		Log(new LogRecord(logType, eventId, source, message, exception, args));
	}

	/// <summary>
	/// Write the <paramref name="record"/> into log
	/// </summary>
	/// <param name="record">The log record to be writen</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Log(LogRecord record)
	{
		if (_writer == null)
		{
			_writer = _service.GetLogWriter(Source);
			_levels = GetLogTypeMask(_writer);
		}
		_writer.Write(record);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IDisposable? Enter(LogType logType, string? section, IDictionary? args)
	{
		if (!IsEnabled(logType))
			return null;
		return Entry.Create(this, section, logType, 0, args);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IDisposable? Timing(LogType logType, string? section, TimeSpan threshold)
	{
		if (!IsEnabled(logType))
			return null;
		int n = (int)(threshold.Ticks / TimeSpan.TicksPerMillisecond);
		return Entry.Create(this, section, section, logType, n == 0 ? -1: n, null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsEnabled(LogType logType)
	{
		if (_writer == null)
		{
			_writer = _service.GetLogWriter(Source);
			_levels = GetLogTypeMask(_writer);
		}
		return (_levels & (LogTypeFilter)(1 << (int)logType)) != 0;
	}

	public void TurnOn()
	{
		_levels = GetLogTypeMask(_writer ??= _service.GetLogWriter(Source));
	}

	public void TurnOff()
	{
		_levels = 0;
	}

	#region ILogger

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool ILogger.IsEnabled(LogLevel logLevel)
		=> LoggingTools.IsEnabled(this, logLevel);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?>? formatter)
		=> LoggingTools.Log(this, logLevel, eventId, state, exception, formatter);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	IDisposable ILogger.BeginScope<TState>(TState state)
		=> LoggingTools.BeginScope(this, state);

	#endregion

	private class Entry: IDisposable
	{
		private readonly Logger _log;
		private readonly string _endMessage;
		private readonly long _stamp;
		private readonly long _threshold;
		private readonly LogType _logType;
		private readonly IDictionary? _arg;

		public Entry(Logger log, string? endMessage, LogType logType, int threshold, IDictionary? arg)
		{
			_log = log;
			_endMessage = endMessage ?? "exiting";
			_threshold = threshold * WatchTimer.TicksPerMillisecond;
			_logType = logType;
			_arg = arg;
			_stamp = WatchTimer.Start();
		}

		public static Entry Create(Logger log, string? sectionName, LogType logType, int threshold, IDictionary? arg)
		{
			if (threshold == 0)
			{
				var rec = new LogRecord(LogGroupingType.BeginGroup, logType, log.Source, (sectionName == null ? SR.LOG_BeginSection() : SR.LOG_BeginSection(sectionName)), arg);
				log.Log(rec);
			}
			return new Entry(log, (sectionName == null ? SR.LOG_EndSection() : SR.LOG_EndSection(sectionName)), logType, threshold, threshold == 0 ? null : arg);
		}

		public static Entry Create(Logger log, string? startMessage, string? endMessage, LogType logType, int threshold, IDictionary? arg)
		{
			if (threshold == 0)
			{
				var rec = new LogRecord(LogGroupingType.BeginGroup, logType, log.Source, startMessage ?? SR.LOG_BeginGroup(), arg);
				log.Log(rec);
			}
			return new Entry(log, (endMessage ?? SR.LOG_EndGroup()), logType, threshold, threshold == 0 ? null : arg);
		}

		void IDisposable.Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				long time = WatchTimer.Stop(_stamp);
				if (_threshold == 0 || _threshold <= time)
				{
					_log.Log(
						new LogRecord(_threshold == 0 ? LogGroupingType.EndGroup : LogGroupingType.Message,
							_logType, _log.Source, _endMessage + " (" + WatchTimer.ToString(time, false) + ")", _arg)
						);
				}
			}
		}
		private bool _disposed;
	}

	private class EmptyLogger: ILogging
	{
		public string Source { get { return "Empty"; } set { } }

		public IDisposable? BeginScope<TState>(TState state) where TState: notnull => null;

		public IDisposable? Enter(LogType logType, string? sectionName, IDictionary? args) => null;

		public bool IsEnabled(LogType logType) => false;

		public bool IsEnabled(LogLevel logLevel) => false;

		public void Log(LogType logType, int eventId, string? source, string? message, Exception? exception, IDictionary? args)
		{
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
		}

		public IDisposable? Timing(LogType logType, string? description, TimeSpan threshold) => null;
	}
}
