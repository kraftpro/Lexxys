using System.Collections.Generic;

namespace Lexxys.Logging;

public class LogWriterFilter
{
	/// <summary>
	/// Level of the logging
	/// </summary>
	public LogTypeFilter? LogType { get; set; }
	/// <summary>
	/// Filters object to be included in the log.
	/// <code>
	/// rule := item [ item ]*
	/// item := text
	///      | '(' rule ')' -- group
	///      | '[' rule ']' -- optional group
	///      | '*'          -- any chars
	///      | ';' | ','    -- OR separator
	/// </code>
	/// </summary>
	public string? Include { get; set; }
	/// <summary>
	/// Filers objects to be excluded from the log.
	/// <code>
	/// rule := item [ item ]*
	/// item := text
	///      | '(' rule ')' -- group
	///      | '[' rule ']' -- optional group
	///      | '*'          -- any chars
	///      | ';' | ','    -- OR separator
	/// </code>
	/// </summary>
	public string? Exclude { get; set; }
}
