using System.Diagnostics.CodeAnalysis;

namespace Lexxys;

/// <summary>
/// Represents an error result.
/// </summary>
public class ErrorResult
{
	/// <summary>
	/// Gets the title associated with the current instance.
	/// </summary>
	public string? Title { get; }

	/// <summary>
	/// Gets the error message associated with the current instance.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Gets the status code associated with the current instance.
	/// </summary>
	public int StatusCode { get; }

	/// <summary>
	/// Gets the additional data associated with the current instance.
	/// </summary>
	public IDictionary<string, string?> Data { get; }


	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified message, title, status code, and additional data.
	/// </summary>
	/// <param name="message">Error message.</param>
	/// <param name="title">Error title.</param>
	/// <param name="statusCode">Error status code.</param>
	/// <param name="data">Additional data to include in the error.</param>
	public ErrorResult(string message, string? title = null, int statusCode = 0, IDictionary<string, string?>? data = null)
	{
		Title = title;
		Message = message;
		StatusCode = statusCode == 0 ? 400: statusCode;
		Data = data ?? new Dictionary<string, string?>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorResult"/> class with the specified message, title, status code, and additional data.
	/// </summary>
	/// <param name="message">Error message.</param>
	/// <param name="title">Error title.</param>
	/// <param name="statusCode">Error status code.</param>
	/// <param name="data">Additional data to include in the error.</param>
	public ErrorResult(string message, IEnumerable<(string Key, string? Value)> data, string? title = null, int statusCode = 0)
	{
		Title = title;
		Message = message;
		StatusCode = statusCode == 0 ? 400: statusCode;
		Data = ToDictionary(data);
	}


	/// <summary>
	/// Adds a key-value pair to the additional data associated with the current instance.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ErrorResult Add(string key, string? value)
	{
		if (key is null) throw new ArgumentNullException(nameof(key));

		Data[key] = value;
		return this;
	}

	/// <summary>
	/// Adds multiple key-value pairs to the additional data associated with the current instance.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public ErrorResult Add(params (string Key, string? Value)[] data)
	{
		foreach (var (key, value) in data)
		{
			if (key is null) throw new ArgumentNullException("data[].Key");
			Data[key] = value;
		}
		return this;
	}

	public override string ToString() => $"Message: {Message} (Title: {Title}, Code: {StatusCode})";

	private static Dictionary<string, string?> ToDictionary(IEnumerable<(string Key, string? Value)>? data)
	{
		if (data is null) return [];

		var dict = new Dictionary<string, string?>();
		foreach (var (key, value) in data)
		{
			if (key is null) throw new ArgumentNullException("data[].Key");
			dict[key] = value;
		}
		return dict;
	}

	public static bool operator !([NotNullWhen(true)] ErrorResult? value) => value is not null;

	public static bool operator true(ErrorResult? value) => value is null;
	public static bool operator false(ErrorResult? value) => value is not null;

	// public static implicit operator bool(ErrorResult? value) => value is null;

	public static ErrorResult? operator &(ErrorResult? left, ErrorResult? right) => left ?? right;
}
