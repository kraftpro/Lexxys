// Lexxys Infrastructural library.
// file: DatabaseQuery.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Lexxys.Configuration
{
	internal class DatabaseQuery
	{
		private readonly string _connectionString;

		public DatabaseQuery(ConnectionStringInfo connection)
		{
			if (connection == null)
				throw EX.ArgumentNull("connection");
			_connectionString = connection.GetConnectionString();
		}

		public List<T> Execute<T>(string statement, int estimatedNumber, Func<IDataRecord, T> action)
		{
			using var cn = new SqlConnection(_connectionString);
			cn.Open();
			using var c = new SqlCommand(statement, cn);
			using SqlDataReader reader = c.ExecuteReader(CommandBehavior.CloseConnection);
			var result = new List<T>(estimatedNumber);
			while (reader.Read())
			{
				result.Add(action(reader));
			}
			return result;
		}
	}
}


