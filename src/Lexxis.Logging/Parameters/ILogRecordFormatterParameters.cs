namespace Lexxys.Logging;

/// <summary>
/// <see cref="ILogRecordFormatter"/> factory.
/// </summary>
public interface ILogRecordFormatterParameters
{
	/// <summary>
	/// Creates a <see cref="ILogRecordFormatter"/>.
	/// </summary>
	/// <returns></returns>
	ILogRecordFormatter CreateFormatter();
}
