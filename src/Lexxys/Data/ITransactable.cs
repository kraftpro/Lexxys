// Lexxys Infrastructural library.
// file: Dc.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys.Data
{
	public interface ITransactable: IContextHolder
	{
		void Commit();
		void Rollback();
	}
}
