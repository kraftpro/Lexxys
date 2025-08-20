namespace Lexxys.Data;

public interface ITimeProvider
{
	public DateTimeOffset LocalNow { get; }
	public DateTimeOffset UtcNow { get; }
	public TimeZoneInfo LocalTimeZone { get; }

	IDisposable Hold(DateTime? utcTime = null);
}
