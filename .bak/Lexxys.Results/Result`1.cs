namespace Lexxys;

/// <summary>
/// Represents the result of an operation that can succeed with a value of the specified type or fail with an error.
/// </summary>
/// <remarks>
/// Use this type to encapsulate the outcome of an operation that may either produce a value or an error.
/// The implicit conversion operators allow seamless conversion between the result type, the value type, and an error
/// result, simplifying usage in method returns and assignments.
/// </remarks>
/// <typeparam name="T">The type of the value returned if the operation is successful.</typeparam>
public abstract class Result<T>: Result, IResultValue<T>
{
	/// <summary>
	/// Gets the value associated with a successful result.
	/// </summary>
	/// <remarks>Accessing this property when the result is not successful will throw an exception. Check the
	/// result's status before accessing this property to avoid an InvalidOperationException.</remarks>
	public virtual T Value => throw new InvalidOperationException("Result is not successful.");

	public static implicit operator Result<T>(T value) => new SuccessResult<T>(value);

	public static implicit operator Result<T>(ErrorResult error) => new FailureResult<T>(error);

	public static implicit operator T(Result<T> result) => result.Value;
}
