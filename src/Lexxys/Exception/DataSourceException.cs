// Lexxys Infrastructural library.
// file: DataSourceException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data.Common;
using System.Runtime.Serialization;

namespace Lexxys;

[Serializable]
public class DataSourceException: Exception
{
	public const string DataConnectionName = "connection";
	public const string DataStatementName = "statement";
	public const string DataParameterPrefix = "parameter.";

	public DataSourceException()
	{
	}

	public DataSourceException(string? message): base(message)
	{
	}

	public DataSourceException(string? message, string? connectionInfo): base(message)
	{
		base.Data[DataConnectionName] = connectionInfo;
	}

	public DataSourceException(string? message, Exception? exception): base(message, exception)
	{
	}

	public DataSourceException(string? message, string? connectionInfo, Exception? exception): base(message, exception)
	{
		if (connectionInfo is { Length: > 0 })
			base.Data[DataConnectionName] = connectionInfo;
	}

	public DataSourceException(string? message, string? connectionInfo, string? statement, Exception? exception): base(message, exception)
	{
		if (connectionInfo is { Length: >0 })
			base.Data[DataConnectionName] = connectionInfo;
		if (statement is { Length: > 0 })
			base.Data[DataStatementName] = statement;
	}

	public DataSourceException(string? message, string? connectionInfo, string? statement, IEnumerable<DbParameter?>? dbParameters, Exception? exception): base(message, exception)
	{
		if (connectionInfo is { Length: >0 })
			base.Data[DataConnectionName] = connectionInfo;
		if (statement is { Length: > 0 })
			base.Data[DataStatementName] = statement;

		if (dbParameters != null)
		{
			foreach (var item in dbParameters)
			{
				if (item != null)
					this.Add(DataParameterPrefix + item.ParameterName, item.Value);
			}
		}
	}

#if !NET8_0_OR_GREATER
	protected DataSourceException(SerializationInfo info, StreamingContext context): base(info, context)
	{
	}
#endif
}


