// Lexxys Infrastructural library.
// file: TasksExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public static class TasksExtensions
{
	public static void Forget(this Task task)
	{
		if (task is null) throw new ArgumentNullException(nameof(task));
		if (task.IsCompleted && !task.IsFaulted) return;
		task.ConfigureAwait(false);
		var t = task;
		_ = Task.Run(async () => await t);
	}
}
