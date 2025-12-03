using System.Collections.Immutable;

namespace Lexxys;

/// <summary>
/// Represents a structured error result containing a message, optional title, status code, and additional data for
/// error reporting or handling scenarios.
/// </summary>
/// <remarks>
/// Use this class to encapsulate error information in a consistent format, such as when returning error
/// details from APIs or services. The additional data dictionary can be used to provide context-specific information,
/// such as validation errors or metadata. The default error code is 400 if not specified.
/// Additioan data associated with an error result can be added using the Add methods, which allow for a fluent interface to build up error details incrementally.
/// This design allows for flexible and extensible error reporting while maintaining a clear structure for error information.
/// </remarks>
public class ErrorResult
{
	public const int DefaultErrorCode = 400;

	private DataBag? _data;

	/// <summary>
	/// Gets the title.
	/// </summary>
	public string? Title { get; }

	/// <summary>
	/// Gets the error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Gets the error code.
	/// </summary>
	public int ErrorCode { get; }

	/// <summary>
	/// Gets the additional data associated with the current instance.
	/// </summary>
	public IReadOnlyDictionary<string, object?> Data => _data ?? (IReadOnlyDictionary<string, object?>)ImmutableDictionary<string, object?>.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message and a default error code of 400.
	/// </summary>
	/// <param name="message">The error message that describes the cause of the error.</param>
	public ErrorResult(string message)
	{
		Message = message;
		ErrorCode = DefaultErrorCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message and optional title.
	/// </summary>
	/// <param name="message">The error message describing the cause of the error.</param>
	/// <param name="title">The optional title that provides additional context for the error.</param>
	public ErrorResult(string message, string? title)
	{
		Title = title;
		Message = message;
		ErrorCode = DefaultErrorCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message and status code.
	/// </summary>
	/// <param name="message">The error message describing the cause of the error.</param>
	/// <param name="errorCode">The error code associated with the error.</param>
	public ErrorResult(string message, int errorCode)
	{
		Message = message;
		ErrorCode = errorCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message, error code, and title.
	/// </summary>
	/// <param name="message">The error message that describes the cause of the error.</param>
	/// <param name="errorCode">The error code associated with the error.</param>
	/// <param name="title">The optional title that provides additional context for the error.</param>
	public ErrorResult(string message, int errorCode, string? title)
	{
		Title = title;
		Message = message;
		ErrorCode = errorCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message, title, error code, and additional data.
	/// </summary>
	/// <param name="message">The error message describing the cause of the error.</param>
	/// <param name="errorCode">An optional error code associated with the error. If not specified, defaults to 400.</param>
	/// <param name="title">An optional title providing a brief summary of the error.</param>
	/// <param name="data">An optional collection of additional data related to the error. If provided and contains items, the data is included in the error result.</param>
	public ErrorResult(string message, int? errorCode = null, string? title = null, IDictionary<string, object?>? data = null)
	{
		Title = title;
		Message = message;
		ErrorCode = errorCode ?? DefaultErrorCode;
		if (data is { Count: >0 })
			_data = new DataBag(data);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message, title, error code, and additional data.
	/// </summary>
	/// <param name="message">The error message describing the cause of the error.</param>
	/// <param name="errorCode">The optional error code associated with the error. If not specified, defaults to 400.</param>
	/// <param name="title">The optional title providing a brief summary of the error.</param>
	/// <param name="data">The optional collection of additional data related to the error. If provided and contains items, the data is included in the error result.</param>
	public ErrorResult(string message, int? errorCode = null, string? title = null, IEnumerable<(string Key, object? Value)>? data = null)
	{
		Title = title;
		Message = message;
		ErrorCode = errorCode ?? DefaultErrorCode;
		if (data is not null && FastCollectionCount(data) != 0)
			_data = new DataBag(data);
	}


	/// <summary>
	/// Adds a key-value pair to the additional data associated with the current instance.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ErrorResult Add(string key, object? value)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));

		_data ??= new DataBag();
		_data.Add(key, value);
		return this;
	}

	/// <summary>
	/// Adds multiple key-value pairs to the additional data associated with the current instance.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ErrorResult Add(params (string Key, object? Value)[] data)
	{
		if (data is not { Length: >0 }) return this;

		if (_data is null)
		{
			_data = new DataBag(data);
			return this;
		}

		foreach (var (key, value) in data)
		{
			_data.Add(key, value);
		}
		return this;
	}

	public override string ToString() => $"Message: {Message} (Title: {Title}, Code: {ErrorCode})";

	private static int FastCollectionCount<T>(IEnumerable<T> collection) => collection switch
	{
		ICollection<T> c => c.Count,
		IReadOnlyCollection<T> rc => rc.Count,
		_ => -1
	};
}
