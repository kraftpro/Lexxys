// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

#nullable enable

namespace Lexxys.Data
{
	public interface IContextHolder: IDisposable
	{
		IDataContext Context { get; }
	}
}
