namespace Lexxys;

public static class ResultValueExtensions
{
	#region Then

	/// <summary>
	/// Invokes the specified function if the current result is successful, and returns its result; otherwise, propagates
	/// the error.
	/// </summary>
	/// <remarks>
	/// This method enables chaining of operations that return results, allowing for fluent error handling
	/// and composition. If the input result is not successful, the function is not invoked and the error is
	/// propagated.
	/// </remarks>
	/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TOut">The type of the value contained in the result returned by the function.</typeparam>
	/// <param name="result">The input result to evaluate. If successful, its value is passed to the function.</param>
	/// <param name="func">A function to invoke if the input result is successful. The function receives the value of the input result and
	/// returns a new result.</param>
	/// <returns>A result containing the value returned by the function if the input result is successful; otherwise, a result
	/// containing the original error.</returns>
	public static ResultValue<TOut> Then<TIn, TOut>(this ResultValue<TIn> result, Func<TIn, ResultValue<TOut>> func)
		=> result.IsSuccess ? func(result.Value): new ResultValue<TOut>(result.Error);

	public static ResultValue<TOut> Then<TIn, TOut>(this ResultValue<TIn> result, Func<TIn, TOut> func)
		=> result.IsSuccess ? func(result.Value) : new ResultValue<TOut>(result.Error);

	/// <summary>
	/// Asynchronously invokes the specified function if the current result is successful, propagating errors otherwise.
	/// </summary>
	/// <remarks>This method enables chaining of asynchronous operations that return results, allowing errors to be
	/// propagated without invoking subsequent functions. If the input result is not successful, the function is not called
	/// and the error is returned immediately.</remarks>
	/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TOut">The type of the value contained in the result returned by the function.</typeparam>
	/// <param name="result">The input result to evaluate. If successful, its value is passed to the function; otherwise, its error is
	/// propagated.</param>
	/// <param name="func">A function to invoke if the input result is successful. The function receives the value of the input result and
	/// returns a task that produces a new result.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is a result containing the output value if both
	/// the input result and the function succeed; otherwise, it contains the propagated error.</returns>
	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this ResultValue<TIn> result, Func<TIn, Task<ResultValue<TOut>>> func)
		=> result.IsSuccess ? await func(result.Value): new ResultValue<TOut>(result.Error);

	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this ResultValue<TIn> result, Func<TIn, Task<TOut>> func)
		=> result.IsSuccess ? await func(result.Value) : new ResultValue<TOut>(result.Error);

	/// <summary>
	/// Chains an asynchronous operation to be executed if the preceding task returns a successful result, propagating
	/// errors otherwise.
	/// </summary>
	/// <remarks>This method enables fluent chaining of asynchronous operations that return results, allowing error
	/// propagation without additional error handling code. If the input result is not successful, the chained function is
	/// not invoked and the error is propagated.</remarks>
	/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TOut">The type of the value contained in the result returned by the chained operation.</typeparam>
	/// <param name="result">A task that represents the asynchronous operation producing a result to evaluate. The chained operation is executed
	/// only if this result is successful.</param>
	/// <param name="func">A function to invoke if the input result is successful. The function receives the successful value and returns a
	/// task that produces the next result.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the output of the chained operation if
	/// the input result is successful; otherwise, it contains the error from the input result.</returns>
	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this Task<ResultValue<TIn>> result, Func<TIn, Task<ResultValue<TOut>>> func)
	{
		var r = await result;
		return r.IsSuccess ? await func(r.Value): new ResultValue<TOut>(r.Error);
	}

	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this Task<ResultValue<TIn>> result, Func<TIn, Task<TOut>> func)
	{
		var r = await result;
		return r.IsSuccess ? await func(r.Value) : new ResultValue<TOut>(r.Error);
	}

	/// <summary>
	/// Chains an asynchronous result-producing task with a synchronous function, propagating errors or applying the
	/// function if the initial result is successful.
	/// </summary>
	/// <remarks>If the input result is unsuccessful, the function is not invoked and the error is propagated. This
	/// method enables fluent chaining of asynchronous result-based operations.</remarks>
	/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TOut">The type of the value contained in the output result.</typeparam>
	/// <param name="result">A task that represents the asynchronous operation producing a result to be processed.</param>
	/// <param name="func">A function to apply to the value of the input result if it is successful. The function returns a new result of type
	/// TOut.</param>
	/// <returns>A task that represents the asynchronous operation. The result contains the output of the function if the input
	/// result is successful; otherwise, it contains the error from the input result.</returns>
	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this Task<ResultValue<TIn>> result, Func<TIn, ResultValue<TOut>> func)
	{
		var r = await result;
		return r.IsSuccess ? func(r.Value): new ResultValue<TOut>(r.Error);
	}

	public static async Task<ResultValue<TOut>> Then<TIn, TOut>(this Task<ResultValue<TIn>> result, Func<TIn, TOut> func)
	{
		var r = await result;
		return r.IsSuccess ? func(r.Value) : new ResultValue<TOut>(r.Error);
	}

	#endregion

	#region Otherwise

	/// <summary>
	/// Returns the original result if it is successful; otherwise, invokes the specified function to provide an
	/// alternative result based on the error.
	/// </summary>
	/// <remarks>Use this method to provide a fallback or recovery strategy when a result represents a failure. The
	/// function is only invoked if the original result is not successful.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">The result to evaluate for success or failure.</param>
	/// <param name="action">A function that takes the error from the failed result and returns an alternative result of the same type. Cannot
	/// be null.</param>
	public static void Otherwise<T>(this ResultValue<T> result, Action<ErrorResult> action)
	{
		if (result.IsFailure)
			action(result.Error);
	}

	/// <summary>
	/// Invokes the specified asynchronous function if the result represents an error; otherwise, returns the original
	/// result.
	/// </summary>
	/// <remarks>Use this method to provide an alternative asynchronous computation or recovery path when the
	/// original result is an error. If the result is successful, the function is not invoked and the original result is
	/// returned.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">The result to evaluate for success or error.</param>
	/// <param name="action">A function to invoke if the result is an error. The function receives the error information and returns a task that
	/// produces a new result.</param>
	public static async Task Otherwise<T>(this ResultValue<T> result, Func<ErrorResult, Task> action)
	{
		if (result.IsFailure)
			await action(result.Error);
	}

	/// <summary>
	/// Invokes the specified asynchronous function if the original task result represents an error, returning its result;
	/// otherwise, returns the successful result.
	/// </summary>
	/// <remarks>Use this method to provide an alternative asynchronous computation or recovery path when the
	/// original result is an error. The function is not invoked if the original result is successful.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">A task that produces a result to evaluate for success or error.</param>
	/// <param name="action">A function to invoke if the original result is an error. The function receives the error result and returns a task
	/// that produces a new result.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is either the original successful result or the
	/// result returned by the specified function if the original result was an error.</returns>
	public static async Task Otherwise<T>(this Task<ResultValue<T>> result, Func<ErrorResult, Task> action)
	{
		var r = await result;
		if (r.IsFailure)
			await action(r.Error);
	}

	/// <summary>
	/// Returns the result of the specified function if the awaited result represents an error; otherwise, returns the
	/// original successful result.
	/// </summary>
	/// <remarks>Use this method to provide an alternative result or recovery logic when an asynchronous operation
	/// fails. The function is only invoked if the awaited result is an error; otherwise, the original result is returned
	/// unchanged.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">A task that represents the asynchronous operation returning a result to evaluate.</param>
	/// <param name="action">A function to invoke if the awaited result is an error. The function receives the error result and returns an
	/// alternative result.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is either the original successful result or the
	/// result returned by the specified function if the original result is an error.</returns>
	public static async Task Otherwise<T>(this Task<ResultValue<T>> result, Action<ErrorResult> action)
	{
		var r = await result;
		if (r.IsFailure)
			action(r.Error);
	}

	#endregion

	#region OnError

	/// <summary>
	/// Invokes the specified action if the result represents a failure and meets the optional error code condition.
	/// </summary>
	/// <remarks>Use this method to handle error cases in a fluent manner when working with result values. The
	/// action is only called if the result indicates failure and the error code meets the specified condition; otherwise, it is ignored.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">The result to evaluate for failure. If the result is a failure, the action will be invoked.</param>
	/// <param name="action">The action to execute if the result is a failure. Receives the error information as an argument.</param>
	/// <param name="errorCode">Optional minimum error code required to invoke the action.</param>
	public static void OnError<T>(this ResultValue<T> result, Action<ErrorResult> action, int? errorCode = null)
	{
		if (result.IsFailure && result.Error.ErrorCode >= (errorCode ?? ErrorResult.DefaultErrorCode))
			action(result.Error);
	}

	/// <summary>
	/// Invokes the specified asynchronous action if the result represents a failure and meets the optional error code condition.
	/// </summary>
	/// <remarks>Use this method to handle errors in a fluent manner when working with asynchronous result
	/// operations. The action is only invoked if the result indicates failure and the error code meets the specified condition; otherwise, it is ignored.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">A task that produces a result of type <see cref="ResultValue{T}"/> to be checked for failure.</param>
	/// <param name="action">An asynchronous action to execute if the result is a failure. Receives the error information as an argument.</param>
	/// <param name="errorCode">Optional minimum error code required to invoke the action.</param>
	/// <returns>A task that completes when the error action has finished executing, or immediately if the result is not a failure.</returns>
	public static async Task OnError<T>(this Task<ResultValue<T>> result, Func<ErrorResult, Task> action, int? errorCode = null)
	{
		var r = await result;
		if (r.IsFailure && r.Error.ErrorCode >= (errorCode ?? ErrorResult.DefaultErrorCode))
			await action(r.Error);
	}

	/// <summary>
	/// Invokes the specified action if the asynchronous result represents a failure and meets the optional error code condition.
	/// </summary>
	/// <remarks>Use this method to handle errors in asynchronous result operations without interrupting the normal
	/// flow. The action is only called if the result indicates failure and the error code meets the specified condition; otherwise, it is not invoked.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">A task that produces a result of type <see cref="ResultValue{T}"/> to be checked for failure.</param>
	/// <param name="action">The action to execute if the result is a failure. Receives the error information as an argument.</param>
	/// <param name="errorCode">Optional minimum error code required to invoke the action.</param>
	/// <returns>A task that completes when the error handling action has finished executing, or immediately if the result is not a
	/// failure.</returns>
	public static async Task OnError<T>(this Task<ResultValue<T>> result, Action<ErrorResult> action, int? errorCode = null)
	{
		var r = await result;
		if (r.IsFailure && r.Error.ErrorCode >= (errorCode ?? ErrorResult.DefaultErrorCode))
			action(r.Error);
	}

	#endregion

	#region Cast

	/// <summary>
	/// Projects the value of a successful result into a new form using the specified mapping function.
	/// </summary>
	/// <remarks>This method enables chaining of result-based operations by projecting the value of a successful
	/// result into a new type. If the input result is not successful, the error is propagated without invoking the mapping
	/// function.</remarks>
	/// <typeparam name="TIn">The type of the value contained in the input result.</typeparam>
	/// <typeparam name="TOut">The type of the value to be returned in the new result.</typeparam>
	/// <param name="result">The result to transform. If the result is successful, its value will be passed to the mapping function.</param>
	/// <param name="func">A function to apply to the value of a successful result. Cannot be null.</param>
	/// <returns>A new result containing the transformed value if the input result is successful; otherwise, a failed result
	/// containing the original error.</returns>
	public static ResultValue<TOut> Cast<TIn, TOut>(this ResultValue<TIn> result, Func<TIn, TOut> func)
		=> result.IsSuccess ? new ResultValue<TOut>(func(result.Value)): new ResultValue<TOut>(result.Error);

	/// <summary>
	/// Asynchronously transforms the value of a successful result using the specified mapping function.
	/// </summary>
	/// <remarks>If the original result represents a failure, the mapping function is not invoked and the failure is
	/// propagated. This method is typically used to chain asynchronous result transformations in a fluent
	/// manner.</remarks>
	/// <typeparam name="TIn">The type of the input value contained in the result.</typeparam>
	/// <typeparam name="TOut">The type of the output value returned by the mapping function.</typeparam>
	/// <param name="result">A task that represents the asynchronous operation and yields a result containing the value to transform.</param>
	/// <param name="func">A function to apply to the value of the successful result. Cannot be null.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a result of type TOut, with the value
	/// transformed by the specified function if the original result was successful; otherwise, propagates the original
	/// failure.</returns>
	public static async Task<ResultValue<TOut>> Cast<TIn, TOut>(this Task<ResultValue<TIn>> result, Func<TIn, TOut> func)
		=> (await result).Cast(func);

	#endregion

	public static T GetValueOrThrow<T>(this ResultValue<T> result, Func<ErrorResult, Exception> exceptionFactory)
	{
		if (result.IsFailure) throw exceptionFactory(result.Error);
		return result.Value;
	}

	#region Assert

	/// <summary>
	/// Validates the value of a successful result using the specified validator function, returning an error result if
	/// validation fails.
	/// </summary>
	/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is
	/// already an error, the validator is not invoked.</remarks>
	/// <typeparam name="T">The type of the value contained in the result.</typeparam>
	/// <param name="result">The result to validate. If the result is not successful, it is returned unchanged.</param>
	/// <param name="validator">A function that takes the value of the result and returns an error result if validation fails, or null if
	/// validation succeeds. Cannot be null.</param>
	/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the validator if
	/// validation fails. If the original result is not successful, it is returned unchanged.</returns>
	public static ResultValue<T> Assert<T>(this ResultValue<T> result, Func<T, ErrorResult?> validator) =>
		result.IsSuccess && validator(result.Value) is { } err ? new ResultValue<T>(err): result;

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="error">Function that returns an <see cref="ErrorResult"/> if the value is invalid.</param>
	/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
	/// If the original result is not successful, it is returned unchanged.</returns>
	public static ResultValue<T> Assert<T>(this ResultValue<T> result, Func<T, bool> predicate, Func<T, ErrorResult> error) =>
		result.IsSuccess && !predicate(result.Value) ? new ResultValue<T>(error(result.Value)): result;

	/// <summary>
	/// Validates the result value using the given predicate. If the predicate returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="message">Error message.</param>
	/// <param name="title">Error title.</param>
	/// <param name="statusCode">Error status code.</param>
	/// <param name="data">Additional data to include in the error.</param>
	/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
	/// If the original result is not successful, it is returned unchanged.</returns>
	public static ResultValue<T> Assert<T>(this ResultValue<T> result, Func<T, bool> predicate, string message, string? title = null, int? statusCode = null, params (string Key, object? Value)[]? data) =>
		result.IsSuccess && !predicate(result.Value) ? new ResultValue<T>(new ErrorResult(message, statusCode, title, data ?? [])) : result;

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns an error, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="validator">Returns null if the value is valid, otherwise returns an <see cref="ErrorResult"/>.</param>
	/// <returns></returns>
	public static async Task<ResultValue<T>> Assert<T>(this Task<ResultValue<T>> result, Func<T, ErrorResult?> validator)
	{
		var r = await result;
		return r.IsSuccess && validator(r.Value) is { } err ? new ResultValue<T>(err): r;
	}

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="error">Function that returns an <see cref="ErrorResult"/> if the value is invalid.</param>
	/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
	/// If the original result is not successful, it is returned unchanged.</returns>
	public static async Task<ResultValue<T>> Assert<T>(this Task<ResultValue<T>> result, Func<T, bool> predicate, Func<T, ErrorResult> error)
	{
		var r = await result;
		return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(error(r.Value)): r;
	}

	/// <summary>
	/// Validates the result value using the given predicate. If the predicate returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="message">Error message.</param>
	/// <param name="title">Error title.</param>
	/// <param name="statusCode">Error status code.</param>
	/// <param name="data">Additional data to include in the error.</param>
	/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
	/// If the original result is not successful, it is returned unchanged.</returns>
	public static async Task<ResultValue<T>> Assert<T>(this Task<ResultValue<T>> result, Func<T, bool> predicate, string message, string? title = null, int statusCode = 0, params (string Key, object? Value)[]? data)
	{
		var r = await result;
		return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(new ErrorResult(message, statusCode, title, data ?? [])) : r;
	}

	#endregion

}
