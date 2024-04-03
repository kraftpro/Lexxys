// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys.Data;

public interface IDataContextFactory
{
	IDataContext CreateContext(ConnectionStringInfo connectionInfo);
}