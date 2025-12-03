namespace Lexxys.Data;

internal class DbTimeProvider: ITimeProvider
{
	private TimeSpan _offset;
	private TimeZoneInfo _timeZone;
	private DateTimeOffset? _utcNow;

	public DbTimeProvider(TimeSpan offset, TimeZoneInfo timeZone)
	{
		_offset = offset;
		_timeZone = timeZone;
	}

	internal void Reset(TimeSpan? offset, TimeZoneInfo? timeZone = null)
	{
		if (offset != null)
			_offset = offset.GetValueOrDefault();
		if (timeZone != null)
			_timeZone = timeZone;
	}

	public DateTimeOffset UtcNow => _utcNow ?? DateTimeOffset.UtcNow.Add(_offset);

	public DateTimeOffset LocalNow => UtcNow.ToOffset(_timeZone.BaseUtcOffset);

	public TimeZoneInfo LocalTimeZone => _timeZone;

	public IDisposable Hold(DateTime? time = null)
	{
		var th = new TimeHolder(this);
		_utcNow = time == default ? UtcNow: UtcTime(time.GetValueOrDefault());
		return th;

		DateTimeOffset UtcTime(DateTime t) => t.Kind == DateTimeKind.Utc ?
			new DateTimeOffset(t.Ticks, TimeSpan.Zero):
			new DateTimeOffset(t.Ticks + _timeZone.BaseUtcOffset.Ticks, TimeSpan.Zero);
	}

	private class TimeHolder: IDisposable
	{
		private readonly DbTimeProvider _provider;
		private readonly DateTimeOffset? _utcNow;

		public TimeHolder(DbTimeProvider provider) => (_provider, _utcNow) = (provider, provider._utcNow);

		public void Dispose() => _provider._utcNow = _utcNow;
	}
}