// Lexxys Infrastructural library.
// file: LogRecordTextFormatter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Logging
{
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

	public readonly struct LogRecordFormatItem
	{
		public readonly FormatItemType Index;
		public readonly string Prefix;
		public readonly string Format;

		public LogRecordFormatItem(FormatItemType index, string prefix, string format)
		{
			Index = index;
			Prefix = prefix;
			Format = format;
		}
	}

	/// <summary>
	/// Convert <see cref="LogRecord"/> to string using formatting template.
	/// </summary>
	public class LogRecordTextFormatter
	{
		public const int MaxIndents = 20;
		private const int MAX_STACK_ALLOC = 16 * 1024;
		private static readonly TextFormatSetting Defaults = new TextFormatSetting(
			"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff} {IndentMark}{Source}{EventId:\\.0}: {Message}",
			"  ",
			". ");

		private const string NullValue = "(null)";
		private const string DbNullValue = "(dbnull)";

		private readonly IReadOnlyCollection<LogRecordFormatItem> _mappedFormat;

		public TextFormatSetting Setting { get; }

		public LogRecordTextFormatter(TextFormatSetting setting)
		{
			Setting = new TextFormatSetting(Defaults).Join(setting);
			_mappedFormat = MapFormat(Setting.Format);
		}

		public LogRecordTextFormatter(string format, string indent = null, string para = null)
		{
			Setting = new TextFormatSetting(format, indent ?? "  ", para ?? ". ");
			_mappedFormat = MapFormat(Setting.Format);
		}

		/// <summary>
		/// Format <paramref name="record"/> to string using <see cref="TextFormatSetting.Format"/> template
		/// </summary>
		/// <param name="record">The <paramref name="record"/> to format</param>
		/// <returns>Formatted string</returns>
		/// <remarks>
		/// We cannot use Regular expressions, because of thread finalization.
		/// Available formating fields options are:
		/// <code>
		/// 	IndentMark
		/// 	MachineName
		/// 	DomainName
		/// 	ProcessID
		/// 	ProcessName
		/// 	ThreadID
		/// 	ThreadSysID
		/// 	SeqNumber
		/// 	TimeStamp
		/// 	Source
		/// 	Message
		/// 	Type
		/// 	RecordType
		/// 	MessageID
		/// 	Indent
		/// </code>
		///</remarks>
		public string Format(LogRecord record)
		{
			if (record == null)
				return "";

			var text = new StringBuilder(512);
			using (var w = new StringWriter(text))
				Format(w, record);
			return text.ToString();
		}

		public TextWriter Format(TextWriter writer, LogRecord record)
		{
			if (record == null)
				return writer;

			var indent = GetIndentString(record.Indent);
			var newLine = Environment.NewLine + Setting.Next + Setting.Next + indent;
			Format(writer, record, _mappedFormat, indent, newLine);

			if (record.Data != null)
			{
				var newLine2 = newLine + Setting.Indent;
				foreach (DictionaryEntry arg in record.Data)
				{
					writer.Write(newLine);
					if (arg.Key == null)
					{
						writer.Write(NullValue + " = ");
					}
					else if (arg.Key == DBNull.Value)
					{
						writer.Write(DbNullValue + " = ");
					}
					else
					{
						writer.Write(arg.Key.ToString().AsSpan().Trim());
						writer.Write(" = ");
					}
					if (arg.Value is string strvalue)
						WriteText(writer, strvalue.AsSpan().Trim(), newLine2);
					else
						Dump(writer, arg.Value, newLine2);
				}
			}
			return writer;
		}

		private void Format(TextWriter writer, LogRecord record, IEnumerable<LogRecordFormatItem> format, string indent, string newLine)
		{
			int length = 0;
			foreach (LogRecordFormatItem item in format)
			{
				writer.Write(item.Prefix);
				switch (item.Index)
				{
					case FormatItemType.IndentMark:
						if (indent != null)
							writer.Write(indent);
						break;
					case FormatItemType.MachineName:
						writer.Write(record.Context.MachineName);
						break;
					case FormatItemType.DomainName:
						writer.Write(record.Context.DomainName);
						break;
					case FormatItemType.ProcessId:
						writer.Write(record.Context.ProcessId.ToString(item.Format));
						break;
					case FormatItemType.ProcessName:
						writer.Write(record.Context.ProcessName);
						break;
					case FormatItemType.ThreadId:
						writer.Write(record.Context.ThreadId.ToString(item.Format));
						break;
					case FormatItemType.ThreadSysId:
						writer.Write(record.Context.ThreadSysId.ToString(item.Format));
						break;
					case FormatItemType.SequencialNumber:
						writer.Write(record.Context.SequentialNumber.ToString(item.Format));
						break;
					case FormatItemType.Timestamp:
						if (item.Format == null)
						{
							AppendTimeStamp(writer, record.Context.Timestamp, true);
							length += 23;
						}
						else if (item.Format == "t")
						{
							AppendTimeStamp(writer, record.Context.Timestamp, false);
							length += 10;
						}
						else
						{
							writer.Write(record.Context.Timestamp.ToString(item.Format));
						}
						break;
					case FormatItemType.Source:
						if (record.Source != null)
							writer.Write(LogRecordTextFormatter.NormalizeWs(record.Source));
						break;
					case FormatItemType.Message:
						if (record.Message != null)
							LogRecordTextFormatter.WriteText(writer, record.Message.AsSpan(), newLine);
						break;
					case FormatItemType.Type:
						if (item.Format == null)
							writer.Write(record.LogType.ToString());
						else if (item.Format == "1")
							writer.Write(record.LogType >= LogType.Output && record.LogType <= LogType.MaxValue ? __severity1[(int)record.LogType] : record.LogType.ToString());
						else if (item.Format == "3")
							writer.Write(record.LogType >= LogType.Output && record.LogType <= LogType.MaxValue ? __severity3[(int)record.LogType] : record.LogType.ToString());
						else
							writer.Write(record.LogType.ToString(item.Format));
						break;
					case FormatItemType.Grouping:
						writer.Write(record.RecordType.ToString(item.Format));
						break;
					case FormatItemType.Indent:
						writer.Write(record.Indent.ToString(item.Format));
						break;
				}
			}
		}
		private static readonly string[] __severity1 = new[] { "O", "E", "W", "I", "T", "D" };
		private static readonly string[] __severity3 = new[] { "OUT", "ERR", "WRN", "INF", "TRC", "DBG" };

		private void AppendTimeStamp(TextWriter writer, DateTime date, bool useDate)
		{
			if (useDate)
			{
				Write4(date.Year);
				writer.Write('-');
				Write2(date.Month);
				writer.Write('-');
				Write2(date.Day);
				writer.Write('T');
			}

			Write2(date.Hour);
			writer.Write(':');
			Write2(date.Minute);
			writer.Write(':');
			Write2(date.Second);
			writer.Write('.');
			var value = (int)(date.Ticks % TicksPerSecond / TicksPerFraction);
			writer.Write((char)(value / 10000 + '0'));
			Write4(value % 10000);

			void Write2(int value)
			{
				writer.Write((char)(value / 10 + '0'));
				writer.Write((char)(value % 10 + '0'));
			}
			void Write4(int value)
			{
				writer.Write((char)(value / 1000 + '0'));
				value %= 1000;
				writer.Write((char)(value / 100 + '0'));
				value %= 100;
				writer.Write((char)(value / 10 + '0'));
				writer.Write((char)(value % 10 + '0'));
			}
		}
		private const long TicksPerFraction = 100L;
		private const long TicksPerSecond = 10_000_000L;

		private string GetIndentString(int indent)
		{
			switch (indent)
			{
				case 1:
					return Setting.Indent;
				case 2:
					return Setting.Indent + Setting.Indent;
				case 3:
					return Setting.Indent + Setting.Indent + Setting.Indent;
				default:
					if (indent <= 0)
						return "";
					if (indent > MaxIndents)
						indent = MaxIndents;
					return String.Join(Setting.Indent, EmptyStringArray, 0, indent);
			}
		}
		private static readonly string[] EmptyStringArray = new string[MaxIndents];

		public static void WriteText(TextWriter writer, ReadOnlySpan<char> value, string newLine)
		{
			value = value.Trim();
			var crLf = CrLfFf.AsSpan();
			while (crLf.IndexOf(value[0]) > 0)
			{
				value = value.Slice(1);
				if (value.Length == 0)
					return;
			}
			int k;
			while ((k = value.IndexOfAny(crLf)) >= 0)
			{
				writer.Write(value.Slice(0, k));
				value = value.Slice(k + 1);
				if (value.Length == 0)
					return;
				while (crLf.IndexOf(value[0]) > 0)
				{
					value = value.Slice(1);
					if (value.Length == 0)
						return;
				}
				writer.Write(newLine);
			}
			writer.Write(value);
		}
		private const string CrLfFf = "\r\n\f\u0085\u2028\u2029";

		private static void Dump(TextWriter writer, object value, string newLine)
		{
			if (value == null)
				writer.Write(NullValue);
			else if (value is IDump idump)
				idump.Dump(new DumpTextWriter(writer, newLine));
			else if (value is IDumpJson jdump)
				jdump.ToJson(JsonBuilder.Create(writer));
			else if (value == DBNull.Value)
				writer.Write(DbNullValue);
			else
				new DumpTextWriter(writer, newLine).Dump(value, true);
		}

		internal static unsafe string NormalizeWs(string value)
		{
			if (value == null)
				return null;

			if (value.Length > MAX_STACK_ALLOC)
			{
				fixed (char* buffer = new char[value.Length])
				{
					return NormalizeWs_(buffer, value);
				}
			}
			else
			{
				char* buffer = stackalloc char[value.Length];
				return NormalizeWs_(buffer, value);
			}
		}

		private static unsafe string NormalizeWs_(char* buffer, string value)
		{
			char* p = buffer;
			int length = value.Length;
			fixed (char* pvalue = value)
			{
				char* q = pvalue;
				char* e = q + value.Length;
				while (q != e)
				{
					char c = *q++;
					if (IsWhiteSpace(c))
					{
						do
						{
							if (q == e)
								goto Finish;
							c = *q++;
						} while (IsWhiteSpace(c));
						*p++ = ' ';
					}
					*p++ = c;
				}
				Finish:;
			}
			return new string(buffer, 0, (int)(p - buffer));
			static bool IsWhiteSpace(char c)
			{
				if (c < '\x7F') return c <= ' ';
				if (c <= '\xA0') return true;
				var k = Char.GetUnicodeCategory(c);
				return k == UnicodeCategory.OtherNotAssigned || k == UnicodeCategory.Control || k == UnicodeCategory.SpaceSeparator;
			}
		}

		private static IReadOnlyCollection<LogRecordFormatItem> MapFormat(string format)
		{
			return __formatItems.GetOrAdd(format, Map);

			static IReadOnlyCollection<LogRecordFormatItem> Map(string format)
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
		}
		private static readonly Regex __formatRe = new Regex(@"\{\s*([a-zA-Z]+)\s*(,\s*[0-9]*)?\s*(:(.*?))?\s*\}", RegexOptions.Compiled);
		private static readonly ConcurrentDictionary<string, IReadOnlyCollection<LogRecordFormatItem>> __formatItems = new ConcurrentDictionary<string, IReadOnlyCollection<LogRecordFormatItem>>();

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

		class DumpTextWriter : DumpWriter
		{
			private readonly TextWriter _w;
			private readonly string _nl;
			private bool _lf;

			public DumpTextWriter(TextWriter writer, string newLine, int maxCapacity = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0)
				: base(maxCapacity, maxDepth, stringLimit, blobLimit, arrayLimit)
			{
				_w = writer ?? throw new ArgumentNullException(nameof(writer));
				_nl = newLine ?? throw new ArgumentNullException(nameof(newLine));
			}

			/// <inheritdoc />
			public override DumpWriter Text(string text)
			{
				if (Left == 0)
					return this;
				if (text == null)
					text = NullValue;
				ReadOnlySpan<char> value;
				int length = text.Length;
				value = Left < length ? text.AsSpan(0, Left): text.AsSpan();
				if (value.Length == 0)
					return this;

				var crLf = CrLfFf.AsSpan();
				int k;
				while ((k = value.IndexOfAny(crLf)) >= 0)
				{
					if (k > 0)
					{
						_lf = false;
						if (k >= Left)
						{
							_w.Write(value.Slice(0, Left));
							Left = 0;
							return this;
						}
						_w.Write(value.Slice(0, k));
						Left -= k;
					}
					value = value.Slice(k + 1);
					while (value.Length > 0 && crLf.IndexOf(value[0]) > 0)
					{
						value = value.Slice(1);
					}
					if (!_lf)
					{
						_lf = true;
						_w.Write(_nl);
						--Left;
						if (Left == 0)
							return this;
					}
				}
				if (value.Length > 0)
				{
					_lf = false;
					if (value.Length > Left)
					{
						_w.Write(value.Slice(0, Left));
						Left = 0;
					}
					else
					{
						_w.Write(value);
						Left -= value.Length;
					}
				}
				return this;
			}

			/// <inheritdoc />
			public override DumpWriter Text(char text)
			{
				if (Left <= 0)
					return this;
				if (CrLfFf.IndexOf(text) < 0)
				{
					_lf = false;
					_w.Write(text);
				}
				else if (!_lf)
				{
					_lf = true;
					_w.Write(_nl);
				}
				--Left;
				return this;
			}
		}
	}
}
