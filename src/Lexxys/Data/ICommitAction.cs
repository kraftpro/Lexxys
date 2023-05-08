// Lexxys Infrastructural library.
// file: DataContext.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Data;

public interface ICommitAction
{
	void Commit();
	void Rollback();
}
