// Lexxys Infrastructural library.
// file: ErrorResultExtensions.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

public static class ErrorResultExtensions
{
	public static ErrorResult? Assure(this ErrorResult? result, Func<ErrorResult?> validator) => result ?? validator();

	public static async Task<ErrorResult?> Assure(this Task<ErrorResult?> result, Func<ErrorResult?> validator) => (await result.ConfigureAwait(false)) ?? validator();

	public static async ValueTask<ErrorResult?> Assure(this ValueTask<ErrorResult?> result, Func<ErrorResult?> validator) => (await result.ConfigureAwait(false)) ?? validator();

	public static ErrorResult? Assure(this ErrorResult? result, Func<bool> predicate, Func<ErrorResult?> error) => result ?? (predicate() ? ErrorResult.NoError: error());

	public static async Task<ErrorResult?> Assure(this Task<ErrorResult?> result, Func<bool> predicate, Func<ErrorResult?> error) => (await result.ConfigureAwait(false)) ?? (predicate() ? ErrorResult.NoError: error());

	public static async ValueTask<ErrorResult?> Assure(this ValueTask<ErrorResult?> result, Func<bool> predicate, Func<ErrorResult?> error) => (await result.ConfigureAwait(false)) ?? (predicate() ? ErrorResult.NoError: error());
	public static ErrorResult? Assure(this ErrorResult? result, Func<bool> predicate, string message, string? title = null, int? statusCode = null,
		params (string Key, object? Value)[]? data) => result ?? (predicate() ? ErrorResult.NoError: new ErrorResult(message, title, statusCode, data));

	public static async Task<ErrorResult?> Assure(this Task<ErrorResult?> result, Func<bool> predicate, string message, string? title = null, int? statusCode = null,
		params (string Key, object? Value)[]? data) => (await result.ConfigureAwait(false)) ?? (predicate() ? ErrorResult.NoError: new ErrorResult(message, title, statusCode, data));

	public static async ValueTask<ErrorResult?> Assure(this ValueTask<ErrorResult?> result, Func<bool> predicate, string message, string? title = null, int? statusCode = null,
		params (string Key, object? Value)[]? data) => (await result.ConfigureAwait(false)) ?? (predicate() ? ErrorResult.NoError: new ErrorResult(message, title, statusCode, data));

	public static ErrorResult? Assure(this ErrorResult? result, bool condition, Func<ErrorResult?> error) => result ?? (condition ? ErrorResult.NoError: error());



	public static ErrorResult? Act(this ErrorResult? result, Action action)
	{
		if (result is null)
			action();
		return result;
	}

	public static async Task<ErrorResult?> Act(this Task<ErrorResult?> result, Action action)
	{
		var error = await result.ConfigureAwait(false);
		if (error is null)
			action();
		return error;
	}

	public static async ValueTask<ErrorResult?> Act(this ValueTask<ErrorResult?> result, Action action)
	{
		var error = await result.ConfigureAwait(false);
		if (error is null)
			action();
		return error;
	}


	public static ErrorResult? ActOnError(this ErrorResult? result, Action<ErrorResult> action)
	{
		if (result is not null)
			action(result);
		return result;
	}
	
	public static async Task<ErrorResult?> ActOnError(this Task<ErrorResult?> result, Action<ErrorResult> action)
	{
		var error = await result.ConfigureAwait(false);
		if (error is not null)
			action(error);
		return error;
	}

	public static async ValueTask<ErrorResult?> ActOnError(this ValueTask<ErrorResult?> result, Action<ErrorResult> action)
	{
		var error = await result.ConfigureAwait(false);
		if (error is not null)
			action(error);
		return error;
	}
}
