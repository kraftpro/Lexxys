// Lexxys Infrastructural library.
// file: DataContext.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Data
{
	class DataContextImplementation: IDisposable
	{
		private static readonly TimeSpan SyncInterval = new TimeSpan(1, 0, 0);
		private static readonly ConcurrentDictionary<string, (DateTime Stamp, long Offset)> _timeSyncMap = new ConcurrentDictionary<string, (DateTime, long)>();

		private int _transactionsCount;
		private int _connectionsCount;
		private readonly Func<DbConnection> _connectionFactory;
		private readonly DbConnection _connection;
		private DbTransaction? _transaction;
		private TimeSpan _commandTimeout;
		private TimeSpan _defaultCommandTimeout;
		private DateTime _lockedTime;
		private DateTime _timeSyncStamp;
		private long _timeSyncOffset;

		private Action? _committed;
		private Action? _cancelled;
		private readonly Dictionary<object, ICommitAction> _broadcast;

		public DataContextImplementation(Func<DbConnection> connectionFactory, TimeSpan commandTimeout, DataAudit audit)
		{
			_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
			_connection = connectionFactory() ?? throw new InvalidOperationException("cannot create a connection");
			_broadcast = new Dictionary<object, ICommitAction>();
			_commandTimeout = _defaultCommandTimeout = commandTimeout;
			if (_timeSyncMap.TryGetValue(_connection.ConnectionString, out var sync))
			{
				_timeSyncStamp = sync.Stamp;
				_timeSyncOffset = sync.Offset;
			}
			Audit = audit;
		}

		public DataAudit Audit { get; }

		public event Action Committed
		{
			add
			{
				if (TransactionsCount > 0)
					_committed += value;
				else
					value?.Invoke();
			}
			remove { }
		}
		public event Action Cancelled
		{
			add
			{
				if (TransactionsCount > 0)
					_cancelled += value;
			}
			remove { }
		}

		public ICommitAction SetCommitAction(object key, Func<ICommitAction> factory)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (_broadcast.TryGetValue(key, out var obj))
				return obj;
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));
			obj = factory();
			_broadcast.Add(key, obj);
			return obj;
		}

		private void SyncTime()
		{
			var syncKey = _connection.ConnectionString;
			if (!_timeSyncMap.TryGetValue(syncKey, out var sv) || sv.Stamp <= _timeSyncStamp)
			{
				long offset = 0;
				DateTime now;
				using (DbCommand cmd = NewCommand("select sysdatetime()"))
				{
					cmd.ExecuteScalar();
					long delta = long.MaxValue;
					int c = 0;
					var dd = new List<long>();
					for (; ; )
					{
						now = DateTime.Now;
						var dbnow = (DateTime)cmd.ExecuteScalar()!;
						long duration = (DateTime.Now - now).Ticks;
						offset = (dbnow - now).Ticks - duration / 2;
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
				}
				sv = _timeSyncMap.AddOrUpdate(syncKey, (now, offset), (_, o) => o.Stamp > now ? o: (now, offset));
			}
			(_timeSyncStamp, _timeSyncOffset) = sv;
		}

		public DateTime Time => DateTime.Now + TimeSpan.FromTicks(_timeSyncOffset);

		public DateTime Now => _lockedTime == default ? DateTime.Now + TimeSpan.FromTicks(_timeSyncOffset): _lockedTime;

		public DataContextImplementation Clone()
			=> new DataContextImplementation(_connectionFactory, _commandTimeout, Audit.Clone())
			{
				_timeSyncOffset = _timeSyncOffset,
				_timeSyncStamp = _timeSyncStamp,
			};

		public bool LockNow(DateTime now)
		{
			if (_lockedTime != default)
				return false;
			_lockedTime = now;
			return true;
		}

		public void UnlockNow()
		{
			_lockedTime = default;
		}

		public DbConnection Connection => _connection;

		public DbTransaction? Transaction => _transaction;

		public int TransactionsCount => _transactionsCount;

		public int ConnectionsCount => _connectionsCount;

		public int Connect()
		{
			var t = Audit.Start();
			if (_connectionsCount > 0)
			{
				Debug.Assert(_connection.State != ConnectionState.Closed);

				++_connectionsCount;
			}
			else
			{
				Debug.Assert(_connection.State == ConnectionState.Closed);

				_connection.Open();
				_connectionsCount = 1;
				if (_timeSyncStamp + SyncInterval < DateTime.Now)
					SyncTime();
			}
			Audit.ConnectionEnd(t);
			return _connectionsCount;
		}

		public async Task<int> ConnectAsync()
		{
			var t = Audit.Start();
			if (_connectionsCount > 0)
			{
				Debug.Assert(_connection.State != ConnectionState.Closed);

				++_connectionsCount;
			}
			else
			{
				Debug.Assert(_connection.State == ConnectionState.Closed);

				await _connection.OpenAsync();
				_connectionsCount = 1;
				if (_timeSyncStamp + SyncInterval < DateTime.Now)
					SyncTime();
			}
			Audit.ConnectionEnd(t);
			return _connectionsCount;
		}

		public int Disconnect()
		{
			Debug.Assert(_connection.State != ConnectionState.Closed);
			if (_connectionsCount > 1)
				return --_connectionsCount;

			var t = Audit.Start();

			_connection.Close();	// throws error when connection is closed already
			_connectionsCount = 0;

			Audit.ConnectionEnd(t);

			return _connectionsCount;
		}

		private void SafeDisconnect()
		{
			var t = Audit.Start();
			if (_connectionsCount > 1)
			{
				--_connectionsCount;
				if (_connection.State == ConnectionState.Closed)
				{
					_connection.Open();
					Audit.ConnectionEnd(t);
				}
			}
			else
			{
				var time = WatchTimer.Start();
				if (_connectionsCount < 1)
					Dc.Log.Error("Dc.SafeDisconnect", "ConnectionCount == 0", null, null);
				if (_connection.State != ConnectionState.Closed)
					_connection.Close();
				_connectionsCount = 0;
				Audit.ConnectionEnd(t);
			}
		}

		public int Begin(IsolationLevel iso)
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

		public void Commit()
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
			var committed = _committed;
			var broadcast = _broadcast.Values.ToList();
			_committed = null;
			_cancelled = null;
			_broadcast.Clear();
			try
			{
				_transaction.Commit();
			}
			catch (Exception flaw)
			{
				Dc.Log.Error("DC.Commit", flaw);
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
				Dc.Log.Error("DC.Commit.Committed", flaw);
			}
			foreach (var item in broadcast)
			{
				try
				{
					item.Commit();
				}
				catch (Exception flaw)
				{
					Dc.Log.Error("DC.Commit.Broadcast", flaw);
				}
			}
		}

		public void Rollback()
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
			var cancelled = _cancelled;
			var broadcast = _broadcast.Values.ToList();
			_committed = null;
			_cancelled = null;
			_broadcast.Clear();

			try
			{
				_transaction.Rollback();
			}
			catch(Exception flaw)
			{
				Dc.Log.Error("DC.Rollback", flaw);
				// 
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
				Dc.Log.Error("DC.Rollback.Cancelled", flaw);
			}
			foreach (var item in broadcast)
			{
				try
				{
					item.Rollback();
				}
				catch (Exception flaw)
				{
					Dc.Log.Error("DC.Rollback.Broadcast", flaw);
				}
			}
		}

		public TimeSpan CommandTimeout
		{
			get { return _commandTimeout; }
			set
			{
				if (value.Ticks < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				_commandTimeout = value;
			}
		}

		internal TimeSpan DefaultCommandTimeout
		{
			get { return _defaultCommandTimeout; }
			set
			{
				if (value.Ticks < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
				_commandTimeout = _defaultCommandTimeout = value;
			}
		}

		public DbCommand Command(string query, params DataParameter[] parameters)
		{
			if (query == null || query.Length == 0)
				throw new ArgumentNullException(nameof(query));

			return SetTimeout(NewCommand(query).WithParameters(parameters));
		}

		private DbCommand SetTimeout(DbCommand command)
		{
			if (_commandTimeout.Ticks > 0)
				command.CommandTimeout = _commandTimeout.Ticks > TimeSpan.TicksPerDay ? 0 : (int)(_commandTimeout.Ticks / TimeSpan.TicksPerSecond);
			_commandTimeout = _defaultCommandTimeout;
			return command;
		}

		private DbCommand NewCommand(string statement)
		{
			if (statement == null || (statement = statement.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(statement));
			var c = _connection.CreateCommand();
			c.CommandText = statement;
			if (_transaction != null)
				c.Transaction = _transaction;
			return c;
		}

		struct TimingNode
		{
			public readonly long Stamp;
			public readonly long Length;
			public readonly string Statement;

			public TimingNode(long stamp, long length, string statement)
			{
				Stamp = stamp;
				Length = length;
				Statement = statement;
			}
		}

		public void Dispose()
		{
			if (!__disposed)
			{
				__disposed = true;
				_connection?.Dispose();
				_transaction?.Dispose();
				_connectionsCount = 0;
				_transactionsCount = 0;
			}
		}
		private bool __disposed;
	}

	class DataAudit
	{
		private long _connectTime;
		private long _transactTime;
		private long _queryTime;
		private int _timingGroupDepth;
		private long _timingGroupStamp;
		private int _lockTiming;
		private readonly List<TimingNode> _timingGroupItems;

		private long _connectionAudit;
		private long _commandAudit;
		private long _batchAudit;

		private ILogging _log;

		public DataAudit(TimeSpan connectionAudit, TimeSpan commandAudit, TimeSpan batchAudit, ILogging? log = null): this(
			Math.Max(0, connectionAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
			Math.Max(0, commandAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
			Math.Max(0, batchAudit.Ticks / TimeSpan.TicksPerMillisecond * WatchTimer.TicksPerMillisecond),
			log ?? Dc.Timing)
		{
		}

		private DataAudit(long connectionAudit, long commandAudit, long batchAudit, ILogging log)
		{
			_connectionAudit = connectionAudit;
			_commandAudit = commandAudit;
			_batchAudit = batchAudit;
			_log = log;
			_timingGroupItems = new List<TimingNode>();
		}

		public TimeSpan TransactTime => WatchTimer.ToTimeSpan(_transactTime);

		public TimeSpan ConnectTime => WatchTimer.ToTimeSpan(_connectTime);

		public TimeSpan QueryTime => WatchTimer.ToTimeSpan(_queryTime);

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

		public long Start()
		{
			return WatchTimer.Start();
		}

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

		struct TimingNode
		{
			public readonly long Stamp;
			public readonly long Length;
			public readonly string Statement;

			public TimingNode(long stamp, long length, string statement)
			{
				Stamp = stamp;
				Length = length;
				Statement = statement;
			}
		}
	}
}
