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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lexxys.Data
{
	public interface ICommitAction
	{
		void Commit();
		void Rollback();
	}

	class DataContextImplementation: IDisposable
	{
		private static readonly TimeSpan SyncInterval = new TimeSpan(1, 0, 0);
		private static readonly ConcurrentDictionary<string, (DateTime Stamp, long Offset)> _timeSyncMap = new ConcurrentDictionary<string, (DateTime, long)>();

		private int _transactionsCount;
		private int _connectionsCount;
		private readonly SqlConnection _connection;
		private SqlTransaction _transaction;
		private TimeSpan _commandTimeout;
		private TimeSpan _defaultCommandTimeout;
		private long _connectTime;
		private long _transactTime;
		private long _queryTime;
		private int _timingGroupDepth;
		private long _timingGroupStamp;
		private int _lockTiming;
		private readonly List<TimingNode> _timingGroupItems;
		private readonly ConnectionStringInfo _connectionInfo;
		private DateTime _lockedTime;
		private DateTime _timeSyncStamp;
		private long _timeSyncOffset;

		private Action _committed;
		private Action _cancelled;
		private readonly Dictionary<object, ICommitAction> _broadcast;

		public DataContextImplementation(ConnectionStringInfo connectionInfo)
		{
			_connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
			_timingGroupItems = new List<TimingNode>();
			_broadcast = new Dictionary<object, ICommitAction>();
			_connection = new SqlConnection(connectionInfo.GetConnectionString());
			_commandTimeout = _defaultCommandTimeout = connectionInfo.CommandTimeout;
			if (_timeSyncMap.TryGetValue(connectionInfo.ToString(), out var sync))
			{
				_timeSyncStamp = sync.Stamp;
				_timeSyncOffset = sync.Offset;
			}
		}

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

		public void ResetStatistics()
		{
			_connectTime = _transactTime = _queryTime = 0;
			_timingGroupDepth = 0;
			_timingGroupStamp = 0;
			_timingGroupItems.Clear();
		}

		private void SyncTime()
		{
			var syncKey = _connectionInfo.ToString();
			if (!_timeSyncMap.TryGetValue(syncKey, out var sv) || sv.Stamp <= _timeSyncStamp)
			{
				long offset = 0;
				DateTime now;
				using (DbCommand cmd = SqlCommand("select sysdatetime()"))
				{
					cmd.ExecuteScalar();
					long delta = long.MaxValue;
					int c = 0;
					var dd = new List<long>();
					for (; ; )
					{
						now = DateTime.Now;
						var dbnow = (DateTime)cmd.ExecuteScalar();
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

		public TimeSpan TransactTime => WatchTimer.ToTimeSpan(_transactTime);

		public TimeSpan ConnectTime => WatchTimer.ToTimeSpan(_connectTime);

		public TimeSpan QueryTime => WatchTimer.ToTimeSpan(_queryTime);

		public TimeSpan TotalTime => WatchTimer.ToTimeSpan(_connectTime + _transactTime + _queryTime);

		public SqlConnection Connection => _connection;

		public SqlTransaction Transaction => _transaction;

		public int TransactionsCount => _transactionsCount;

		public int ConnectionsCount => _connectionsCount;

		public int Connect()
		{
			var time = WatchTimer.Start();
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

				var threshold = WatchTimer.Query(time);
				if (threshold > _connectionInfo.ConnectionAuditThreshold.Ticks)
					Dc.Log.Info(SR.ConnectionTiming(threshold));
			}

			_connectTime += WatchTimer.Stop(time);
			return _connectionsCount;
		}

		public async Task<int> ConnectAsync()
		{
			var time = WatchTimer.Start();
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

				var threshold = WatchTimer.Query(time);
				if (threshold > _connectionInfo.ConnectionAuditThreshold.Ticks)
					Dc.Log.Info(SR.ConnectionTiming(threshold));
			}

			_connectTime += WatchTimer.Stop(time);
			return _connectionsCount;
		}

		public int Disconnect()
		{
			Debug.Assert(_connection.State != ConnectionState.Closed);
			if (_connectionsCount > 1)
				return --_connectionsCount;

			var time = WatchTimer.Start();

			_connection.Close();	// throws error when connection is closed already
			_connectionsCount = 0;

			_connectTime += WatchTimer.Stop(time);

			return _connectionsCount;
		}

		private void SafeDisconnect()
		{
			if (_connectionsCount > 1)
			{
				--_connectionsCount;
				if (_connection.State == ConnectionState.Closed)
				{
					var time = WatchTimer.Start();
					_connection.Open();
					_connectTime += WatchTimer.Stop(time);
				}
			}
			else
			{
				var time = WatchTimer.Start();
				if (_connectionsCount < 1)
					Dc.Log.Error("Dc.SafeDisconnect", "ConnectionCount == 0");
				_connectionsCount = 0;
				if (_connection.State != ConnectionState.Closed)
					_connection.Close();
				_connectTime += WatchTimer.Stop(time);
			}
		}

		public int Begin(IsolationLevel iso)
		{
			if (_transactionsCount > 0)
				return ++_transactionsCount;

			var time = WatchTimer.Start();
			Connect();
			TimingGroupBegin();
			_transaction = _connection.BeginTransaction(iso == default ? Dc.DefaultIsolationLevel: iso);
			_transactionsCount = 1;
			_transactTime += WatchTimer.Stop(time);
			return 1;
		}

		public void Commit()
		{
			if (_transactionsCount != 1)
			{
				if (_transactionsCount > 1)
				{
					--_transactionsCount;
					return;
				}
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToCommit());
				return;
			}

			var time = WatchTimer.Start();
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
				_transaction = null;
			}
			_transactTime += WatchTimer.Stop(time);

			SafeDisconnect();
			TimingGroupEnd();

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
			if (_transactionsCount <= 0)
			{
				_transactionsCount = 0;
				Dc.Log.Error(SR.NothingToRollback());
				return;
			}

			var time = WatchTimer.Start();
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
			_transactTime += WatchTimer.Stop(time);

			SafeDisconnect();
			TimingGroupEnd();

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

		public void LockTiming()
		{
			++_lockTiming;
		}

		public void UnlockTiming()
		{
			Debug.Assert(_lockTiming > 0);
			if (_lockTiming > 0)
				--_lockTiming;
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

		public DbCommand Command(string query, params DbParameter[] parameters)
		{
			if (query == null || query.Length == 0)
				throw new ArgumentNullException(nameof(query));

			var command = SetTimeout(SqlCommand(query));
			if (parameters != null && parameters.Length > 0)
				command.Parameters.AddRange(parameters);
			return command;
		}

		private SqlCommand SetTimeout(SqlCommand command)
		{
			if (_commandTimeout.Ticks > 0)
				command.CommandTimeout = _commandTimeout.Ticks > TimeSpan.TicksPerDay ? 0 : (int)(_commandTimeout.Ticks / TimeSpan.TicksPerSecond);
			_commandTimeout = _defaultCommandTimeout;
			return command;
		}

		private SqlCommand SqlCommand(string statement)
		{
			if (statement == null || (statement = statement.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(statement));

			return new SqlCommand(statement, _connection, _transaction);
		}

		public static long TimingBegin()
		{
			return WatchTimer.Start();
		}

		public long TimingEnd(string query, long time)
		{
			var t = WatchTimer.Query(time);
			_queryTime += t;
			if (_lockTiming > 0)
				return t;
			if (_timingGroupDepth > 0)
			{
				_timingGroupItems.Add(new TimingNode(WatchTimer.Query(_timingGroupStamp) - t, t, query));
			}
			if (_connectionInfo.CommandAuditThreshold != TimeSpan.Zero &&
				CommandTimeout.Ticks <= TimeSpan.TicksPerDay &&
				t / WatchTimer.TicksPerMillisecond > _connectionInfo.CommandAuditThreshold.Ticks / TimeSpan.TicksPerMillisecond)
				Dc.Timing.Info(SR.SqlQueryTiming(t, query));
			return t;
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

		public void TimingGroupBegin()
		{
			if (_connectionInfo.BatchAuditThreshold == TimeSpan.Zero)
				return;
			if (++_timingGroupDepth == 1)
				_timingGroupStamp = WatchTimer.Start();
		}

		public void TimingGroupEnd()
		{
			if (_connectionInfo.BatchAuditThreshold == TimeSpan.Zero)
				return;
			if (--_timingGroupDepth <= 0)
			{
				if (_timingGroupDepth < 0)
				{
					_timingGroupDepth = 0;
					return;
				}
				long time = WatchTimer.Query(_timingGroupStamp);
				if (_lockTiming == 0 && time / WatchTimer.TicksPerMillisecond >= _connectionInfo.BatchAuditThreshold.Ticks / TimeSpan.TicksPerMillisecond)
				{
					using (Dc.Timing.InfoEnter("SQL Timing: " + WatchTimer.ToString(time)))
					{
						long t0 = 0;
						foreach (var t in _timingGroupItems)
						{
							Dc.Timing.Info(SR.SqlGroupQueryTiming(t.Length, t.Stamp - t0, t.Statement));
							t0 = t.Stamp + t.Length;
						}
					}
				}
				_timingGroupItems.Clear();
			}
		}

		public List<RowsCollection> Records(int count, string query, params DbParameter[] parameters)
		{
			if (query == null || query.Length == 0)
				throw EX.ArgumentNull(nameof(query));
			if (_connectionsCount < 1)
				throw EX.InvalidOperation();

			var result = new List<RowsCollection>();

			using var da = new SqlDataAdapter();
			SqlCommand command = SetTimeout(SqlCommand(query));
			if (parameters != null && parameters.Length > 0)
				command.Parameters.AddRange(Array.ConvertAll(parameters,
					p => p == null ? null : new SqlParameter
					{
						ParameterName = p.ParameterName,
						DbType = p.DbType,
						IsNullable = p.IsNullable,
						Size = p.Size,
						Scale = p.Scale,
						Precision = p.Precision,
						Value = p.Value,
					}));
			da.SelectCommand = command;

			using var ds = new DataSet();
			da.Fill(ds);
			for (int i = 0; i < count && i < ds.Tables.Count; ++i)
			{
				result.Add(new RowsCollection(ds.Tables[i]));
			}
			return result;
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
}
