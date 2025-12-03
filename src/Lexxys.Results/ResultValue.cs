// Lexxys Infrastructural library.
// file: ResultValue.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lexxys;

/// <summary>
/// Represents the result of an operation that can either succeed with a value of type <typeparamref name="T"/> or fail with an error represented by <see cref="ErrorResult"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by a successful operation.</typeparam>
public readonly struct ResultValue<T>: IResultValue<T>
{
	private readonly T _value;
	private readonly ErrorResult? _error;

	public T Value => _error is null ? _value: throw new InvalidOperationException("Result is failure.");
	public ErrorResult Error => _error is not null ? _error: throw new InvalidOperationException("Result is successful.");

	public ResultValue(T value)
	{
		_value = value;
	}

	public ResultValue(ErrorResult error)
	{
		Unsafe.SkipInit(out _value);
		_error = error ?? throw new ArgumentNullException(nameof(error));
	}

	public bool IsSuccess => _error is null;
	public bool IsFailure => _error is not null;

	[return: MaybeNull]
	public T GetValueOrDefault() => _error is null ? _value: default;
	public T GetValueOrDefault(T defaultValue) => _error is null ? _value: defaultValue;
	public T GetValueOrThrow(Func<ErrorResult, Exception> exceptionFactory) => _error is null ? _value: throw exceptionFactory(_error);

	public ErrorResult? GetErrorOrDefault() => _error;

	public void Deconstruct(out T value, out ErrorResult? error)
	{
		value = _value;
		error = _error;
	}

	public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";

	public static implicit operator bool(ResultValue<T> result) => result.IsSuccess;

	public static implicit operator ResultValue<T>(T value) => new ResultValue<T>(value);

	public static implicit operator ResultValue<T>(ErrorResult error) => new ResultValue<T>(error);
}

public static class ResultValue
{
	public static ResultValue<object?> Empty => default;
	public static ResultValue<T> Create<T>(T value) => new ResultValue<T>(value);
	public static ResultValue<T> Success<T>(T value) => new ResultValue<T>(value);
	public static ResultValue<T> Failure<T>(ErrorResult error) => new ResultValue<T>(error);
}
