using System.Collections.Generic;

namespace Lexxys.Logging;

public class LogWriterFilter
{
	public LogTypeFilter? LogType { get; set; }
	public string? Include { get; set; }
	public string? Exclude { get; set; }
}
