using System.Collections;

namespace Lexxys;

/// <summary>
/// Represents a successful result that contains a value of the specified type.
/// </summary>
/// <remarks>
/// This type is typically used to indicate the successful outcome of an operation and to provide the
/// resulting value. Implicit conversions are provided to and from the encapsulated value, allowing seamless assignment
/// and usage in expressions. The <see cref="SuccessResult{T}"/> type always represents a successful result; its <see
/// cref="Result{T}.IsSuccess"/> property is always <see langword="true"/>.
/// </remarks>
/// <typeparam name="T">The type of the value encapsulated by the result.</typeparam>
/// <param name="value">The value to be associated with the successful result.</param>
public class SuccessResult<T>(T value): Result<T>, ISuccessResult<T>
{
	public override T Value { get;} = value;
	public override bool IsSuccess => true;

	public override string ToString() => Value switch
	{
		null => "Success: null",
		string str => $"Success: {str}",
		IEnumerable<object?> enumerable => $"Success: [{String.Join(", ", enumerable)}]",
		IEnumerable enumerable => $"Success: [{String.Join(", ", enumerable.Cast<object?>())}]",
		_ => $"Success: {Value}"
	};

	public static implicit operator SuccessResult<T>(T value) => new SuccessResult<T>(value);

	public static implicit operator T(SuccessResult<T> result) => result.Value;

	public static implicit operator bool(SuccessResult<T> _) => true;
}
