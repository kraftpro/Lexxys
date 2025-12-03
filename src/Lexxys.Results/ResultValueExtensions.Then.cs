namespace Lexxys;

public static partial class ResultValueExtensions
{
	extension<TIn>(ResultValue<TIn> result)
	{
		/// <summary>
		/// Returns the result of invoking the specified function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain operations that depend on the successful result of a previous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure. If the result is successful, its value will be passed to the specified function; otherwise, the error will be propagated.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		public ResultValue<TOut> Then<TOut>(Func<TIn, ResultValue<TOut>> func)
			=> result.IsSuccess ? func(result.Value): result.Error;

		public ResultValue<TOut> Then<TOut>(Func<TIn, TOut> func) => result.IsSuccess ? func(result.Value): result.Error;

		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, Task<TOut>> func) => result.IsSuccess ? (await func(result.Value)).AsResultValue(): result.Error;

		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, ValueTask<TOut>> func) => result.IsSuccess ? (await func(result.Value)).AsResultValue(): result.Error;

		/// <summary>
		/// Returns the result of invoking the specified asynchronous function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain asynchronous operations that depend on the successful result of a previous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure. If the result is successful, its value will be passed to the specified function; otherwise, the error will be propagated.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, Task<ResultValue<TOut>>> func)
			=> result.IsSuccess ? await func(result.Value).ConfigureAwait(false): result.Error;

		public async ValueTask<ResultValue<TOut>> Then<TOut>(Func<TIn, ValueTask<ResultValue<TOut>>> func)
			=> result.IsSuccess ? await func(result.Value).ConfigureAwait(false) : result.Error;
	}

	extension<TIn>(Task<ResultValue<TIn>> result)
	{
		/// <summary>
		/// Returns the result of invoking the specified function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain operations that depend on the successful result of a previous asynchronous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The task representing the result to evaluate for success or failure. If the result is successful, its value will be passed to the specified function; otherwise, the error will be propagated.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, ResultValue<TOut>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? func(r.Value): r.Error;
		}

		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, TOut> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? func(r.Value): r.Error;
		}

		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, Task<TOut>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? await func(r.Value).ConfigureAwait(false): r.Error;
		}

		/// <summary>
		/// Returns the result of invoking the specified asynchronous function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain asynchronous operations that depend on the successful result of a previous asynchronous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The task representing the result to evaluate for success or failure. If the result is successful, its value will be passed to the specified function; otherwise, the error will be propagated.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		public async Task<ResultValue<TOut>> Then<TOut>(Func<TIn, Task<ResultValue<TOut>>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? await func(r.Value).ConfigureAwait(false): r.Error;
		}
	}

	extension<TIn>(ValueTask<ResultValue<TIn>> result)
	{
		/// <summary>
		/// Returns the result of invoking the specified asynchronous function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain asynchronous operations that depend on the successful result of a previous asynchronous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		public async ValueTask<ResultValue<TOut>> Then<TOut>(Func<TIn, ResultValue<TOut>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? func(r.Value): r.Error;
		}

		public async ValueTask<ResultValue<TOut>> Then<TOut>(Func<TIn, TOut> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? func(r.Value): r.Error;
		}

		public async ValueTask<ResultValue<TOut>> Then<TOut>(Func<TIn, ValueTask<TOut>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? await func(r.Value).ConfigureAwait(false): r.Error;
		}

		/// <summary>
		/// Returns the result of invoking the specified asynchronous function on the value of a successful result, or propagates the error if the result is not successful.
		/// </summary>
		/// <remarks>Use this method to chain asynchronous operations that depend on the successful result of a previous asynchronous operation. The function is only invoked if the
		/// input result is successful; otherwise, the error is propagated without invoking the function.</remarks>
		/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
		/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure.</param>
		/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing the value returned by the specified function if the input result is successful; otherwise, a failed result containing the original error.</returns>
		// (ValueTask, ValueTask) -> ValueTask
		public async ValueTask<ResultValue<TOut>> Then<TOut>(Func<TIn, ValueTask<ResultValue<TOut>>> func)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? await func(r.Value).ConfigureAwait(false): r.Error;
		}
	}
}
