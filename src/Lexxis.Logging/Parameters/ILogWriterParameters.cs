using System;
using System.Collections.Generic;

namespace Lexxys.Logging;

public interface ILogWriterParameters
{
	string? Name { get; set; }
	string? Include { get; set; }
	string? Exclude { get; set; }
	LogType? LogLevel { get; set; }
	int? MaxQueueSize { get; set; }
	TimeSpan? FlushTimeout { get; set; }
	ILogRecordFormatterParameters? Formatter { get; set; }
	ICollection<LogWriterFilter>? Rules { get; set; }
	ILogWriter CreateWriter();
}
