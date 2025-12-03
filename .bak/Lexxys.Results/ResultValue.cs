namespace Lexxys;

public readonly struct ResultValue<T>: IResultValue<T>
{
	private readonly T _value;
	private readonly ErrorResult? _error;

	public T Value => IsSuccess ? _value: throw new InvalidOperationException("Result is not successful.");
	public ErrorResult Error => IsFailure ? _error!: throw new InvalidOperationException("Result is successful.");

	public ResultValue()
	{
		_value = default!;
		_error = NoResults;
	}

	public ResultValue(T value)
	{
		_value = value;
		_error = default!;
	}

	public ResultValue(ErrorResult error)
	{
		_value = default!;
		_error = error ?? throw new ArgumentNullException(nameof(error));
	}

	public bool IsSuccess => _error == null;
	public bool IsFailure => _error?.ErrorCode > 0;

	public ErrorResult? GetErrorOrDefault() => _error;

	public static bool operator true(ResultValue<T> result) => result.IsSuccess;
	public static bool operator false(ResultValue<T> result) => result.IsFailure;
	public static bool operator !(ResultValue<T> result) => result.IsFailure;

	public static implicit operator ResultValue<T>(T value) => new ResultValue<T>(value);

	public static implicit operator ResultValue<T>(ErrorResult error) => new ResultValue<T>(error);

	public static implicit operator T(ResultValue<T> result) => result.IsSuccess ? result.Value: throw new InvalidOperationException("Result is not successful.");

	private static readonly ErrorResult NoResults = new ErrorResult("No results.");
}

public interface IResultValue<out T>: IOperationResult
{
	T Value { get; }
	ErrorResult Error { get; }
}

