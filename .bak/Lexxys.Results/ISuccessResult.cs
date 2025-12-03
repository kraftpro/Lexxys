namespace Lexxys;

/// <summary>
/// Represents the result of an operation that has succeeded and provides a value of the specified type.
/// </summary>
/// <typeparam name="T">The type of the value returned by the successful operation.</typeparam>
public interface ISuccessResult<out T>: IOperationResult
{
	T Value { get; }
}
