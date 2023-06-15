namespace Lexxys.Logging;

public class LogRecordTextParameters: ILogRecordFormatterParameters
{
	private const string DefaultFormat = @"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}{EventId:\.0}: {Message}";

	public string? Format { get; set; }
	public string? Indent { get; set; }
	public string? Section { get; set; }

	public LogRecordTextParameters()
	{
	}
	
	public LogRecordTextParameters(string? format, string? indent = null, string? section = null)
	{
		Format = format;
		Indent = indent;
		Section = section;
	}

	public ILogRecordFormatter CreateFormatter() => new LogRecordTextFormatter(new TextFormatSetting(Format ?? DefaultFormat, Indent ?? "  ", Section));
}
