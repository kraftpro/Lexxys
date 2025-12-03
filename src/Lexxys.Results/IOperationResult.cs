// Lexxys Infrastructural library.
// file: IOperationResult.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

/// <summary>
/// Defines the contract for an operation result, indicating whether the operation succeeded or failed.
/// </summary>
/// <remarks>
/// Implementations of this interface provide a standardized way to represent the outcome of an
/// operation. Use the properties to check the result status and handle success or failure accordingly.
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
#if NET
	bool IsFailure => !IsSuccess;
#else
	bool IsFailure { get; }
#endif
	/// <summary>
	/// Gets the error contained in the current instance of <see cref="IOperationResult"/> if the operation failed; otherwise, throws an exception.
	/// </summary>
	ErrorResult Error { get; }
}
