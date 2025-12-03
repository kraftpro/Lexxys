using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Argument.Generator;

internal static class Extensions
{
	public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) => dictionary.TryGetValue(key, out var value) ? value: default;

	public static StringBuilder Indent(this StringBuilder text, int indent)
	{
		if (indent < 0) throw new ArgumentOutOfRangeException(nameof(indent), indent, null);
		if (indent == 0 || ArgumentCodeGen.IndentString.Length == 0) return text;
		if (ArgumentCodeGen.IndentString.Length == 1)
			return text.Append(ArgumentCodeGen.IndentString[0], indent);

		for (int i = 0; i < indent; ++i)
		{
			text.Append(ArgumentCodeGen.IndentString);
		}
		return text;
	}

	public static StringBuilder AppendArgumentName(this StringBuilder text, ArgumentPropertyModel item, CliArgumentsModel? arg)
	{
		if (item.ExplicitName is not null)
			return text.Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(item.ExplicitName, quote: true));

		var namingStyleText = arg?.NamingStyle;
		if (!string.IsNullOrEmpty(namingStyleText))
		{
			var i = namingStyleText!.IndexOf('.');
			if (i >= 0)
				namingStyleText = namingStyleText.Substring(i + 1);
		}
		var namingStyle = namingStyleText?.ToUpperInvariant() switch
		{
			"PASCALECASE" => ParameterNaming.PascalCase,
			"CAMELCASE" => ParameterNaming.CamelCase,
			"SNAKECASE" => ParameterNaming.SnakeCase,
			"DOTCASE" => ParameterNaming.DotCase,
			_ => ParameterNaming.KebabCase
		};

		var name = GetArgumentName(item.MemberName ?? String.Empty, namingStyle);
		return text.Append(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(name, quote: true));
	}

	public enum ParameterNaming
	{
		/// <summary>
		/// Kebab-case (e.g. <c>--output-directory</c>)
		/// </summary>
		KebabCase,
		/// <summary>
		/// CamelCase (e.g. <c>--outputDirectory</c>) or PascalCase (e.g. <c>--OutputDirectory</c>), depending on the case of the first character.
		/// </summary>
		CamelCase,
		/// <summary>
		/// PascalCase (e.g. <c>--OutputDirectory</c>)
		/// </summary>
		PascalCase,
		/// <summary>
		/// Snake_case (e.g. <c>--output_directory</c>)
		/// </summary>
		SnakeCase,
		/// <summary>
		/// Dot.case (e.g. <c>--output.directory</c>)
		/// </summary>
		DotCase,
	}

	public static string GetArgumentName(string name, ParameterNaming namingStyle)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));
		if (name.Length == 0) return name;
		if (name.IndexOfAny(['-', '_', '.', ' ']) >= 0)
			return name.Trim().ToLowerInvariant().Replace(' ', namingStyle switch
			{
				ParameterNaming.SnakeCase => '_',
				ParameterNaming.DotCase => '.',
				_ => '-'
			});
		if (namingStyle == ParameterNaming.CamelCase)
			return char.ToLowerInvariant(name[0]) + name.Substring(1);
		if (namingStyle == ParameterNaming.PascalCase)
			return char.ToUpperInvariant(name[0]) + name.Substring(1);
		char separator = namingStyle switch
		{
			ParameterNaming.SnakeCase => '_',
			ParameterNaming.DotCase => '.',
			_ => '-'
		};
		var parts = Strings.SplitByCapitals(name.AsSpan());
		if (parts.Count == 0)
			return name.ToLowerInvariant();
		var result = new StringBuilder();
		var lower = name.ToLowerInvariant();
		for (int i = 0; i < parts.Count; ++i)
		{
			if (i > 0)
				result.Append(separator);
			result.Append(lower, parts[i].Index, parts[i].Length);
		}
		return result.ToString();
	}
}

public static class Strings
{
	private enum CharType
	{
		Digit,
		Lower,
		Upper,
		Other,
	}

	public static List<(int Index, int Length)> SplitByCapitals(ReadOnlySpan<char> identifier)
	{
		if (identifier.Length == 0)
			return [];

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
}
