// Lexxys Infrastructural library.
// file: ResultValueExtensions.Otherwise.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

public static partial class ResultValueExtensions
{
	extension<T>(ResultValue<T> result)
	{
		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified function to provide an
		/// alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure.
		/// The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type.</param>
		/// <returns>A new result containing either the original value if the result is successful, or the value returned by the specified function if the result is a failure.</returns>
		public ResultValue<T> Otherwise(Func<ErrorResult, ResultValue<T>> action)
			=> result.IsSuccess ? result : action(result.Error);

		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified asynchronous function to provide an alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure.
		/// The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type asynchronously.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing either the original value if the result
		/// is successful, or the value returned by the specified function if the result is a failure.</returns>
		public async Task<ResultValue<T>> Otherwise(Func<ErrorResult, Task<ResultValue<T>>> action)
			=> result.IsSuccess ? result : await action(result.Error).ConfigureAwait(false);
	}

	extension<T>(Task<ResultValue<T>> result)
	{
		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified function to provide an alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure. The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The task representing the result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing either the original value if the result
		/// is successful, or the value returned by the specified function if the result is a failure.</returns>
		public async Task<ResultValue<T>> Otherwise(Func<ErrorResult, ResultValue<T>> action)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? r : action(r.Error);
		}

		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified asynchronous function to provide an alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure.
		/// The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The task representing the result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type asynchronously.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing either the original value if the result
		/// is successful, or the value returned by the specified function if the result is a failure.</returns>
		public async Task<ResultValue<T>> Otherwise(Func<ErrorResult, Task<ResultValue<T>>> action)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? r : await action(r.Error).ConfigureAwait(false);
		}
	}

	extension<T>(ValueTask<ResultValue<T>> result)
	{
		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified asynchronous function to provide an alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure.
		/// The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The value representing the result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type asynchronously.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing either the original value if the result
		/// is successful, or the value returned by the specified function if the result is a failure.</returns>
		public async ValueTask<ResultValue<T>> Otherwise(Func<ErrorResult, ResultValue<T>> action)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? r : action(r.Error);
		}

		/// <summary>
		/// Returns the original result if it is successful; otherwise, invokes the specified asynchronous function to provide an alternative result based on the error.
		/// </summary>
		/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure.
		/// The function is only invoked if the original result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The value representing the result to evaluate for success or failure.</param>
		/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type asynchronously.</param>
		/// <returns>A task representing the asynchronous operation. The task result contains a new result containing either the original value if the result
		/// is successful, or the value returned by the specified function if the result is a failure.</returns>
		public async ValueTask<ResultValue<T>> Otherwise(Func<ErrorResult, ValueTask<ResultValue<T>>> action)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess ? r : await action(r.Error).ConfigureAwait(false);
		}
	}
}
