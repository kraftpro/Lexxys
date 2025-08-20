// Lexxys Infrastructural library.
// file: DataContext.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Lexxys.Data;

class DataContextImplementation: IDisposable
{
	private static readonly TimeSpan TimeSyncInterval = new TimeSpan(1, 0, 0);
	private static readonly ConcurrentDictionary<string, (DateTime Stamp, DbTimeProvider Provider)> _timeSyncMap = new ConcurrentDictionary<string, (DateTime, DbTimeProvider)>();

	private int _transactionsCount;
	private int _connectionsCount;
	private readonly Func<DbConnection> _connectionFactory;
	private readonly DbConnection _connection;
	private DbTransaction? _transaction;
	private TimeSpan _commandTimeout;
	private TimeSpan _defaultCommandTimeout;
	private DateTime _timeSyncStamp;
	private DbTimeProvider? _timeProvider;

	private Action? _committed;
	private Action? _cancelled;
	private readonly Dictionary<object, ICommitAction> _broadcast;

	public DataContextImplementation(Func<DbConnection> connectionFactory, TimeSpan commandTimeout, DataAudit audit)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
		_connection = connectionFactory() ?? throw new InvalidOperationException("cannot create a connection");
		_broadcast = [];
		_commandTimeout = _defaultCommandTimeout = commandTimeout;
		if (_timeSyncMap.TryGetValue(_connection.ConnectionString, out var sync))
		{
			_timeSyncStamp = sync.Stamp;
			_timeProvider = sync.Provider;
		}
		Audit = audit;
	}

	private object SyncLock => _connectionFactory;

	public DataAudit Audit { get; }

	public event Action Committed
	{
		add
		{
			lock (SyncLock)
			{
				if (TransactionsCount > 0)
				{
					_committed += value;
					return;
				}
			}
			value?.Invoke();
		}

		remove
		{
			lock (SyncLock)
			{
				_committed -= value;
			}
		}
	}
	public event Action Cancelled
	{
		add
		{
			lock (SyncLock)
			{
				if (TransactionsCount > 0)
				{
					_cancelled += value;
				}
			}
		}

		remove
		{
			lock (SyncLock)
			{
				_cancelled -= value;
			}
		}
	}

	public ICommitAction SetCommitAction(object key, Func<ICommitAction> factory)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		lock (SyncLock)
		{
			if (_broadcast.TryGetValue(key, out var obj))
				return obj;
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));
			obj = factory();
			_broadcast.Add(key, obj);
			return obj;
		}
	}

	private void SyncTime()
	{
		var syncKey = _connection.ConnectionString;
		if (_timeSyncMap.TryGetValue(syncKey, out var sv) && sv.Stamp > _timeSyncStamp) return;

		_timeProvider ??= sv.Provider ?? GetTimeProvider();

		long offset;
		DateTime now;
		long delta = long.MaxValue;
		int c = 0;
		var dd = new List<long>();

		using DbCommand cmd = NewCommand("select sysutcdatetime();");
		for (; ; )
		{
			now = DateTime.UtcNow;
			var t = WatchTimer.Start();
			var dbNow = (DateTime)cmd.ExecuteScalar()!;
			long duration = TimeSpan.FromTicks(WatchTimer.Query(t)).Ticks;
			offset = (dbNow - now).Ticks - duration / 2;
			if (duration < delta)
			{
				delta = duration;
				c = 0;
				dd.Clear();
				dd.Add(offset);
			}
			else
			{
				dd.Add(offset);
				if (++c >= 3)
					break;
			}
		}

		offset = (long)(dd.Average() + 0.5);
		_timeProvider.Reset(new TimeSpan(offset));
		sv = _timeSyncMap.AddOrUpdate(syncKey, _ => (now, _timeProvider), (_, o) => o.Stamp == sv.Stamp ? (now, _timeProvider) : o);
		(_timeSyncStamp, _timeProvider) = (sv.Stamp, sv.Provider);
	}

	private DbTimeProvider GetTimeProvider()
	{
		if (_timeSyncMap.TryGetValue(_connection.ConnectionString, out var sv))
			return sv.Provider;

		using DbCommand cmd = NewCommand("select sysdatetimeoffset();");

		var dto = (DateTimeOffset)cmd.ExecuteScalar()!;
		cmd.CommandText = "select current_timezone_id();";
		string? tzid = null;
		try { tzid = (string?)cmd.ExecuteScalar(); } catch { }
		if (tzid == null)
		{
			cmd.CommandText = """
					exec master.dbo.xp_regread
						'HKEY_LOCAL_MACHINE',
						'SYSTEM\CurrentControlSet\Control\TimeZoneInformation',
						'TimeZoneKeyName',@tz out
					select @tz;
					""";
			try { tzid = (string?)cmd.ExecuteScalar(); } catch { }
		}
		var tz = FindTimeZone(tzid) ?? TimeZoneInfo.CreateCustomTimeZone("Database Time", dto.Offset, null, null);
		return new DbTimeProvider(TimeSpan.Zero, tz);

		static TimeZoneInfo? FindTimeZone(string? id)
		{
#if NET
			return id == null || !TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) ? null : tz;
#else
			return id == null ? null: TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(o => String.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));
#endif
		}
	}

	public ITimeProvider Time => _timeProvider ??= GetTimeProvider();

	public DataContextImplementation Clone()
		=> new DataContextImplementation(_connectionFactory, _commandTimeout, Audit.Clone())
		{
			_timeProvider = _timeProvider,
			_timeSyncStamp = _timeSyncStamp,
		};

	public DbConnection Connection => _connection;

	public DbTransaction? Transaction => _transaction;

	public int TransactionsCount => _transactionsCount;

	public int ConnectionsCount => _connectionsCount;

	public int Connect()
	{
		var t = Audit.Start();
		lock (SyncLock)
		{
			if (_connectionsCount > 0)
			{
				Debug.Assert(_connection.State != ConnectionState.Closed);
				Audit.ConnectionEnd(t);
				++_connectionsCount;
			}
			else
			{
				Debug.Assert(_connection.State == ConnectionState.Closed);
				_connection.Open();
				_connectionsCount = 1;
				if (_timeSyncStamp + TimeSyncInterval < DateTime.Now)
					SyncTime();
			}
			Audit.ConnectionEnd(t);
			return _connectionsCount;
		}
	}

	public int Disconnect()
	{
		var t = Audit.Start();
		lock (SyncLock)
		{
			if (--_connectionsCount > 0)
			{
				Debug.Assert(_connection.State != ConnectionState.Closed);
			}
			else
			{
				Debug.Assert(_connection.State != ConnectionState.Closed);
				if (_connectionsCount < 0)
				{
					_connectionsCount = 0;
					Dc.Log.Error("Dc.Disconnect", "ConnectionCount == 0", null, null);
				}
				_connection.Close();
			}
			Audit.ConnectionEnd(t);
			return _connectionsCount;
		}
	}

	private void SafeDisconnect()
	{
		var t = Audit.Start();
		if (--_connectionsCount > 0)
		{
			if (_connection.State == ConnectionState.Closed)
				_connection.Open();
		}
		else
		{
			if (_connectionsCount < 0)
			{
				_connectionsCount = 0;
				Dc.Log.Error("Dc.SafeDisconnect", "ConnectionCount == 0", null, null);
			}
			if (_connection.State != ConnectionState.Closed)
				_connection.Close();
		}
		Audit.ConnectionEnd(t);
	}

	public int Begin(IsolationLevel iso)
	{
		lock (SyncLock)
		{
			if (_transactionsCount > 0)
				return ++_transactionsCount;

			Connect();
			var t = Audit.Start();
			Audit.GroupBegin();
			_transaction = _connection.BeginTransaction(iso == default ? Dc.DefaultIsolationLevel: iso);
			_transactionsCount = 1;
			Audit.TransactionEnd(t);
			return 1;
		}
	}

	public void Commit()
	{
		lock (SyncLock)
		{
			if (_transaction == null)
			{
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToCommit());
				return;
			}
			if (_transactionsCount != 1)
			{
				if (_transactionsCount > 1)
				{
					--_transactionsCount;
					return;
				}
				_transaction = null;
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToCommit());
				return;
			}

			var t = Audit.Start();
			_cancelled = null;
			var committed = _committed;
			_committed = null;
			var broadcast = _broadcast.Values.ToList();
			_broadcast.Clear();
			try
			{
				_transaction.Commit();
			}
			catch (Exception flaw)
			{
				Dc.Log.Error("Dc.Commit", flaw);
			}
			finally
			{
				_transactionsCount = 0;
				_transaction.Dispose();
			}
			_transaction = null;

			Audit.TransactionEnd(t);
			SafeDisconnect();
			Audit.GroupEnd();

			try
			{
				committed?.Invoke();
			}
			catch (Exception flaw)
			{
				Dc.Log.Error("Dc.Commit.Committed", flaw);
			}
			foreach (var item in broadcast)
			{
				try
				{
					item.Commit();
				}
				catch (Exception flaw)
				{
					Dc.Log.Error("Dc.Commit.Broadcast", flaw);
				}
			}
		}
	}

	public void Rollback()
	{
		lock (SyncLock)
		{
			if (_transaction == null)
			{
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToRollback());
				return;
			}
			if (_transactionsCount <= 0)
			{
				_transaction = null;
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToRollback());
				return;
			}

			var t = Audit.Start();
			_cancelled = null;
			var cancelled = _cancelled;
			_committed = null;
			var broadcast = _broadcast.Values.ToList();
			_broadcast.Clear();

			try
			{
				_transaction.Rollback();
			}
			catch (Exception flaw)
			{
				Dc.Log.Error("Dc.Rollback", flaw);
			}
			finally
			{
				_transactionsCount = 0;
				_transaction.Dispose();
				_transaction = null;
			}

			Audit.TransactionEnd(t);
			SafeDisconnect();
			Audit.GroupEnd();

			try
			{
				cancelled?.Invoke();
			}
			catch (Exception flaw)
			{
				Dc.Log.Error("Dc.Rollback.Cancelled", flaw);
			}
			foreach (var item in broadcast)
			{
				try
				{
					item.Rollback();
				}
				catch (Exception flaw)
				{
					Dc.Log.Error("Dc.Rollback.Broadcast", flaw);
				}
			}
		}
	}

	public TimeSpan CommandTimeout
	{
		get => _commandTimeout;
		set
		{
			if (value.Ticks < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			_commandTimeout = value;
		}
	}

	internal TimeSpan DefaultCommandTimeout
	{
		get => _defaultCommandTimeout;
		set
		{
			if (value.Ticks < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, null);
			_commandTimeout = _defaultCommandTimeout = value;
		}
	}

	public DbCommand Command(SqlPart query, params DataParameter[] parameters)
	{
		if (query.IsEmpty)
			throw new ArgumentNullException(nameof(query));

		return SetTimeout(NewCommand(query.Value).WithParameters(parameters));
	}

	private DbCommand SetTimeout(DbCommand command)
	{
		if (_commandTimeout.Ticks > 0)
			command.CommandTimeout = _commandTimeout.Ticks > TimeSpan.TicksPerDay ? 0: (int)(_commandTimeout.Ticks / TimeSpan.TicksPerSecond);
		_commandTimeout = _defaultCommandTimeout;
		return command;
	}

	private DbCommand NewCommand(string statement)
	{
		if ((statement = statement.Trim()).Length == 0)
			throw new ArgumentNullException(nameof(statement));
		var c = _connection.CreateCommand();
		c.CommandText = statement;
		if (_transaction != null)
			c.Transaction = _transaction;
		return c;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_connection.Dispose();
			_transaction?.Dispose();
			_connectionsCount = 0;
			_transactionsCount = 0;
		}
	}
	private bool _disposed;
}
