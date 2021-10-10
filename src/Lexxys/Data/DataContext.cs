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
using Lexxys;

#nullable enable

namespace Lexxys.Data
{
	public sealed class DataContext: IDataContext, IDisposable
	{
		private static readonly IValue<ConnectionStringInfo?> __globalConnetionString = Config.Default.GetSection<ConnectionStringInfo>(Dc.ConfigSection, null);

		private readonly DataContextImplementation _context;


		public DataContext(): this(__globalConnetionString?.Value)
		{
		}

		public DataContext(ConnectionStringInfo? connectionInfo)
		{
			if (connectionInfo == null)
				throw new ArgumentNullException(nameof(connectionInfo));
			_context = new DataContextImplementation(connectionInfo);
		}

		public TimeSpan ConnectTime => _context.ConnectTime;

		public TimeSpan TransactTime => _context.TransactTime;

		public TimeSpan QueryTime => _context.QueryTime;

		public TimeSpan TotalTime => _context.TotalTime;

		public event Action Committed
		{
			add => _context.Committed += value;
			remove { }
		}

		public event Action Canceled
		{
			add => _context.Cancelled += value;
			remove { }
		}

		public ICommitAction SetCommitAction(object key, Func<ICommitAction> factory) => _context.SetCommitAction(key, factory);

		public bool InTransation => _context.TransactionsCount > 0;

		public DateTime Time => _context.Time;

		public DateTime Now => _context.Now;

		public IDisposable? HoldTheMoment() => _context.LockNow(_context.Time) ? new Dc.TimeHolder(_context) : null;

		public IDisposable Connection() => new Dc.Connecting(_context);

		public ITransactable Transaction(bool autoCommit = false, IsolationLevel isolation = default) => new Dc.Transacting(_context, autoCommit, isolation);

		public IDisposable NoTiming() => new Dc.TimingLocker(_context);

		public void Commit() => _context.Commit();

		public void Rollback() => _context.Rollback();

		public void SetQueryTimeout(TimeSpan timeout) => _context.CommandTimeout = timeout;

		public IDisposable? CommadTimeout(TimeSpan timeout, bool always = false) => always || _context.CommandTimeout < timeout ? new Dc.TimeoutLocker(_context, timeout) : null;

		public void ResetStatistics() => _context.ResetStatistics();

		public T? GetValue<T>(string query, params DbParameter[] parameters) => Map(Dc.ValueMapper<T>, query, parameters);

		public Task<T> GetValueAsync<T>(string query, params DbParameter[] parameters) => MapAsync(Dc.ValueMapperAsync<T>, query, parameters);

		public List<T> GetList<T>(string query, params DbParameter[] parameters) => Map(Dc.ListMapper<T>, query, parameters);

		public Task<List<T>> GetListAsync<T>(string query, params DbParameter[] parameters) => MapAsync(Dc.ListMapperAsync<T>, query, parameters);

		public bool ReadXmlText(TextWriter text, string query, params DbParameter[] parameters) => Map(o => Dc.XmlTextMapper(text, o), query, parameters);

		public Task<bool> ReadXmlTextAsync(TextWriter text, string query, params DbParameter[] parameters) => MapAsync(o => Dc.XmlTextMapperAsync(text, o), query, parameters);

		public List<Xml.XmlLiteNode> ReadXml(string query, params DbParameter[] parameters) => Map(Dc.XmlMapper, query, parameters);

		public Task<List<Xml.XmlLiteNode>> ReadXmlAsync(string query, params DbParameter[] parameters) => MapAsync(Dc.XmlMapperAsync, query, parameters);

		public List<RowsCollection> Records(int count, string query, params DbParameter[] parameters)
		{
			long t = 0;
			try
			{
				t = DataContextImplementation.TimingBegin();
				var result = _context.Records(count, query, parameters);
				_context.TimingEnd(query, t);
				return result;
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(count), count)
					.Add(nameof(query), query);
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				flaw.Add(t);
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
					using DbCommand cmd = _context.Command(query);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataContextImplementation.TimingBegin();
					T result = mapper(cmd);
					_context.TimingEnd(query, t);
					return result;
				}
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(query), query)
					.Add("type", typeof(T));
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				flaw.Add(t);
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
			bool connected = false;
			try
			{
				await _context.ConnectAsync();
				connected = true;
				using DbCommand cmd = _context.Command(query);
				if (parameters != null && parameters.Length > 0)
					cmd.Parameters.AddRange(parameters);
				t = DataContextImplementation.TimingBegin();
				var result = await mapper(cmd).ConfigureAwait(false);
				return result;
			}
			catch (Exception flaw)
			{
				flaw.Add(nameof(query), query)
					.Add("type", typeof(T));
				if (parameters != null && parameters.Length > 0)
				{
					foreach (var item in parameters)
					{
						if (item != null)
							flaw.Add(item.ParameterName, $"{item.DbType}, {item.Value}.");
					}
				}
				flaw.Add(t);
				throw;
			}
			finally
			{
				if (t != 0)
					_context.TimingEnd(query, t);
				if (connected)
					_context.Disconnect();
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
					command.Connection = _context.Connection;
					command.Transaction = _context.Transaction;
					t = DataContextImplementation.TimingBegin();
					var result = command.ExecuteNonQuery();
					_context.TimingEnd(command.CommandText, t);
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
			bool connected = false;
			try
			{
				await _context.ConnectAsync();
				connected = true;
				command.Connection = _context.Connection;
				command.Transaction = _context.Transaction;
				t = DataContextImplementation.TimingBegin();
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
					_context.TimingEnd(command.CommandText, t);
				if (connected)
					_context.Disconnect();
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
					using DbCommand cmd = _context.Command(statement);
					if (parameters != null && parameters.Length > 0)
						cmd.Parameters.AddRange(parameters);
					t = DataContextImplementation.TimingBegin();
					int result = cmd.ExecuteNonQuery();
					_context.TimingEnd(statement, t);
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
			bool connected = false;
			try
			{
				await _context.ConnectAsync();
				connected = true;
				using DbCommand cmd = _context.Command(statement);
				if (parameters != null && parameters.Length > 0)
					cmd.Parameters.AddRange(parameters);
				t = DataContextImplementation.TimingBegin();
				int result = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				_context.TimingEnd(statement, t);
				return result;
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
			finally
			{
				if (t != 0)
					_context.TimingEnd(statement, t);
				if (connected)
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
}


