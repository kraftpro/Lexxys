namespace Lexxys.Logging;

public interface ILoggingService
{
	ILogRecordWriter GetLogWriter(string source);
}
