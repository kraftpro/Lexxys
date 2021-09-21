// Lexxys Infrastructural library.
// file: LogRecordTextFormatter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.IO;
using System.Text;

#nullable enable

namespace Lexxys.Logging
{
	public interface ILogRecordFormatter
	{
		TextWriter Format(TextWriter writer, LogRecord? record);
	}

	public static class LogRecordFormatter
	{
		public static readonly ILogRecordFormatter NullFormatter = new NullLogRecordFormatter();

		class NullLogRecordFormatter : ILogRecordFormatter
		{
			public TextWriter Format(TextWriter writer, LogRecord? record) => writer;
		}
	}

	public static class LogRecordFormatterExtensions
	{
		public static TextWriter Write(this TextWriter writer, LogRecord? record, ILogRecordFormatter format)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (record == null)
				return writer;
			if (format == null)
				throw new ArgumentNullException(nameof(format));
			return format.Format(writer, record);
		}

		public static StringBuilder Format(this ILogRecordFormatter formatter, StringBuilder? text, LogRecord? record)
		{
			if (text == null)
				text = new StringBuilder();
			if (record == null)
				return text;
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));
			using (var s = new StringWriter(text))
				formatter.Format(s, record);
			return text;
		}

		public static string Format(this ILogRecordFormatter formatter, LogRecord? record)
		{
			if (record == null)
				return String.Empty;
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));
			var text = new StringBuilder();
			using (var s = new StringWriter(text))
				formatter.Format(s, record);
			return text.ToString();
		}
	}
}