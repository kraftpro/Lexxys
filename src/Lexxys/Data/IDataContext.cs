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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Data
{
	using Xml;

	public interface IDataContext: IDisposable
	{
		TimeSpan ConnectTime { get; }
		bool InTransation { get; }
		DateTime Now { get; }
		TimeSpan QueryTime { get; }
		DateTime Time { get; }
		TimeSpan TotalTime { get; }
		TimeSpan TransactTime { get; }

		/// <summary>
		/// Action to be executed when the database transaction is canceled.
		/// </summary>
		event Action Canceled;
		/// <summary>
		/// Action to be executed when the database transaction is committed.
		/// </summary>
		event Action Committed;

		/// <summary>
		/// Resets database statistics.
		/// </summary>
		void ResetStatistics();
		/// <summary>
		/// Sets timeout value for the next SQL command.
		/// </summary>
		/// <param name="timeout"></param>
		void SetQueryTimeout(TimeSpan timeout);
		/// <summary>
		/// Sets timeout value for all SQL commands in the disposable region.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="always"></param>
		/// <returns></returns>
		IContextHolder CommadTimeout(TimeSpan timeout, bool always = false);
		/// <summary>
		/// Stops timing calculation inside the disposable region.
		/// </summary>
		/// <returns></returns>
		IContextHolder NoTiming();
		/// <summary>
		/// Sets Dc.Now to be returning the same value inside the disposable region.
		/// </summary>
		/// <returns></returns>
		IContextHolder HoldTheMoment();
		/// <summary>
		/// Connects to the database and keeps the connection open inside the disposable region.
		/// </summary>
		/// <returns></returns>
		IContextHolder Connection();
		/// <summary>
		/// Opens a transaction context and optionally commits or rollbacks the transaction when the region has disposed of.
		/// </summary>
		/// <param name="autoCommit">Indicates that transaction should be committed when disposing</param>
		/// <param name="isolation">Desired transaction isolation level</param>
		/// <returns></returns>
		ITransactable Transaction(bool autoCommit = false, IsolationLevel isolation = 0);
		/// <summary>
		/// Decreases the number of active transactions and commits the database transaction when the number of transactions becomes zero.
		/// </summary>
		void Commit();
		/// <summary>
		/// Rollbacks the database transaction.
		/// </summary>
		void Rollback();
		/// <summary>
		/// Sets operation to be committed or rolled back along with the database transaction.
		/// </summary>
		/// <param name="key">Unique name of the operation.</param>
		/// <param name="factory">Function to produce the operation.</param>
		/// <returns></returns>
		ICommitAction SetCommitAction(object key, Func<ICommitAction> factory);
		/// <summary>
		/// Executes database command command.
		/// </summary>
		/// <param name="command">The database comment to be executed.</param>
		/// <returns>Number of records affected</returns>
		int Execute(DbCommand command);
		/// <summary>
		/// Executes SQL statement.
		/// </summary>
		/// <param name="statement">SQL statement to be executed.</param>
		/// <param name="parameters">Database parameters</param>
		/// <returns>Number of records affected</returns>
		int Execute(string statement, params DbParameter[] parameters);
		/// <summary>
		/// Executes database command command.
		/// </summary>
		/// <param name="command">The database comment to be executed.</param>
		/// <returns>Number of records affected</returns>
		Task<int> ExecuteAsync(DbCommand command);
		/// <summary>
		/// Executes SQL statement.
		/// </summary>
		/// <param name="statement">SQL statement to be executed.</param>
		/// <param name="parameters">Database parameters</param>
		/// <returns>Number of records affected</returns>
		Task<int> ExecuteAsync(string statement, params DbParameter[] parameters);
		#nullable disable
		List<T> GetList<T>(string query, params DbParameter[] parameters);
		Task<List<T>> GetListAsync<T>(string query, params DbParameter[] parameters);
		T GetValue<T>(string query, params DbParameter[] parameters);
		Task<T> GetValueAsync<T>(string query, params DbParameter[] parameters);
		T Map<T>(Func<DbCommand, T> mapper, string query, params DbParameter[] parameters);
		Task<T> MapAsync<T>(Func<DbCommand, Task<T>> mapper, string query, params DbParameter[] parameters);
		#nullable enable
		List<XmlLiteNode> ReadXml(string query, params DbParameter[] parameters);
		Task<List<XmlLiteNode>> ReadXmlAsync(string query, params DbParameter[] parameters);
		bool ReadXmlText(TextWriter text, string query, params DbParameter[] parameters);
		Task<bool> ReadXmlTextAsync(TextWriter text, string query, params DbParameter[] parameters);
		List<RowsCollection> Records(int count, string query, params DbParameter[] parameters);
		//DbCommand CreateCommand();
	}

	public static class DataContextExtensions
	{
		/// <summary>
		/// Sets operation to be committed or rolled back along with the database transaction.
		/// </summary>
		/// <typeparam name="T">Unique type of the operation.</typeparam>
		/// <param name="context">IDataContext</param>
		/// <param name="factory">Function to produce the operation.</param>
		/// <returns></returns>
		public static T SetCommitAction<T>(this IDataContext context, Func<T> factory) where T: ICommitAction
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			var value = context.SetCommitAction(typeof(T), () => factory());
			return value is T t ? t: throw new InvalidOperationException();
		}

		public static T GetValueOrDefault<T>(this IDataContext context, T @default, string query, params DbParameter[] parameters) where T: class
		{
			return context.GetValue<T>(query, parameters) ?? @default;
		}

		public static async Task<T> GetValueOrDefaultAsync<T>(this IDataContext context, T @default, string query, params DbParameter[] parameters) where T: class
		{
			return await context.GetValueAsync<T>(query, parameters).ConfigureAwait(false) ?? @default;
		}

		public static int Map(this IDataContext context, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			return Map(context, Int32.MaxValue, mapper, query, parameters);
		}

		public static Task<int> MapAsync(this IDataContext context, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			return context.MapAsync(Int32.MaxValue, mapper, query, parameters);
		}

		public static int Map(this IDataContext context, int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return context.Map(o => ActionMapper(o, limit, mapper), query, parameters);

			static int ActionMapper(DbCommand cmd, int limit, Action<IDataRecord> mapper)
			{
				using DbDataReader reader = cmd.ExecuteReader();
				int count = 0;
				while (count < limit && reader.Read())
				{
					++count;
					mapper(reader);
				}
				return count;
			}
		}

		public static Task<int> MapAsync(this IDataContext context, int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (mapper == null)
				throw new ArgumentNullException(nameof(mapper));
			return context.MapAsync(o => ActionMapper(o, limit, mapper), query, parameters);

			static async Task<int> ActionMapper(DbCommand cmd, int limit, Action<IDataRecord> mapper)
			{
				using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
				int count = 0;
				while (count < limit && await reader.ReadAsync().ConfigureAwait(false))
				{
					++count;
					mapper(reader);
				}
				return count;
			}
		}

	}
}