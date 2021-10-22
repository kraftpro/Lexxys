// Lexxys Infrastructural library.
// file: DatabaseLogWriter.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;

namespace Lexxys.Logging
{
	using Xml;

	class DatabaseLogWriter : ILogWriter, IDisposable
	{
		public const string DefaultSchema = "log";
		public const string DefaultTable = "App";
		private const string LogSource = "Lexxys.Logging.DatabaseLogWriter";
		public const string ConfigSection = "database.connection";

		private ConnectionStringInfo _connectionInfo;
		private string _schema;
		private string _table;
		private bool _dedicatedConnection;
		private bool _cloneCommand;
		private SqlCommand _insertInstanceCommand;
		private SqlCommand _insertEntryCommand;
		private SqlCommand _insertArgumentCommand;
		private SqlConnection _connection;
		private readonly LoggingRule _rule;

		public DatabaseLogWriter(string name, XmlLiteNode config)
		{
			if (config == null)
				config = XmlLiteNode.Empty;

			_connectionInfo = ConnectionStringInfo.Create(config.FirstOrDefault("connection")) ??
				Config.Default.GetValue<ConnectionStringInfo>(XmlTools.GetString(config["connection"], ConfigSection)).Value;
			if (_connectionInfo == null)
				LogWriter.WriteErrorMessage(LogSource, SR.CollectionIsEmpty(), null);

			_schema = __cleanRex.Replace(config["schema"] ?? "", "");
			if (_schema.Length == 0)
				_schema = DefaultSchema;
			_table = __cleanRex.Replace(config["table"] ?? "", "");
			if (_table.Length == 0)
				_table = DefaultTable;
			_dedicatedConnection = XmlTools.GetBoolean(config["dedicated"], false);
			_cloneCommand = XmlTools.GetBoolean(config["clone"], false);
			Name = name;
			_rule = LoggingRule.Create(config);
		}
		private static readonly Regex __cleanRex = new Regex(@"[\x00- '""\]\[\x7F\*/]");

		public string Name { get; }

		public string Target => _connectionInfo == null ? "Empty database": $"{_connectionInfo.Server}:{_connectionInfo.Database}.{_schema}.{_table}LogEntries";

		public bool WillWrite(string source, LogType type) => _rule.Contains(source, type);

		public void Write(IEnumerable<LogRecord> records)
		{
			if (records == null)
				return;

			if (_connectionInfo == null)
				return;

			try
			{
				SqlCommand instance = null;
				SqlCommand entry = null;
				SqlCommand arument = null;
				try
				{
					SqlConnection connection = OpenDatabaseCommection();
					entry = GetInsertEntryCommand();
					entry.Connection = connection;
					arument = GetInsertArgumentsCommand();
					arument.Connection = connection;

					foreach (LogRecord item in records)
					{
						LogRecord record = item;
						int instanceId = __instancesMap.GetOrAdd(
							new Tuple<string, string, int>(record.Context.MachineName, record.Context.DomainName, record.Context.ProcessId),
							o=> {
									if (instance == null)
									{
										instance = GetInsertInstanceCommand();
										instance.Connection = connection;
									}
									return InsertInstance(instance, record);
								});
							int entryId = InsertEntry(entry, instanceId, record);
							if (record.Data != null && record.Data.Count > 0)
								InsertArgument(arument, entryId, record.Data);
					}
				}
				finally
				{
					CloseDatabaseCommand(entry);
					CloseDatabaseCommand(arument);
					CloseDatabaseCommection();
				}
			}
			catch (Exception exception)
			{
				LogWriter.WriteErrorMessage("DatabaseLogWriter", exception);
			}
		}
		private static readonly ConcurrentDictionary<Tuple<string, string, int>, int> __instancesMap = new ConcurrentDictionary<Tuple<string, string, int>, int>();

		public void Open()
		{
			if (_connectionInfo == null)
			{
				LogWriter.WriteErrorMessage("DatabaseLogWriter", SR.ConnectionStringIsEmpty(), null);
				return;
			}

			DisposeResources();
			try
			{
				_insertInstanceCommand = CreateInsertInstanceCommand();
				_insertEntryCommand = CreateInsertEntryCommand();
				_insertArgumentCommand = CreateInsertArgumentsCommand();
				_connection = new SqlConnection(_connectionInfo.GetConnectionString());
				_connection.Open();
				TestLogTables(_connection, _schema, _table);
				if (!_dedicatedConnection)
					_connection.Close();
			}
			catch (Exception exception)
			{
				exception
					.Add("ConnectionString", _connectionInfo.ToString())
					.Add("Schema", _schema)
					.Add("Table", _table);
				LogWriter.WriteErrorMessage("DatabaseLogWriter", exception);
				DisposeResources();
			}
		}

		public void Close()
		{
			DisposeResources();
		}

		private void DisposeResources()
		{
			Interlocked.Exchange(ref _connection, null)?.Dispose();
			Interlocked.Exchange(ref _insertInstanceCommand, null)?.Dispose();
			Interlocked.Exchange(ref _insertEntryCommand, null)?.Dispose();
			Interlocked.Exchange(ref _insertArgumentCommand, null)?.Dispose();
		}

		private SqlCommand GetInsertInstanceCommand()
		{
			return _cloneCommand ? _insertInstanceCommand.Clone(): _insertInstanceCommand;
		}

		private SqlCommand GetInsertEntryCommand()
		{
			return _cloneCommand ? _insertEntryCommand.Clone(): _insertEntryCommand;
		}

		private SqlCommand GetInsertArgumentsCommand()
		{
			return _cloneCommand ? _insertArgumentCommand.Clone(): _insertArgumentCommand;
		}

		private SqlConnection OpenDatabaseCommection()
		{
			if (_connection.State != System.Data.ConnectionState.Open)
				_connection.Open();
			return _connection;
		}

		private void CloseDatabaseCommection()
		{
			if (!_dedicatedConnection)
				_connection.Close();
		}

		private void CloseDatabaseCommand(SqlCommand command)
		{
			if (!_cloneCommand)
				command?.Dispose();
		}

		private static int InsertInstance(SqlCommand command, LogRecord record)
		{
			command.Parameters[0].Value = record.Context.Timestamp;
			command.Parameters[1].Value = record.Context.MachineName;
			command.Parameters[2].Value = record.Context.DomainName;
			command.Parameters[3].Value = record.Context.ProcessId;
			command.ExecuteNonQuery();
			return (int)command.Parameters[4].Value;
		}


		private SqlCommand CreateInsertInstanceCommand()
		{
			var command = new SqlCommand(String.Format(CultureInfo.InvariantCulture, InsertInstanceCommandText, _schema, _table));
			command.Parameters.Add("@LocalTime", System.Data.SqlDbType.DateTime);
			command.Parameters.Add("@ComputerName", System.Data.SqlDbType.VarChar, 120);
			command.Parameters.Add("@DomainName", System.Data.SqlDbType.VarChar, 120);
			command.Parameters.Add("@ProcessId", System.Data.SqlDbType.Int);
			command.Parameters.Add("@ID", System.Data.SqlDbType.Int);
			command.Parameters["@ID"].Direction = System.Data.ParameterDirection.Output;
			return command;
		}

		private static int InsertEntry(SqlCommand command, int instanceId, LogRecord record)
		{
			command.Parameters[0].Value = record.Context.SequentialNumber;
			command.Parameters[1].Value = record.Context.Timestamp;
			command.Parameters[2].Value = instanceId;
			command.Parameters[3].Value = record.Source;
			command.Parameters[4].Value = record.Context.ThreadId;
			command.Parameters[5].Value = record.Context.ThreadSysId;
			command.Parameters[6].Value = (byte)record.LogType;
			command.Parameters[7].Value = (byte)record.RecordType;
			command.Parameters[8].Value = record.Indent;
			command.Parameters[9].Value = record.Message;
			command.ExecuteNonQuery();
			return (int)command.Parameters[10].Value;
		}

		private SqlCommand CreateInsertEntryCommand()
		{
			var command = new SqlCommand(String.Format(CultureInfo.InvariantCulture, InsertEntryCommandText, _schema, _table));
			command.Parameters.Add("@SeqNumber", System.Data.SqlDbType.Int);
			command.Parameters.Add("@LocalTime", System.Data.SqlDbType.DateTime);
			command.Parameters.Add("@Instance", System.Data.SqlDbType.Int);
			command.Parameters.Add("@Source", System.Data.SqlDbType.VarChar, 250);
			command.Parameters.Add("@ThreadId", System.Data.SqlDbType.Int);
			command.Parameters.Add("@ThreadSysId", System.Data.SqlDbType.Int);
			command.Parameters.Add("@LogType", System.Data.SqlDbType.TinyInt);
			command.Parameters.Add("@RecordType", System.Data.SqlDbType.TinyInt);
			command.Parameters.Add("@Indentlevel", System.Data.SqlDbType.Int);
			command.Parameters.Add("@Message", System.Data.SqlDbType.VarChar, -1);
			command.Parameters.Add("@ID", System.Data.SqlDbType.Int);
			command.Parameters["@ID"].Direction = System.Data.ParameterDirection.Output;
			return command;
		}

		private static void InsertArgument(SqlCommand command, int entryId, IDictionary arguments)
		{
			foreach (var key in arguments.Keys)
			{
				command.Parameters[0].Value = entryId;
				command.Parameters[1].Value = key.ToString();
				command.Parameters[2].Value = arguments[key].ToString();
				command.ExecuteNonQuery();
			}
		}

		private SqlCommand CreateInsertArgumentsCommand()
		{
			var command = new SqlCommand(String.Format(CultureInfo.InvariantCulture, InsertArgumentCommandText, _schema, _table));
			command.Parameters.Add("@Entry", System.Data.SqlDbType.Int);
			command.Parameters.Add("@ArgName", System.Data.SqlDbType.VarChar, 120);
			command.Parameters.Add("@ArgValue", System.Data.SqlDbType.VarChar, -1);
			return command;
		}

		private static void TestLogTables(SqlConnection connection, string schemaName, string logName)
		{
			using var command = new SqlCommand { Connection = connection };
			foreach (string statement in TestTablesCommandText)
			{
				command.CommandText = String.Format(CultureInfo.InvariantCulture, statement, schemaName, logName);
				command.ExecuteNonQuery();
			}
		}

		public void Dispose()
		{
			Close();
		}

		#region SQL Statements
		private static readonly string[] TestTablesCommandText =
		{
			@"
if (schema_id('{0}') is null)
	exec ('create schema [{0}]');
", @"if (object_id('[{0}].[{1}LogInstances]') is null)
begin
exec ('
create table [{0}].[{1}LogInstances]
	(
	ID int not null identity (1, 1)
		constraint [PK_{0}_{1}LogInstances] primary key,
	LocalTime datetime not null,
	ComputerName varchar(120) null,
	DomainName varchar(120) null,
	ProcessId int not null,
	)
');
exec dbo.alter_index '[{0}].[{1}LogInstances](unique)', 'ProcessId', 'ComputerName', 'DomainName';
end;
", @"if (object_id('[{0}].[{1}LogEntries]') is null)
exec ('
create table [{0}].[{1}LogEntries]
	(
	ID int not null identity (1, 1)
		constraint [PK_{0}_{1}LogEntries] primary key,
	SeqNumber int not null,
	LocalTime datetime not null,
	Instance int not null
		constraint [FK_{0}_{1}LogEntries_Instance]
		foreign key references [{0}].[{1}LogInstances] (ID),
	Source varchar(250) null,
	ThreadId int not null,
	ThreadSysId int not null,
	LogType tinyint not null,
	RecordType tinyint not null,
	Indentlevel int not null,
	Message varchar(max) null
	)
');
", @"if (object_id('[{0}].[{1}LogArguments]') is null)
exec ('
create table [{0}].[{1}LogArguments]
	(
	ID int not null identity (1, 1)
		constraint [PK_{0}_{1}LogArguments] primary key,
	Entry int not null
		constraint [FK_{0}_{1}LogArguments_Entry]
		foreign key references [{0}].[{1}LogEntries](ID),
	ArgName varchar(120) not null,
	ArgValue varchar(max)
	)
');
", @"if (object_id('[{0}].[Insert{1}LogInstance]') is null)
exec ('
create proc [{0}].[Insert{1}LogInstance]
	(
	@LocalTime datetime,
	@ComputerName varchar(120),
	@DomainName varchar(120),
	@ProcessId int,
	@ID int output
	)
as
	begin
	set @ID = (select ID
		from [{0}].[{1}LogInstances]
		where ComputerName=@ComputerName
			and DomainName=@DomainName
			and ProcessId=@ProcessId
		);
	if (@ID is null)
		begin
		insert into [{0}].[{1}LogInstances]
			( LocalTime, ComputerName, DomainName, ProcessId)
		values
			(@LocalTime,@ComputerName,@DomainName,@ProcessId);
		set @ID = scope_identity();
		end;
	end;
');
", @"if (object_id('[{0}].[Insert{1}LogEntry]') is null)
exec ('
create proc [{0}].[Insert{1}LogEntry]
	(
	@SeqNumber int,
	@LocalTime datetime,
	@Instance int,
	@Source varchar(250),
	@ThreadId int,
	@ThreadSysId int,
	@LogType tinyint,
	@RecordType tinyint,
	@Indentlevel int,
	@Message varchar(max),
	@ID int output
	)
as
	begin
	declare @proc int;
	insert into [{0}].[{1}LogEntries]
		( SeqNumber, LocalTime, Instance, Source, ThreadId, ThreadSysId, LogType, RecordType, Indentlevel, Message)
	values
		(@SeqNumber,@LocalTime,@Instance,@Source,@ThreadId,@ThreadSysId,@LogType,@RecordType,@Indentlevel,@Message);
	set @ID = scope_identity();
	end
');
", @"if (object_id('[{0}].[Insert{1}LogArgument]') is null)
exec ('
create proc [{0}].[Insert{1}LogArgument](@Entry int, @ArgName varchar(120), @ArgValue varchar(max))
as
	insert into [{0}].[{1}LogArguments]
		( Entry, ArgName, ArgValue)
	values
		(@Entry, @ArgName, @ArgValue);
');
"
		};

		private const string InsertInstanceCommandText = @"exec [{0}].[Insert{1}LogInstance] @LocalTime,@ComputerName,@DomainName,@ProcessId,@ID output;";
		private const string InsertEntryCommandText = @"exec [{0}].[Insert{1}LogEntry] @SeqNumber,@LocalTime,@Instance,@Source,@ThreadId,@ThreadSysId,@LogType,@RecordType,@Indentlevel,@Message,@ID output;";
		private const string InsertArgumentCommandText = @"exec [{0}].[Insert{1}LogArgument] @Entry,@ArgName,@ArgValue;";

		#endregion
	}
}
