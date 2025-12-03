namespace Lexxys;

/// <summary>
/// Represents the result of an operation that has failed and provides details about the error encountered.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IOperationResult"/> to include error information specific to failed operations.
/// </remarks>
public interface IFailureResult: IOperationResult
{
	/// <summary>
	/// Gets the error information associated with the current operation.
	/// </summary>
	ErrorResult Error { get; }
}
