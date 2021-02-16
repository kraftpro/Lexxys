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
using System.Threading.Tasks;
using Lexxys.Xml;

namespace Lexxys.Data
{
	public interface IDataContext: IDisposable
	{
		TimeSpan ConnectTime { get; }
		bool InTransation { get; }
		DateTime Now { get; }
		TimeSpan QueryTime { get; }
		DateTime Time { get; }
		TimeSpan TotalTime { get; }
		TimeSpan TransactTime { get; }

		event Action Cancelled;
		event Action Committed;

		IDisposable CommadTimeout(TimeSpan timeout, bool always = false);
		void Commit();
		IDisposable Connection();
		void Dispose();
		int Execute(DbCommand command);
		int Execute(string statement, params DbParameter[] parameters);
		Task<int> ExecuteAsync(DbCommand command);
		Task<int> ExecuteAsync(string statement, params DbParameter[] parameters);
		List<T> GetList<T>(string query, params DbParameter[] parameters);
		Task<List<T>> GetListAsync<T>(string query, params DbParameter[] parameters);
		T GetValue<T>(string query, params DbParameter[] parameters);
		Task<T> GetValueAsync<T>(string query, params DbParameter[] parameters);
		T GetValueOrDefault<T>(T @default, string query, params DbParameter[] parameters) where T : class;
		Task<T> GetValueOrDefaultAsync<T>(T @default, string query, params DbParameter[] parameters) where T : class;
		IDisposable HoldTheMoment();
		int Map(Action<IDataRecord> mapper, string query, params DbParameter[] parameters);
		int Map(int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters);
		T Map<T>(Func<DbCommand, T> mapper, string query, params DbParameter[] parameters);
		Task<int> MapAsync(Action<IDataRecord> mapper, string query, params DbParameter[] parameters);
		Task<int> MapAsync(int limit, Action<IDataRecord> mapper, string query, params DbParameter[] parameters);
		Task<T> MapAsync<T>(Func<DbCommand, Task<T>> mapper, string query, params DbParameter[] parameters);
		IDisposable NoTiming();
		List<XmlLiteNode> ReadXml(string query, params DbParameter[] parameters);
		Task<List<XmlLiteNode>> ReadXmlAsync(string query, params DbParameter[] parameters);
		bool ReadXmlText(TextWriter text, string query, params DbParameter[] parameters);
		Task<bool> ReadXmlTextAsync(TextWriter text, string query, params DbParameter[] parameters);
		List<RowsCollection> Records(int count, string query, params DbParameter[] parameters);
		void ResetStatistics();
		void Rollback();
		ICommitAction SetCommitAction(object key, Func<ICommitAction> factory);
		T SetCommitAction<T>(Func<T> factory) where T : ICommitAction;
		void SetQueryTimeout(TimeSpan timeout);
		ITransactable Transaction(bool autoCommit = false, IsolationLevel isolation = 0);
	}
}