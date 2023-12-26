// Lexxys Infrastructural library.
// file: LogRecordTextFormatter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Logging;

/// <summary>
/// Convert <see cref="LogRecord"/> to string using formatting template.
/// </summary>
public class LogRecordTextFormatter: ILogRecordFormatter
{
	public const int MaxIndents = 20;
	private const int MAX_STACK_ALLOC = Lexxys.Tools.MaxStackAllocSize;

	private const string NullValue = "<null>";
	private const string DbNullValue = "<db-null>";

	private readonly List<LogRecordFormatItem> _mappedFormat;

	public TextFormatSetting Setting { get; }

	public LogRecordTextFormatter(TextFormatSetting setting)
	{
		Setting = setting;
		_mappedFormat = MapFormat(Setting.Format);
	}

	public LogRecordTextFormatter(string format, string? indent = null, string? section = null)
	{
		Setting = new TextFormatSetting(format, indent ?? section ?? "  ", section);
		_mappedFormat = MapFormat(Setting.Format);
	}

	/// <summary>
	/// Format <paramref name="record"/> to string using <see cref="TextFormatSetting.Format"/> template
	/// </summary>
	/// <param name="record">The <paramref name="record"/> to format</param>
	/// <returns>Formatted string</returns>
	/// <remarks>
	/// Available formatting fields options are:
	/// <code>
	/// 	IndentMark
	/// 	EventID
	/// 	MachineName
	/// 	DomainName
	/// 	ProcessID
	/// 	ThreadID
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
	public string Format(LogRecord? record)
	{
		if (record == null)
			return "";

		var text = new StringBuilder(512);
		using (var w = new StringWriter(text))
			Format(w, record);
		return text.ToString();
	}

	public void Format(TextWriter writer, LogRecord record)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		if (record is null)
			throw new ArgumentNullException(nameof(record));

		var section = GetIndentString(record.Indent);
		var newLine = Environment.NewLine + Setting.Indent + section;

		Format(writer, record, _mappedFormat, section, newLine);

		if (record.Data != null)
			WriteArgs(writer, record.Data, newLine, Setting.Indent);
		if (record.Exception != null)
			WriteException(writer, "Exception: ", record.Exception, newLine, Setting.Indent);
	}

	private static void WriteArgs(TextWriter writer, IDictionary args, string newLine, string indent)
	{
		var newLine2 = newLine + indent;
		foreach (DictionaryEntry arg in args)
		{
			writer.Write(newLine);
			if (arg.Key == null)
				writer.Write(NullValue);
			else if (arg.Key == DBNull.Value)
				writer.Write(DbNullValue);
			else
				writer.Write(arg.Key.ToString().AsSpan().Trim());

			writer.Write(" = ");
			if (arg.Value is string str)
				WriteText(writer, str.AsSpan().Trim(), newLine2);
			else
				Dump(writer, arg.Value, newLine2);
		}
	}

	private static void WriteException(TextWriter writer, string label, LogRecord.ExceptionInfo exception, string newLine, string indent)
	{
		var newLine2 = newLine + indent;
		writer.Write(newLine + label);
		WriteText(writer, exception.Message.AsSpan().Trim(), newLine2);
		if (exception.Data != null)
			WriteArgs(writer, exception.Data, newLine2, indent);
		if (exception.StackTrace != null)
		{
			writer.Write(newLine);
			WriteText(writer, exception.StackTrace.AsSpan(), newLine);
		}
		if (exception.InnerExceptions != null)
		{
			foreach (var item in exception.InnerExceptions)
			{
				WriteException(writer, "Inner Exception: ", item, newLine2, indent);
			}
		}
	}

	private static void Format(TextWriter writer, LogRecord record, List<LogRecordFormatItem> format, string indent, string newLine)
	{
		foreach (LogRecordFormatItem item in format)
		{
			if (item.Prefix.Length > 0)
				WriteTextStraight(writer, item.Prefix.AsSpan(), newLine);
			switch (item.Index)
			{
				case FormatItemType.IndentMark:
					if (indent is { Length: > 0 })
						writer.Write(indent);
					break;
				case FormatItemType.EventId:
					if (record.EventId != 0)
					{
						writer.Write('<');
						writer.Write(record.EventId);
						writer.Write('>');
					}
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
				case FormatItemType.ThreadId:
					writer.Write(record.Context.ThreadId.ToString(item.Format));
					break;
				case FormatItemType.SequentialNumber:
					writer.Write(record.Context.SequentialNumber.ToString(item.Format));
					break;
				case FormatItemType.Timestamp:
					if (item.Format == null)
						AppendTimeStamp(writer, record.Context.Timestamp, true);
					else if (item.Format == "t")
						AppendTimeStamp(writer, record.Context.Timestamp, false);
					else
						writer.Write(record.Context.Timestamp.ToString(item.Format));
					break;
				case FormatItemType.Source:
					if (record.Source != null)
						writer.Write(NormalizeWs(record.Source));
					break;
				case FormatItemType.Message:
					if (record.Message != null)
						WriteText(writer, record.Message.AsSpan().Trim(), newLine);
					break;
				case FormatItemType.Type:
					string s;
					if (record.LogType is < LogType.Output or > LogType.MaxValue)
						s = record.LogType.ToString();
					else
						s = item.Format?.ToUpperInvariant() switch
						{
							"1" => __severity1[(int)record.LogType],
							"3" => __severity3[(int)record.LogType],
							"4" => __severity4[(int)record.LogType],
							"5" => __severity5[(int)record.LogType],
							"X" => item.Format[0] == 'x' ?
										__severity1[(int)record.LogType].ToLowerInvariant() :
										__severity1[(int)record.LogType],
							"XXX" => item.Format[0] == 'x' ?
										__severity3[(int)record.LogType].ToLowerInvariant() :
									item.Format[1] == 'X' ?
										__severity3[(int)record.LogType].ToUpperInvariant() :
										__severity3[(int)record.LogType],
							"XXXX" => item.Format[0] == 'x' ?
										__severity4[(int)record.LogType].ToLowerInvariant() :
									item.Format[1] == 'X' ?
										__severity5[(int)record.LogType].ToUpperInvariant() :
										__severity4[(int)record.LogType],
							"XXXXX" => item.Format[0] == 'x' ?
										__severity5[(int)record.LogType].ToLowerInvariant() :
									item.Format[1] == 'X' ?
										__severity5[(int)record.LogType].ToUpperInvariant() :
										__severity5[(int)record.LogType],
							"D" => record.LogType.ToString(item.Format),
							_ => __severity5[(int)record.LogType],
						};
					writer.Write(s);
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
	private static readonly string[] __severity1 = ["O", "E", "W", "I", "D", "T"];
	private static readonly string[] __severity3 = ["Out", "Err", "Wrn", "Inf", "Dbg", "Trc"];
	private static readonly string[] __severity4 = ["Outp", "Fail", "Warn", "Info", "Dbug", "Trce"];
	private static readonly string[] __severity5 = ["Output", "Error", "Warning", "Information", "Debug", "Trace"];

	private static void AppendTimeStamp(TextWriter writer, DateTime date, bool useDate)
	{
		Span<char> buffer = stackalloc char[26];
		Span<char> b = buffer;
		var n = 0;
		if (useDate)
		{
			Write4(b, date.Year);
			b[4] = '-';
			b = b.Slice(5);
			Write2(b, date.Month);
			b[2] = '-';
			b = b.Slice(3);
			Write2(b, date.Day);
			b[2] = 'T';
			b = b.Slice(3);
			// 11
		}

		Write2(b, date.Hour);
		b[2] = ':';
		b = b.Slice(3);
		Write2(b, date.Minute);
		b[2] = ':';
		b = b.Slice(3);
		Write2(b, date.Second);
		b[2] = '.';
		b = b.Slice(3);
		Write5(b, (int)(date.Ticks % TicksPerSecond / TicksPerFraction));
		b = b.Slice(5);
		// 14
		writer.Write(buffer.Slice(0, useDate ? 11 + 14: 14));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Write2(Span<char> buffer, int quot)
		{
			int rem;
			quot = Math.DivRem(quot, 10, out rem);
			buffer[0] = (char)(quot + '0');
			buffer[1] = (char)(rem + '0');
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Write4(Span<char> buffer, int quot)
		{
			int rem;
			quot = Math.DivRem(quot, 10, out rem);
			buffer[3] = (char)(rem + '0');
			quot = Math.DivRem(quot, 10, out rem);
			buffer[2] = (char)(rem + '0');
			quot = Math.DivRem(quot, 10, out rem);
			buffer[1] = (char)(rem + '0');
			buffer[0] = (char)(quot + '0');
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Write5(Span<char> buffer, int quot)
		{
			int rem;
			quot = Math.DivRem(quot, 10, out rem);
			buffer[4] = (char)(rem + '0');
			quot = Math.DivRem(quot, 10, out rem);
			buffer[3] = (char)(rem + '0');
			quot = Math.DivRem(quot, 10, out rem);
			buffer[2] = (char)(rem + '0');
			quot = Math.DivRem(quot, 10, out rem);
			buffer[1] = (char)(rem + '0');
			buffer[0] = (char)(quot + '0');
		}
	}
	private const long TicksPerFraction = 100L;
	private const long TicksPerSecond = 10_000_000L;

	private string GetIndentString(int indent) => indent switch
	{
		0 => String.Empty,
		1 => Setting.Section,
		2 => Setting.Section + Setting.Section,
		3 => Setting.Section + Setting.Section + Setting.Section,
		_ => String.Join(Setting.Section, EmptyStringArray, 0, Math.Min(indent, MaxIndents)),
	};
	private static readonly string[] EmptyStringArray = new string[MaxIndents];

	public static void WriteText(TextWriter writer, ReadOnlySpan<char> value, string newLine)
	{
		var crLf = CrLfFf.AsSpan();
		int i = 0;
		for (; i < value.Length && crLf.IndexOf(value[i]) >= 0; ++i)
		{
		}
		WriteTextStraight(writer, value.Slice(i), newLine);
	}

	private static void WriteTextStraight(TextWriter writer, ReadOnlySpan<char> value, string newLine)
	{
		var crLf = CrLfFf.AsSpan();
		int k;
		while ((k = value.IndexOfAny(crLf)) >= 0)
		{
			if (k > 0)
				writer.Write(value.Slice(0, k));
			writer.Write(newLine);
			while (++k < value.Length && crLf.IndexOf(value[k]) >= 0)
			{
			}
			value = value.Slice(k);
		}
		if (value.Length > 0)
			writer.Write(value);
	}
	private const string CrLfFf = "\r\n\f\u0085\u2028\u2029";

	private static void Dump(TextWriter writer, object? value, string newLine)
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

	private static unsafe string NormalizeWs(string value)
	{
		if (value.Length > MAX_STACK_ALLOC)
		{
			var array = ArrayPool<char>.Shared.Rent(value.Length);
			try
			{
				fixed (char* buffer = array)
				{
					return NormalizeWs_(buffer, value);
				}
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array);
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
		fixed (char* pvalue = value)
		{
			char* q = pvalue;
			char* e = q + value.Length;
			bool ctrl = false;
			while (q != e)
			{
				char c = *q++;
				if (IsWhiteSpace(c))
				{
					ctrl |= c != ' ';
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
			int len = (int)(p - buffer);
			return len == value.Length && !ctrl ? value: new string(buffer, 0, (int)(p - buffer));
		}

		static bool IsWhiteSpace(char c)
		{
			if (c <= '\xA0') return c is <'!' or >'~';
			var k = Char.GetUnicodeCategory(c);
			return k == UnicodeCategory.OtherNotAssigned || k == UnicodeCategory.Control || k == UnicodeCategory.SpaceSeparator;
		}
	}

	private static List<LogRecordFormatItem> MapFormat(string format)
	{
		return __formatItems.GetOrAdd(format, Map);

		static List<LogRecordFormatItem> Map(string format)
		{
			var result = new List<LogRecordFormatItem>();
			var template = format.AsSpan();

			while (template.Length > 0)
			{
				int k = TextPart(template, '{', out string text);
				template = template.Slice(k);
				if (template.Length == 0)
				{
					result.Add(new LogRecordFormatItem(FormatItemType.Empty, text, null));
					break;
				}
				k = TextPart(template, '}', out string val);
				template = template.Slice(k);
				k = val.IndexOf(':');
				string fmt, key;
				if (k > 0)
				{
					fmt = val.Substring(k + 1);
					key = val.Substring(0, k);
				}
				else
				{
					fmt = String.Empty;
					key = val;
				}
				if (!NamesMap.TryGetValue(key, out var id))
				{
					result.Add(new LogRecordFormatItem(FormatItemType.Empty, $"{text}{{{key}{(fmt.Length > 0 ? ":" + fmt: String.Empty)}}}", null));
					continue;
				}

				if (id == FormatItemType.Timestamp)
					if (fmt.Equals("yyyy-MM-ddTHH:mm:ss.fffff", StringComparison.OrdinalIgnoreCase))
						fmt = String.Empty;
					else if (fmt.Equals("HH:mm:ss.fffff", StringComparison.OrdinalIgnoreCase))
						fmt = "t";
				result.Add(new LogRecordFormatItem(id, text, fmt));
			}
			return result;

			static int TextPart(ReadOnlySpan<char> template, char brace, out string text)
			{
				int k = template.IndexOf(brace);
				if (k < 0)
				{
					text = template.ToString();
					return template.Length;
				}

				string ta = template.Slice(0, k).ToString();
				int pad = 1;
				while (k + 1 < template.Length && template[k + 1] == brace)
				{
					pad += k + 2;
					ta += brace.ToString();
					template = template.Slice(k + 2);
					k = template.IndexOf(brace);
					if (k < 0)
					{
						text = String.Concat(ta, template);
						return pad + template.Length - 1;
					}
					ta = String.Concat(ta, template.Slice(0, k));
				}
				text = ta;
				return pad + k;
			}
		}
	}
	private static readonly Regex __formatRe = new Regex(@"\{\s*([a-zA-Z]+)\s*(,\s*[0-9]*)?\s*(:(.*?))?\s*\}", RegexOptions.Compiled);
	private static readonly ConcurrentDictionary<string, List<LogRecordFormatItem>> __formatItems = new ConcurrentDictionary<string, List<LogRecordFormatItem>>();

	private static readonly Dictionary<string, FormatItemType> NamesMap = new Dictionary<string, FormatItemType>(14, StringComparer.OrdinalIgnoreCase)
		{
			{ "INDENTMARK", FormatItemType.IndentMark },

			{ "EVENTID", FormatItemType.EventId },
			{ "MACHINENAME", FormatItemType.MachineName },
			{ "DOMAINNAME", FormatItemType.DomainName },
			{ "PROCESSID", FormatItemType.ProcessId },
			{ "THREADID", FormatItemType.ThreadId },
			{ "SEQNUMBER", FormatItemType.SequentialNumber },
			{ "TIMESTAMP", FormatItemType.Timestamp },

			{ "SOURCE", FormatItemType.Source },
			{ "MESSAGE", FormatItemType.Message },
			{ "TYPE", FormatItemType.Type },
			{ "GROUPING", FormatItemType.Grouping },
			{ "INDENT", FormatItemType.Indent },
		};

	class DumpTextWriter: DumpWriter
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
		public override DumpWriter Text(string? text)
		{
			if (Left == 0)
				return this;
			text ??= NullValue;
			int length = text.Length;
			var value = Left < length ? text.AsSpan(0, Left) : text.AsSpan();
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

	private enum FormatItemType
	{
		IndentMark = 0,
		EventId = 1,
		MachineName = 2,
		DomainName = 3,
		ProcessId = 4,
		ThreadId = 5,
		SequentialNumber = 6,
		Timestamp = 7,

		Source = 8,
		Message = 9,
		Type = 10,
		Grouping = 11,
		Indent = 12,

		Empty = 13,
	}

	// ReSharper disable once MemberHidesStaticFromOuterClass
	private readonly record struct LogRecordFormatItem(FormatItemType Index, string Prefix, string? Format);
}
