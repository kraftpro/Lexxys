// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data;
using System.Data.Common;

namespace Lexxys.Data;

/// <summary>
/// The default implementation of <see cref="IDataContext" />.
/// </summary>
public sealed class DataContext: IDataContext
{
	private static readonly IValue<ConnectionStringInfo> __globalConnectionString = Config.Current.GetValue<ConnectionStringInfo>(Dc.ConfigSection, null);
	private static Func<string, DbConnection> __defaultConnectionFactory = o => new Microsoft.Data.SqlClient.SqlConnection(o);

	private readonly DataContextImplementation _context;

	public static Func<string, DbConnection> DefaultConnectionFactory
	{
		get => __defaultConnectionFactory;
		set => __defaultConnectionFactory = value ?? throw new ArgumentNullException(nameof(value));
	}

	private DataContext(DataContextImplementation context)
	{
		_context = context;
	}

	public DataContext(Func<string, DbConnection>? factory = null) : this(__globalConnectionString.Value, factory)
	{
	}

	public DataContext(ConnectionStringInfo connectionInfo, Func<string, DbConnection>? connectionFactory = null)
	{
		if (connectionInfo == null)
			throw new ArgumentNullException(nameof(connectionInfo));
		connectionFactory ??= __defaultConnectionFactory;
		string connectionString = connectionInfo.GetConnectionString();
		_context = new DataContextImplementation(() => connectionFactory(connectionString), connectionInfo.CommandTimeout, new DataAudit(connectionInfo.ConnectionAuditThreshold, connectionInfo.ConnectionAuditThreshold, connectionInfo.BatchAuditThreshold));
	}

	internal DataContextImplementation Context => _context;

	public TimeSpan ConnectTime => _context.Audit.ConnectTime;

	public TimeSpan TransactTime => _context.Audit.TransactTime;

	public TimeSpan QueryTime => _context.Audit.QueryTime;

	public TimeSpan TotalTime => _context.Audit.TotalTime;

	public event Action Committed
	{
		add => _context.Committed += value;
		remove => _context.Committed -= value;
	}

	public event Action Canceled
	{
		add => _context.Cancelled += value;
		remove => _context.Cancelled -= value;
	}

	public ICommitAction GetOrSetCommitAction(object key, Func<ICommitAction> factory) => _context.SetCommitAction(key, factory);

	public bool InTransaction => _context.TransactionsCount > 0;

	public DateTime Time => _context.Time;

	public DateTime Now => _context.Now;

	public IContextHolder HoldTheMoment() => new Dc.TimeHolder(this);

	public IContextHolder Connection() => new Dc.Connecting(this);

	public ITransactable Transaction(bool autoCommit = false, IsolationLevel isolation = default) => new Dc.Transacting(this, autoCommit, isolation);

	public IContextHolder NoTiming() => new Dc.TimingLocker(this);

	public IDataContext Clone() => new DataContext(_context.Clone());

	public void Commit() => _context.Commit();

	public void Rollback() => _context.Rollback();

	public DbCommand Command(string statement, params DataParameter[] parameters) => _context.Command(statement, parameters);

	public void SetQueryTimeout(TimeSpan timeout) => _context.CommandTimeout = timeout;

	public IContextHolder CommandTimeout(TimeSpan timeout, bool always = false) => always || _context.CommandTimeout < timeout ? new Dc.TimeoutLocker(this, timeout): new Dc.ContextHolder(this);

	public void ResetStatistics() => _context.Audit.Reset();

	public T Map<T>(Func<DbCommand, T> mapper, DbCommand command)
	{
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		if (command.CommandText is not { Length: > 0 })
			throw new ArgumentException("CommandText parameter is empty", nameof(command));

		long t = 0;
		int connect = 0;
		try
		{
			connect = _context.Connect();
			command.Connection = _context.Connection;
			t = _context.Audit.Start();
			T result = mapper(command);
			return result;
		}
		catch (Exception flaw)
		{
			flaw.Add(nameof(command), command.CommandText)
				.Add("type", typeof(T));
			if (command.Parameters is { Count: >0 })
			{
				for (int i = 0; i < command.Parameters.Count; ++i)
				{
					var param = command.Parameters[i];
					if (param != null)
						flaw.Add(param.ParameterName, $"{param.DbType}, {param.Value}.");
				}
			}
			flaw.Add(t);
			throw;
		}
		finally
		{
			if (t != 0)
				_context.Audit.QueryEnd(command.CommandText, t);
			if (connect > 0)
				_context.Disconnect();
			command.Connection = null;
		}
	}

	public async Task<T> MapAsync<T>(Func<DbCommand, Task<T>> mapper, DbCommand command)
	{
		if (mapper == null)
			throw new ArgumentNullException(nameof(mapper));
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		if (command.CommandText is not { Length: >0 })
			throw new ArgumentException("CommandText parameter is empty", nameof(command));

		long t = 0;
		int connect = 0;
		try
		{
			connect = await _context.ConnectAsync().ConfigureAwait(false);
			command.Connection = _context.Connection;
			t = _context.Audit.Start();
			var result = await mapper(command).ConfigureAwait(false);
			return result;
		}
		catch (Exception flaw)
		{
			flaw.Add(nameof(command), command.CommandText)
				.Add("type", typeof(T));
			if (command.Parameters is { Count: > 0 })
			{
				for (int i = 0; i < command.Parameters.Count; ++i)
				{
					var param = command.Parameters[i];
					if (param != null)
						flaw.Add(param.ParameterName, $"{param.DbType}, {param.Value}.");
				}
			}
			flaw.Add(t);
			throw;
		}
		finally
		{
			if (t != 0)
				_context.Audit.QueryEnd(command.CommandText, t);
			if (connect > 0)
				_context.Disconnect();
		}
	}

	public int Execute(DbCommand command)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));

		long t = 0;
		int connect = 0;
		try
		{
			connect = _context.Connect();
			command.Connection = _context.Connection;
			command.Transaction = _context.Transaction;
			t = _context.Audit.Start();
			var result = command.ExecuteNonQuery();
			return result;
		}
		catch (Exception flaw)
		{
			flaw.Add(nameof(command), command)
				.Add(t);
			throw;
		}
		finally
		{
			if (t != 0)
				_context.Audit.QueryEnd(command.CommandText, t);
			if (connect > 0)
				_context.Disconnect();
		}
	}

	public async Task<int> ExecuteAsync(DbCommand command)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));

		long t = 0;
		int connect = 0;
		try
		{
			connect = await _context.ConnectAsync().ConfigureAwait(false);
			command.Connection = _context.Connection;
			command.Transaction = _context.Transaction;
			t = _context.Audit.Start();
			var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
			return result;
		}
		catch (Exception flaw)
		{
			flaw.Add(nameof(command), command)
				.Add(t);
			throw;
		}
		finally
		{
			if (t != 0)
				_context.Audit.QueryEnd(command.CommandText, t);
			if (connect > 0)
				_context.Disconnect();
		}
	}

	public void Dispose()
	{
		_context.Dispose();
	}
}

static class ExceptionExtensions
{
	public static Exception Add(this Exception flaw, long time)
	{
		if (time != 0)
			flaw.Add("time", WatchTimer.ToString(WatchTimer.Query(time)));
		return flaw;
	}
}


