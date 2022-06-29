// Lexxys Infrastructural library.
// file: DatabaseLogWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

// #define TRACE_CONSOLE
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Logging;
using Xml;
using Data;

public class DatabaseLogWriter: ILogWriter, IDisposable
{
	public const int MaxInsertRowsCount = 1000;
	public const string DefaultSchema = "log";
	public const string DefaultTable = "App";
	private const string LogSource = "Lexxys.Logging.DatabaseLogWriter";
	public const string ConfigSection = "database.connection";

	private readonly string _server;
	private readonly string _database;
	private readonly string _schema;
	private readonly string _table;
	private readonly LoggingRule _rule;
	private readonly string _connectionString;
	private IDataContext? _dataContext;
	private int _errorsCount;

	public DatabaseLogWriter(string name, XmlLiteNode? config)
	{
		Name = name;
		config ??= XmlLiteNode.Empty;

		ConnectionStringInfo? connectionInfo = ConnectionStringInfo.Create(config.FirstOrDefault("connection")) ??
			Config.Current.GetValue<ConnectionStringInfo>(XmlTools.GetString(config["connection"], ConfigSection)).Value;

		if (connectionInfo == null)
		{
			_connectionString = _server = _database = _schema = _table = "?";
			_rule = LoggingRule.Empty;
			SystemLog.WriteErrorMessage(LogSource, SR.ConnectionStringIsEmpty(), null);
			return;
		}

		_dataContext = new DataContext(connectionInfo);
		_connectionString = connectionInfo.ToString();
		_server = connectionInfo.Server ?? "?";
		_database = connectionInfo.Database ?? "?";
		_schema = Clean(config["schema"], DefaultSchema);
		_table = Clean(config["table"], DefaultTable);
		_rule = LoggingRule.Create(config);

		static string Clean(string? val, string def) => (val = __cleanRex.Replace(val ?? "", "")).Length > 0 ? val : def;
	}
	private static readonly Regex __cleanRex = new(@"[\x00- '""\]\[\x7F\*/]");

	public string Name { get; }

	public string Target => $"{_server}:{_database}.{_schema}.{_table}LogEntries";

	public bool Accepts(string? source, LogType type) => _rule.Contains(source, type);

	private static int __xyz;

	public void Write(IEnumerable<LogRecord> records)
	{
		if (records == null || _dataContext == null || _errorsCount > 10)
			return;
		var text = new StringBuilder(8192);
		bool entry = true;
		int row = 0;
		int xyz = Interlocked.Increment(ref __xyz);
		int total = 0;
#if TRACE_CONSOLE
		Console.Write($"beg[{xyz}] ");
#endif
		try
		{
			foreach (var record in records)
			{
				int instanceId = __instancesMap.GetOrAdd((record.Context.MachineName, record.Context.DomainName, record.Context.ProcessId), InsertInstance);
				AppendInsertEntryStatement(instanceId, record);
				if (++row >= MaxInsertRowsCount)
				{
					total += row;
					_dataContext.Execute(text.ToString());
					text.Clear();
					entry = true;
					row = 0;
				}
			}
			if (text.Length > 0)
				_dataContext.Execute(text.ToString());
			_errorsCount = 0;
		}
		catch (Exception exception)
		{
			++_errorsCount;
			SystemLog.WriteErrorMessage("DatabaseLogWriter", exception);
#if TRACE_CONSOLE
			Console.WriteLine($"err[{xyz}] {_errorsCount} {exception.Message}");
#endif
		}
#if TRACE_CONSOLE
		Console.WriteLine($"end[{xyz}] {total}");
#endif
		void AppendInsertEntryStatement(int instanceId, LogRecord record)
		{
			if (entry)
			{
				text.Append(_insertEntryTemplate);
				entry = false;
			}
			else
			{
				text.Append(',');
			}
			text.Append("\n(")
				.Append(Dc.Value(record.Context.SequentialNumber)).Append(',')
				.Append(record.Context.UtcTimestamp.ToString(@"\'yyyyMMdd HH:mm:ss.fffffff\'")).Append(',')
				.Append(Dc.Value(instanceId)).Append(',')
				.Append(Dc.Value(record.Source)).Append(',')
				.Append(Dc.Value(record.Context.ThreadId)).Append(',')
				.Append(Dc.Value(record.Context.ThreadSysId)).Append(',')
				.Append(Dc.Value((int)record.LogType)).Append(',')
				.Append(Dc.Value((int)record.RecordType)).Append(',')
				.Append(Dc.Value(record.Indent)).Append(',')
				.Append(Dc.TextValue(record.Message)).Append(')');
			if (record.Data != null && record.Data.Count > 0)
			{
				text.Append(';').Append(_insertArgTemplate);
				int line = 0;
				foreach (DictionaryEntry item in record.Data)
				{
					if (++line >= MaxInsertRowsCount)
						break;
					text.Append("\n (@id,")
						.Append(Dc.Value(item.Key.ToString())).Append(',')
						.Append(Dc.TextValue(item.Value?.ToString())).Append(')');
				}
				text.Append(';');
				entry = true;
			}
		}
	}

	private int InsertInstance((string Machine, string Domain, int Process) instance)
	{
		Debug.Assert(_dataContext != null);
		var text = new StringBuilder(_insertInstanceTemplate).Append('(')
			.Append(DateTime.UtcNow.ToString(@"\'yyyyMMdd HH:mm:ss.fffffff\'")).Append(',')
			.Append(Dc.Value(instance.Machine)).Append(',')
			.Append(Dc.Value(instance.Domain)).Append(',')
			.Append(Dc.Value(instance.Process)).Append(')')
			.Append(";\nselect cast(scope_identity() as int);");
		return _dataContext!.GetValue<int>(text.ToString());
	}

	private static readonly ConcurrentDictionary<(string Machine, string Domain, int Process), int> __instancesMap = new ConcurrentDictionary<(string, string, int), int>();

	public void Open()
	{
		if (_dataContext == null)
		{
			SystemLog.WriteErrorMessage("DatabaseLogWriter", SR.ConnectionStringIsEmpty(), null);
			return;
		}
		try
		{
			//_connection = _dataContext.Connection();
			PrepareTables();
			PrepareTemplates();
		}
		catch (Exception flaw)
		{
			flaw.Add("ConnectionString", _connectionString)
				.Add("Schema", _schema)
				.Add("Table", _table);
			SystemLog.WriteErrorMessage("DatabaseLogWriter", flaw);
			Close();
		}
	}

	public void Close()
	{
		Interlocked.Exchange(ref _dataContext, null)?.Dispose();
	}

	public void Dispose()
	{
		Close();
	}

	private void PrepareTemplates()
	{
		const string InsertInstanceTemplate = "insert into [{S}].[{T}LogInstances] (LocalTime,ComputerName,DomainName,ProcessId) values ";
		const string InstertEntryTemplate = "\ninsert into[{S}].[{T}LogEntries] (SeqNumber,LocalTime,InstanceId,Source,ThreadId,ThreadSysId,LogType,RecordType,Indentlevel,Message) values";
		const string InstertArgTemplate = "\n declare @id int=scope_identity();\n insert into [{S}].[{T}LogArguments] (Entry,ArgName,ArgValue) values";

		_insertInstanceTemplate = InsertInstanceTemplate.Replace("{S}", _schema).Replace("{T}", _table);
		_insertEntryTemplate = InstertEntryTemplate.Replace("{S}", _schema).Replace("{T}", _table);
		_insertArgTemplate = InstertArgTemplate.Replace("{S}", _schema).Replace("{T}", _table);
	}
	private string? _insertInstanceTemplate;
	private string? _insertEntryTemplate;
	private string? _insertArgTemplate;

	private void PrepareTables()
	{
		Debug.Assert(_dataContext != null);
		foreach (string table in CreateTableTemplates)
		{
			_dataContext!.Execute(table.Replace("{S}", _schema).Replace("{T}", _table));
		}
	}

	private static readonly string[] CreateTableTemplates =
	{
		@"
if (schema_id('{S}') is null)
	exec ('create schema [{S}]');
", @"if (object_id('[{S}].[{T}LogInstances]') is null)
create table [{S}].[{T}LogInstances]
	(
	ID int not null identity (1, 1)
		constraint [PK_{S}_{T}LogInstances] primary key,
	LocalTime datetime2 not null,
	ComputerName varchar(120) null,
	DomainName varchar(120) null,
	ProcessId int not null,
	)
", @"if (object_id('[{S}].[{T}LogEntries]') is null)
create table [{S}].[{T}LogEntries]
	(
	ID int not null identity (1, 1)
		constraint [PK_{S}_{T}LogEntries] primary key,
	SeqNumber int not null,
	LocalTime datetime2 not null,
	InstanceId int not null,
		-- constraint [FK_{S}_{T}LogEntries_Instance]
		-- foreign key references [{S}].[{T}LogInstances] (ID),
	Source varchar(250) null,
	ThreadId int not null,
	ThreadSysId int not null,
	LogType tinyint not null,
	RecordType tinyint not null,
	Indentlevel int not null,
	Message nvarchar(max) null
	)
", @"if (object_id('[{S}].[{T}LogArguments]') is null)
create table [{S}].[{T}LogArguments]
	(
	ID int not null identity (1, 1)
		constraint [PK_{S}_{T}LogArguments] primary key,
	EntryId int not null,
	--	constraint [FK_{S}_{T}LogArguments_Entry]
	--	foreign key references [{S}].[{T}LogEntries](ID),
	ArgName varchar(120) not null,
	ArgValue nvarchar(max)
	)
",
	};
}
