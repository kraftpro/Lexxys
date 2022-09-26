using System;
using System.Collections.Generic;

namespace Lexxys.Logging;

public abstract class LogWriterParameters: ILogWriterParameters
{
	public string? Name { get; set; }
	public string? Include { get; set; }
	public string? Exclude { get; set; }
	public TimeSpan? FlushTimeout { get; set; }
	public int? MaxQueueSize { get; set; }
	public ILogRecordFormatterParameters? Formatter { get; set; }
	public ICollection<LogWriterFilter>? Rules { get; set; }
	public LogType? LogLevel { get; set; }

	public abstract ILogWriter CreateWriter();
}


public class LoggingConsoleParameters: LogWriterParameters
{
	public override ILogWriter CreateWriter() => new ConsoleLogWriter(this);
}


public class LoggingDatabaseParameters: LogWriterParameters
{
	public string? Connection { get; set; }
	public string? Schema { get; set; }
	public string? Table { get; set; }

	public override ILogWriter CreateWriter() => new DatabaseLogWriter(this);
}


public class LoggingDebugParameters: LogWriterParameters
{
	public override ILogWriter CreateWriter() => new DebuggerLogWriter(this);
}


public class LoggingFileParameters: LogWriterParameters
{
	public string? Path { get; set; }
	public TimeSpan? Timeout { get; set; }
	public bool Overwrite { get; set; }

	public LoggingFileParameters SetFile(string? path = default, bool? overwrite = default, TimeSpan? timeout = default)
	{
		if (path != null)
			Path = path;
		if (overwrite != null)
			Overwrite = overwrite.GetValueOrDefault();
		if (timeout != null)
			Timeout = timeout;
		return this;
	}

	public override ILogWriter CreateWriter() => new FileLogWriter(this);
}


public class LoggingTraceParameters: LogWriterParameters
{
	public override ILogWriter CreateWriter() => new TraceLogWriter(this);
}


public class LoggingEventParameters: LogWriterParameters
{
	public string? EventSource { get; set; }
	public string? LogName { get; set; }

	public override ILogWriter CreateWriter() => new EventLogLogWriter(this);
}
