// Lexxys Infrastructural library.
// file: LogRecord.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lexxys.Logging;

public enum LogGroupingType
{
	Message = 0,
	BeginGroup = 1,
	EndGroup = 2
}

public class LogRecord: IDumpJson
{
	private const string NullArg = "null";
	private static readonly AsyncLocal<int> _currentIndent = new();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LogRecord(LogType logType, string? source, string? message, IEnumerable<NameValueTuple<string, object?>>? args = null)
		: this(logType, 0, source, message, null, args)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LogRecord(LogType logType, string? source, Exception exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
		: this(logType, 0, source, null, exception, args)
	{
	}

	public LogRecord(LogType logType, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
		: this(logType, 0, source, message, exception, args)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LogRecord(LogType logType, int eventId, string? source, string? message, IEnumerable<NameValueTuple<string, object?>>? args = null)
		: this(logType, eventId, source, message, null, args)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public LogRecord(LogType logType, int eventId, string? source, Exception exception, IEnumerable<NameValueTuple<string, object?>>? args = null)
		: this(logType, eventId, source, null, exception, args)
	{
	}

	public LogRecord(LogType logType, int eventId, string? source, string? message, Exception? exception, IEnumerable<NameValueTuple<string, object?>>? args)
	{
		if (source != null)
		{
			Source = source;
		}
		else if (exception != null)
		{
			MethodBase? method = exception.TargetSite;
			Source = (method == null) ? exception.Source: method.GetType().Name;
		}
		Message = message;
		Data = args?.ToIReadOnlyCollection();
        Exception = exception == null ? null: new ExceptionInfo(exception);
		LogType = logType;
		EventId = eventId;
		RecordType = LogGroupingType.Message;
		Indent = Math.Max(0, _currentIndent.Value);
		Context = new SystemContext();
	}

	public LogRecord(LogGroupingType recordType, LogType logType, string? source, string? message, IEnumerable<NameValueTuple<string, object?>>? args)
    {
        Source = source;
        Message = message;
        Data = args?.ToIReadOnlyCollection();
        LogType = logType;
        RecordType = recordType;
        Indent = Math.Max(0, _currentIndent.Value);
        if (RecordType == LogGroupingType.BeginGroup)
            ++_currentIndent.Value;
		else if (RecordType == LogGroupingType.EndGroup && Indent > 0)
			--_currentIndent.Value;
		Context = new SystemContext();
	}

    /// <summary>Name of class and method generated the log item</summary>
    public string? Source { get; }

	/// <summary>Log message</summary>
	public string? Message { get; }

	public int Indent { get; }

	/// <summary>Actual parameters values</summary>
	public IReadOnlyCollection<NameValueTuple<string, object?>>? Data { get; }

	/// <summary>Exception</summary>
	public ExceptionInfo? Exception { get; }

	/// <summary>Priority of the log item. (5 - critical, 0 - verbose)</summary>
	public int Priority => (int)LogType.MaxValue - (int)LogType;

	public LogType LogType { get; }

	public int EventId { get; }

	public LogGroupingType RecordType { get; }

	public SystemContext Context { get; }

	public static IReadOnlyCollection<NameValueTuple<string, object?>>? Args(params object?[] args)
	{
		if (args is not { Length: >0 })
			return null;

		var arg = new List<NameValueTuple<string, object?>>((args.Length + 1) / 2);
		int count = args.Length & ~1;
		for (int i = 0; i < count; i += 2)
		{
			arg.Add(new NameValueTuple<string, object?>(args[i]?.ToString() ?? NullArg, args[i + 1]));
		}
		if (count < args.Length && args[count] != null)
			arg.Add(new NameValueTuple<string, object?>(args[count]!.ToString()!, null));
		return arg;
	}

	public JsonBuilder ToJsonContent(JsonBuilder json)
	{
		return json
			.Item("eventId", EventId)
			.Item("type", LogType)
			.Item("indent", Indent)
			.Item("source", Source)
			.Item("message", Message)
			.Item("data", Data)
			.Item("exception", Exception)
			.Item("context", Context);
	}

	public class ExceptionInfo: IDumpJson
	{
		private readonly IReadOnlyCollection<NameValueTuple<string, object?>>? _data;

		public ExceptionInfo(Exception exception)
		{
			Message = exception.Message;
			_data = FromDictionary(exception.Data);
			StackTrace = exception.StackTrace;
			if (exception is AggregateException aggregate)
				InnerExceptions = ReadOnly.WrapCopy(aggregate.InnerExceptions
					.Where(o => o != null)
					.Select(o => new ExceptionInfo(o)));
			else if (exception.InnerException != null)
				InnerExceptions = ReadOnly.Wrap([new ExceptionInfo(exception.InnerException)]);
		}

        private IReadOnlyCollection<NameValueTuple<string, object?>>? FromDictionary(IDictionary data)
        {
            if (data is not { Count: >0 })
                return null;
            var args = new List<NameValueTuple<string, object?>>(data.Count);
            var xx = data.GetEnumerator();
            while (xx.MoveNext())
            {
                args.Add(new NameValueTuple<string, object?>(xx.Key?.ToString() ?? NullArg, xx.Value));
            }
            return args;
        }

        public string Message { get; }
		public IReadOnlyCollection<NameValueTuple<string, object?>>? Data => _data;
		public string? StackTrace { get; }
		public IReadOnlyList<ExceptionInfo>? InnerExceptions { get; }

		public JsonBuilder ToJsonContent(JsonBuilder json)
		{
			return json
				.Item("message", Message)
				.Item("data", Data)
				.Item("stackTrace", StackTrace)
				.Item("innerException", InnerExceptions);
		}
	}
}


