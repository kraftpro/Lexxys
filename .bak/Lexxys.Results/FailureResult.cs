namespace Lexxys;

/// <summary>
/// Represents a failed result of an operation, containing error information and no value of type T.
/// </summary>
/// <remarks>
/// Use this class to represent an operation that did not complete successfully and to provide structured
/// error information to the caller. The result always indicates failure, and no value of type T is available.</remarks>
/// <typeparam name="T">The type of the value that would have been returned if the operation had succeeded.</typeparam>
/// <param name="error">The error details associated with the failed result.
/// </param>
public class FailureResult<T>(ErrorResult error): Result<T>, IFailureResult
{
	public FailureResult(string message): this(new ErrorResult(message))
	{
	}

	public FailureResult(string message, string? title): this(new ErrorResult(message, title))
	{
	}

	public FailureResult(string message, int statusCode): this(new ErrorResult(message, statusCode))
	{
	}

	public FailureResult(string message, int statusCode, string? title): this(new ErrorResult(message, statusCode, title))
	{
	}


	public FailureResult(string message, int? statusCode = null, string? title = null, IDictionary<string, object?>? data = null):
		this(new ErrorResult(message, statusCode, title, data))
	{
	}

	public FailureResult(string message, int? statusCode = null, string? title = null, IEnumerable<(string Key, object? Value)>? data = null):
		this(new ErrorResult(message, statusCode, title, data))
	{
	}

	public override ErrorResult Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
	public override bool IsFailure => true;
	public override string ToString() => $"Error: {Error}";

	public static implicit operator FailureResult<T>(ErrorResult error) => new FailureResult<T>(error);

	public static implicit operator bool(FailureResult<T> _) => false;
}
