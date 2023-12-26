using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Arguments.Generator;

internal static class Strings
{
	public static IList<(int Index, int Length)> SplitByCapitals(ReadOnlySpan<char> identifier)
	{
		if (identifier.Length == 0)
			return Array.Empty<(int, int)>();
		var ss = new List<(int Index, int Length)>();
		var c = identifier[0];
		CharType ot =
			Char.IsUpper(c) ? CharType.Upper :
			Char.IsLower(c) ? CharType.Lower :
			Char.IsDigit(c) ? CharType.Digit : CharType.Other;

		int i0 = 0;

		for (int i = 1; i < identifier.Length; ++i)
		{
			c = identifier[i];
			CharType ct =
				Char.IsUpper(c) ? CharType.Upper :
				Char.IsLower(c) ? CharType.Lower :
				Char.IsDigit(c) ? CharType.Digit : CharType.Other;

			if (ct == ot)
				continue;

			if (ct > ot || ot == CharType.Other)
			{
				if (i > i0)
					ss.Add((i0, i - i0));
				i0 = i;
			}
			else if (ct == CharType.Lower && ot == CharType.Upper)
			{
				if (i > i0 + 1)
				{
					ss.Add((i0, i - i0 - 1));
					i0 = i - 1;
				}
			}

			ot = ct;
		}

		if (identifier.Length > i0)
			ss.Add((i0, identifier.Length - i0));
		return ss;
	}

	public static StringBuilder AppendArgName(this StringBuilder text, string? name)
	{
		if (name == null) return text;
		var parts = SplitByCapitals(name.AsSpan());
		name = name.ToLowerInvariant();
		if (parts.Count == 0)
			return text.Append(name);
		bool first = true;
		foreach (var part in parts)
		{
			if (first)
				first = false;
			else
				text.Append('-');
			text.Append(name, part.Index, part.Length);
		}
		return text;
	}

	private enum CharType
	{
		Digit,
		Lower,
		Upper,
		Other,
	}
}
