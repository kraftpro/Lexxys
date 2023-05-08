﻿// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data;
using System.Data.Common;

#pragma warning disable CA1003 // Use generic event handler instances

namespace Lexxys.Data;

public interface IDataContext: IDisposable
{
	TimeSpan ConnectTime { get; }
	bool InTransaction { get; }
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
	IContextHolder CommandTimeout(TimeSpan timeout, bool always = false);
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
	/// Creates a new <see cref="DbCommand"/> for the specified <paramref name="statement"/> and <paramref name="parameters"/>.
	/// </summary>
	/// <param name="statement">The SQL statement.</param>
	/// <param name="parameters">Optional <see cref="DbCommand"/> parameters.</param>
	/// <returns></returns>
	DbCommand Command(string statement, params DataParameter[] parameters);
	/// <summary>
	/// Gets or sets operation associated with the <paramref name="key"/> to be committed or rolled back along with the database transaction.
	/// </summary>
	/// <param name="key">Unique name of the operation.</param>
	/// <param name="factory">Function to produce the operation.</param>
	/// <returns></returns>
	ICommitAction GetOrSetCommitAction(object key, Func<ICommitAction> factory);
	/// <summary>
	/// Executes database command command.
	/// </summary>
	/// <param name="command">The database comment to be executed.</param>
	/// <returns>Number of records affected</returns>
	int Execute(DbCommand command);
	/// <summary>
	/// Executes database command command.
	/// </summary>
	/// <param name="command">The database comment to be executed.</param>
	/// <returns>Number of records affected</returns>
	Task<int> ExecuteAsync(DbCommand command);

	T Map<T>(Func<DbCommand, T> mapper, DbCommand command);

	Task<T> MapAsync<T>(Func<DbCommand, Task<T>> mapper, DbCommand command);

	IDataContext Clone();
}