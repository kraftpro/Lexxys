// Lexxys Infrastructural library.
// file: ResultValueExtensions.Act.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

public static partial class ResultValueExtensions
{
	extension<T>(ResultValue<T> result)
	{
		public ResultValue<T> Act(Action<T> action)
		{
			if (result.IsSuccess)
				action(result.Value);
			return result;
		}

		public async Task<ResultValue<T>> Act(Func<T, Task> action)
		{
			if (result.IsSuccess)
				await action(result.Value);
			return result;
		}

		public ResultValue<T> Act(bool condition, Action<T> action) => condition ? result.Act(action): result;

		public async Task<ResultValue<T>> Act(bool condition, Func<T, Task> action)
		{
			if (condition && result.IsSuccess)
				await action(result.Value);
			return result;
		}

		public ResultValue<T> Act(Func<T, bool> predicate, Action<T> action)
		{
			if (result.IsSuccess && predicate(result.Value))
				action(result.Value);
			return result;
		}

		public async Task<ResultValue<T>> Act(Func<T, bool> predicate, Func<T, Task> action)
		{
			if (result.IsSuccess && predicate(result.Value))
				await action(result.Value).ConfigureAwait(false);
			return result;
		}
	}

	extension<T>(Task<ResultValue<T>> result)
	{
		public async Task<ResultValue<T>> Act(Action<T> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess)
				action(r.Value);
			return r;
		}
		public async Task<ResultValue<T>> Act(Func<T, Task> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess)
				await action(r.Value).ConfigureAwait(false);
			return r;
		}

		public Task<ResultValue<T>> Act(bool condition, Action<T> action) => condition ? result.Act(action): result;
		public Task<ResultValue<T>> Act(bool condition, Func<T, Task> action) => condition ? result.Act(action): result;
		public async Task<ResultValue<T>> Act(Func<T, bool> predicate, Action<T> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess && predicate(r.Value))
				action(r.Value);
			return r;
		}

		public async Task<ResultValue<T>> Act(Func<T, bool> predicate, Func<T, Task> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess && predicate(r.Value))
				await action(r.Value).ConfigureAwait(false);
			return r;
		}
	}

	extension<T>(ValueTask<ResultValue<T>> result)
	{
		public async ValueTask<ResultValue<T>> Act(Action<T> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess)
				action(r.Value);
			return r;
		}
		public async ValueTask<ResultValue<T>> Act(Func<T, ValueTask> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess)
				await action(r.Value).ConfigureAwait(false);
			return r;
		}

		public ValueTask<ResultValue<T>> Act(bool condition, Action<T> action) => condition ? result.Act(action): result;
		public ValueTask<ResultValue<T>> Act(bool condition, Func<T, ValueTask> action) => condition ? result.Act(action): result;
		public async ValueTask<ResultValue<T>> Act(Func<T, bool> predicate, Action<T> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess && predicate(r.Value))
				action(r.Value);
			return r;
		}

		public async ValueTask<ResultValue<T>> Act(Func<T, bool> predicate, Func<T, ValueTask> action)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsSuccess && predicate(r.Value))
				await action(r.Value).ConfigureAwait(false);
			return r;
		}
	}
}
