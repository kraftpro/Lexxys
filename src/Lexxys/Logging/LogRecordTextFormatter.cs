// Lexxys Infrastructural library.
// file: LogRecordTextFormatter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace Lexxys.Logging
{
	/// <summary>
	/// Convert <see cref="LogRecord"/> to string using formatting template.
	/// </summary>
	public class LogRecordTextFormatter
	{
		public const int MaxIndents = 20;
		private const int MAX_STACK_ALLOC = 16 * 1024;
		private static readonly TextFormatSetting Defaults = new TextFormatSetting("  ", ". ",
			"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff} {IndentMark}{Source}: {Message}");

		private readonly LogRecordFormatItem[] _mappedFormat;

		public TextFormatSetting Setting { get; }

		public LogRecordTextFormatter(TextFormatSetting setting)
		{
			Setting = new TextFormatSetting(Defaults);
			if (setting != null)
				Setting.Join(setting);

			Setting.Indent = KeepLf(Setting.Indent);
			Setting.Para = KeepLf(Setting.Para);
			Setting.Format = KeepLf(Setting.Format);

			if (Setting.Format.IndexOf("{IndentMark", StringComparison.OrdinalIgnoreCase) < 0)
			{
				int i = Setting.Format.IndexOf("{Source", StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					i = Setting.Format.IndexOf("{Message", StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					i = 0;
				Setting.Format = Setting.Format.Substring(0, i) + "{IndentMark}" + Setting.Format.Substring(i);
			}

			List<LogRecordFormatItem> xx = LogRecord.MapFormat(Setting.Format);
			_mappedFormat = new LogRecordFormatItem[xx.Count];
			for (int i = 0; i < xx.Count; ++i)
			{
				string s = KeepLf(xx[i].Left);
				if (s == xx[i].Left)
					_mappedFormat[i] = xx[i];
				else
					_mappedFormat[i] = new LogRecordFormatItem(xx[i].Index, s, xx[i].Format);
			}
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

			string newLine = GetIndentString(record.Indent);

			var buffer = new StringBuilder(512);
			record.Format(buffer, _mappedFormat, newLine);

			var text = buffer.ToString().AsSpan();
			int j = text.Length - 1;
			while (j >= 0 && text[j] == '\n')
			{
				--j;
			}
			if (j + 1 < text.Length)
				text = text.Slice(0, j + 1);

			newLine = Environment.NewLine + Setting.Para + Setting.Para + newLine;
			WriteText(writer, text, newLine);

			if (record.Data != null)
			{
				string newLine2 = newLine + Setting.Indent;
				foreach (DictionaryEntry arg in record.Data)
				{
					writer.Write(newLine);
					WriteText(writer,
						arg.Key == null ? "<null>".AsSpan():
						arg.Key == DBNull.Value ? "<dbnull>".AsSpan():
						KeepLf(arg.Key.ToString().Trim()).AsSpan(), newLine2);
					writer.Write(" = ");
					if (arg.Value is string strvalue)
						WriteText(writer, KeepLf(strvalue.Trim()).AsSpan(), newLine2);
					else
						Dump(writer, arg.Value, newLine2);
				}
			}

			if (record.StackTrace != null)
				SetTab(writer, record.StackTrace.AsSpan(), newLine);
			return writer;
		}

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

		private static void WriteText(TextWriter text, ReadOnlySpan<char> value, string lineFeed)
		{
			int k;
			while ((k = value.IndexOf('\n')) >= 0)
			{
				text.Write(value.Slice(0, k));
				text.Write(lineFeed);
				value = value.Slice(k + 1);
			}
			text.Write(value);
		}

		private static void Dump(TextWriter text, object value, string newLine)
		{
			switch (value)
			{
				case null:
					text.Write("<null>");
					return;
				case string str:
					Strings.EscapeCsString(text, str.AsSpan());
					return;
				case IDump idump:
					idump.Dump(DumpWriter.Create(text));
					return;
				case IDumpJson jdump:
					jdump.ToJson(JsonBuilder.Create(text));
					return;
				case IEnumerable<byte> bytes:
					text.Write("0x");
					foreach (var b in bytes)
					{
						text.Write(HexChar[b >> 4]);
						text.Write(HexChar[b & 15]);
					}
					return;
				case IEnumerable collection:
					text.Write('{');
					bool first = true;
					foreach (object item in collection)
					{
						if (first)
							first = false;
						else
							text.Write(", ");
						Dump(text, item, newLine);
					}
					text.Write('}');
					return;
			}

			if (value == DBNull.Value)
			{
				text.Write("<dbnull>");
				return;
			}

			Type t = value.GetType();
			var s = KeepLf(value.ToString().Trim());
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
			{
				int i = s.IndexOf(", ", StringComparison.Ordinal);
				if (i >= 0)
					s = s.Substring(0, i) + " = " + s.Substring(i + 2);
			}
			WriteText(text, s.AsSpan(), newLine);
		}
		private static readonly char[] HexChar = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

		private static unsafe string RemoveLetters(ReadOnlySpan<char> value)
		{
			//private static Regex __nonSpaceChar = new Regex(@"[^\s]");
			//return __nonSpaceChar.Replace(value, " ");
			if (value.Length > MAX_STACK_ALLOC)
			{
				fixed (char* buffer = new char[value.Length])
				{
					return RemoveLetters_(buffer, value);
				}
			}
			else
			{
				char* buffer = stackalloc char[value.Length];
				return RemoveLetters_(buffer, value);
			}
		}

		private static unsafe string RemoveLetters_(char* buffer, ReadOnlySpan<char> value)
		{
			//private static Regex __nonSpaceChar = new Regex(@"[^\s]");
			//return __nonSpaceChar.Replace(value, " ");
			char* p = buffer;
			foreach (char c in value)
			{
				*p++ = Char.IsWhiteSpace(c) ? c : ' ';
			}
			return new string(buffer, 0, value.Length);
		}

		private static void SetTab(TextWriter text, ReadOnlySpan<char> value, string tab)
		{
			//private static Regex __setTab = new Regex(@"(\r\n?|\n\r?)\s*");
			//string s = __setTab.Replace(value, tab);
			//return s.EndsWith(tab, StringComparison.Ordinal) ? s.Substring(0, s.Length - tab.Length): s;

			int j = 0;
			while (j < value.Length && Char.IsWhiteSpace(value[j]))
			{
				++j;
			}
			if (j == value.Length)
				return;
			if (j > 0)
				value = value.Slice(j);

			text.Write(tab);

			int i;
			while ((i = value.IndexOfAny(CrLf)) >= 0)
			{
				text.Write(value.Slice(0, i));
				value = value.Slice(value.Length > i + 1 && value[i + 1] == (value[i] ^ ('\r' ^ '\n')) ? i + 2 : i + 1);
				if (value.Length == 0)
					return;
				j = 0;
				while (j < value.Length && Char.IsWhiteSpace(value[j]) && value[j] != '\n' && value[j] != '\r')
					++j;
				if (j > 0)
					value = value.Slice(j);
				if (value.Length == 0)
					return;
				if (value[0] != '\n' && value[0] != '\r')
					text.Write(tab);
			}
			text.Write(value);
		}
		private static readonly char[] CrLf = { '\r', '\n' };

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
			static bool IsWhiteSpace(char c) => c < '\x1680' ? c <= ' ' || c == '\x00a0' || c == '\x0085':
				(c >= '\x2000' && c <= '\x200a') ||
				c == '\x1680' ||
				c == '\x2028' ||
				c == '\x2029' ||
				c == '\x202f' ||
				c == '\x205f' ||
				c == '\x3000' ||
				c == '\xFEFF';
		}

		internal static unsafe string KeepLf(string value)
		{
			if (value == null)
				return null;
			int i = value.IndexOfAny(CrCr);
			if (i < 0)
				return value;
			if (i > 0 && value[i - 1] == '\n')
				--i;
			if (value.Length > MAX_STACK_ALLOC)
			{
				fixed (char* buffer = new char[value.Length])
				{
					return KeepLf_(buffer, i, value);
				}
			}
			else
			{
				char* buffer = stackalloc char[value.Length];
				return KeepLf_(buffer, i, value);
			}
		}
		private static readonly char[] CrCr = { '\r', '\f', '\u0085', '\u2028', '\u2029' };

		private static unsafe string KeepLf_(char* buffer, int i, string value)
		{
			//private static Regex __keepLf = new Regex(@"\r\n?|\n\r?");
			//return __keepLf.Replace(value, "\n");
			char* p = buffer;
			int length = value.Length - 1;
			fixed (char* pvalue = value)
			{
				int len = value.Length;
				Buffer.MemoryCopy(pvalue, p, len * sizeof(char), i * sizeof(char));

				char* q = pvalue + i;
				char* e = pvalue + len;

				while (q != e)
				{
					char c = *q++;
					switch (c)
					{
						case '\r':
							if (q != e && *q == '\n')
								++q;
							c = '\n';
							break;
						case '\n':
							if (q != e && *q == '\r')
								++q;
							break;
						case '\f':
						case '\u0085':
						case '\u2028':
						case '\u2029':
							c = '\n';
							break;
					}
					*p++ = c;
				}
			}
			return new string(buffer, 0, (int)(p - buffer));
		}
	}
}


