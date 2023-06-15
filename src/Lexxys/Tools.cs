// Lexxys Infrastructural library.
// file: Tools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lexxys;

public static class Comparer
{
	public static IComparer<T> Create<T>(Func<T, T, int> compare)
		=> new GenericComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)));

	public static IComparer Create<T1, T2>(Func<T1, T2, int> compare)
		=> new GenericComparer<T1, T2>(compare ?? throw new ArgumentNullException(nameof(compare)));

	public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare)
		=> new GenericEqualityComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)));

	public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare, Func<T, int> hash)
		=> new GenericEqualityComparer<T>(compare ?? throw new ArgumentNullException(nameof(compare)), hash ?? throw new ArgumentNullException(nameof(hash)));

	public static bool Equals<T>(IEnumerable<T>? left, IEnumerable<T>? right, IEqualityComparer<T>? comparer = null)
	{
		if (left == null || right == null)
			return Object.ReferenceEquals(left, right);

		if (left is ICollection cl && right is ICollection cr && cl.Count != cr.Count)
			return false;

		comparer ??= EqualityComparer<T>.Default;

		using IEnumerator<T> enumerator1 = left.GetEnumerator();
		using IEnumerator<T> enumerator2 = right.GetEnumerator();
		while (enumerator1.MoveNext())
		{
			if (!enumerator2.MoveNext())
				return false;
			if (!comparer.Equals(enumerator1.Current, enumerator2.Current))
				return false;
		}
		return !enumerator2.MoveNext();
	}

	public static int CompareBytes(byte[]? left, byte[]? right, bool forEqualsOnly)
	{
		if (left == null)
			return (right == null) ? 0: -1;
		if (right == null)
			return 1;
		if (forEqualsOnly && left.Length != right.Length)
			return -2;
		int len = left.Length > right.Length ? right.Length: left.Length;
		for (int i = 0; i < len; ++i)
		{
			if (left[i] != right[i])
				return (left[i] > right[i]) ? 1: -1;
		}
		return (left.Length > len) ? 1 :
			(right.Length > len) ? -1 : 0;
	}

	#region Internal classes

	private class GenericComparer<T1, T2>: IComparer
	{
		private readonly Func<T1, T2, int> _compare;

		public GenericComparer(Func<T1, T2, int> compare) => _compare = compare;

		public int Compare(object? x, object? y) => x is T1 t1 && y is T2 t2 ? _compare(t1, t2) : 2;
	}

	private class GenericComparer<T>: IComparer<T>
	{
		private readonly Func<T, T, int> _compare;

		public GenericComparer(Func<T, T, int> compare) => _compare = compare;

		public int Compare(T? x, T? y) => x is null ? (y is null ? 0 : 1) : y is null ? -1 : _compare(x, y);
	}

	private class GenericEqualityComparer<T>: IEqualityComparer<T>
	{
		private readonly Func<T, T, bool> _equals;
		private readonly Func<T, int> _hash;

		public GenericEqualityComparer(Func<T, T, bool> equals)
			=> (_equals, _hash) = (equals, o => o?.GetHashCode() ?? 0);

		public GenericEqualityComparer(Func<T, T, bool> equals, Func<T, int> hash)
			=> (_equals, _hash) = (equals, hash);

		public bool Equals(T? x, T? y) => x is null ? y is null : y is not null && _equals(x, y);

		public int GetHashCode(T obj) => _hash(obj);
	}

	#endregion
}

public static class HashCode
{
	public static int Join(int h1, int h2)
	{
		return ((h1 << 5) + h1) ^ h2;
	}

	public static int Join(int h1, int h2, int h3)
	{
		int h = ((h1 << 5) + h1) ^ h2;
		return ((h << 5) + h) ^ h3;
	}

	public static int Join(int h1, int h2, int h3, int h4)
	{
		int h = ((h1 << 5) + h1) ^ h2;
		h = ((h << 5) + h) ^ h3;
		return ((h << 5) + h) ^ h4;
	}

	public static int Join(int h1, params int[]? hh)
	{
		if (hh == null || hh.Length == 0)
			return h1;
		int h = h1;
		for (int i = 0; i < hh.Length; ++i)
		{
			h = ((h << 5) + h) ^ hh[i];
		}
		return h;
	}

	public static int Join(int offset, IEnumerable<int>? items)
	{
		if (items != null)
		{
			foreach (int h in items)
			{
				offset += ((offset << 5) + offset) ^ h;
			}
		}
		return offset;
	}

	public static int Join<T>(int offset, IEnumerable<T>? items)
	{
		if (items != null)
		{
			foreach (var t in items)
			{
				offset += ((offset << 5) + offset) ^ (t?.GetHashCode() ?? 0);
			}
		}
		return offset;
	}
}

public static class Strings
{
	public static string EscapeCsString(string value)
		=> EscapeCsString(new StringBuilder(), value.AsSpan(), '"').ToString();

	public static string EscapeCsString(string value, char marker)
		=> EscapeCsString(new StringBuilder(), value.AsSpan(), marker).ToString();

	public static StringBuilder EscapeCsString(StringBuilder text, string value, char marker = '"')
		=> EscapeCsString(text, value.AsSpan(), marker);

	public static string EscapeCsString(ReadOnlySpan<char> value)
		=> EscapeCsString(value, '"');

	public static string EscapeCsString(ReadOnlySpan<char> value, char marker)
		=> EscapeCsString(new StringBuilder(), value, marker).ToString();

	public static StringBuilder EscapeCsString(StringBuilder text, ReadOnlySpan<char> value, char marker = '"')
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (marker != '\0')
			text.Append(marker);
		foreach (char c in value)
		{
			if (c < ' ' || c == 127)
			{
				switch (c)
				{
					case '\n':
						text.Append("\\n");
						break;
					case '\r':
						text.Append("\\r");
						break;
					case '\t':
						text.Append("\\t");
						break;
					case '\f':
						text.Append("\\f");
						break;
					case '\v':
						text.Append("\\v");
						break;
					case '\a':
						text.Append("\\a");
						break;
					case '\b':
						text.Append("\\b");
						break;
					case '\0':
						text.Append("\\0");
						break;
					default:
						text.Append("\\x00");
						text.Append(__hex[(c & 0xF0) >> 4]);
						text.Append(__hex[c & 0xF]);
						break;
				}
			}
			else if (c >= '\xd800')
			{
				text.Append("\\x");
				text.Append(__hex[(c & 0xF000) >> 12]);
				text.Append(__hex[(c & 0xF00) >> 8]);
				text.Append(__hex[(c & 0xF0) >> 4]);
				text.Append(__hex[c & 0xF]);
			}
			else if (c == marker)
			{
				text.Append('\\').Append(marker);
			}
			else if (c == '\\')
			{
				text.Append("\\\\");
			}
			else
			{
				text.Append(c);
			}
		}
		if (marker != '\0')
			text.Append(marker);
		return text;
	}

	public static void EscapeCsString(TextWriter text, ReadOnlySpan<char> value, char marker = '"')
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (marker != '\0')
			text.Write(marker);
		foreach (char c in value)
		{
			if (c < ' ' || c == 127)
			{
				switch (c)
				{
					case '\n':
						text.Write("\\n");
						break;
					case '\r':
						text.Write("\\r");
						break;
					case '\t':
						text.Write("\\t");
						break;
					case '\f':
						text.Write("\\f");
						break;
					case '\v':
						text.Write("\\v");
						break;
					case '\a':
						text.Write("\\a");
						break;
					case '\b':
						text.Write("\\b");
						break;
					case '\0':
						text.Write("\\0");
						break;
					default:
						text.Write("\\x00");
						text.Write(__hex[(c & 0xF0) >> 4]);
						text.Write(__hex[c & 0xF]);
						break;
				}
			}
			else if (c >= '\xd800')
			{
				text.Write("\\x");
				text.Write(__hex[(c & 0xF000) >> 12]);
				text.Write(__hex[(c & 0xF00) >> 8]);
				text.Write(__hex[(c & 0xF0) >> 4]);
				text.Write(__hex[c & 0xF]);
			}
			else if (c == marker)
			{
				text.Write('\\');
				text.Write(marker);
			}
			else if (c == '\\')
			{
				text.Write("\\\\");
			}
			else
			{
				text.Write(c);
			}
		}
		if (marker != '\0')
			text.Write(marker);
	}

	public static IEnumerable<char> EscapeCsCharArray(IEnumerable<char> value, char marker = '"')
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (marker != '\0')
			yield return marker;
		foreach (char c in value)
		{
			if (c < ' ' || c == 127)
			{
				switch (c)
				{
					case '\n':
						yield return '\\';
						yield return 'n';
						break;
					case '\r':
						yield return '\\';
						yield return 'r';
						break;
					case '\t':
						yield return '\\';
						yield return 't';
						break;
					case '\f':
						yield return '\\';
						yield return 'f';
						break;
					case '\v':
						yield return '\\';
						yield return 'v';
						break;
					case '\a':
						yield return '\\';
						yield return 'a';
						break;
					case '\b':
						yield return '\\';
						yield return 'b';
						break;
					case '\0':
						yield return '\\';
						yield return '0';
						break;
					default:
						yield return '\\';
						yield return 'x';
						yield return '0';
						yield return '0';
						yield return __hex[(c & 0xF0) >> 4];
						yield return __hex[c & 0xF];
						break;
				}
			}
			else if (c >= '\xd800')
			{
				yield return '\\';
				yield return 'x';
				yield return __hex[(c & 0xF000) >> 12];
				yield return __hex[(c & 0xF00) >> 8];
				yield return __hex[(c & 0xF0) >> 4];
				yield return __hex[c & 0xF];
			}
			else if (c == marker)
			{
				yield return '\\';
				yield return marker;
			}
			else if (c == '\\')
			{
				yield return '\\';
				yield return '\\';
			}
			else
			{
				yield return c;
			}
		}
		if (marker != '\0')
			yield return marker;
	}
	private static readonly char[] __hex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

	public static void EscapeUtf8CsString(Stream stream, ReadOnlySpan<char> value, char marker = '"')
	{
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		if (marker != '\0')
			if (marker < 128)
				stream.WriteByte((byte)marker);
			else
				WriteChar(stream, marker);
		foreach (char c in value)
		{
			if (c < 128)
			{
				if (c >= ' ' && c < 127)
				{
					if (c == '\\' || c == marker)
						stream.WriteByte((byte)'\\');
					stream.WriteByte((byte)c);
				}
				else
				{
					stream.WriteByte((byte)'\\');
					switch (c)
					{
						case '\n':
							stream.WriteByte((byte)'n');
							break;
						case '\r':
							stream.WriteByte((byte)'r');
							break;
						case '\t':
							stream.WriteByte((byte)'t');
							break;
						case '\f':
							stream.WriteByte((byte)'f');
							break;
						case '\v':
							stream.WriteByte((byte)'v');
							break;
						case '\a':
							stream.WriteByte((byte)'a');
							break;
						case '\b':
							stream.WriteByte((byte)'b');
							break;
						case '\0':
							stream.WriteByte((byte)'0');
							break;
						default:
							stream.WriteByte((byte)'u');
							stream.WriteByte((byte)'0');
							stream.WriteByte((byte)'0');
							stream.WriteByte(__hexB[(c & 0xF0) >> 4]);
							stream.WriteByte(__hexB[c & 0xF]);
							break;
					}
				}
			}
			else if (c >= '\xd800')
			{
				stream.WriteByte((byte)'\\');
				stream.WriteByte((byte)'u');
				stream.WriteByte(__hexB[(c & 0xF000) >> 12]);
				stream.WriteByte(__hexB[(c & 0xF00) >> 8]);
				stream.WriteByte(__hexB[(c & 0xF0) >> 4]);
				stream.WriteByte(__hexB[c & 0xF]);
			}
			else
			{
				if (c == marker)
					stream.WriteByte((byte)'\\');
				WriteChar(stream, c);
			}
		}
		if (marker != '\0')
			if (marker < 128)
				stream.WriteByte((byte)marker);
			else
				WriteChar(stream, marker);

		static unsafe void WriteChar(Stream memory, char value)
		{
			var bytes = stackalloc byte[4];
			var count = Encoding.UTF8.GetBytes(&value, 1, bytes, 4);
			while (--count >= 0)
					memory.WriteByte(*bytes++);
		}
	}
	private static readonly byte[] __hexB = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };

	[return: NotNullIfNotNull(nameof(value))]
	public static string? RemoveExtraBraces(string? value)
	{
		if (value == null)
			return default;
		var str = value.AsSpan().Trim();
		while (str.Length > 1 && str[0] == '(' && str[str.Length - 1] == ')')
		{
			str = str.Slice(1, str.Length - 2).Trim();
		}
		return str.Length == value.Length ? value: str.ToString();
	}

	public static IList<(int Index, int Length)> SplitByCapitals(string? identifier)
	{
		return identifier == null ? Array.Empty<(int, int)>(): SplitByCapitals(identifier.AsSpan());
	}

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

	public static IList<(int Index, int Length)> SplitByWordBound(string? value, int width, int count = 0)
	{
		if (value == null)
			return Array.Empty<(int, int)>();
		if (count == 1 || value.Length <= width)
			return new[] { (0, value.Length) };
		var list = new List<(int, int)>();
		var span = value.AsSpan();
		int ix = 0;
		while (span.Length > 0)
		{
			while (span.Length > 0 && Char.IsWhiteSpace(span[0]))
			{
				span = span.Slice(1);
				++ix;
			}
			if (list.Count == count - 1)
			{
				if (span.Length > 0)
					list.Add((ix, span.Length));
				break;
			}

			int bound = GetBound(span, width);
			if (bound > 0)
			{
				int i;
				for (i = 0; i < bound; ++i)
				{
					if (!Char.IsWhiteSpace(span[bound - (i + 1)]))
						break;
				}
				if (bound > i)
					list.Add((ix, bound - i));
				span = span.Slice(bound);
				ix += bound;
			}
		}
		return list;
	}

	private static int GetBound(ReadOnlySpan<char> value, int width)
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
			if (char.IsLetter(c))
				continue;
			if (c == '(' || c == '[' || c == '{' || Char.IsWhiteSpace(c))
				return i;
			if (i < width)
				return i + 1;
		}
		return width;
	}
	private static readonly char[] CrLf = {'\n', '\r'};

	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToTitleCase(string? value)
	{
		if (value is not { Length: >0 })
			return value;
		var a = new char[value.Length];
		a[0] = Char.ToUpperInvariant(value[0]);
		value.AsSpan(1).ToLowerInvariant(a.AsSpan(1));
		return new String(a);
	}

	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToCamelCase(string? value)
	{
		if (value is not { Length: >0 })
			return value;
		Debug.Assert(value != null);

		bool upper = false;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var ax = new char[value.Length].AsSpan();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		var v = value.AsSpan();
		v.ToLowerInvariant(ax);
		foreach (var (index, length) in SplitByCapitals(v))
		{
			var f = ax[index];
			if (length == 1 && f == '_')
				continue;
			if (upper)
				ax[index] = Char.ToUpperInvariant(f);
			upper = Char.IsLetter(f);
		}
		return ax.ToString();
	}

	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToPascalCase(string? value)
	{
		if (value is not { Length: >0 })
			return value;
		Debug.Assert(value != null);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var ax = new char[value.Length].AsSpan();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		var v = value.AsSpan();
		v.ToLowerInvariant(ax);
		foreach (var (index, _) in SplitByCapitals(v))
		{
			ax[index] = Char.ToUpperInvariant(ax[index]);
		}
		return ax.ToString();
	}

	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToDashed(string? value, bool pascalCase, params char[]? dash)
	{
		if (value is not { Length: >0 })
			return value;
		Debug.Assert(value != null);
		if (dash is not { Length: >0 })
			dash = __dashes;
		Debug.Assert(dash != null);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var c = dash[0];
#pragma warning restore CS8602 // Dereference of a possibly null reference.

		var ix = 0;
		var ss = value.AsSpan();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var buffer = ArrayPool<char>.Shared.Rent(value.Length * 2);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		var ax = buffer.AsSpan();
		foreach (var (index, length) in SplitByCapitals(ss))
		{
			var s = ss.Slice(index, length).Trim(dash);
			if (s.Length == 0 || (s.Length == 1 && s[0] == '_'))
				continue;
			if (ix > 0)
			{
				ax[ix] = c;
				++ix;
			}
			if (pascalCase)
			{
				ax[ix] = Char.ToUpperInvariant(s[0]);
				s.Slice(1).ToLowerInvariant(ax.Slice(ix + 1));
			}
			else
			{
				s.CopyTo(ax.Slice(ix));
			}
			ix += length;
		}
		var result = ax.Slice(0, ix).ToString();
		ArrayPool<char>.Shared.Return(buffer);
		return result;
	}
	private static readonly char[] __dashes = new[] { '-' };

	[return: NotNullIfNotNull(nameof(value))]
	public static string? ToNamingRule(string? value, NamingCaseRule rule)
	{
		if (value is not { Length: >0 })
			return value;
		Debug.Assert(value != null);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		return (rule & ~NamingCaseRule.Force) switch
		{
			NamingCaseRule.PreferLowerCase => value.ToLowerInvariant(),
			NamingCaseRule.PreferCamelCase => Strings.ToCamelCase(value),
			NamingCaseRule.PreferPascalCase => Strings.ToPascalCase(value),
			NamingCaseRule.PreferUpperCase => value.ToUpperInvariant(),
			NamingCaseRule.PreferLowerCaseWithDashes => Strings.ToDashed(value, false, __dashChars)!.ToLowerInvariant(),
			NamingCaseRule.PreferPascalCaseWithDashes => Strings.ToDashed(value, true, __dashChars),
			NamingCaseRule.PreferUpperCaseWithDashes => Strings.ToDashed(value, false, __dashChars)!.ToUpperInvariant(),
			NamingCaseRule.PreferLowerCaseWithUnderscores => Strings.ToDashed(value, false, __underscoreChars)!.ToLowerInvariant(),
			NamingCaseRule.PreferPascalCaseWithUnderscores => Strings.ToDashed(value, true, __underscoreChars),
			NamingCaseRule.PreferUpperCaseWithUnderscores => Strings.ToDashed(value, false, __underscoreChars)!.ToUpperInvariant(),
			_ => value,
		};
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
	}
	private static readonly char[] __dashChars = new[] { '-', '_' };
	private static readonly char[] __underscoreChars = new[] { '_', '-' };

	[return: NotNullIfNotNull(nameof(value))]
	public static string? Ellipsis(string? value, int length, string? pad = null)
	{
		if (value == null || value.Length <= length)
			return value;
		pad ??= "�";
		return length <= pad.Length ? pad.Substring(0, length): value.Substring(0, length - pad.Length) + pad;
	}

	public static string JoinAnd(IEnumerable<string>? values, string? comma = null, string? and = null)
	{
		if (values == null)
			return "";
		var text = new StringBuilder();
		string value = "";
		bool first = true;
		comma ??= ", ";
		string pad = "";
		foreach (var item in values.Where(o => !String.IsNullOrEmpty(o)))
		{
			if (first)
			{
				first = false;
			}
			else
			{
				text.Append(pad).Append(value);
				pad = comma;
			}
			value = item;
		}
		if (text.Length == 0)
			return value;
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
		fixed (char* dgts = __hexDigits)
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
	private static readonly char[] __hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

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

	public static string ToHexString(byte[] value, string? prefix = null)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		int offset = prefix?.Length ?? 0;
		int length = value.Length;
		char[] chars = new char[offset + length * 2];
		for (int i = 0; i < offset; ++i)
		{
			chars[i] = prefix![i];
		}
		ToHexCharArrayInternal(value, 0, length, chars, offset);
		return new string(chars);
	}

	public static string ToBitsString(byte[] value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));
		if (value.Length == 0)
			return "";
		var text = new StringBuilder(value.Length * 9);
		for (int i = 0; i < value.Length; ++i)
		{
			text.Append(__bits[value[i] >> 4]).Append(__bits[value[i] & 0x0F]).Append(' ');
		}
		--text.Length;
		return text.ToString();
	}

	private static readonly string[] __bits =
	{
		"0000", "0001", "0010", "0011",
		"0100", "0101", "0110", "0111",
		"1000", "1001", "1010", "1011",
		"1100", "1101", "1110", "1111",
	};

	public static string CutIndents(ReadOnlySpan<string?> source, int tabSize = 4, string? newLine = null)
	{
		if (tabSize < 1 || tabSize > 32)
			throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

		if (source.Length == 0)
			return "";
		if (source.Length == 1)
			return source[0]?.Trim() ?? String.Empty;
		newLine ??= Environment.NewLine;
		var result = new StringBuilder(source.Length * 80);
		if ((source[source.Length - 1]?.Length ?? 0) == 0)
		{
			for (int i = 0; i < source.Length - 1; ++i)
			{
				result.Append(source[i].AsSpan().TrimEnd()).Append(newLine);
			}
			return result.ToString(0, result.Length - newLine.Length);
		}

		int indent = Int32.MaxValue; // source[source.Length - 1].Length;

		for (int i = 0; i < source.Length; ++i)
		{
			var s = source[i].AsSpan();
			if (s.IsWhiteSpace())
				continue;
			int column = 0;
			for (int j = 0; j < s.Length; ++j)
			{
				if (s[j] == '\t')
					column += tabSize - (column % tabSize);
				else if (s[j] == ' ')
					++column;
				else
					break;
				if (column >= indent)
					break;
			}
			if (column < indent)
			{
				indent = column;
				if (indent == 0)
					break;
			}
		}

		if (indent == 0 || indent == Int32.MaxValue)
		{
			for (int i = 0; i < source.Length; ++i)
			{
				result.Append(source[i].AsSpan().TrimEnd()).Append(newLine);
			}
			return result.ToString(0, result.Length - newLine.Length);
		}

		for (int i = 0; i < source.Length; ++i)
		{
			var s = source[i].AsSpan().TrimEnd();
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


	public static string CutIndents(ReadOnlySpan<char> text, int tabSize = 4)
	{
		if (tabSize < 1 || tabSize > 32)
			throw new ArgumentOutOfRangeException(nameof(tabSize), tabSize, null);

		if (text.Length == 0)
			return String.Empty;

		var p = NextNewLine(text);
		if (p.Nl == 0)
			return text.Trim().ToString();
		var result = new StringBuilder(text.Length);
		int indent = Int32.MaxValue;

		static (int Index, int Nl) NextNewLine(ReadOnlySpan<char> text)
		{
			int k = text.IndexOfAny(CrLf);
			if (k < 0)
				return (k, 0);
			if (k < text.Length - 1 && text[k + 1] == (text[k] ^ ('\r' ^ '\n')))
				return (k, 2);
			return (k, 1);
		}

		static int GetIndent(ReadOnlySpan<char> s, int indent, int tabSize)
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
		if (indent > 0)
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

	//public static unsafe string EncodeUrl(string value)
	//{
	//	if (value == null)
	//		throw new ArgumentNullException(nameof(value));

	//	fixed (char* str = value)
	//	{
	//		char* s = str;
	//		char* e = s + value.Length;
	//		bool space = false;
	//		int count5 = 0;
	//		int count2 = 0;
	//		while (s != e)
	//		{
	//			char c = *s;
	//			if (c > 0x007F)
	//			{
	//				if (c > 0x07FF)
	//					count5 += 2;
	//				else
	//					count5 += 1;
	//			}
	//			else if (!IsSafeUrlChar(c))
	//			{
	//				if (c == ' ')
	//					space = true;
	//				else
	//					++count2;
	//			}
	//			++s;
	//		}
	//		if (count2 == 0 && count5 == 0 && !space)
	//			return value;

	//		char[] buffer = new char[value.Length + count2 * 2 + count5 * 5];
	//		byte* temp = stackalloc byte[3];
	//		int i = 0;

	//		for (s = str; s != e; ++s)
	//		{
	//			char c = *s;
	//			if (IsSafeUrlChar(c))
	//			{
	//				buffer[i++] = c;
	//			}
	//			else if (c > 127)
	//			{
	//				int count = Encoding.UTF8.GetBytes(s, 1, temp, 3);
	//				for (int j = 0; j < count; ++j)
	//				{
	//					buffer[i++] = '%';
	//					buffer[i++] = __hexDigits[temp[j] >> 4];
	//					buffer[i++] = __hexDigits[temp[j] & 15];
	//				}
	//			}
	//			else if (c != ' ')
	//			{
	//				buffer[i++] = '%';
	//				buffer[i++] = __hexDigits[c >> 4];
	//				buffer[i++] = __hexDigits[c & 15];
	//			}
	//			else
	//			{
	//				buffer[i++] = '+';
	//			}
	//		}
	//		return new String(buffer);
	//	}

	//	static bool IsSafeUrlChar(char value)
	//	{
	//		if ((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9'))
	//			return true;
	//		return value switch
	//		{
	//			'(' or ')' or '*' or '-' or '.' or ':' or '_' or '!' or '~' => true,
	//			_ => false,
	//		};
	//	}
	//}

	//public static unsafe string DecodeUrl(string value)
	//{
	//	if (value == null)
	//		throw new ArgumentNullException(nameof(value));

	//	int i = value.IndexOf('%');
	//	value = value.Replace('+', ' ');
	//	if (i < 0)
	//		return value;
	//	var text = new StringBuilder(value.Length);
	//	text.Append(value, 0, i);
	//	char[] buffer = new char[value.Length];
	//	byte* temp = stackalloc byte[value.Length / 3];
	//	fixed (char* str = value)
	//	{
	//		fixed (char* buf = buffer)
	//		{
	//			value.CopyTo(0, buffer, 0, i);
	//			char* s = str + i;
	//			char* e = str + value.Length;
	//			char* b = buf + i;
	//			while (s != e)
	//			{
	//				char c = *s++;
	//				if (c != '%')
	//				{
	//					*b++ = c;
	//				}
	//				else
	//				{
	//					int x = Unpack(s, e);
	//					if (x < 0)
	//					{
	//						*b++ = '%';
	//					}
	//					else if (x < 128)
	//					{
	//						s += 2;
	//						*b++ = (char)x;
	//					}
	//					else
	//					{
	//						s += 2;
	//						temp[0] = (byte)x;
	//						int j = 1;
	//						while (s != e && *s == '%' && (x = Unpack(s + 1, e)) >= 0)
	//						{
	//							temp[j++] = (byte)x;
	//							s += 3;
	//						}
	//						b += Encoding.UTF8.GetChars(temp, j, b, j);
	//					}
	//				}
	//			}
	//			return new String(buf, 0, (int)(b - buf));
	//		}
	//	}
	//}

	//private static unsafe int Unpack(char* s, char* e)
	//{
	//	if (s == e)
	//		return -1;
	//	char a = *s++;
	//	if (s == e)
	//		return -1;
	//	char b = *s;
	//	int x;
	//	if (a >= '0' && a <= '9')
	//		x = (a - '0') << 4;
	//	else if (a >= 'A' && a <= 'F')
	//		x = (a - ('A' - 10)) << 4;
	//	else if (a >= 'a' && a <= 'f')
	//		x = (a - ('a' - 10)) << 4;
	//	else
	//		return -1;
	//	if (b >= '0' && b <= '9')
	//		x += b - '0';
	//	else if (b >= 'A' && b <= 'F')
	//		x += b - ('A' - 10);
	//	else if (b >= 'a' && b <= 'f')
	//		x += b - ('a' - 10);
	//	else
	//		return -1;
	//	return x;
	//}
}

public static class Files
{
	public const int MaxReadFileSize = 1024 * 1024 * 1024;

	/// <summary>
	/// Creates a new temporary file in the specified <paramref name="directory"/> and with <paramref name="suffix"/>.
	/// </summary>
	/// <param name="directory">Directory to create temporary file or null to use curren user's temporary directory</param>
	/// <param name="suffix">Temporary file suffix</param>
	/// <returns><see cref="FileInfo"/> of the temporary file.</returns>
	public static FileInfo GetTempFile(string? directory, string? suffix = null)
	{
		directory ??= Path.GetTempPath();

		const int LogThreshold = 20;
		const int TotalLimit = 30;
		int index = 0;
		for (;;)
		{
			try
			{
				var temp = new FileInfo(Path.Combine(directory, OrderedName() + suffix));
				using (temp.Open(FileMode.CreateNew))
				{
					return temp;
				}
			}
			catch (IOException flaw)
			{
				if (++index >= TotalLimit)
					throw;
				if (index >= LogThreshold)
					flaw.LogError();
			}
		}
	}

	/// <summary>
	/// Creates a new temporary file in the specified <paramref name="directory"/> and with <paramref name="suffix"/>;
	/// and after the creation executes the <paramref name="action"/> with the created file.
	/// </summary>
	/// <param name="directory">Directory to create temporary file or null to use curren user's temporary directory</param>
	/// <param name="suffix">Temporary file suffix</param>
	/// <param name="action">Action to execute for the created file</param>
	public static void ActTempFile(string? directory, string? suffix, Action<FileInfo> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		action(GetTempFile(directory, suffix));
	}

	/// <summary>
	/// Creates a new temporary file in the specified <paramref name="directory"/> and then executes the <paramref name="action"/> with the created file.
	/// </summary>
	/// <param name="directory">Directory to create temporary file or null to use curren user's temporary directory</param>
	/// <param name="action">Action to execute for the created file</param>
	public static void ActTempFile(string? directory, Action<FileInfo> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		action(GetTempFile(directory));
	}

	/// <summary>
	/// The asynchronous version of the <see cref="GetTempFile(string, string)"/>.
	/// </summary>
	/// <param name="directory"></param>
	/// <param name="suffix"></param>
	/// <returns></returns>
	public static Task<FileInfo> GetTempFileAsync(string? directory, string? suffix = null)
	{
		return Task.Run(() => GetTempFile(directory, suffix));
	}

	/// <summary>
	/// The asynchronous version of the <see cref="ActTempFile(string, string, Action{FileInfo})"/>.
	/// </summary>
	/// <param name="directory"></param>
	/// <param name="suffix"></param>
	/// <param name="action"></param>
	/// <returns></returns>
	public static Task ActTempFileAsync(string? directory, string? suffix, Action<FileInfo> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Task.Run(() => ActTempFile(directory, suffix, action));
	}

	/// <summary>
	/// The asynchronous version of the <see cref="ActTempFile(string, Action{FileInfo})"/>.
	/// </summary>
	/// <param name="directory"></param>
	/// <param name="action"></param>
	/// <returns></returns>
	public static Task ActTempFileAsync(string? directory, Action<FileInfo> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Task.Run(() => ActTempFile(directory, action));
	}

	private static string OrderedName()
	{
		DateTime t = DateTime.Now;
		char[] buffer = new char[20];
		buffer[0] = Line[(t.Year + 6) % Line.Length];
		buffer[1] = Line[t.Month];
		buffer[2] = Line[t.Day];
		buffer[3] = Line[t.Hour];
		buffer[4] = (char)('0' + t.Minute / 10);
		buffer[5] = (char)('0' + t.Minute % 10);
		long ms = Stopwatch.GetTimestamp() % (Stopwatch.Frequency * 60);
		int i = 0;
		while (ms > 0)
		{
			buffer[6 + i] += Line[ms % Line.Length];
			ms /= Line.Length;
			++i;
		}
		Array.Reverse(buffer, 6, i);
		return new String(buffer, 0, 6 + i);
	}

	private static readonly char[] Line =
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
		'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
	};
}

public static class Tools
{
	internal const int MaxStackAllocSize = 4096;

	/// <summary>
	/// Converts floating point value to rational number
	/// </summary>
	/// <param name="value">The value to convert</param>
	/// <param name="precision">Precision of the conversion</param>
	/// <param name="maxWidth">Maximum possible number of digits in minimum of both the numerator and the denominator</param>
	/// <returns>Tuple type with Item = enumerator, Item2 = denominator, Item3 = precision</returns>
	/// <remarks>
	///	Conversion stops when achieved required <paramref name="precision"/> or minimal number of digits in numerator or denominator is greater or equal then <paramref name="maxWidth"/>.
	///	special cases:
	///		0 = 0/1 with 0 precision
	///		NaN = 0/0 with NaN precision
	///		+infinity = 1/0 with positive infinity precision
	///		-infinity = -1/0 with negative infinity precision.
	/// </remarks>
	public static (long Numerator, long Denominator, double Precision) ToRational(double value, double precision = 0, int maxWidth = 0)
	{
		if (double.IsNaN(value))
			return (0L, 0L, double.NaN);
		if (double.IsPositiveInfinity(value))
			return (1L, 0L, double.PositiveInfinity);
		if (double.IsNegativeInfinity(value))
			return (-1L, 0L, double.PositiveInfinity);

		if (maxWidth <= 0)
			maxWidth = 20;
		if (precision < double.Epsilon)
			precision = double.Epsilon;
		bool neg = false;
		if (value < 0)
		{
			value = -value;
			neg = true;
		}

		if (value < precision)
			return (0L, 1L, value);

		long h0 = 0, h1 = 1;
		long k0 = 1, k1 = 0;
		long n = 1;
		double v = value;
		while (v != Math.Floor(v))
		{
			n *= 2;
			v *= 2;
		}
		long d = (long)v;
		long num = 0;
		long den = 1;
		int w = 0;
		double delta = value;

		for (int i = 0; i < 64; ++i)
		{
			long a = n > 0 ? d / n: 0;
			if (i > 0 && a == 0)
				break;
			long x = d;
			d = n;
			n = x % n;
			x = a;
			long h2 = x * h1 + h0; h0 = h1; h1 = h2;
			long k2 = x * k1 + k0; k0 = k1; k1 = k2;
			int w2 = DigitsNumber(Math.Min(k1, h1));
			if (w2 > maxWidth)
				break;
			if (delta <= precision)
			{
				if (w == 0)
					w = DigitsNumber(Math.Min(den, num));
				if (w2 > w)
					break;
			}
			num = h1;
			den = k1;
			delta = Math.Abs((double)num / den - value);
		}
		return (neg ? -num: num, den, delta);
	}

	/// <summary>
	/// Calculates number of decimal digits of the <paramref name="value"/>, i.e. ceil(1 + log10(<paramref name="value"/>))
	/// </summary>
	/// <param name="value">The value</param>
	/// <returns>With of the value</returns>
	public static int DigitsNumber(long value)
	{
		return (value < 0 ? -value: value) switch
		{
			< 10 => 1,
			< 100 => 2,
			< 1000 => 3,
			< 10000 => 4,
			< 100000 => 5,
			< 1000000 => 6,
			< 10000000 => 7,
			< 100000000 => 8,
			< 1000000000 => 9,
			< 10000000000 => 10,
			< 100000000000 => 11,
			< 1000000000000 => 12,
			< 10000000000000 => 13,
			< 100000000000000 => 14,
			< 1000000000000000 => 15,
			< 10000000000000000 => 16,
			< 100000000000000000 => 17,
			< 1000000000000000000 => 18,
			_ => 19
		};
	}

	public static ulong Power2Round(ulong value)
	{
		--value;
		value |= value >> 1;
		value |= value >> 2;
		value |= value >> 4;
		value |= value >> 8;
		value |= value >> 16;
		value |= value >> 32;
		return value + 1;
	}

	public static object? GetUnderlyingValue(object? value)
	{
		if (value == null)
			return null;
		if (value is IValue iv)
			value = iv.Value;
		if (value == null)
			return null;
		if (DBNull.Value.Equals(value))
			return null;
		if (!value.GetType().IsEnum)
			return value;

		return (Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType()))) switch
		{
			TypeCode.Byte => (byte)value,
			TypeCode.Char => (char)value,
			TypeCode.Int16 => (short)value,
			TypeCode.Int32 => (int)value,
			TypeCode.Int64 => (long)value,
			TypeCode.SByte => (sbyte)value,
			TypeCode.UInt16 => (ushort)value,
			TypeCode.UInt32 => (uint)value,
			TypeCode.UInt64 => (ulong)value,
			_ => (int)value,
		};
	}

	public static BitArray InitializeBitArray(params int[] values)
	{
		if (values == null)
			throw new ArgumentNullException(nameof(values));
		if (values.Length <= 1)
			return values.Length == 0 ? new BitArray(0): new BitArray(1, true);

		int mi = int.MaxValue;
		int ma = int.MinValue;
		for (int i = 0; i < values.Length; ++i)
		{
			if (mi > values[i])
				mi = values[i];
			if (ma < values[i])
				ma = values[i];
		}
		var ba = new BitArray(ma - mi + 1);
		for (int i = 0; i < values.Length; ++i)
		{
			ba[values[i] - mi] = true;
		}
		return ba;
	}

	public static T Cast<T>(object value)
	{
		return (T)value;
	}

	internal static string MachineName
	{
		get
		{
			try
			{
				return Environment.MachineName;
			}
			catch (InvalidOperationException)
			{
				return "local";
			}
		}
	}

	/// <summary>
	/// Get current system process ID.
	/// </summary>
	internal static int ProcessId =>
#if NET5_0_OR_GREATER
		Environment.ProcessId;
#else
		__processId ??= Process.GetCurrentProcess().Id;
	private static int? __processId;
#endif

	/// <summary>
	/// Get executable name of the current module.
	/// </summary>
	internal static string ModuleFileName => __moduleFileName ??= Process.GetCurrentProcess().MainModule?.FileName ?? "internal";
	private static string? __moduleFileName;

	internal static string DomainName
	{
		get
		{
			try
			{
				return (__domainName ??= AppDomain.CurrentDomain.FriendlyName);
			}
			catch (AppDomainUnloadedException)
			{
				return "unloaded";
			}
		}
	}
	private static string? __domainName;
}
