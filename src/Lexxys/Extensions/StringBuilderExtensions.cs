// Lexxys Infrastructural library.
// file: StringBuilderExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

public static class StringBuilderExtensions
{
	public static StringBuilder Append(this StringBuilder text, params string?[]? values)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (values == null)
			return text;
		for (int i = 0; i < values.Length; ++i)
		{
			text.Append(values[i]);
		}
		return text;
	}

	public static StringBuilder Append<T>(this StringBuilder text, IEnumerable<T>? source, Func<T, string?> producer, string? separator = null)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (source == null)
			return text;
		if (producer == null)
			throw new ArgumentNullException(nameof(producer));

		string? sep = "";
		foreach (T item in source)
		{
			text.Append(sep).Append(producer(item));
			sep = separator;
		}
		return text;
	}

	public static unsafe StringBuilder Append(this StringBuilder text, ReadOnlySpan<char> value)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));

		fixed (char* p = value)
		{
			text.Append(p, value.Length);
		}
		return text;
	}
}


