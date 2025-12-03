// Lexxys Infrastructural library.
// file: ErrorResult.cs
//
// Copyright (c) 2001-2024, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#undef ERRORRESULT_MUTABLE_DATA
using System.Collections;

namespace Lexxys;

public class ErrorResult
{
	public const int DefaultErrorCode = 400;
	public const int DefaultExceptionCode = 500;

#if ERRORRESULT_MUTABLE_DATA
	private List<KeyValuePair<string, object?>>? _data;
#else
	private readonly KeyValuePair<string, object?>[]? _data;
#endif

	/// <summary>
	/// Gets the error message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Gets the title.
	/// </summary>
	public string? Title { get; }

	/// <summary>
	/// Gets the error code.
	/// </summary>
	public int ErrorCode { get; }

	/// <summary>
	/// Gets the exception that caused the current operation to fail.
	/// </summary>
	public Exception? Exception { get; }

	/// <summary>
	/// Gets the additional data associated with the current instance.
	/// </summary>
	public IReadOnlyList<KeyValuePair<string, object?>> Data => _data ?? [];

	/// <summary>
	/// Gets the next error in the chain of errors, if any. This property can be used to represent a sequence of related errors.
	/// </summary>
	public ErrorResult? NextError { get; }

	/// <summary>
	/// Initializes a new instance of the ErrorResult class from an exception.
	/// </summary>
	/// <param name="exception">The exception to create the error result from.</param>
	/// <param name="title">An optional title for the error.</param>
	/// <param name="errorCode">An optional error code. If not specified, defaults to 500.</param>
	/// <param name="data">Optional additional data associated with the error.</param>
	/// <param name="nextError">Optional next error in a chain of errors.</param>
	public ErrorResult(Exception exception, string? title = null, int? errorCode = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, ErrorResult? nextError = null)
		: this(exception.Message, title, errorCode ?? DefaultExceptionCode, data, exception, nextError)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified error message and other optional parameters.
	/// </summary>
	/// <param name="message">The error message describing the cause of the error.</param>
	/// <param name="title">An optional title that provides additional context for the error.</param>
	/// <param name="errorCode">An optional error code associated with the error. If not specified, defaults to 400.</param>
	/// <param name="data">An optional collection of additional data related to the error. The data can be provided as <see cref="IEnumerable{T}"/> of either an <see cref="KeyValuePair{TKey, TValue}"/> of <c>(string, object?)</c> or a tuple of <c>(string, object?)</c>.</param>
	/// <param name="exception">An optional exception that caused the current operation to fail. If provided, the exception is included in the error result.</param>
	/// <param name="nextError">An optional reference to the next error in a chain of errors.</param>
	public ErrorResult(string message, string? title = null, int? errorCode = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, Exception? exception = null, ErrorResult? nextError = null)
	{
		Title = title;
		Message = message;
		ErrorCode = errorCode ?? (exception is not null ? DefaultExceptionCode: DefaultErrorCode);
#if ERRORRESULT_MUTABLE_DATA
		_data = CollectData(data);
#else
		_data = CollectData(data)?.ToArray();
#endif
		Exception = exception;
		NextError = nextError;
	}

#if ERRORRESULT_MUTABLE_DATA

	/// <summary>
	/// Appends a key-value pair to the data bag.
	/// </summary>
	/// <param name="key">The key associated with the value to add.</param>
	/// <param name="value">The value to associate with the specified key.</param>
	/// <returns>The current <see cref="ErrorResult"/> instance with the appended data.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
	public ErrorResult With(string key, object? value)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));
		(_data ??= []).Add(new KeyValuePair<string, object?>(key, value));
		return this;
	}

	/// <summary>
	/// Appends one or more key-value pairs to the internal data bag.
	/// </summary>
	/// <param name="items">An array of key-value pairs to add to the data bag.</param>
	/// <returns>The current <see cref="ErrorResult"/> instance with the appended data.</returns>
	public ErrorResult With(params (string Key, object? Value)[] items)
	{
		if (items is not { Length: >0 }) return this;

		_data ??= [];
		foreach (var (key, value) in items)
		{
			_data.Add(new KeyValuePair<string, object?>(key, value));
		}
		return this;
	}

	/// <summary>
	/// Appends a collection of key-value pairs to the internal data bag.
	/// </summary>
	/// <param name="items">A collection of key-value pairs to add to the data bag.</param>
	/// <returns>The current <see cref="ErrorResult"/> instance with the appended data.</returns>
	public ErrorResult With(IEnumerable<KeyValuePair<string, object?>> items)
	{
		(_data ??= []).AddRange(items);
		return this;
	}

#else

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified values.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="errorCode">The error code.</param>
	/// <param name="title">The error title.</param>
	/// <param name="data">The collection of key-value pairs containing additional error data.</param>
	/// <param name="exception">The exception associated with the error.</param>
	/// <param name="nextError">The next error in the chain.</param>
	private ErrorResult(string message, int errorCode, string? title, KeyValuePair<string, object?>[]? data, Exception? exception, ErrorResult? nextError)
		=> (Title, Message, ErrorCode, _data, Exception, NextError) = (title, message, errorCode, data, exception, nextError);

	/// <summary>
	/// Creates a new <see cref="ErrorResult"/> with an additional key-value pair added to the data collection.
	/// </summary>
	/// <param name="key">The key of the data entry to add.</param>
	/// <param name="value">The value of the data entry to add.</param>
	/// <returns>A new <see cref="ErrorResult"/> instance with the specified key-value pair added.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
	public ErrorResult With(string key, object? value)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));

		KeyValuePair<string, object?>[] data = _data is null ?
			[new KeyValuePair<string, object?>(key, value)]:
			[.._data, new KeyValuePair<string, object?>(key, value)];
		return new ErrorResult(Message, ErrorCode, Title, data, Exception, NextError);
	}

	/// <summary>
	/// Creates a new ErrorResult with additional key-value pairs added to the data collection.
	/// </summary>
	/// <param name="items">The key-value pairs to add to the error data.</param>
	/// <returns>A new ErrorResult instance with the additional data, or the current instance if no items are provided.</returns>
	public ErrorResult With(params (string Key, object? Value)[] items)
	{
		if (items is not { Length: >0 }) return this;
		KeyValuePair<string, object?>[] data = _data is null ?
			items.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)).ToArray():
			[.._data, ..items.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value))];
		return new ErrorResult(Message, ErrorCode, Title, data, Exception, NextError);
	}

	/// <summary>
	/// Creates a new ErrorResult with additional data items, or returns the current instance if no items are added.
	/// </summary>
	/// <param name="items">The data items to add to the error result.</param>
	/// <returns>A new ErrorResult instance with the combined data, or the current instance if no items were added.</returns>
	public ErrorResult With(IEnumerable<KeyValuePair<string, object?>> items)
	{
		KeyValuePair<string, object?>[] data = _data is null ?
			[.. items]: [.. _data, .. items];
		return data.Length == (_data?.Length ?? 0) ?
			this:
			new ErrorResult(Message, ErrorCode, Title, data, Exception, NextError);
	}

#endif

	public ResultValue<T> AsResultValue<T>() => new ResultValue<T>(this);


	public static ErrorResult? NoError => null;

	public static ErrorResult Problem(string message, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, Exception? exception = null, ErrorResult? nextError = null)
		=> new ErrorResult(message, title, 400, data, exception, nextError);

	public static ErrorResult Forbidden(string message, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, Exception? exception = null, ErrorResult? nextError = null)
		=> new ErrorResult(message, title, 403, data, exception, nextError);

	public static ErrorResult NotFound(string message, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, Exception? exception = null, ErrorResult? nextError = null)
		=> new ErrorResult(message, title, 404, data, exception, nextError);

	public static ErrorResult Error(int errorCode, string message, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, Exception? exception = null, ErrorResult? nextError = null)
		=> new ErrorResult(message, title, errorCode, data, exception, nextError);

	public static ErrorResult Problem(Exception exception, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, ErrorResult? nextError = null)
		=> new ErrorResult(exception, title, 400, data, nextError);

	public static ErrorResult Forbidden(Exception exception, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, ErrorResult? nextError = null)
		=> new ErrorResult(exception, title, 403, data,	nextError);

	public static ErrorResult NotFound(Exception exception, string? title = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, ErrorResult? nextError = null)
		=> new ErrorResult(exception, title, 404, data, nextError);

	public static ErrorResult Error(Exception exception, string? title = null, int? errorCode = null, IReadOnlyCollection<(string Key, object? Value)>? data = null, ErrorResult? nextError = null)
		=> new ErrorResult(exception, title, errorCode ?? 500, data, nextError);

	public Dictionary<string, object?> GetExtensionsData(bool includeNext = false, bool includeStackTrace = false)
	{
		var dataDict = new Dictionary<string, object?>(StringComparer.Ordinal);
		AppendData(dataDict, "", includeStackTrace, includeNext);
		return dataDict;
	}

	private void AppendData(Dictionary<string, object?> dataDict, string prefix, bool includeStackTrace, bool includeNext)
	{
		if (_data is not null)
			AppendData(dataDict, prefix, _data);
		if (Exception is not null)
			AppendException(dataDict, $"{prefix}$exception.", Exception, includeStackTrace);
		if (includeNext && NextError is not null)
			NextError.AppendNext(dataDict, $"{prefix}$next.", includeStackTrace);
	}

	private static void AppendData(Dictionary<string, object?> dataDict, string prefix, IEnumerable<KeyValuePair<string, object?>> items)
	{
		foreach (var pair in items)
		{
			AppendItem(dataDict, $"{prefix}{pair.Key}", pair.Value);
		}
	}

	private void AppendNext(Dictionary<string, object?> dataDict, string prefix, bool includeStackTrace)
	{
		dataDict[$"{prefix}Message"] = Message;
		dataDict[$"{prefix}ErrorCode"] = ErrorCode;
		if (Title != null)
			dataDict[$"{prefix}Title"] = Title;
		AppendData(dataDict, prefix, includeStackTrace, true);
	}

	private void AppendException(Dictionary<string, object?> dataDict, string prefix, Exception exception, bool includeStackTrace)
	{
		if (exception is AggregateException { InnerExceptions.Count: > 0 } aggEx)
		{
			if (aggEx.InnerExceptions.Count == 1)
			{
				AppendException(dataDict, prefix, aggEx.InnerExceptions[0], includeStackTrace);
				return;
			}

			int i = 0;
			foreach (var ex in aggEx.InnerExceptions)
			{
				AppendException(dataDict, $"{prefix}{++i}.", ex, includeStackTrace);
			}
			return;
		}

		if (Message != exception.Message)
		{
			dataDict[$"{prefix}Message"] = exception.Message;
		}
		if (includeStackTrace)
		{
			AppendItem(dataDict, $"{prefix}StackTrace", exception.StackTrace);
		}
		if (exception.Data.Count > 0)
		{
			foreach (DictionaryEntry entry in exception.Data)
			{
				AppendItem(dataDict, $"{prefix}{entry.Key}", entry.Value);
			}
		}
	}

	private static void AppendItem(Dictionary<string, object?> dataDict, string key, object? value)
	{
		string variantKey = key;
		int suffixIndex = 0;
		while (!dataDict.TryAdd(variantKey, value))
		{
			if (dataDict.TryGetValue(variantKey, out var existingValue) && Equals(existingValue, value))
				return;
			variantKey = $"{key}.{++suffixIndex}";
		}
	}

	public override string ToString() => $"Message: {Message}; Title: {Title}; Code: {ErrorCode}; Exception: {Exception}; Data: {Data.Count} items; NextError: {(NextError is not null ? "Yes": "No")}";

	private static List<KeyValuePair<string, object?>>? CollectData(IEnumerable<(string Key, object? Value)>? items, IDictionary? exceptionData = null)
	{
		int count = FastCollectionCount(items);
		List<KeyValuePair<string, object?>>? list = count == 0 ? null: [.. items!.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value))];
		if (exceptionData is not null && exceptionData.Count > 0)
		{
			list ??= new List<KeyValuePair<string, object?>>(exceptionData.Count);
			list.AddRange(exceptionData.Cast<DictionaryEntry>().Select(entry => new KeyValuePair<string, object?>(entry.Key?.ToString() ?? "null", entry.Value)));
		}
		return list;
	}

	private static int FastCollectionCount<T>(IEnumerable<T>? collection) => collection switch
	{
		null => 0,
		ICollection<T> c => c.Count,
		IReadOnlyCollection<T> rc => rc.Count,
		_ => -1
	};
}
