using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Arguments.Generator;

internal static class Extensions
{
	public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) => dictionary.TryGetValue(key, out var value) ? value : default;

	public static StringBuilder Indent(this StringBuilder text, int indent)
	{
		if (indent < 0) throw new ArgumentOutOfRangeException(nameof(indent), indent, null);
		return text.Append('\t', indent);
	}
}
