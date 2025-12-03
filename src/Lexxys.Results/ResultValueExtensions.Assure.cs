// Lexxys Infrastructural library.
// file: ResultValueExtensions.Assure.cs
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
		/// Validates the result value using the given validator function. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="error">Function that returns an <see cref="ErrorResult"/> if the value is invalid.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public ResultValue<T> Assure(Func<T, bool> predicate, Func<T, ErrorResult> error) =>
			result.IsSuccess && !predicate(result.Value) ? new ResultValue<T>(error(result.Value)): result;

		/// <summary>
		/// Validates the result value using the given predicate. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="message">Error message.</param>
		/// <param name="title">Error title.</param>
		/// <param name="errorCode">Error status code.</param>
		/// <param name="data">Additional data to include in the error.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public ResultValue<T> Assure(Func<T, bool> predicate, string message, string? title = null, int? errorCode = null, params (string Key, object? Value)[]? data) =>
			result.IsSuccess && !predicate(result.Value) ? new ResultValue<T>(new ErrorResult(message, title, errorCode, data)): result;

		/// <summary>
		/// Validates the value of a successful result using the specified validator function, returning an error result if
		/// validation fails.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is
		/// already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate. If the result is not successful, it is returned unchanged.</param>
		/// <param name="validator">A function that takes the value of the result and returns an error result if validation fails, or null if
		/// validation succeeds.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the validator if
		/// validation fails. If the original result is not successful, it is returned unchanged.</returns>
		public ResultValue<T> Assure(Func<T, ErrorResult?> validator) =>
			result.IsSuccess && validator(result.Value) is { } err ? new ResultValue<T>(err): result;

		public async Task<ResultValue<T>> Assure(Func<T, Task<ErrorResult?>> validator)
		{
			return result.IsSuccess && (await validator(result.Value).ConfigureAwait(false)) is { } err ? new ResultValue<T>(err): result;
		}
	}

	extension<T>(Task<ResultValue<T>> result)
	{
		/// <summary>
		/// Validates the result value using the given validator function. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="error">Function that returns an <see cref="ErrorResult"/> if the value is invalid.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public async Task<ResultValue<T>> Assure(Func<T, bool> predicate, Func<T, ErrorResult> error)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(error(r.Value)): r;
		}

		/// <summary>
		/// Validates the result value using the given predicate. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="message">Error message.</param>
		/// <param name="title">Error title.</param>
		/// <param name="errorCode">Error status code.</param>
		/// <param name="data">Additional data to include in the error.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public async Task<ResultValue<T>> Assure(Func<T, bool> predicate, string message, string? title = null, int? errorCode = null, params (string Key, object? Value)[]? data)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(new ErrorResult(message, title, errorCode, data ?? [])): r;
		}

		/// <summary>
		/// Validates the value of a successful result using the specified asynchronous validator function, returning an error result if
		/// validation fails.
		/// </summary>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="validator">A function that takes the value of the result and returns an error result if validation fails, or null if
		/// validation succeeds.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the validator if
		/// validation fails. If the original result is not successful, it is returned unchanged.</returns>
		public async Task<ResultValue<T>> Assure(Func<T, ErrorResult?> validator)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && validator(r.Value) is { } err ? new ResultValue<T>(err): r;
		}

		public async Task<ResultValue<T>> Assure(Func<T, Task<ErrorResult?>> validator)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && (await validator(r.Value).ConfigureAwait(false)) is { } err ? new ResultValue<T>(err): r;
		}
	}

	extension<T>(ValueTask<ResultValue<T>> result)
	{
		/// <summary>
		/// Validates the result value using the given validator function. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="error">Function that returns an <see cref="ErrorResult"/> if the value is invalid.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public async ValueTask<ResultValue<T>> Assure(Func<T, bool> predicate, Func<T, ErrorResult> error)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(error(r.Value)): r;
		}

		/// <summary>
		/// Validates the result value using the given predicate. If the <paramref name="predicate"/> returns <c>false</c>, the result is replaced with the error.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="predicate">Predicate that returns <c>true</c> if the value is valid.</param>
		/// <param name="message">Error message.</param>
		/// <param name="title">Error title.</param>
		/// <param name="errorCode">Error status code.</param>
		/// <param name="data">Additional data to include in the error.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the error function if validation fails.
		/// If the original result is not successful, it is returned unchanged.</returns>
		public async ValueTask<ResultValue<T>> Assure(Func<T, bool> predicate, string message, string? title = null, int errorCode = 0, params (string Key, object? Value)[]? data)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && !predicate(r.Value) ? new ResultValue<T>(new ErrorResult(message, title, errorCode, data ?? [])): r;
		}

		/// <summary>
		/// Validates the value of a successful result using the specified asynchronous validator function, returning an error result if
		/// validation fails.
		/// </summary>
		/// <remarks>Use this method to enforce additional validation rules on a successful result. If the result is already an error, the validator is not invoked.</remarks>
		/// <typeparam name="T">The type of the value contained in the result.</typeparam>
		/// <param name="result">The result to validate.</param>
		/// <param name="validator">A function that takes the value of the result and returns an error result if validation fails, or null if
		/// validation succeeds.</param>
		/// <returns>A new result containing the original value if validation succeeds, or the error result returned by the validator if
		/// validation fails. If the original result is not successful, it is returned unchanged.</returns>
		public async ValueTask<ResultValue<T>> Assure(Func<T, ErrorResult?> validator)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && validator(r.Value) is { } err ? new ResultValue<T>(err): r;
		}

		public async ValueTask<ResultValue<T>> Assure(Func<T, ValueTask<ErrorResult?>> validator)
		{
			var r = await result.ConfigureAwait(false);
			return r.IsSuccess && (await validator(r.Value).ConfigureAwait(false)) is { } err ? new ResultValue<T>(err): r;
		}
	}
}
