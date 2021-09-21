// Lexxys Infrastructural library.
// file: LogRecord.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable

namespace Lexxys.Logging
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

	public enum LogGroupingType
	{
		Message=0,
		BeginGroup=1,
		EndGroup=2
	}

	public class LogRecord: IDumpJson
	{
		private const string NullArg = "null";
		private static readonly AsyncLocal<int> _currentIndent = new();
		private OrderedBag<string, object?>? _data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LogRecord(LogType logType, string? source, string? message, IDictionary? args)
			: this(logType, source, message, null, args)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public LogRecord(LogType logType, string? source, Exception exception)
			: this(logType, source, null, exception, null)
		{
		}

		public LogRecord(LogType logType, string? source, string? message, Exception? exception, IDictionary? args)
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
			_data = CopyDictionary(args);
			Exception = exception == null ? null: new ExceptionInfo(exception);
			LogType = logType;
			RecordType = LogGroupingType.Message;
			Indent = Math.Max(0, _currentIndent.Value);
			Context = new SystemContext();
		}

		public LogRecord(LogGroupingType recordType, LogType logType, string? source, string? message, IDictionary? args)
		{
			Source = source;
			Message = message;
			_data = CopyDictionary(args);
			LogType = logType;
			RecordType = recordType;
			Indent = Math.Max(0, _currentIndent.Value);
			if (RecordType == LogGroupingType.BeginGroup)
				++_currentIndent.Value;
			else if (RecordType == LogGroupingType.EndGroup && Indent > 0)
				--_currentIndent.Value;
			Context = new SystemContext();
		}

		private static OrderedBag<string, object?>? CopyDictionary(IDictionary? data)
		{
			if (data == null)
				return null;
			var bag = new OrderedBag<string, object?>(data.Count);
			var xx = data.GetEnumerator();
			while (xx.MoveNext())
			{
				bag.Add(new KeyValuePair<string, object?>(xx.Key?.ToString() ?? NullArg, xx.Value));
			}
			return bag;
		}

		/// <summary>Name of class and method generated the log item</summary>
		public string? Source { get; }

		/// <summary>Log message</summary>
		public string? Message { get; }

		public int Indent { get; }

		/// <summary>Actual parameters values</summary>
		public IDictionary? Data => _data;

		/// <summary>Exception</summary>
		public ExceptionInfo? Exception { get; }

		/// <summary>Priority of the log item. (5 - critical, 0 - verbose)</summary>
		public int Priority => (int)LogType.MaxValue - (int)LogType;

		public LogType LogType { get; }

		public LogGroupingType RecordType { get; }

		public SystemContext Context { get; }

		public void Add(string argName, object? argValue)
		{
			(_data ??= new OrderedBag<string, object?>(1)).Add(argName ?? NullArg, argValue);
		}

		public void Add(string argName, object? argValue, string argName2, object? argValue2)
		{
			if (_data == null)
				_data = new OrderedBag<string, object?>(2);
			_data.Add(argName ?? NullArg, argValue);
			_data.Add(argName2 ?? NullArg, argValue2);
		}

		public void Add(object?[] args)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			if (_data == null)
				_data = new OrderedBag<string, object?>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				_data.Add(args[i - 1]?.ToString() ?? NullArg, args[i]);
			}
			if (args.Length % 2 > 0)
			{
				_data.Add(NullArg, args[args.Length - 1]);
			}
		}

		public static OrderedBag<string, object?>? Args(params object?[] args)
		{
			if (args == null || args.Length == 0)
				return null;

			var arg = new OrderedBag<string, object?>((args.Length + 1) / 2);
			int count = args.Length & ~1;
			for (int i = 0; i < count; i += 2)
			{
				arg.Add(args[i]?.ToString() ?? NullArg, args[i + 1]);
			}
			if (count != args.Length)
				arg.Add(NullArg, args[args.Length - 1]);
			return arg;
		}

		public JsonBuilder ToJsonContent(JsonBuilder json)
		{
			return json
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
			private readonly OrderedBag<string, object?>? _data;

			public ExceptionInfo(Exception exception)
			{
				Message = exception.Message;
				_data = CopyDictionary(exception.Data);
				StackTrace = exception.StackTrace;
				if (exception.InnerException != null)
					InnerException = new ExceptionInfo(exception.InnerException);
			}

			public string Message { get; }
			public IDictionary? Data => _data;
			public string? StackTrace { get; }
			public ExceptionInfo? InnerException { get; }

			public JsonBuilder ToJsonContent(JsonBuilder json)
			{
				return json
					.Item("meessage", Message)
					.Item("data", Data)
					.Item("stackTrace", StackTrace)
					.Item("innetException", InnerException);
			}
		}
	}
}


