namespace Lexxys;

public static class ResultExtensions
{
	#region Then

	/// <summary>
	/// Executes the given function if the result is successful, passing the value of the result to the function and returning a new result. 
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static Result<TOut> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func)
		=> result ? func(result.Value): result.Error;

	/// <summary>
	/// Executes the given function if the result is successful, passing the value of the result to the function and returning a new result. 
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<TOut>> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func)
		=> result ? await func(result.Value): result.Error;

	/// <summary>
	/// Executes the given function if the result is successful, passing the value of the result to the function and returning a new result. 
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<TOut>> Then<TIn, TOut>(this Task<Result<TIn>> result, Func<TIn, Task<Result<TOut>>> func)
	{
		var r = await result;
		return r ? await func(r.Value): r.Error;
	}

	/// <summary>
	/// Executes the given function if the result is successful, passing the value of the result to the function and returning a new result. 
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<TOut>> Then<TIn, TOut>(this Task<Result<TIn>> result, Func<TIn, Result<TOut>> func)
	{
		var r = await result;
		return r ? func(r.Value): r.Error;
	}

	#endregion

	#region Otherwise

	/// <summary>
	/// Executes the given function if the result is not successful, passing the error of the result to the function. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static Result<T> Otherwise<T>(this Result<T> result, Func<ErrorResult, Result<T>> func)
		=> !result ? func(result.Error): result;

	/// <summary>
	/// Executes the given function if the result is not successful, passing the error of the result to the function. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static Task<Result<T>> Otherwise<T>(this Result<T> result, Func<ErrorResult, Task<Result<T>>> func)
		=> !result ? func(result.Error): Task.FromResult(result);

	/// <summary>
	/// Executes the given function if the result is not successful, passing the error of the result to the function. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<T>> Otherwise<T>(this Task<Result<T>> result, Func<ErrorResult, Task<Result<T>>> func)
	{
		var r = await result;
		return !r ? await func(r.Error): r;
	}

	/// <summary>
	/// Executes the given function if the result is not successful, passing the error of the result to the function. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<T>> Otherwise<T>(this Task<Result<T>> result, Func<ErrorResult, Result<T>> func)
	{
		var r = await result;
		return !r ? func(r.Error): r;
	}

	#endregion

	#region Cast

	/// <summary>
	/// Casts the value of the result to a new type using the given function if the result is successful. 
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static Result<TOut> Cast<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
		=> result ? func(result.Value): result.Error;

	/// <summary>
	/// Casts the value of the result to a new type using the given function if the result is successful.
	/// </summary>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <returns></returns>
	public static async Task<Result<TOut>> Cast<TIn, TOut>(this Task<Result<TIn>> result, Func<TIn, TOut> func)
		=> (await result).Cast(func);

	#endregion

	#region Assert

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns an error, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="validator">Returns null if the value is valid, otherwise returns an <see cref="ErrorResult"/>.</param>
	/// <returns></returns>
	public static Result<T> Assert<T>(this Result<T> result, Func<T, ErrorResult?> validator) =>
		!result.IsSuccess ? result:
		validator(result.Value) is { } x ? x: result;

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="error"></param>
	/// <returns></returns>
	public static Result<T> Assert<T>(this Result<T> result, Func<T, bool> predicate, Func<ErrorResult> error) =>
		!result.IsSuccess ? result:
		predicate(result.Value) ? result: error();

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
	/// <returns></returns>
	public static Result<T> Assert<T>(this Result<T> result, Func<T, bool> predicate, string message, string? title = null, int statusCode = 0, params (string Key, string? Value)[]? data) =>
		!result.IsSuccess ? result:
		predicate(result.Value) ? result: Result.Fail(message, title, statusCode, data);

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns an error, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="validator">Returns null if the value is valid, otherwise returns an <see cref="ErrorResult"/>.</param>
	/// <returns></returns>
	public static async Task<Result<T>> Assert<T>(this Task<Result<T>> result, Func<T, ErrorResult?> validator)
	{
		var r = await result;
		return !r.IsSuccess ? r: validator(r.Value) is { } x ? x: r;
	}

	/// <summary>
	/// Validates the result value using the given validator function. If the validator returns <c>false</c>, the result is replaced with the error.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="result"></param>
	/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
	/// <param name="error"></param>
	/// <returns></returns>
	public static async Task<Result<T>> Assert<T>(this Task<Result<T>> result, Func<T, bool> predicate, Func<ErrorResult> error)
	{
		var r = await result;
		return !r.IsSuccess ? r: predicate(r.Value) ? r: error();
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
	/// <returns></returns>
	public static async Task<Result<T>> Assert<T>(this Task<Result<T>> result, Func<T, bool> predicate, string message, string? title = null, int statusCode = 0, params (string Key, string? Value)[]? data)
	{
		var r = await result;
		return !r.IsSuccess ? r: predicate(r.Value) ? r: Result.Fail(message, title, statusCode, data);
	}

	#endregion
}
