using Microsoft.Extensions.Logging;

namespace Lexxys.Data;

class DataAudit: IDataContextAudit
{
	private long _connectTime;
	private long _transactTime;
	private long _queryTime;
	private int _timingGroupDepth;
	private long _timingGroupStamp;
	private int _lockTiming;
	private readonly List<TimingNode> _timingGroupItems;

	private readonly long _connectionAudit;
	private readonly long _commandAudit;
	private readonly long _batchAudit;

	private readonly ILogger _log;

	public DataAudit(TimeSpan connectionAudit, TimeSpan commandAudit, TimeSpan batchAudit, ILogger? log = null) : this(
		Math.Max(0, connectionAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
		Math.Max(0, commandAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
		Math.Max(0, batchAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
		log ?? Dc.Timing)
	{
	}

	private DataAudit(long connectionAudit, long commandAudit, long batchAudit, ILogger log)
	{
		_connectionAudit = connectionAudit;
		_commandAudit = commandAudit;
		_batchAudit = batchAudit;
		_log = log;
		_timingGroupItems = [];
	}

	/// <summary>
	/// Gets the total time spent opening, committing, and rolling back transactions.
	/// </summary>
	public TimeSpan TransactTime => WatchTimer.ToTimeSpan(_transactTime);

	/// <summary>
	/// Gets the total time spent establishing connections.
	/// </summary>
	public TimeSpan ConnectTime => WatchTimer.ToTimeSpan(_connectTime);

	/// <summary>
	/// Gets the total time spent executing SQL statements.
	/// </summary>
	public TimeSpan QueryTime => WatchTimer.ToTimeSpan(_queryTime);

	/// <summary>
	/// Gets the total time spent on all activities, including connections, transactions, and queries.
	/// </summary>
	public TimeSpan TotalTime => WatchTimer.ToTimeSpan(_connectTime + _transactTime + _queryTime);

	public void LockTiming()
	{
		++_lockTiming;
	}

	public void UnlockTiming()
	{
		if (_lockTiming > 0)
			--_lockTiming;
	}

	public long Start() => WatchTimer.Start();

	public void ConnectionEnd(long time)
	{
		var t = WatchTimer.Query(time);
		if (t > _connectionAudit)
			_log.Info(SR.ConnectionTiming(t));
		_connectTime += t;
	}

	public void TransactionEnd(long time)
	{
		var t = WatchTimer.Query(time);
		_transactTime += t;
	}

	public void QueryEnd(string query, long time)
	{
		var t = WatchTimer.Query(time);
		_queryTime += t;
		if (_lockTiming > 0)
			return;
		if (_timingGroupDepth > 0)
		{
			_timingGroupItems.Add(new TimingNode(WatchTimer.Query(_timingGroupStamp) - t, t, query));
		}
		if (_commandAudit > 0 && t > _commandAudit)
			_log.Info(SR.SqlQueryTiming(t, query));
	}

	public void GroupBegin()
	{
		if (_batchAudit == 0)
			return;
		if (++_timingGroupDepth == 1)
			_timingGroupStamp = WatchTimer.Start();
	}

	public void GroupEnd()
	{
		if (_batchAudit == 0)
			return;
		if (--_timingGroupDepth <= 0)
		{
			if (_timingGroupDepth < 0)
				_timingGroupDepth = 0;
			else if (_lockTiming == 0)
				LogGroupTiming();
			_timingGroupItems.Clear();
		}
	}

	public void Reset()
	{
		_connectTime = _transactTime = _queryTime = 0;
		_timingGroupDepth = 0;
		_timingGroupStamp = 0;
		_timingGroupItems.Clear();
	}

	public DataAudit Clone()
	{
		return new DataAudit(_connectionAudit, _commandAudit, _batchAudit, _log);
	}

	private void LogGroupTiming()
	{
		long t = WatchTimer.Query(_timingGroupStamp);
		if (t >= _batchAudit)
		{
			using (_log.InfoEnter("SQL Timing: " + WatchTimer.ToString(t)))
			{
				long t0 = 0;
				foreach (var item in _timingGroupItems)
				{
					_log.Info(SR.SqlGroupQueryTiming(item.Length, item.Stamp - t0, item.Statement));
					t0 = item.Stamp + item.Length;
				}
			}
		}
	}

	record struct TimingNode(long Stamp, long Length, string Statement);
}
