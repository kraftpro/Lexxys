// Lexxys Infrastructural library.
// file: LogRecordTextFormatter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.IO;
using System.Text;

namespace Lexxys.Logging;

/// <summary>
/// <see cref="LogRecord"/> formatter.
/// </summary>
public interface ILogRecordFormatter
{
	/// <summary>
	/// Writes the <paramref name="record"/> in to the text stream.
	/// </summary>
	/// <param name="writer">Text stream to write the <paramref name="record"/> to.</param>
	/// <param name="record">The <see cref="LogRecord"/> is to be formatted.</param>
	void Format(TextWriter writer, LogRecord record);
}

public static class LogRecordFormatterExtensions
{
	public static TextWriter Write(this TextWriter writer, LogRecord record, ILogRecordFormatter format)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));
		if (record is null)
			throw new ArgumentNullException(nameof(record));
		if (format == null)
			throw new ArgumentNullException(nameof(format));
		format.Format(writer, record);
		return writer;
	}

	public static StringBuilder Format(this ILogRecordFormatter formatter, StringBuilder? text, LogRecord record)
	{
		if (formatter == null)
			throw new ArgumentNullException(nameof(formatter));
		if (record is null)
			throw new ArgumentNullException(nameof(record));
		text ??= new StringBuilder();
		using (var s = new StringWriter(text))
			formatter.Format(s, record);
		return text;
	}

	public static string Format(this ILogRecordFormatter formatter, LogRecord record)
	{
		if (formatter == null)
			throw new ArgumentNullException(nameof(formatter));
		if (record is null)
			throw new ArgumentNullException(nameof(record));
		var text = new StringBuilder();
		using (var s = new StringWriter(text))
			formatter.Format(s, record);
		return text.ToString();
	}
}