﻿// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Lexxys.Data
{
	public class DataParameter
	{
		public DataParameter(string name, object? value = null, DbType? type = null, int? size = null)
		{
			Name = name;
			Value = value;
			Type = type;
			Size = size;
		}

		public string Name { get; set; }
		public object? Value { get; set; }
		public DbType? Type { get; set; }
		public int? Size { get; set; }
		public ParameterDirection? Direction { get; set; }
	}

	public static class DataParameterExtensions
	{
		public static DbCommand WithParameters(this DbCommand command, params DataParameter[] parameters) => WithParameters(command, (IEnumerable<DataParameter>)parameters);

		public static DbCommand WithParameters(this DbCommand command, IEnumerable<DataParameter>? parameters, bool append = false)
		{
			if (command is null)
				throw new ArgumentNullException(nameof(command));
			if (!append)
				command.Parameters.Clear();
			if (parameters == null)
				return command;
			foreach (var p in parameters)
			{
				if (p == null)
					continue;
				var q = command.CreateParameter();
				q.ParameterName = p.Name;
				q.Value = p.Value;
				if (p.Size != null)
					q.Size = p.Size.GetValueOrDefault();
				if (p.Type != null)
					q.DbType = p.Type.GetValueOrDefault();
				if (p.Direction != null)
					q.Direction = p.Direction.GetValueOrDefault();
				command.Parameters.Add(q);
			}
			return command;
		}

		public static void SetOutput(this DbCommand command, IReadOnlyList<DataParameter>? parameters)
		{
			if (command is null)
				throw new ArgumentNullException(nameof(command));
			if (parameters == null || parameters.Count == 0)
				return;

			foreach (var parameter in parameters)
			{
				if (parameter.Direction is ParameterDirection.InputOutput or ParameterDirection.Output or ParameterDirection.ReturnValue &&
					command.Parameters.Contains(parameter.Name))
					parameter.Value = command.Parameters[parameter.Name].Value;
			}
		}
	}
}


