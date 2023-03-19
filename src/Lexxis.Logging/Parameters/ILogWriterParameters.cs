using System;
using System.Collections.Generic;

namespace Lexxys.Logging;

public interface ILogWriterParameters
{
	/// <summary>
	/// Log writer name.
	/// </summary>
	string? Name { get; set; }
	/// <summary>
	/// Objects to be included in the log.
	/// <code>
	/// rule := item [ item ]*
	/// item := text
	///      | '(' rule ')' -- group
	///      | '[' rule ']' -- optional group
	///      | '*'          -- any chars
	///      | ';' | ','    -- OR separator
	/// </code>
	/// </summary>
	string? Include { get; set; }
	/// <summary>
	/// Objects to be excluded from the log.
	/// <code>
	/// rule := item [ item ]*
	/// item := text
	///      | '(' rule ')' -- group
	///      | '[' rule ']' -- optional group
	///      | '*'          -- any chars
	///      | ';' | ','    -- OR separator
	/// </code>
	/// </summary>
	string? Exclude { get; set; }
	/// <summary>
	/// Level of the logging
	/// </summary>
	LogType? LogLevel { get; set; }
	/// <summary>
	/// Max number of log records may be kept in the memory.
	/// </summary>
	int? MaxQueueSize { get; set; }
	/// <summary>
	/// Logger flush timeout when closing.
	/// </summary>
	TimeSpan? FlushTimeout { get; set; }
	/// <summary>
	/// <see cref="ILogRecordFormatter"/> factory.
	/// </summary>
	ILogRecordFormatterParameters? Formatter { get; set; }
	/// <summary>
	/// Collection of additional filters in the form: [ LogLevel, Source to include, Source to exclude ]*
	/// </summary>
	ICollection<LogWriterFilter>? Rules { get; set; }
	/// <summary>
	/// Creates a <see cref="ILogWriter"/> with the specified <see cref="ILogWriterParameters"/>.
	/// </summary>
	/// <returns></returns>
	ILogWriter CreateWriter();
}
