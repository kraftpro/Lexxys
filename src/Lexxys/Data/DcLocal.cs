// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Data
{
	public sealed class DataContext: IDisposable
	{
		private static readonly IOptions<ConnectionStringInfo> __globalConnetionString = Config.GetOptions<ConnectionStringInfo>(Dc.ConfigSection);

		private readonly ConnectionStringInfo _connectionInfo;
		private DataDriver _context;

		public DataContext()
		{
			_connectionInfo = __globalConnetionString.Value;
		}

		public DataContext(ConnectionStringInfo connectionInfo)
		{
			_connectionInfo = connectionInfo;
		}

		public TimeSpan ConnectTime => Driver.ConnectTime;

		public TimeSpan TransactTime => Driver.TransactTime;

		public TimeSpan QueryTime => Driver.QueryTime;

		public TimeSpan TotalTime => Driver.TotalTime;

		private DataDriver Driver => _context ??= new DataDriver(_connectionInfo);

		public event Action Committed
		{
			add => Driver.Committed += value;
			remove { }
		}

		public event Action Cancelled
		{
			add => Driver.Cancelled += value;
			remove { }
		}

		public T SetCommitAction<T>(Func<T> factory) where T : ICommitAction => Driver.SetCommitAction(factory);

		public ICommitAction SetCommitAction(object key, Func<ICommitAction> factory) => Driver.SetCommitAction(key, factory);

		public bool InTransation => Driver.TransactionsCount > 0;

		public DateTime Time => Driver.Time;

		public DateTime Now => Driver.Now;

		public IDisposable HoldTheMoment()
		{
			return Driver.LockNow(Driver.Time) ? new Dc.TimeHolder(Driver) : null;
		}

		public IDisposable Connection()
		{
			return new Dc.Connecting(Driver);
		}

		public ITransactable Transaction(bool autoCommit = false, IsolationLevel isolation = default)
		{
			return new Dc.Transacting(Driver, autoCommit, isolation);
		}

		public IDisposable NoTiming()
		{
			return new Dc.TimingLocker(Driver);
		}

		public void Commit()
		{
			Driver.Commit();
		}

		public void Rollback()
		{
			Driver.Rollback();
		}

		public void SetQueryTimeout(TimeSpan timeout)
		{
			Driver.CommandTimeout = timeout;
		}

		public IDisposable CommadTimeout(TimeSpan timeout, bool always = false)
		{
			return always || Driver.CommandTimeout < timeout ? new Dc.TimeoutLocker(Driver, timeout) : null;
		}

		public void ResetStatistics()
		{
			Driver.ResetStatistics();
		}

		public T GetValue<T>(string query, params DbParameter[] parameters)
		{
			return Map(Dc.ValueMapper<T>, query, parameters);
		}

		public Task<T> GetValueAsync<T>(string query, params DbParameter[] parameters)
		{
			return MapAsync(Dc.ValueMapperAsync<T>, query, parameters);
		}

		public T GetValueOrDefault<T>(T @default, string query, params DbParameter[] parameters) where T: class
		{
			return GetValue<T>(query, parameters) ?? @default;
		}

		public async Task<T> GetValueOrDefaultAsync<T>(T @default, string query, params DbParameter[] parameters) where T : class
		{
			return await GetValueAsync<T>(query, parameters).ConfigureAwait(false) ?? @default;
		}

		public List<T> GetList<T>(string query, params DbParameter[] parameters)
		{
			return Map(Dc.ListMapper<T>, query, parameters);
		}

		public Task<List<T>> GetListAsync<T>(string query, params DbParameter[] parameters)
		{
			return MapAsync(Dc.ListMapperAsync<T>, query, parameters);
		}

		public bool ReadXmlText(TextWriter text, string query, params DbParameter[] parameters)
		{
			return Map(o => Dc.XmlTextMapper(text, o), query, parameters);
		}

		public Task<bool> ReadXmlTextAsync(TextWriter text, string query, params DbParameter[] parameters)
		{
			return MapAsync(o => Dc.XmlTextMapperAsync(text, o), query, parameters);
		}

		public List<Xml.XmlLiteNode> ReadXml(string query, params DbParameter[] parameters)
		{
			return Map(Dc.XmlMapper, query, parameters);
		}

		public Task<List<Xml.XmlLiteNode>> ReadXmlAsync(string query, params DbParameter[] parameters)
		{
			return MapAsync(Dc.XmlMapperAsync, query, parameters);
		}

		public List<RowsCollection> Records(int count, string query, params DbParameter[] parameters)
		{
			long t = 0;
			try
			{
				t = DataDriver.TimingBegin();
				var result = Driver.Records(count, query, parameters);
				Driver.TimingEnd(query, t);
				return result;
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(count), count)
					.Add(nameof(query), query)
					.Add(t);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				throw;
			}
		}

		public int Map(int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return Map(o => Dc.ActionMapper(o, limit, mapper), query, parameters);
		}

		public Task<int> MapAsync(int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return MapAsync(o => Dc.ActionMapperAsync(o, limit, mapper), query, parameters);
		}

		public int Map(Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return Map(o => Dc.ActionMapper(o, Int32.MaxValue, mapper), query, parameters);
		}

		public Task<int> MapAsync(Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return MapAsync(o => Dc.ActionMapperAsync(o, Int32.MaxValue, mapper), query, parameters);
		}

		public T Map<T>(Func<DbCommand, T> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			if (query == null)
				throw new ArgumentNullException(nameof(query));

			long t = 0;
			try
			{
				using (Connection())
				{
					using DbCommand cmd = Driver.Command(query);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataDriver.TimingBegin();
					T result = mapper(cmd);
					Driver.TimingEnd(query, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(query), query)
					.Add("type", typeof(T))
					.Add(t);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				throw;
			}
		}

		public async Task<T> MapAsync<T>(Func<DbCommand, Task<T>> mapper, string query, params DbParameter[] parameters)
		{
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			if (query == null)
				throw new ArgumentNullException(nameof(query));

			long t = 0;
			try
			{
				using (Connection())
				{
					using DbCommand cmd = Driver.Command(query);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataDriver.TimingBegin();
					var result = await mapper(cmd).ConfigureAwait(false);
					Driver.TimingEnd(query, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(query), query)
					.Add("type", typeof(T))
					.Add(t);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				throw;
			}
		}


		public int Execute(DbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			long t = 0;
			try
			{
				using (Connection())
				{
					command.Connection = Driver.Connection;
					command.Transaction = Driver.Transaction;
					t = DataDriver.TimingBegin();
					var result = command.ExecuteNonQuery();
					Driver.TimingEnd(command.CommandText, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(command), command)
					.Add(t);
				throw;
			}
		}

		public async Task<int> ExecuteAsync(DbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			long t = 0;
			try
			{
				using (Connection())
				{
					command.Connection = Driver.Connection;
					command.Transaction = Driver.Transaction;
					t = DataDriver.TimingBegin();
					var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
					Driver.TimingEnd(command.CommandText, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(command), command)
					.Add(t);
				throw;
			}
		}

		public int Execute(string statement, params DbParameter[] parameters)
		{
			if (statement == null)
				throw new ArgumentNullException(nameof(statement));

			long t = 0;
			try
			{
				using (Connection())
				{
					using DbCommand cmd = Driver.Command(statement);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataDriver.TimingBegin();
					int result = cmd.ExecuteNonQuery();
					Driver.TimingEnd(statement, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(statement), statement)
					.Add(t);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				throw;
			}
		}

		public async Task<int> ExecuteAsync(string statement, params DbParameter[] parameters)
		{
			if (statement == null)
				throw new ArgumentNullException(nameof(statement));

			long t = 0;
			try
			{
				using (Connection())
				{
					using DbCommand cmd = Driver.Command(statement);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataDriver.TimingBegin();
					int result = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
					Driver.TimingEnd(statement, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(statement), statement)
					.Add(t);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				throw;
			}
		}

		public void Dispose()
		{
			_context?.Dispose();
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
}


