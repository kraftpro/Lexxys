// Lexxys Infrastructural library.
// file: LogRecord.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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

	public enum FormatItemType
	{
		IndentMark = 0,
		MachineName = 1,
		DomainName = 2,
		ProcessId = 3,
		ProcessName = 4,
		ThreadId = 5,
		ThreadSysId = 6,
		SequencialNumber = 7,
		Timestamp = 8,

		Source = 9,
		Message = 10,
		Type = 11,
		Grouping = 12,
		Indent = 13,

		Empty = 14,
	}

	public class LogRecordFormatItem
	{
		public readonly FormatItemType Index;
		public readonly string Left;
		public readonly string Format;

		public LogRecordFormatItem()
		{
		}

		public LogRecordFormatItem(FormatItemType index, string left, string format)
		{
			Index = index;
			Left = LogRecordTextFormatter.KeepLf(left);
			Format = LogRecordTextFormatter.KeepLf(format);
		}
	}

	public class LogRecord
	{
		private IDictionary _data;

		[ThreadStatic]
		private static int _currentIndent;

		public LogRecord(string source, string message, LogType logType, IDictionary argument)
			: this(LogGroupingType.Message, source, message, logType, argument)
		{
		}

		public LogRecord(string source, LogType logType, Exception exception)
			: this(source, logType, exception, _currentIndent)
		{
		}

		public LogRecord(LogGroupingType recordType, string source, string message, LogType logType, IDictionary argument)
		{
			Source = source;
			Message = message;
			_data = CopyDictionary(argument);
			LogType = logType;
			RecordType = recordType;
			Indent = _currentIndent;
			if (RecordType == LogGroupingType.BeginGroup)
				++_currentIndent;
			else if (RecordType == LogGroupingType.EndGroup && Indent > 0)
				--_currentIndent;
			Context = new SystemContext();
		}

		public LogRecord(string source, LogType logType, Exception exception, int indent)
		{
			if (exception == null)
				throw EX.ArgumentNull(nameof(exception));
			if (source != null)
			{
				Source = source;
			}
			else
			{
				MethodBase method = exception.TargetSite;
				Source = ((method == null) ? exception.Source: method.GetType().Name);
			}
			Message = exception.Message;
			_data = CopyDictionary(exception.Data);
			StackTrace = exception.StackTrace;
			LogType = logType;
			RecordType = LogGroupingType.Message;
			Indent = indent;
			Context = new SystemContext();
		}

		private static IDictionary CopyDictionary(IDictionary data)
		{
			if (data == null)
				return null;
			var copy = new OrderedDictionary<object, object>(data.Count);
			foreach (var obj in data)
			{
				if (obj is DictionaryEntry item)
					copy.Add(new KeyValuePair<object, object>(item.Key, item.Value));
			}
			return copy;
		}

		/// <summary>Name of class and method generated the log item</summary>
		public string Source { get; }

		/// <summary>Log message</summary>
		public string Message { get; }

		public int Indent { get; }

		/// <summary>Actual parameters values</summary>
		public IDictionary Data => _data;

		public string StackTrace { get; set; }

		/// <summary>Priority of the log item. (5 - critical, 0 - verbose)</summary>
		public int Priority => (int)LogType.MaxValue - (int)LogType;

		public LogType LogType { get; }

		public LogGroupingType RecordType { get; }

		public SystemContext Context { get; }

		public void Add(string argName, object argValue)
		{
			if (_data == null)
				_data = new Dictionary<string, object>(1);
			AddInternal(argName, argValue);
		}
		public void Add(string argName, object argValue, string argName2, object argValue2)
		{
			if (_data == null)
				_data = new Dictionary<string, object>(2);
			AddInternal(argName, argValue);
			AddInternal(argName2, argValue2);
		}
		public void Add(object[] args)
		{
			if (_data == null)
				_data = new Dictionary<string, object>((args.Length + 1) / 2);
			for (int i = 1; i < args.Length; i += 2)
			{
				AddInternal(args[i - 1]?.ToString(), args[i]);
			}
			if (args.Length % 2 > 0)
			{
				AddInternal(null, args[args.Length - 1]);
			}
		}

		private void AddInternal(string key, object value)
		{
			if (key == null || key.Length == 0)
				key = "argument";

			if (_data.Contains(key))
			{
				int k = 2;
				string key2 = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", key, k);
				while (_data.Contains(key))
				{
					++k;
					key2 = String.Format(CultureInfo.InvariantCulture, "{0} ({1})", key, k);
				}
				key = key2;
			}
			_data.Add(key, value);
		}

		public static Dictionary<string, object> Args(params object[] args)
		{
			if (args == null || args.Length == 0)
				return null;

			int nn = args.Length - 1;
			int n = (args.Length + 1) / 2;
			var arg = new Dictionary<string, object>(n);
			for (int i = 0; i < args.Length; ++i)
			{
				string name = (i == nn) ? "last": args[i++].ToString();
				if (arg.ContainsKey(name))
				{
					int k = 1;
					do
					{
						++k;
					} while (arg.ContainsKey(String.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, k)));
					name = String.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, k);
				}
				arg.Add(name, args[i]);
			}
			return arg;
		}

		public static List<LogRecordFormatItem> MapFormat(string format)
		{
			var result = new List<LogRecordFormatItem>();
			MatchCollection mc = __formatRe.Matches(format);
			int last = 0;
			foreach (Match m in mc)
			{
				if (NamesMap.TryGetValue(m.Groups[1].Value.ToUpperInvariant(), out FormatItemType id))
				{
					string f = m.Groups[4].Value;
					if (f.Length == 0)
						f = null;
					else if (id == FormatItemType.Timestamp)
						if (f.Equals("yyyy-MM-ddTHH:mm:ss.fffff", StringComparison.OrdinalIgnoreCase))
							f = null;
						else if (f.Equals("HH:mm:ss.fffff", StringComparison.OrdinalIgnoreCase))
							f = "t";
					result.Add(new LogRecordFormatItem(id, format.Substring(last, m.Index - last), f));
					last = m.Index + m.Length;
				}
			}
			if (last < format.Length)
				result.Add(new LogRecordFormatItem(FormatItemType.Empty, format.Substring(last), null));
			return result;
		}
		private static readonly Regex __formatRe = new Regex(@"\{\s*([a-zA-Z]+)\s*(,\s*[0-9]*)?\s*(:(.*?))?\s*\}", RegexOptions.Compiled);
		
		public string ToString(LogRecordFormatItem[] format)
		{
			var text = new StringBuilder(256);
			Format(text, format, null);
			return text.ToString();
		}

		public int Format(StringBuilder text, LogRecordFormatItem[] format, string indentText)
		{
			int indent = -1;
			foreach (LogRecordFormatItem item in format)
			{
				text.Append(item.Left);
				switch (item.Index)
				{
					case FormatItemType.IndentMark:
						if (indentText != null)
							text.Append(indentText);
						indent = text.Length;
						break;
					case FormatItemType.MachineName:
						text.Append(Context.MachineName);
						break;
					case FormatItemType.DomainName:
						text.Append(Context.DomainName);
						break;
					case FormatItemType.ProcessId:
						text.Append(Context.ProcessId.ToString(item.Format));
						break;
					case FormatItemType.ProcessName:
						text.Append(Context.ProcessName);
						break;
					case FormatItemType.ThreadId:
						text.Append(Context.ThreadId.ToString(item.Format));
						break;
					case FormatItemType.ThreadSysId:
						text.Append(Context.ThreadSysId.ToString(item.Format));
						break;
					case FormatItemType.SequencialNumber:
						text.Append(Context.SequentialNumber.ToString(item.Format));
						break;
					case FormatItemType.Timestamp:
						if (item.Format == null)
							AppendTimeStamp(text, true);
						else if (item.Format == "t")
							AppendTimeStamp(text, false);
						else
							text.Append(Context.Timestamp.ToString(item.Format));
						break;
					case FormatItemType.Source:
						text.Append(LogRecordTextFormatter.NormalizeWs(Source));
						break;
					case FormatItemType.Message:
						text.Append(LogRecordTextFormatter.KeepLf(Message));
						break;
					case FormatItemType.Type:
						if (item.Format == null)
							text.Append(LogType.ToString());
						else if (item.Format == "1")
							text.Append(LogType >= LogType.Output && LogType <= LogType.MaxValue ? __severity1[(int)LogType]: LogType.ToString());
						else if (item.Format == "3")
							text.Append(LogType >= LogType.Output && LogType <= LogType.MaxValue ? __severity3[(int)LogType] : LogType.ToString());
						else
							text.Append(LogType.ToString(item.Format));
						break;
					case FormatItemType.Grouping:
						text.Append(RecordType.ToString(item.Format));
						break;
					case FormatItemType.Indent:
						text.Append(Indent.ToString(item.Format));
						break;
				}
			}
			return indent;
		}
		private static readonly string[] __severity1 = new[] { "O", "E", "W", "I", "T", "D" };
		private static readonly string[] __severity3 = new[] { "OUT", "ERR", "WRN", "INF", "TRC", "DBG" };

		private void AppendTimeStamp(StringBuilder text, bool date)
		{
			DateTime d = Context.Timestamp;
			if (date)
			{
				AppendInt(text, d.Year, 4);
				text.Append('-');
				AppendInt(text, d.Month, 2);
				text.Append('-');
				AppendInt(text, d.Day, 2);
				text.Append('T');
			}

			AppendInt(text, d.Hour, 2);
			text.Append(':');
			AppendInt(text, d.Minute, 2);
			text.Append(':');
			AppendInt(text, d.Second, 2);
			text.Append('.');
			AppendInt(text, (int)(d.Ticks % TicksPerSecond / TicksPerFraction), 5);
		}
		private const long TicksPerFraction = 100L;
		private const long TicksPerSecond = 10000000L;

		private static unsafe void AppendInt(StringBuilder text, int value, int width)
		{
			if (width > 4)
			{
				value %= 100000;
				text.Append((char)(value / 10000 + '0'));
			}
			if (width > 3)
			{
				value %= 10000;
				text.Append((char)(value / 1000 + '0'));
			}
			if (width > 2)
			{
				value %= 1000;
				text.Append((char)(value / 100 + '0'));
			}
			if (width > 1)
			{
				value %= 100;
				text.Append((char)(value / 10 + '0'));
			}
			value %= 10;
			text.Append((char)(value + '0'));
		}

		private static readonly Dictionary<string, FormatItemType> NamesMap = new Dictionary<string, FormatItemType>(14)
			{
				{ "INDENTMARK", FormatItemType.IndentMark },

				{ "MACHINENAME", FormatItemType.MachineName },
				{ "DOMAINNAME", FormatItemType.DomainName },
				{ "PROCESSID", FormatItemType.ProcessId },
				{ "PROCESSNAME", FormatItemType.ProcessName },
				{ "THREADID", FormatItemType.ThreadId },
				{ "THREADSYSID", FormatItemType.ThreadSysId },
				{ "SEQNUMBER", FormatItemType.SequencialNumber },
				{ "TIMESTAMP", FormatItemType.Timestamp },

				{ "SOURCE", FormatItemType.Source },
				{ "MESSAGE", FormatItemType.Message },
				{ "TYPE", FormatItemType.Type },
				{ "GROUPING", FormatItemType.Grouping },
				{ "INDENT", FormatItemType.Indent },
			};

	}
}


