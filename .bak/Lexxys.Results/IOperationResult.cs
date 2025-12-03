namespace Lexxys;

/// <summary>
/// Defines the contract for an operation result, indicating whether the operation succeeded, failed, or is in an unknown state.
/// </summary>
/// <remarks>
/// Implementations of this interface provide a standardized way to represent the outcome of an
/// operation. Use the properties to check the result status and handle success, failure, or unknown state accordingly.
/// </remarks>
public interface IOperationResult
{
	/// <summary>
	/// Gets a value indicating whether the operation completed successfully.
	/// </summary>
	bool IsSuccess { get; }
	/// <summary>
	/// Gets a value indicating whether the result represents a failure state.
	/// </summary>
	bool IsFailure { get; }

}

public static class OperationResultExtensions
{
	extension(IOperationResult result)
	{
		/// <summary>
		/// Determines whether the specified result is in an unknown state, meaning it is neither a success nor a failure.
		/// </summary>
		/// <param name="result">The result to evaluate.</param>
		/// <returns><see langword="true"/> if the result is in an unknown state; otherwise, <see langword="false"/>.</returns>
		public bool IsUnknown => !result.IsSuccess && !result.IsFailure;
	}
}