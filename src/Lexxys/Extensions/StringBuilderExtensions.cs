// Lexxys Infrastructural library.
// file: StringBuilderExtensions.cs
//
// Copyright (c) 20++ ANN.
// You may use this code under the terms of the MIT license
//
#if !NET5_0_OR_GREATER
using System.Text;

namespace Lexxys;

public static class StringBuilderExtensions
{
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
#endif


