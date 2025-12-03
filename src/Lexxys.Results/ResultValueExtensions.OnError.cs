// Lexxys Infrastructural library.
// file: ResultValueExtensions.OnError.cs
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
		/// Invokes the specified action if the result represents a failure, passing the error to the action. The action is only invoked if the result is not successful
		/// and the error code meets the specified threshold. The original result is returned unchanged.
		/// </summary>
		/// <remarks>Use this method to perform side effects or logging when a result represents a failure. The action is only invoked if the result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
		/// <param name="action">The action to execute if the result is a failure. Receives the error information as an argument.</param>
		/// <param name="errorCode">Optional minimum error code required to invoke the action. (default is 0)</param>
		/// <returns>The original result for chaining further operations.</returns>
		public ResultValue<T> ActOnError(Action<ErrorResult> action, int errorCode = 0)
		{
			if (result.IsFailure && result.Error.ErrorCode >= errorCode)
				action(result.Error);
			return result;
		}
	}

	extension<T>(Task<ResultValue<T>> result)
	{
		/// <summary>
		/// Invokes the specified action if the result represents a failure, passing the error to the action. The action is only invoked if the result is not successful
		/// and the error code meets the specified threshold. The original result is returned unchanged.
		/// </summary>
		/// <remarks>Use this method to perform side effects or logging when a result represents a failure. The action is only invoked if the result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
		/// <param name="action">The action to execute if the result is a failure. Receives the error information as an argument.</param>
		/// <param name="errorCode">Optional minimum error code required to invoke the action. (default is 0)</param>
		/// <returns>The original result for chaining further operations.</returns>
		public async Task<ResultValue<T>> ActOnError(Action<ErrorResult> action, int errorCode = 0)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsFailure && r.Error.ErrorCode >= errorCode)
				action(r.Error);
			return r;
		}

		/// <summary>
		/// Invokes the specified asynchronous action if the result represents a failure, passing the error to the action. The action is only invoked if the result is not successful
		/// and the error code meets the specified threshold. The original result is returned unchanged.
		/// </summary>
		/// <remarks>Use this method to perform side effects or logging when a result represents a failure. The action is only invoked if the result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
		/// <param name="action">The asynchronous action to execute if the result is a failure. Receives the error information as an argument.</param>
		/// <param name="errorCode">Optional minimum error code required to invoke the action. (default is 0)</param>
		/// <returns>The original result for chaining further operations.</returns>
		public async Task<ResultValue<T>> ActOnError(Func<ErrorResult, Task> action, int errorCode = 0)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsFailure && r.Error.ErrorCode >= errorCode)
				await action(r.Error).ConfigureAwait(false);
			return r;
		}
	}

	extension<T>(ValueTask<ResultValue<T>> result)
	{
		/// <summary>
		/// Invokes the specified asynchronous action if the result represents a failure, passing the error to the action. The action is only invoked if the result is not successful
		/// and the error code meets the specified threshold. The original result is returned unchanged.
		/// </summary>
		/// <remarks>Use this method to perform side effects or logging when a result represents a failure. The action is only invoked if the result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
		/// <param name="action">The action to execute if the result is a failure. Receives the error information as an argument.</param>
		/// <param name="errorCode">Optional minimum error code required to invoke the action. (default is 0)</param>
		/// <returns>The original result for chaining further operations.</returns>
		public async ValueTask<ResultValue<T>> ActOnError(Action<ErrorResult> action, int errorCode = 0)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsFailure && r.Error.ErrorCode >= errorCode)
				action(r.Error);
			return r;
		}

		/// <summary>
		/// Invokes the specified asynchronous action if the result represents a failure, passing the error to the action. The action is only invoked if the result is not successful
		/// and the error code meets the specified threshold. The original result is returned unchanged.
		/// </summary>
		/// <remarks>Use this method to perform side effects or logging when a result represents a failure. The action is only invoked if the result is not successful.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
		/// <param name="action">The asynchronous action to execute if the result is a failure. Receives the error information as an argument.</param>
		/// <param name="errorCode">Optional minimum error code required to invoke the action. (default is 0)</param>
		/// <returns>The original result for chaining further operations.</returns>
		public async ValueTask<ResultValue<T>> ActOnError(Func<ErrorResult, ValueTask> action, int errorCode = 0)
		{
			var r = await result.ConfigureAwait(false);
			if (r.IsFailure && r.Error.ErrorCode >= errorCode)
				await action(r.Error).ConfigureAwait(false);
			return r;
		}
	}
}
