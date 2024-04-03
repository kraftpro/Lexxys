// Lexxys Infrastructural library.
// file: DcLocal.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Data;

namespace Lexxys.Data;
public interface IDataParameter
{
	ParameterDirection? Direction { get; set; }
	string Name { get; set; }
	int? Size { get; set; }
	DbType? Type { get; set; }
	object? Value { get; set; }
}