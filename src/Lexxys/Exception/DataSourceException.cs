// Lexxys Infrastructural library.
// file: DataSourceException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;

namespace Lexxys
{
	[Serializable]
	public class DataSourceException: Exception
	{
		public DataSourceException()
		{
		}

		public DataSourceException(string? message)
			: base(message)
		{
		}

		public DataSourceException(string? message, string? connectionInfo)
			: base(message)
		{
			base.Data["connection"] = connectionInfo;
		}

		public DataSourceException(string? message, Exception? exception)
			: base(message, exception)
		{
		}

		public DataSourceException(string? message, string? connectionInfo, Exception? exception)
			: base(message, exception)
		{
			base.Data["connection"] = connectionInfo;
		}

		public DataSourceException(string? message, string? connectionInfo, string? statement, Exception? exception)
			: base(message, exception)
		{
			if (connectionInfo != null && connectionInfo.Length > 0)
				base.Data["connection"] = connectionInfo;

			base.Data["statement"] = statement;
		}

		public DataSourceException(string? message, string? connectionInfo, string? statement, IEnumerable<DbParameter>? dbParameters, Exception? exception)
			: base(message, exception)
		{
			if (connectionInfo != null && connectionInfo.Length > 0)
				base.Data["connection"] = connectionInfo;

			base.Data["statement"] = statement;
			if (dbParameters != null)
			{
				foreach (var item in dbParameters)
				{
					if (item != null)
						this.Add(item.ParameterName, item.Value);
				}
			}
		}

		protected DataSourceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}


