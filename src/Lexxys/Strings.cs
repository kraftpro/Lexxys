// Lexxys Infrastructural library.
// file: Tools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Reflection;

namespace Lexxys;

/// <summary>
/// String tools.
/// </summary>
public static partial class Strings
{
	/// <summary>
	/// Escapes string for use in C#/JavaScript.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <returns></returns>
	public static string EscapeCsString(string value)
		=> EscapeCsString(new StringBuilder(), value.AsSpan(), '"').ToString();

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified string's marker.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <returns></returns>
	public static string EscapeCsString(string value, char marker = '"', char escape = '\\')
		=> EscapeCsString(new StringBuilder(), value.AsSpan(), marker, escape).ToString();

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified string's marker.
	/// </summary>
	/// <param name="text">The string builder to append the result.</param>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <returns></returns>
	public static StringBuilder EscapeCsString(StringBuilder text, string value, char marker = '"', char escape = '\\')
		=> EscapeCsString(text, value.AsSpan(), marker, escape);

	/// <summary>
	/// Escapes string for use in C#/JavaScript.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <returns></returns>
	public static string EscapeCsString(ReadOnlySpan<char> value)
		=> EscapeCsString(value, '"');

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified string's marker.
	/// </summary>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <returns></returns>
	public static string EscapeCsString(ReadOnlySpan<char> value, char marker = '"', char escape = '\\')
		=> EscapeCsString(new StringBuilder(), value, marker, escape).ToString();

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified string's marker.
	/// </summary>
	/// <param name="text">The string builder to append the result.</param>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static StringBuilder EscapeCsString(StringBuilder text, ReadOnlySpan<char> value, char marker = '"', char escape = '\\')
	{
		if (text == null) throw new ArgumentNullException(nameof(text));

		if (marker != '\0')
			text.Append(marker);
		foreach (char c in value)
		{
			if (c < ' ' || c == 127)
			{
				text.Append(escape);
				switch (c)
				{
					case '\n':
						text.Append('n');
						break;
					case '\r':
						text.Append('r');
						break;
					case '\t':
						text.Append('t');
						break;
					case '\f':
						text.Append('f');
						break;
					case '\v':
						text.Append('v');
						break;
					case '\a':
						text.Append('a');
						break;
					case '\b':
						text.Append('b');
						break;
					case '\0':
						text.Append('0');
						break;
					default:
						text.Append("x00")
							.Append(HexDigits[(c & 0xF0) >> 4])
							.Append(HexDigits[c & 0xF]);
						break;
				}
			}
			else if (c >= '\xd800')
			{
				text.Append(escape).Append('x');
				text.Append(HexDigits[(c & 0xF000) >> 12]);
				text.Append(HexDigits[(c & 0xF00) >> 8]);
				text.Append(HexDigits[(c & 0xF0) >> 4]);
				text.Append(HexDigits[c & 0xF]);
			}
			else
			{
				if (c == marker || c == escape)
					text.Append(escape);
				text.Append(c);
			}
		}
		if (marker != '\0')
			text.Append(marker);
		return text;
	}

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified string's marker.
	/// </summary>
	/// <param name="text">The <see cref="TextWriter"/> to append the result.</param>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void EscapeCsString(TextWriter text, ReadOnlySpan<char> value, char marker = '"', char escape = '\\')
	{
		if (text == null) throw new ArgumentNullException(nameof(text));

		char[] buffer = ArrayPool<char>.Shared.Rent(6);
		try
		{
			if (marker != '\0')
				text.Write(marker);
			foreach (char c in value)
			{
				if (c < ' ' || c == 127)
				{
					buffer[0] = escape;
					int len = 2;
					switch (c)
					{
						case '\n':
							buffer[1] = 'n';
							break;
						case '\r':
							buffer[1] = 'r';
							break;
						case '\t':
							buffer[1] = 't';
							break;
						case '\f':
							buffer[1] = 'f';
							break;
						case '\v':
							buffer[1] = 'v';
							break;
						case '\a':
							buffer[1] = 'a';
							break;
						case '\b':
							buffer[1] = 'b';
							break;
						case '\0':
							buffer[1] = '0';
							break;
						default:
							buffer[1] = 'x';
							buffer[2] = '0';
							buffer[3] = '0';
							buffer[4] = HexDigits[(c & 0xF0) >> 4];
							buffer[5] = HexDigits[c & 0xF];
							len = 6;
							break;
					}
					text.Write(buffer, 0, len);
				}
				else if (c >= '\xd800')
				{
					buffer[0] = escape;
					buffer[1] = 'x';
					buffer[2] = HexDigits[(c & 0xF000) >> 12];
					buffer[3] = HexDigits[(c & 0xF00) >> 8];
					buffer[4] = HexDigits[(c & 0xF0) >> 4];
					buffer[5] = HexDigits[c & 0xF];
					text.Write(buffer, 0, 6);
				}
				else if (c == marker || c == escape)
				{
					buffer[0] = escape;
					buffer[1] = c;
					text.Write(buffer, 0, 2);
				}
				else
				{
					text.Write(c);
				}
			}
			if (marker != '\0')
				text.Write(marker);
		}
		finally
		{
			ArrayPool<char>.Shared.Return(buffer);
		}
	}
	private static readonly char[] HexDigits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];

	/// <summary>
	/// Escapes string for use in C#/JavaScript using the specified strings marker and appends the result to the specified UTF8 stream.
	/// </summary>
	/// <param name="stream">The <see cref="Stream"/> to append the result.</param>
	/// <param name="value">The string to escape.</param>
	/// <param name="marker">Strings merger.</param>
	/// <param name="escape">Escape character.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void EscapeUtf8CsString(Stream stream, ReadOnlySpan<char> value, char marker = '"', char escape = '\\')
	{
		if (stream == null) throw new ArgumentNullException(nameof(stream));
		int k = escape < 128 ? 1: Encoding.UTF8.GetByteCount([escape]);
		int len = (value.Length + 2) * (k + 5);
		byte[]? array = len > Tools.SafeStackAllocByte ? ArrayPool<byte>.Shared.Rent(len): null;
		var buffer = array == null ? stackalloc byte[len]: array.AsSpan();
		int i = 0;
		if (marker != '\0')
			if (marker < 128)
				buffer[i++] = (byte)marker;
			else
				i = WriteChar(buffer, i, marker);
		foreach (char c in value)
		{
			if (c < 128)
			{
				if (c >= ' ' && c < 127)
				{
					if (c == '\\' || c == marker)
						buffer[i++] = (byte)'\\';
					buffer[i++] = (byte)c;
				}
				else
				{
					buffer[i++] = (byte)escape;
					switch (c)
					{
						case '\n':
							buffer[i++] = (byte)'n';
							break;
						case '\r':
							buffer[i++] = (byte)'r';
							break;
						case '\t':
							buffer[i++] = (byte)'t';
							break;
						case '\f':
							buffer[i++] = (byte)'f';
							break;
						case '\v':
							buffer[i++] = (byte)'v';
							break;
						case '\a':
							buffer[i++] = (byte)'a';
							break;
						case '\b':
							buffer[i++] = (byte)'b';
							break;
						case '\0':
							buffer[i++] = (byte)'0';
							break;
						default:
							buffer[i++] = (byte)'u';
							buffer[i++] = (byte)'0';
							buffer[i++] = (byte)'0';
							buffer[i++] = HexByteDigits[(c & 0xF0) >> 4];
							buffer[i++] = HexByteDigits[c & 0xF];
							break;
					}
				}
			}
			else if (c >= '\xd800')
			{
				buffer[i++] = (byte)escape;
				buffer[i++] = (byte)'u';
				buffer[i++] = HexByteDigits[(c & 0xF000) >> 12];
				buffer[i++] = HexByteDigits[(c & 0xF00) >> 8];
				buffer[i++] = HexByteDigits[(c & 0xF0) >> 4];
				buffer[i++] = HexByteDigits[c & 0xF];
			}
			else
			{
				if (c == marker || c == escape)
					buffer[i++] = (byte)escape;
				i = WriteChar(buffer, i, c);
			}
		}
		if (marker != '\0')
			if (marker < 128)
				buffer[i++] = (byte)marker;
			else
				i = WriteChar(buffer, i, marker);

		stream.Write(buffer.Slice(0, i));
		if (array != null)
			ArrayPool<byte>.Shared.Return(array);

		static unsafe int WriteChar(Span<byte> buffer, int i, char value)
		{
			var bytes = stackalloc byte[4];
			var count = Encoding.UTF8.GetBytes(&value, 1, bytes, 4);
			while (--count >= 0)
					buffer[i++] = *bytes++;
			return i;
		}
	}
	private static readonly byte[] HexByteDigits = [(byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f'];

	/// <summary>
	/// Removes extra braces from the string.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string RemoveExtraBraces(string value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		
		var str = value.AsSpan().Trim();
		while (str.Length > 1 && str[0] == '(' && str[^1] == ')')
		{
			str = str.Slice(1, str.Length - 2).Trim();
		}
		return str.Length == value.Length ? value: str.ToString();
	}

	/// <summary>
	/// Splits the string <paramref name="identifier"/> by capital and by non-word letters.
	/// </summary>
	/// <param name="identifier">The string to split.</param>
	/// <returns></returns>
	public static IList<(int Index, int Length)> SplitByCapitals(string? identifier) => SplitByCapitals(identifier.AsSpan());

	/// <summary>
	/// Splits the string <paramref name="identifier"/> by capital and by non-word letters.
	/// </summary>
	/// <param name="identifier">The string to split.</param>
	/// <returns></returns>
	public static IList<(int Index, int Length)> SplitByCapitals(ReadOnlySpan<char> identifier)
	{
		if (identifier.Length == 0)
			return Array.Empty<(int, int)>();
		var ss = new List<(int Index, int Length)>();
		var c = identifier[0];
		CharType ot =
			Char.IsUpper(c) ? CharType.Upper:
			Char.IsLower(c) ? CharType.Lower:
			Char.IsDigit(c) ? CharType.Digit: CharType.Other;

		int i0 = 0;

		for (int i = 1; i < identifier.Length; ++i)
		{
			c = identifier[i];
			CharType ct =
				Char.IsUpper(c) ? CharType.Upper:
				Char.IsLower(c) ? CharType.Lower:
				Char.IsDigit(c) ? CharType.Digit: CharType.Other;

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

	/// <summary>
	/// Splits the string <paramref name="value"/> by word bound for the specified <paramref name="width"/>.
	/// </summary>
	/// <param name="value">The string to split.</param>
	/// <param name="width">Paragraph width.</param>
	/// <param name="count">Maximum number of lines or zero.</param>
	/// <param name="openBrace">Allow open brace being the last character of the line.</param>
	/// <returns></returns>
	public static IList<(int Index, int Length)> SplitByWordBound(string? value, int width, int count = 0, bool openBrace = false) => SplitByWordBound(value.AsSpan(), width, count, openBrace);

	/// <summary>
	/// Splits the string <paramref name="value"/> by word bound for the specified <paramref name="width"/>.
	/// </summary>
	/// <param name="value">The string to split.</param>
	/// <param name="width">Paragraph width.</param>
	/// <param name="count">Maximum number of lines or zero.</param>
	/// <param name="openBrace">Allow open brace being the last character of the line.</param>
	/// <returns></returns>
	public static IList<(int Index, int Length)> SplitByWordBound(ReadOnlySpan<char> value, int width, int count = 0, bool openBrace = false)
	{
		if (count == 1 || value.Length <= width)
			return value.Length == 0 ? Array.Empty<(int, int)>(): new[] { (0, value.Length) };

		var list = new List<(int, int)>();
		int ix = 0;
		while (value.Length > 0)
		{
			while (value.Length > 0 && Char.IsWhiteSpace(value[0]))
			{
				value = value.Slice(1);
				++ix;
			}
			if (list.Count == count - 1)
			{
				if (value.Length > 0)
					list.Add((ix, value.Length));
				break;
			}

			int bound = GetBound(value, width, openBrace);
			if (bound > 0)
			{
				int i;
				for (i = 0; i < bound; ++i)
				{
					if (!Char.IsWhiteSpace(value[bound - (i + 1)]))
						break;
				}
				if (bound > i)
					list.Add((ix, bound - i));
				value = value.Slice(bound);
				ix += bound;
			}
		}
		return list;
	}

	private static int GetBound(ReadOnlySpan<char> value, int width, bool openBrace)
	{
		int ii = value.IndexOfAny(CrLf);
		if (ii >= 0 && ii <= width)
		{
			while (ii > 0 && Char.IsWhiteSpace(value[ii - 1]))
				--ii;
			return ii;
		}
		if (width >= value.Length)
			return value.Length;
		for (int i = width; i > 0; --i)
		{
			char c = value[i];
			if (Char.IsLetter(c))
				continue;
			if (Char.IsWhiteSpace(c))
				return i;
			if (!openBrace)
				continue;
			if (c is '(' or '[' or '{')
				return i;
			if (i < width)
				return i + 1;
		}
		return width;
	}
	private static readonly char[] CrLf = ['\n', '\r'];

	/// <summary>
	/// Converts the first character of the string to upper case and the rest of string to the lower case.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToTitleCase(string value) => value is null ? throw new ArgumentNullException(nameof(value)): ToTitleCase(value.AsSpan());

	/// <summary>
	/// Converts the first character of the string to upper case and the rest of string to the lower case.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static unsafe string ToTitleCase(ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
			return String.Empty;
		Span<char> a = value.Length < Tools.MaxStackAllocSize ? stackalloc char[value.Length]: new char[value.Length];
		a[0] = Char.ToUpperInvariant(value[0]);
		value.Slice(1).ToLowerInvariant(a.Slice(1));
		return a.ToString();
	}

	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the camel case naming convention.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToCamelCase(string value) => value is null ? throw new ArgumentNullException(nameof(value)): ToCamelCase(value.AsSpan());

	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the camel case naming convention.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToCamelCase(ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
			return String.Empty;

		bool upper = false;
		Span<char> ax = value.Length < Tools.MaxStackAllocSize ? stackalloc char[value.Length]: new char[value.Length];
		value.ToLowerInvariant(ax);
		foreach (var (index, length) in SplitByCapitals(value))
		{
			char f = ax[index];
			if (length == 1 && f == '_')
				continue;
			bool letter = Char.IsLetter(f); 
			if (letter && upper)
				ax[index] = Char.ToUpperInvariant(f);
			upper = letter;
		}
		return ax.ToString();
	}

	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the camel case naming convention.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToPascalCase(string value) => value is null ? throw new ArgumentNullException(nameof(value)): ToPascalCase(value.AsSpan());

	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the pascal case naming convention.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToPascalCase(ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
			return String.Empty;

		Span<char> ax = value.Length < Tools.MaxStackAllocSize ? stackalloc char[value.Length]: new char[value.Length];
		value.ToLowerInvariant(ax);
		foreach (var (index, _) in SplitByCapitals(value))
		{
			ax[index] = Char.ToUpperInvariant(ax[index]);
		}
		return ax.ToString();
	}

	public delegate void Converter(ReadOnlySpan<char> span, Span<char> buffer);
	
	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the names separated by the specified dash.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <param name="convert">Converts the word to the desired case.</param>
	/// <param name="dash">Dash characters. The first character is used as a separator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToDashed(string value, Converter? convert, char[]? dash = null) => value is null ? throw new ArgumentNullException(nameof(value)): ToDashed(value.AsSpan(), convert, dash);

	/// <summary>
	/// Converts the specified string <paramref name="value"/> to the names separated by the specified dash.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <param name="convert">Converts the word to the desired case.</param>
	/// <param name="dash">Dash characters. The first character is used as a separator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToDashed(ReadOnlySpan<char> value, Converter? convert, char[]? dash = null)
	{
		if (value.Length == 0)
			return String.Empty;

		if (dash is not { Length: >0 })
			dash = __dashes;

		var c = dash[0];

		var ix = 0;
		int len = value.Length * 2;
		char[]? buffer = len > Tools.MaxStackAllocSize ? ArrayPool<char>.Shared.Rent(len): null;
		var ax = buffer == null ? stackalloc char[len]: buffer.AsSpan();
		foreach (var (index, length) in SplitByCapitals(value))
		{
			var s = value.Slice(index, length).Trim(dash);
			if (s.Length == 0 || (s.Length == 1 && s[0] == '_'))
				continue;
			if (ix > 0)
			{
				ax[ix] = c;
				++ix;
			}
			if (convert == null)
				s.CopyTo(ax.Slice(ix));
			else
				convert(s, ax.Slice(ix));
			ix += length;
		}
		var result = ax.Slice(0, ix).ToString();
		if (buffer != null)
			ArrayPool<char>.Shared.Return(buffer);
		return result;
	}
	private static readonly char[] __dashes = ['-'];

	/// <summary>
	/// Converts the specified string <paramref name="value"/> according to the specified naming rule.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <param name="rule">The naming rule.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToNamingRule(string value, NamingCaseRule rule) => value is null ? throw new ArgumentNullException(nameof(value)): ToNamingRule(value.AsSpan(), rule);

	/// <summary>
	/// Converts the specified string <paramref name="value"/> according to the specified naming rule.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <param name="rule">The naming rule.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string ToNamingRule(ReadOnlySpan<char> value, NamingCaseRule rule)
	{
		if (value.Length == 0) return String.Empty;

		char[]? delimiter = (rule & NamingCaseRule.Separators) switch
		{
			NamingCaseRule.Underscore => __underscoreChars,
			NamingCaseRule.Dash => __dashChars,
			NamingCaseRule.Dot => __dotChars,
			_ => null
		};

		return (rule & ~(NamingCaseRule.Force | NamingCaseRule.Separators)) switch
		{
			NamingCaseRule.PreferLowerCase => delimiter is null ? ToLowerString(value): ToDashed(value, ToLower, delimiter),
			NamingCaseRule.PreferCamelCase => delimiter is null ? ToCamelCase(value): FirstCharToLower(ToDashed(value, ToPascal, delimiter)),
			NamingCaseRule.PreferPascalCase => delimiter is null ? ToPascalCase(value): ToDashed(value, ToPascal, delimiter),
			NamingCaseRule.PreferUpperCase => delimiter is null ? ToUpperString(value): ToDashed(value, ToUpper, delimiter),
			_ => value.ToString(),
		};
		
		static void ToLower(ReadOnlySpan<char> span, Span<char> buffer) => span.ToLowerInvariant(buffer);

		static void ToUpper(ReadOnlySpan<char> span, Span<char> buffer) => span.ToUpperInvariant(buffer);
		
		static void ToPascal(ReadOnlySpan<char> span, Span<char> buffer)
		{
			span.ToLowerInvariant(buffer);
			buffer[0] = Char.ToUpperInvariant(buffer[0]);
		}

		static string ToLowerString(ReadOnlySpan<char> span)
		{
			Span<char> buffer = span.Length < Tools.MaxStackAllocSize ? stackalloc char[span.Length]: new char[span.Length];
			span.ToLowerInvariant(buffer);
			return buffer.ToString();
		}

		static string ToUpperString(ReadOnlySpan<char> span)
		{
			Span<char> buffer = span.Length < Tools.MaxStackAllocSize ? stackalloc char[span.Length]: new char[span.Length];
			span.ToUpperInvariant(buffer);
			return buffer.ToString();
		}

		static string FirstCharToLower(string value)
		{
			if (value is not { Length: > 0 } || Char.IsLower(value[0]))
				return value;
#if NET5_0_OR_GREATER
			var s = value.AsSpan();
			Span<char> buffer = [Char.ToLowerInvariant(s[0])];
			return String.Concat(buffer, s.Slice(1));
#else
			return value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
#endif
		}
	}
	private static readonly char[] __dashChars = ['-', '_', '.'];
	private static readonly char[] __underscoreChars = ['_', '-', '.'];
	private static readonly char[] __dotChars = ['.', '_', '-'];

	/// <summary>
	/// Adds ellipsis to the string if it is longer than the specified length.
	/// </summary>
	/// <param name="value">The string to process.</param>
	/// <param name="length">Required length.</param>
	/// <param name="ellipsis">The ellipsis string. (default "&#2026;")</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string Ellipsis(string value, int length, string? ellipsis = null)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length <= length)
			return value;
		ellipsis ??= "\u2026";
		return length <= ellipsis.Length ? ellipsis.Substring(0, length): value.Substring(0, length - ellipsis.Length) + ellipsis;
	}

	/// <summary>
	/// Joins the specified values using the specified <paramref name="comma"/> as a regular separator and <paramref name="and"/> as the last separator.
	/// </summary>
	/// <param name="values">Collection of values to join.</param>
	/// <param name="comma">Regular separator. (default ", ")</param>
	/// <param name="and">Last separator. (default " and ")</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static string JoinAnd(IEnumerable<string> values, string? comma = null, string? and = null)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		var text = new StringBuilder();
		string value = "";
		bool one = false;
		bool two = false;
		string pad = comma ?? ", ";
		foreach (var item in values)
		{
			if (String.IsNullOrEmpty(item)) continue;
			if (!one)
			{
				one = true;
			}
			else
			{
				if (two)
					text.Append(pad);
				else
					two = true;
				text.Append(value);
			}
			value = item;
		}
		if (text.Length == 0)
			return value;
		if (two && comma == null && and == null)
			text.Append(", and ").Append(value);
		else
			text.Append(and ?? " and ").Append(value);
		return text.ToString();
	}

	private enum CharType
	{
		Digit,
		Lower,
		Upper,
		Other,
	}

	private static unsafe void ToHexCharArrayInternal(byte[] bitsValue, int offset, int length, char[] hexValue, int outOffset)
	{
		fixed (byte* bits = bitsValue)
		fixed (char* hexv = hexValue)
		fixed (char* dgts = HexDigits)
		{
			var b = bits + offset;
			var h = hexv + outOffset;
			while (--length >= 0)
			{
				*h++ = dgts[(*b) >> 4];
				*h++ = dgts[(*b) & 15];
				++b;
			}
		}
	}

	public static void ToHexCharArray(byte[] bitsValue, int offset, int length, char[] hexValue, int outOffset)
	{
		if (bitsValue == null)
			throw new ArgumentNullException(nameof(bitsValue));
		if (hexValue == null)
			throw new ArgumentNullException(nameof(hexValue));
		if (offset < 0 || offset + length > bitsValue.Length)
			if (offset < 0 || offset > bitsValue.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			else
				throw new ArgumentOutOfRangeException(nameof(length));
		if (outOffset < 0 || outOffset + 2*length > hexValue.Length)
			if (outOffset < 0 || outOffset > hexValue.Length)
				throw new ArgumentOutOfRangeException(nameof(outOffset));
			else
				throw new ArgumentOutOfRangeException(nameof(length));
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length));

		ToHexCharArrayInternal(bitsValue, offset, length, hexValue, outOffset);
	}

	public static char[] ToHexCharArray(byte[] value, int offset, int length)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (offset < 0 || offset + length > value.Length)
			if (offset < 0 || offset > value.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));
			else
				throw new ArgumentOutOfRangeException(nameof(length));
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length));

		char[] chars = new char[length * 2];
		ToHexCharArrayInternal(value, offset, length, chars, 0);
		return chars;
	}

	public static char[] ToHexCharArray(byte[] value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		int length = value.Length;
		char[] chars = new char[length*2];
		ToHexCharArrayInternal(value, 0, length, chars, 0);
		return chars;
	}

	public static string ToHexString(byte[] value, ReadOnlySpan<char> prefix = default)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		int offset = prefix.Length;
		int len = offset + value.Length * 2;
		char[]? array = len > Tools.SafeStackAllocChar ? ArrayPool<char>.Shared.Rent(len): null;
		Span<char> s = array == null ? stackalloc char[len]: array.AsSpan();
		Span<char> t = s;
		if (offset > 0)
		{
			prefix.CopyTo(t);
			t = t.Slice(offset);
		}
		foreach (var b in value.AsSpan())
		{
			t[0] = HexDigits[(b & 0xF0) >> 4];
			t[1] = HexDigits[b & 0x0F];
			t = t.Slice(2);
		}
		var result = s.ToString();
		if (array != null)
			ArrayPool<char>.Shared.Return(array);
		return result;
	}

	public static string ToBitsString(byte[] value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length == 0)
			return "";
		var text = new StringBuilder(value.Length * 9);
		foreach (var b in value)
		{
			text.Append(__bits[b >> 4]).Append(__bits[b & 0x0F]).Append(' ');
		}
		--text.Length;
		return text.ToString();
	}
	private static readonly string[] __bits =
	[
		"0000", "0001", "0010", "0011",
		"0100", "0101", "0110", "0111",
		"1000", "1001", "1010", "1011",
		"1100", "1101", "1110", "1111",
	];

	public static string CutIndents(IReadOnlyCollection<string?> source, int tabSize = 4, string? newLine = null)
	{
		if (tabSize is <1 or >32)
			throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

		if (source.Count <= 1)
			return source.FirstOrDefault()?.Trim() ?? String.Empty;

		newLine ??= Environment.NewLine;
		var result = new StringBuilder(source.Count * 80);
		int indent = Int32.MaxValue;

		foreach (var t in source)
		{
			var s = t.AsSpan();
			if (s.IsWhiteSpace())
				continue;
			indent = GetIndent(s, indent, tabSize);
			if (indent == 0)
				break;
		}

		if (indent is 0 or Int32.MaxValue)
		{
			foreach (var s in source)
			{
				result.Append(s.AsSpan().TrimEnd()).Append(newLine);
			}
			return result.ToString(0, result.Length - newLine.Length);
		}

		foreach (var t in source)
		{
			var s = t.AsSpan().TrimEnd();
			if (s.Length == 0)
			{
				result.Append(newLine);
				continue;
			}
			int column = 0;
			int j;
			for (j = 0; j < s.Length; ++j)
			{
				if (s[j] == '\t')
					column += tabSize - (column % tabSize);
				else if (s[j] == ' ')
					++column;
				else
					break;
			}
			if (column > indent)
				result.Append(' ', column - indent);
			result.Append(s.Slice(j)).Append(newLine);
		}

		return result.ToString(0, result.Length - newLine.Length);
	}

	private static int GetIndent(ReadOnlySpan<char> s, int indent, int tabSize)
	{
		int column = 0;
		foreach (var c in s)
		{
			if (c == '\t')
				column += tabSize - (column % tabSize);
			else if (c == ' ')
				++column;
			else
				break;
			if (column >= indent)
				break;
		}
		return Math.Min(column, indent);
	}

	public static string CutIndents(ReadOnlySpan<char> text, int tabSize = 4)
	{
		if (tabSize is <1 or >32)
			throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

		if (text.Length == 0)
			return String.Empty;

		var p = NextNewLine(text);
		if (p.Nl == 0)
			return text.Trim().ToString();
		var result = new StringBuilder(text.Length);
		int indent = Int32.MaxValue;

		var rest = text;
		do
		{
			var s = rest.Slice(0, p.Index);
			if (!s.IsWhiteSpace())
			{
				indent = GetIndent(s, indent, tabSize);
				if (indent == 0)
					break;
			}
			rest = rest.Slice(p.Index + p.Nl);
			p = NextNewLine(rest);
		} while (p.Nl > 0);
		if (indent > 0 && !rest.IsWhiteSpace())
			indent = GetIndent(rest, indent, tabSize);
		
		if (indent is 0 or Int32.MaxValue)
		{
			rest = text;
			p = NextNewLine(rest);
			do
			{
				result.Append(rest.Slice(0, p.Index).TrimEnd()).Append(rest.Slice(p.Index, p.Nl));
				rest = rest.Slice(p.Index + p.Nl);
				p = NextNewLine(rest);
			} while (p.Nl > 0);

			result.Append(rest.TrimEnd());
			return result.ToString();
		}

		rest = text;
		p = NextNewLine(rest);
		do
		{
			var s = rest.Slice(0, p.Index).TrimEnd();
			AppendLine(s);
			result.Append(rest.Slice(p.Index, p.Nl));
			rest = rest.Slice(p.Index + p.Nl);
			p = NextNewLine(rest);
		} while (p.Nl > 0);

		AppendLine(rest.TrimEnd());
		return result.ToString();

		static (int Index, int Nl) NextNewLine(ReadOnlySpan<char> text)
		{
			int k = text.IndexOfAny(CrLf);
			if (k < 0)
				return (k, 0);
			if (k < text.Length - 1 && text[k + 1] == (text[k] ^ ('\r' ^ '\n')))
				return (k, 2);
			return (k, 1);
		}

		void AppendLine(ReadOnlySpan<char> value)
		{
			if (value.Length == 0)
				return;
			int column = 0;
			int j;
			for (j = 0; j < value.Length; ++j)
			{
				if (value[j] == '\t')
					column += tabSize - (column % tabSize);
				else if (value[j] == ' ')
					++column;
				else
					break;
			}
			if (column > indent)
				result.Append(' ', column - indent);
			result.Append(value.Slice(j));
		}
	}
}
