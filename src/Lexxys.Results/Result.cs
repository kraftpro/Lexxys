using System.Collections;

namespace Lexxys;

/// <summary>
/// Represents a result of an operation.
/// </summary>
public interface IResult
{
	bool IsSuccess { get; }
	bool IsFailure { get; }
}

/// <summary>
/// Represents a successful result of an operation.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISuccessResult<out T>: IResult
{
	T Value { get; }
}

/// <summary>
/// Represents a failed result of an operation.
/// </summary>
public interface IFailedResult: IResult
{
	ErrorResult Error { get; }
}

/// <summary>
/// Base class that represents a result of an operation.
/// </summary>
public abstract class Result: IResult
{
	public virtual bool IsSuccess => false;
	public virtual bool IsFailure => false;

	public override string ToString() => IsSuccess ? "Success": IsFailure ? "Failure": "Neither";

	public static implicit operator Result(ErrorResult error) => new FailureResult<bool>(error);

	public static bool operator true(Result result) => result.IsSuccess;

	public static bool operator false(Result result) => result.IsFailure;

	//public static implicit operator bool(Result result) => result.IsSuccess;

	public static bool operator !(Result result) => result.IsFailure;
	private static readonly Result success = new SuccessResult<byte>(1);

	public static Result Success() => success;

	public static Result<T> Success<T>(T value) => value;

	public static ErrorResult Fail(string errorMessage, string? title = null, int statusCode = 0, params (string Key, string? Value)[]? data)
		=> new ErrorResult(errorMessage, data ?? [], title, statusCode);

	public static ErrorResult BadRequest(string errorMessage, string? title = null, params (string Key, string? Value)[]? data)
		=> new ErrorResult(errorMessage, data ?? [], title, 400);

	public static ErrorResult NotFound(string errorMessage, string? title = null, params (string Key, string? Value)[]? data)
		=> new ErrorResult(errorMessage, data ?? [], title, 404);
}

/// <summary>
/// Represents a result of an operation with a value.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Result<T>: Result
{
	public virtual T Value => throw new InvalidOperationException("Result is not successful.");
	public virtual ErrorResult Error => throw new InvalidOperationException("Result is not failure.");

	public override bool IsSuccess => false;
	public override bool IsFailure => false;

	public static implicit operator Result<T>(T value) => new SuccessResult<T>(value);

	public static implicit operator Result<T>(ErrorResult error) => new FailureResult<T>(error);

	public static implicit operator T(Result<T> result) => result.Value;
}

/// <summary>
/// Represents a successful result of an operation with a value.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="value"></param>
public class SuccessResult<T>(T value): Result<T>, ISuccessResult<T>
{
	public override T Value { get;} = value;
	public override bool IsSuccess => true;

	public override string ToString() => Value switch
	{
		null => "Success: null",
		string str => $"Success: {str}",
		IEnumerable<object?> enumerable => $"Success: [{string.Join(", ", enumerable)}]",
		IEnumerable enumerable => $"Success: [{string.Join(", ", enumerable.Cast<object?>())}]",
		_ => $"Success: {Value}"
	};

	public static implicit operator SuccessResult<T>(T value) => new SuccessResult<T>(value);
	public static implicit operator T(SuccessResult<T> result) => result.Value;
	public static implicit operator bool(SuccessResult<T> _) => true;
}

/// <summary>
/// Represents a failed result of an operation.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="error">Error result.</param>
public class FailureResult<T>(ErrorResult error): Result<T>, IFailedResult
{
	public FailureResult(string message, string? title = null, int statusCode = 0, IDictionary<string, string?>? data = null):
		this(new ErrorResult(message, title, statusCode, data))
	{
	}

	public FailureResult(string message, IEnumerable<(string Key, string? Value)> data, string? title = null, int statusCode = 0):
		this(new ErrorResult(message, data, title, statusCode))
	{
	}

	public override ErrorResult Error { get; } = error;
	public override bool IsFailure => true;
	public override string ToString() => $"Error: {Error}";

	public static implicit operator FailureResult<T>(ErrorResult error) => new FailureResult<T>(error);

	public static implicit operator bool(FailureResult<T> _) => false;
}
