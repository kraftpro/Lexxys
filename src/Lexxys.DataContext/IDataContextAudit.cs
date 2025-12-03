namespace Lexxys.Data;

/// <summary>
/// Represents audit data for database operations, including connection, query execution, and transaction timings.
/// </summary>
public interface IDataContextAudit
{
	/// <summary>
	/// The time elapsed to establish the connection.
	/// </summary>
	TimeSpan ConnectTime { get; }
	/// <summary>
	/// The time elapsed executing SQL statements.
	/// </summary>
	TimeSpan QueryTime { get; }
	/// <summary>
	/// The time elapsed opening, committing, and rolling back transactions.
	/// </summary>
	TimeSpan TransactTime { get; }
}
