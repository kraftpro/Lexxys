namespace Lexxys;

/// <summary>
/// Represents the outcome of an operation, indicating success or failure and providing factory methods for creating
/// result instances.
/// </summary>
/// <remarks>
/// The Result class serves as a base type for representing the result of an operation without exposing
/// implementation details. It provides static methods to create success or failure results, as well as convenience
/// methods for common error scenarios such as bad requests or not found errors. Use the IsSuccess and IsFailure
/// properties to check the result state. Derived types may carry additional information, such as error details or a
/// value associated with a successful result.
/// </remarks>
public abstract class Result: IOperationResult
{
	public virtual bool IsSuccess => false;
	public virtual bool IsFailure => false;

	/// <summary>
	/// Gets the error information associated with a failed result.
	/// </summary>
	/// <remarks>Accessing this property when the result does not represent a failure will throw an exception. Check
	/// the result's status before accessing this property.</remarks>
	public virtual ErrorResult Error => throw new InvalidOperationException("Result is not failure.");

	public override string ToString() => IsSuccess ? "Success": IsFailure ? "Failure": "Undefined";

	public static implicit operator Result(ErrorResult error) => new FailureResult<bool>(error);

	/// <summary>
	/// Defines the true operator for the <see cref="Result"/> type, indicating whether the result represents a successful outcome.
	/// </summary>
	/// <remarks>This operator enables the use of a Result instance in boolean expressions, such as in if
	/// statements, to check for success directly.</remarks>
	/// <param name="result">The Result instance to evaluate for success.</param>
	/// <returns>true if the result represents success; otherwise, false.</returns>
	public static bool operator true(Result result) => result.IsSuccess;

	/// <summary>
	/// Defines the false operator for the <see cref="Result"/> type, indicating whether the result represents a failure outcome.
	/// </summary>
	/// <remarks>This operator enables the use of a Result instance in conditional statements, such as 'if' or
	/// 'while', to check for failure.</remarks>
	/// <param name="result">The result to evaluate for failure.</param>
	/// <returns>true if the result represents a failure; otherwise, false.</returns>
	public static bool operator false(Result result) => result.IsFailure;

	/// <summary>
	/// Determines whether the specified result represents a failure.
	/// </summary>
	/// <remarks>This operator provides a convenient way to check for failure using the logical negation operator.
	/// It is equivalent to evaluating the IsFailure property.</remarks>
	/// <param name="result">The result to evaluate for failure.</param>
	/// <returns><see langword="true"/> if the result indicates a failure; otherwise, <see langword="false"/>.</returns>
	public static bool operator !(Result result) => result.IsFailure;

	/// <summary>
	/// Returns a successful result.
	/// </summary>
	/// <returns>A <see cref="Result"/> that represents a successful operation.</returns>
	public static Result Success() => Ok.SuccessResult;

	/// <summary>
	/// Creates a successful result containing the specified value.
	/// </summary>
	/// <typeparam name="T">The type of the value to be wrapped in the result.</typeparam>
	/// <param name="value">The value to include in the successful result.</param>
	/// <returns>A result that represents a successful operation and contains the specified value.</returns>
	public static Result<T> Success<T>(T value) => value;

	/// <summary>
	/// Creates a new error result with the specified error message and optional details.
	/// </summary>
	/// <param name="errorMessage">The message that describes the error. Cannot be null or empty.</param>
	/// <param name="title">An optional title that provides a short summary of the error. May be null.</param>
	/// <param name="statusCode">An optional status code associated with the error. Defaults to 0 if not specified.</param>
	/// <param name="data">An optional array of key-value pairs containing additional data related to the error. May be null.</param>
	/// <returns>An <see cref="ErrorResult"/> instance representing the error, including the specified message, title, status code,
	/// and any additional data.</returns>
	public static ErrorResult Fail(string errorMessage, string? title = null, int statusCode = 0, params (string Key, object? Value)[]? data)
		=> new ErrorResult(errorMessage, statusCode, title, data ?? []);

	public static ResultValue<T> FailValue<T>(string errorMessage, string? title = null, int statusCode = 0, params (string Key, object? Value)[]? data)
		=> new ErrorResult(errorMessage, statusCode, title, data ?? []);

	public static ErrorResult BadRequest(string errorMessage, string? title = null, params (string Key, object? Value)[]? data)
		=> new ErrorResult(errorMessage, 400, title, data ?? []);

	public static ErrorResult NotFound(string errorMessage, string? title = null, params (string Key, object? Value)[]? data)
		=> new ErrorResult(errorMessage, 404, title, data ?? []);

	private class Ok: Result
	{
		public static readonly Ok SuccessResult = new Ok();

		private Ok()
		{
		}

		public override bool IsSuccess => true;

		public override string ToString() => "Success";
	}
}
