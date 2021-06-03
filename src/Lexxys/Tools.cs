// Lexxys Infrastructural library.
// file: Tools.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public static class Comparer
	{
		public static IComparer<T> Create<T>(Func<T, T, int> compare) => new GenericComparer<T>(compare);

		public static IComparer Create<T1, T2>(Func<T1, T2, int> compare) => new GenericComparer<T1, T2>(compare);

		public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare)
		{
			if (compare == null)
				throw new ArgumentNullException(nameof(compare));
			return new GenericEqualityComparer<T>(compare);
		}

		public static IEqualityComparer<T> Create<T>(Func<T, T, bool> compare, Func<T, int> hash)
		{
			if (compare == null)
				throw new ArgumentNullException(nameof(compare));
			return new GenericEqualityComparer<T>(compare, hash);
		}

		public static bool Equals<T>(IEnumerable<T> left, IEnumerable<T> right, IEqualityComparer<T> comparer = null)
		{
			if (left == null || right == null)
				return Object.ReferenceEquals(left, right);

			if (left is ICollection cl && right is ICollection cr && cl.Count != cr.Count)
				return false;

			if (comparer == null)
				comparer = EqualityComparer<T>.Default;

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

		public static int CompareBytes(byte[] left, byte[] right, bool forEqualsOnly)
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

			public GenericComparer(Func<T1, T2, int> compare)
			{
				_compare = compare;
			}

			public int Compare(object x, object y)
			{
				return x is T1 t1 && y is T2 t2 ? _compare(t1, t2) : 2;
			}
		}

		private class GenericComparer<T>: IComparer<T>
		{
			private readonly Func<T, T, int> _compare;

			public GenericComparer(Func<T, T, int> compare)
			{
				_compare = compare;
			}

			public int Compare(T x, T y)
			{
				return _compare(x, y);
			}
		}

		private class GenericEqualityComparer<T>: IEqualityComparer<T>
		{
			private readonly Func<T, T, bool> _equals;
			private readonly Func<T, int> _hash;

			public GenericEqualityComparer(Func<T, T, bool> equals)
			{
				_equals = equals;
				_hash = o => o == null ? 0: o.GetHashCode();
			}

			public GenericEqualityComparer(Func<T, T, bool> equals, Func<T, int> hash)
			{
				_equals = equals;
				_hash = hash;
			}

			public bool Equals(T x, T y)
			{
				return _equals(x, y);
			}

			public int GetHashCode(T obj)
			{
				return _hash(obj);
			}
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

		public static int Join(params int[] hh)
		{
			if (hh == null || hh.Length == 0)
				return 0;
			int h = hh[0];
			for (int i = 1; i < hh.Length; ++i)
			{
				h = ((h << 5) + h) ^ hh[i];
			}
			return h;
		}

		public static int Join(int offset, IEnumerable<int> items)
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

		public static int Join<T>(int offset, IEnumerable<T> items)
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
		{
			return EscapeCsString(value, '"');
		}

		public static string EscapeCsString(string value, char marker)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			return EscapeCsString(new StringBuilder(), value, marker).ToString();
		}

		public static StringBuilder EscapeCsString(StringBuilder text, string value, char marker = '"')
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
							text.Append("\\b");
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
							text.Write("\\b");
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
							yield return 'b';
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

		public static void EscapeUtf8CsString(MemoryStream memory, ReadOnlySpan<char> value, char marker = '"')
		{
			if (memory == null)
				throw new ArgumentNullException(nameof(memory));
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (marker != '\0')
				if (marker < 128)
					memory.WriteByte((byte)marker);
				else
					WriteChar(memory, marker);
			foreach (char c in value)
			{
				if (c < 128)
				{
					if (c >= ' ' && c < 127)
					{
						if (c == marker)
						{
							memory.WriteByte((byte)'\\');
							memory.WriteByte((byte)c);
						}
						else if (c == '\\')
						{
							memory.WriteByte((byte)'\\');
							memory.WriteByte((byte)'\\');
						}
						else
						{
							memory.WriteByte((byte)c);
						}
					}
					else
					{
						memory.WriteByte((byte)'\\');
						switch (c)
						{
							case '\n':
								memory.WriteByte((byte)'n');
								break;
							case '\r':
								memory.WriteByte((byte)'r');
								break;
							case '\t':
								memory.WriteByte((byte)'t');
								break;
							case '\f':
								memory.WriteByte((byte)'f');
								break;
							case '\v':
								memory.WriteByte((byte)'v');
								break;
							case '\a':
								memory.WriteByte((byte)'b');
								break;
							case '\b':
								memory.WriteByte((byte)'b');
								break;
							case '\0':
								memory.WriteByte((byte)'0');
								break;
							default:
								memory.WriteByte((byte)'0');
								memory.WriteByte((byte)'0');
								memory.WriteByte(__hexB[(c & 0xF0) >> 4]);
								memory.WriteByte(__hexB[c & 0xF]);
								break;
						}
					}
				}
				else if (c >= '\xd800')
				{
					memory.WriteByte((byte)'\\');
					memory.WriteByte((byte)'x');
					memory.WriteByte(__hexB[(c & 0xF000) >> 12]);
					memory.WriteByte(__hexB[(c & 0xF00) >> 8]);
					memory.WriteByte(__hexB[(c & 0xF0) >> 4]);
					memory.WriteByte(__hexB[c & 0xF]);
				}
				else if (c == marker)
				{
					memory.WriteByte((byte)'\\');
					WriteChar(memory, marker);
				}
				else
				{
					WriteChar(memory, c);
				}
			}
			if (marker != '\0')
				if (marker < 128)
					memory.WriteByte((byte)marker);
				else
					WriteChar(memory, marker);

			unsafe static void WriteChar(MemoryStream memory, char value)
			{
				var bytes = stackalloc byte[4];
				var count = Encoding.UTF8.GetBytes(&value, 1, bytes, 4);
				while (--count >= 0)
						memory.WriteByte(*bytes++);
			}
		}
		private static readonly byte[] __hexB = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };

		public static string RemoveExtraBraces(string value)
		{
			if (value == null)
				return null;
			value = value.Trim();
			if (value.Length == 0)
				return value;
			while (value.Length > 1 && value[0] == '(' && value[value.Length - 1] == ')')
			{
				value = value.Substring(1, value.Length - 2).Trim();
			}
			return value;
		}

		public static string[] SplitByCapitals(string identifier)
		{
			if (identifier == null || identifier.Length == 0)
				return EmptyArray<string>.Value;
			var ss = new List<string>();
			char[] cc = identifier.ToCharArray();
			CharType ot =
				Char.IsUpper(cc[0]) ? CharType.Upper:
				Char.IsLower(cc[0]) ? CharType.Lower:
				Char.IsDigit(cc[0]) ? CharType.Digit: CharType.Other;
			int i0 = 0; //ot == CharType.Other ? 1: 0;

			for (int i = 1; i < cc.Length; ++i)
			{
				CharType ct =
					Char.IsUpper(cc[i]) ? CharType.Upper:
					Char.IsLower(cc[i]) ? CharType.Lower:
					Char.IsDigit(cc[i]) ? CharType.Digit: CharType.Other;

				if (ct == ot)
					continue;

				if (ct > ot || ot == CharType.Other)
				{
					if (i > i0)
						ss.Add(new string(cc, i0, i - i0));
					i0 = i;
				}
				else if (ct == CharType.Lower && ot == CharType.Upper)
				{
					if (i > i0 + 1)
					{
						ss.Add(new string(cc, i0, i - i0 - 1));
						i0 = i - 1;
					}
				}

				ot = ct;
			}

			if (cc.Length > i0)
				ss.Add(new string(cc, i0, cc.Length - i0));
			return ss.ToArray();
		}

		public static string[] SplitByWordBound(string value, int width, int count = 0)
		{
			Contract.Ensures(Contract.Result<string[]>() != null);
			if (value == null)
				return EmptyArray<string>.Value;
			if (count == 1 || value.Length <= width)
				return new[] { value };
			var list = new List<string>();
			int start = 0;
			while (value.Length > start)
			{
				if (list.Count == count - 1)
				{
					list.Add(value.Substring(start));
					break;
				}
				int bound = GetBound(value, start, width);
				if (bound > start)
					list.Add(value.Substring(start, bound - start));
				start = bound;
				while (start < value.Length && Char.IsWhiteSpace(value[start]))
				{
					++start;
				}
			}
			return list.ToArray();
		}

		private static int GetBound(string value, int start, int width)
		{
			if (start >= value.Length)
				return value.Length;
			int ii = value.IndexOfAny(CrLf, start);
			if (ii >= start && ii <= start + width)
			{
				while (ii > start && Char.IsWhiteSpace(value, ii - 1))
					--ii;
				return ii;
			}
			if (start + width >= value.Length)
				return value.Length;
			for (int i = start + width; i > start; --i)
			{
				char c = value[i];
				if (char.IsLetter(c))
					continue;
				if (c == '(' || c == '[' || c == '{' || Char.IsWhiteSpace(c))
					return i;
				if (i < start + width)
					return i + 1;
			}
			return start + width;
		}
		private static readonly char[] CrLf = new[] {'\n', '\r'};

		public static string ToTitleCase(string value)
		{
			return String.IsNullOrEmpty(value) ? value: value.Substring(0, 1).ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
		}

		public static string ToCamelCase(string value)
		{
			if (String.IsNullOrEmpty(value))
				return value;

			var text = new StringBuilder(value.Length);
			bool upper = false;
			foreach (var s in SplitByCapitals(value))
			{
				if (s.Length == 0 || s == "_")
					continue;
				if (upper)
					text.Append(Char.ToUpperInvariant(s[0])).Append(s.Substring(1).ToLowerInvariant());
				else
					text.Append(s.ToLowerInvariant());
				upper = Char.IsLetter(s[s.Length - 1]);
			}
			return text.ToString();
		}

		public static string ToPascalCase(string value)
		{
			if (String.IsNullOrEmpty(value))
				return value;

			var text = new StringBuilder(value.Length);
			foreach (var s in SplitByCapitals(value))
			{
				if (s.Length == 0 || s == "_")
					continue;
				text.Append(Char.ToUpperInvariant(s[0])).Append(s.Substring(1).ToLowerInvariant());
			}
			return text.ToString();
		}

		public static string ToDashed(string value, bool pascalCase, params char[] dash)
		{
			if (String.IsNullOrEmpty(value))
				return value;
			if (dash == null || dash.Length == 0)
				dash = __dasches;

			var text = new StringBuilder(value.Length);
			var c = dash[0];
			foreach (var s0 in SplitByCapitals(value))
			{
				var s = s0.Trim(dash);
				if (s.Length == 0 || s == "_")
					continue;
				if (text.Length > 0)
					text.Append(c);
				if (pascalCase)
					text.Append(Char.ToUpperInvariant(s[0])).Append(s.Substring(1).ToLowerInvariant());
				else
					text.Append(s);
			}
			return text.ToString();
		}
		private static readonly char[] __dasches = new[] {'-'};

		public static string ToNamingRule(string value, NamingCaseRule rule)
		{
			if (String.IsNullOrEmpty(value))
				return value;
			return (rule & ~NamingCaseRule.Force) switch
			{
				NamingCaseRule.PreferLowerCase => value.ToLowerInvariant(),
				NamingCaseRule.PreferCamelCase => Strings.ToCamelCase(value),
				NamingCaseRule.PreferPascalCase => Strings.ToPascalCase(value),
				NamingCaseRule.PreferUpperCase => value.ToUpperInvariant(),
				NamingCaseRule.PreferLowerCaseWithDashes => Strings.ToDashed(value, false, __dashChars).ToLowerInvariant(),
				NamingCaseRule.PreferUpperCaseWithDashes => Strings.ToDashed(value, false, __dashChars).ToUpperInvariant(),
				NamingCaseRule.PreferPascalCaseWithDashes => Strings.ToDashed(value, true, __dashChars),
				NamingCaseRule.PreferLowerCaseWithUnserscores => Strings.ToDashed(value, false, __underscoreChars).ToLowerInvariant(),
				NamingCaseRule.PreferUpperCaseWithUnserscores => Strings.ToDashed(value, false, __underscoreChars).ToUpperInvariant(),
				NamingCaseRule.PreferPascalCaseWithUnserscores => Strings.ToDashed(value, true, __underscoreChars),
				_ => value,
			};
		}
		private static readonly char[] __dashChars = new[] { '-', '_' };
		private static readonly char[] __underscoreChars = new[] { '_', '-' };

		public static string Ellipsis(string value, int length, string pad = null)
		{
			if (pad == null)
				pad = "...";
			return value == null ? null: value.Length <= length ? value:
				length <= pad.Length ? pad.Substring(0, length): value.Substring(0, length - pad.Length) + pad;
		}

		public static string JoinAnd(IEnumerable<string> values, string comma = null, string and = null)
		{
			if (values == null)
				return "";
			var text = new StringBuilder();
			string value = "";
			bool first = true;
			if (comma == null)
				comma = ", ";
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

		private static void ToHexCharArrayInternal(byte[] bitsValue, int offset, int length, char[] hexValue, int outOffset)
		{
			while (length-- > 0)
			{
				hexValue[outOffset++] = "0123456789ABCDEF"[bitsValue[offset] >> 4];
				hexValue[outOffset++] = "0123456789ABCDEF"[bitsValue[offset] & 15];
				offset++;
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

		public static string ToHexString(byte[] value, string prefix = null)
		{
			if (value == null)
				return null;
			int offset = prefix?.Length ?? 0;
			int length = value.Length;
			char[] chars = new char[offset + length * 2];
			for (int i = 0; i < offset; ++i)
			{
				chars[i] = prefix[i];
			}
			ToHexCharArrayInternal(value, 0, length, chars, offset);
			return new string(chars);
		}

		public static string ToBitsString(byte[] value)
		{
			if (value == null)
				return null;
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

		public static string CutIndents(string[] source, int tabSize = 4, string newLine = null)
		{
			if (tabSize < 1 || tabSize > 32)
				throw new ArgumentOutOfRangeException(nameof(tabSize));

			if (source == null || source.Length == 0)
				return "";
			if (source.Length == 1)
				return source[0]?.Trim() ?? "";
			if (newLine == null)
				newLine = Environment.NewLine;
			var result = new StringBuilder(source.Length * 80);
			if ((source[source.Length - 1]?.Length ?? 0) == 0)
			{
				for (int i = 0; i < source.Length - 1; ++i)
				{
					result.Append(source[i]).Append(newLine);
				}
				return result.ToString(0, result.Length - newLine.Length);
			}

			int indent = Int32.MaxValue; // source[source.Length - 1].Length;

			for (int i = 0; i < source.Length; ++i)
			{
				string s = source[i];
				if (!String.IsNullOrWhiteSpace(s))
				{
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
			}

			if (indent == 0 || indent == Int32.MaxValue)
			{
				for (int i = 0; i < source.Length; ++i)
				{
					result.Append(source[i].TrimEnd()).Append(newLine);
				}
				return result.ToString(0, result.Length - newLine.Length);
			}

			for (int i = 0; i < source.Length; ++i)
			{
				string s = source[i]?.TrimEnd() ?? "";
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
				result.Append(s.Substring(j).TrimEnd()).Append(newLine);
			}

			return result.ToString(0, result.Length - newLine.Length);
		}

		public static unsafe string EncodeUrl(string value)
		{
			if (value == null)
				return null;

			fixed (char* str = value)
			{
				char* s = str;
				char* e = s + value.Length;
				bool space = false;
				int count5 = 0;
				int count2 = 0;
				while (s != e)
				{
					char c = *s;
					if (c > 0x007F)
					{
						if (c > 0x07FF)
							count5 += 2;
						else
							count5 += 1;
					}
					else if (!IsSafeUrlChar(c))
					{
						if (c == ' ')
							space = true;
						else
							++count2;
					}
					++s;
				}
				if (count2 == 0 && count5 == 0 && !space)
					return value;

				char[] buffer = new char[value.Length + count2 * 2 + count5 * 5];
				byte* temp = stackalloc byte[3];
				int i = 0;

				for (s = str; s != e; ++s)
				{
					char c = *s;
					if (IsSafeUrlChar(c))
					{
						buffer[i++] = c;
					}
					else if (c > 127)
					{
						int count = Encoding.UTF8.GetBytes(s, 1, temp, 3);
						for (int j = 0; j < count; ++j)
						{
							buffer[i++] = '%';
							buffer[i++] = __hexDigits[temp[j] >> 4];
							buffer[i++] = __hexDigits[temp[j] & 15];
						}
					}
					else if (c != ' ')
					{
						buffer[i++] = '%';
						buffer[i++] = __hexDigits[c >> 4];
						buffer[i++] = __hexDigits[c & 15];
					}
					else
					{
						buffer[i++] = '+';
					}
				}
				return new String(buffer);
			}
		}
		private static readonly char[] __hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		public static unsafe string DecodeUrl(string value)
		{
			if (value == null)
				return null;

			int i = value.IndexOf('%');
			value = value.Replace('+', ' ');
			if (i < 0)
				return value;
			var text = new StringBuilder(value.Length);
			text.Append(value, 0, i);
			char[] buffer = new char[value.Length];
			byte* temp = stackalloc byte[value.Length / 3];
			fixed (char* str = value)
			{
				fixed (char* buf = buffer)
				{
					value.CopyTo(0, buffer, 0, i);
					char* s = str + i;
					char* e = str + value.Length;
					char* b = buf + i;
					while (s != e)
					{
						char c = *s++;
						if (c != '%')
						{
							*b++ = c;
						}
						else
						{
							int x = Unpack(s, e);
							if (x < 0)
							{
								*b++ = '%';
							}
							else if (x < 128)
							{
								s += 2;
								*b++ = (char)x;
							}
							else
							{
								s += 2;
								temp[0] = (byte)x;
								int j = 1;
								while (s != e && *s == '%' && (x = Unpack(s + 1, e)) >= 0)
								{
									temp[j++] = (byte)x;
									s += 3;
								}
								b += Encoding.UTF8.GetChars(temp, j, b, j);
							}
						}
					}
					return new String(buf, 0, (int)(b - buf));
				}
			}
		}

		private static unsafe int Unpack(char* s, char* e)
		{
			if (s == e)
				return -1;
			char a = *s++;
			if (s == e)
				return -1;
			char b = *s;
			int x;
			if (a >= '0' && a <= '9')
				x = (a - '0') << 4;
			else if (a >= 'A' && a <= 'F')
				x = (a - ('A' - 10)) << 4;
			else if (a >= 'a' && a <= 'f')
				x = (a - ('a' - 10)) << 4;
			else
				return -1;
			if (b >= '0' && b <= '9')
				x += b - '0';
			else if (b >= 'A' && b <= 'F')
				x += b - ('A' - 10);
			else if (b >= 'a' && b <= 'f')
				x += b - ('a' - 10);
			else
				return -1;
			return x;
		}

		private static bool IsSafeUrlChar(char value)
		{
			if ((value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9'))
				return true;
			return value switch
			{
				'(' or ')' or '*' or '-' or '.' or ':' or '_' or '!' or '~' => true,
				_ => false,
			};
		}
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
		public static FileInfo GetTempFile(string directory, string suffix = null)
		{
			if (directory == null)
				directory = Path.GetTempPath();

			const int LogThreshold = 20;
			const int TotalLimit = 50;
			int index = 0;
			for (;;)
			{
				++index;
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
					if (index == TotalLimit)
						throw;
					if (index >= LogThreshold)
						flaw.LogError();
				}
#pragma warning restore
			}
		}

		/// <summary>
		/// Creates a new temporary file in the specified <paramref name="directory"/> and with <paramref name="suffix"/>;
		/// and agter then executes the <paramref name="action"/> with the created file.
		/// </summary>
		/// <param name="directory">Directory to create temporary file or null to use curren user's temporary directory</param>
		/// <param name="suffix">Temporary file suffix</param>
		/// <param name="action">Action to execute for the created file</param>
		public static void ActTempFile(string directory, string suffix, Action<FileInfo> action)
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
		public static void ActTempFile(string directory, Action<FileInfo> action)
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
		public static async Task<FileInfo> GetTempFileAsync(string directory, string suffix = null)
		{
			return await Task.Factory.StartNew(() => GetTempFile(directory, suffix)).ConfigureAwait(false);
		}

		/// <summary>
		/// The asynchronous version of the <see cref="ActTempFile(string, string, Action{FileInfo})"/>.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="suffix"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static async Task ActTempFileAsync(string directory, string suffix, Action<FileInfo> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			await Task.Factory.StartNew(() => ActTempFile(directory, suffix, action)).ConfigureAwait(false);
		}

		/// <summary>
		/// The asynchronous version of the <see cref="ActTempFile(string, Action{FileInfo})"/>.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static async Task ActTempFileAsync(string directory, Action<FileInfo> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			await Task.Factory.StartNew(() => ActTempFile(directory, action)).ConfigureAwait(false);
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
			long ms = WatchTimer.Query(0) % WatchTimer.TicksPerMinute;
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
				int w2 = ValueWidth(Math.Max(k1, h1));
				if (w2 > maxWidth)
					break;
				if (delta <= precision)
				{
					if (w == 0)
						w = ValueWidth(Math.Max(den, num));
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
		public static int ValueWidth(long value)
		{
			int k = Array.BinarySearch(ValueWidthTable, value >= 0 ? value: -value);
			return k < 0 ? ~k: k;
		}

		private static readonly long[] ValueWidthTable =
		{
			-1,
			9L,
			99L,
			999L,
			9999L,
			99999L,
			999999L,
			9999999L,
			99999999L,
			999999999L,
			9999999999L,
			99999999999L,
			999999999999L,
			9999999999999L,
			99999999999999L,
			999999999999999L,
			9999999999999999L,
			99999999999999999L,
			999999999999999999L,
		};

		public static object GetUnderlyingValue(object value)
		{
			if (value == null)
				return null;
			if (value is IValue iv)
				value = iv.Value;
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
		internal static int ProcessId => NativeMethods.GetCurrentProcessId();

		/// <summary>
		/// Get current system thread ID.
		/// </summary>
		internal static int ThreadId => NativeMethods.GetCurrentThreadId();

		/// <summary>
		/// Get executable name of the current module.
		/// </summary>
		internal static string ModuleFileName => System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

		internal static string DomainName
		{
			get
			{
				try
				{
					return AppDomain.CurrentDomain.FriendlyName;
				}
				catch (AppDomainUnloadedException)
				{
					return "unloaded";
				}
			}
		}

		private static class NativeMethods
		{
			[StructLayout(LayoutKind.Sequential)]
			class SECURITY_ATTRIBUTES
			{
				public int nLength;
				public unsafe byte* pSecurityDescriptor;
				public int bInheritHandle;
			}

			[DllImport("kernel32.dll")]
			public static extern int GetCurrentProcessId();

			[DllImport("kernel32.dll")]
			public static extern int GetCurrentThreadId();
		}
	}

	public static class EmptyArray<T>
	{
		public static readonly T[] Value = Array.Empty<T>();
	}
}
