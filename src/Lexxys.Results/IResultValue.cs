// Lexxys Infrastructural library.
// file: IResultValue.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

/// <summary>
/// Represents the result of an operation that produces a value or an error.
/// </summary>
/// <remarks>Use this interface to access the outcome of an operation, including its value if successful or error
/// details if failed. The value is only valid when the operation succeeds; otherwise, the error provides information
/// about the failure.</remarks>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public interface IResultValue<out T>: IOperationResult
{
	/// <summary>
	/// Gets the value contained in the current instance of <see cref="IResultValue{T}"/> if the operation succeeded; otherwise, throws an exception.
	/// </summary>
	T Value { get; }
}

