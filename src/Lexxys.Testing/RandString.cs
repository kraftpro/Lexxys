#if false
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Testing
{
	public class RandString
	{
		public static readonly RandString Empty = new RandString(null);

		private readonly Func<string>[] _generators;

		private RandString(int count)
		{
			_generators = new Func<string>[count];
		}

		public RandString(Func<string> generator)
		{
			_generators = new[] { generator ?? Null };
		}

		private static string Null()
		{
			return "";
		}

		public RandString And(RandString other)
		{
			if (other == null || other._generators.Length == 0)
				return this;
			if (_generators.Length == 0)
				return other;
			var result = new RandString(_generators.Length + other._generators.Length);
			if (_generators.Length == 1)
				result._generators[0] = _generators[0];
			else
				Array.Copy(_generators, result._generators, _generators.Length);
			if (other._generators.Length == 1)
				result._generators[_generators.Length] = other._generators[0];
			else
				Array.Copy(other._generators, 0, result._generators, _generators.Length, other._generators.Length);
			return result;
		}

		public RandString And(Func<string> generator)
		{
			if (generator == null)
				return this;
			if (_generators.Length == 0)
				return new RandString(generator);
			var result = new RandString(_generators.Length + 1);
			if (_generators.Length == 1)
				result._generators[0] = _generators[0];
			else
				Array.Copy(_generators, result._generators, _generators.Length);
			result._generators[_generators.Length] = generator;
			return result;
		}

		public string[] GetArray(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, null);

			string[] values = new string[count];
			for (int i = 0; i < values.Length; ++i)
			{
				values[i] = ToString();
			}
			return values;
		}

		public override string ToString()
		{
			if (_generators.Length == 0)
				return "";
			if (_generators.Length == 1)
				return _generators[0]() ?? "";

			StringBuilder text = new StringBuilder();
			foreach (var item in _generators)
			{
				text.Append(item());
			}
			return text.ToString();
		}

		public static implicit operator string(RandString value)
		{
			return value?.ToString();
		}

		public static RandString operator +(RandString left, RandString right)
		{
			return right == null ? left :
				left == null ? right : left.And(right);
		}

		public static RandString operator +(RandString left, string right)
		{
			return right == null ? left :
				left == null ? new RandString(() => right) : left.And(() => right);
		}

		public static RandString operator +(RandString left, Func<string> right)
		{
			return right == null ? left :
				left == null ? new RandString(right) : left.And(right);
		}
	}

	public static class StringsExtensions
	{
		private const int MaxExceptions = 100000;

		private static RandString With(this RandString that, Func<string> generator)
		{
			return that == null ? new RandString(generator) : that.And(generator);
		}

		public static RandString Digit(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Digit(that, length, length, filter);
		}
		public static RandString Digit(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __digit, filter));
		}
		private static readonly char[] __digit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		public static RandString Upper(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Upper(that, length, length, filter);
		}
		public static RandString Upper(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __upper, filter));
		}
		private static readonly char[] __upper = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

		public static RandString Lower(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Lower(that, length, length, filter);
		}
		public static RandString Lower(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __lower, filter));
		}
		private static readonly char[] __lower = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

		public static RandString Letter(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Letter(that, length, length, filter);
		}
		public static RandString Letter(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __letter, filter));
		}
		private static readonly char[] __letter = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

		public static RandString UpperOrDigit(this RandString that, int length, Func<char, bool> filter = null)
		{
			return UppeOrDigitr(that, length, length, filter);
		}
		public static RandString UppeOrDigitr(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __upperOrDigit, filter));
		}
		private static readonly char[] __upperOrDigit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

		public static RandString LowerOrDigit(this RandString that, int length, Func<char, bool> filter = null)
		{
			return LowerOrDigit(that, length, length, filter);
		}
		public static RandString LowerOrDigit(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __lowerOrDigit, filter));
		}
		private static readonly char[] __lowerOrDigit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

		public static RandString LetterOrDigit(this RandString that, int length, Func<char, bool> filter = null)
		{
			return LetterOrDigit(that, length, length, filter);
		}
		public static RandString LetterOrDigit(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, __letterOrDigit, filter));
		}
		private static readonly char[] __letterOrDigit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

		public static RandString Chars(this RandString that, IReadOnlyList<char> items, int length, Func<char, bool> filter = null)
		{
			return Chars(that, items, length, length, filter);
		}
		public static RandString Chars(this RandString that, IReadOnlyList<char> items, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return that.With(CharsGenerator(minLength, maxLength, items, filter));
		}

		public static RandString Ascii(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Ascii(that, length, length, filter);
		}
		public static RandString Ascii(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return Range(that, ' ', (char)126, minLength, maxLength, filter);
		}

		public static RandString Ascii0(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Ascii0(that, length, length, filter);
		}
		public static RandString Ascii0(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return Range(that, (char)0, (char)127, minLength, maxLength, filter);
		}

		public static RandString Ansi(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Ansi(that, length, length, filter);
		}
		public static RandString Ansi(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return Range(that, (char)0, (char)255, minLength, maxLength, filter);
		}

		public static RandString Unicode(this RandString that, UnicodeCategory category, int length, Func<char, bool> filter = null)
		{
			return Unicode(that, category, length, length, filter);
		}
		public static RandString Unicode(this RandString that, UnicodeCategory category, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			List<char> uc;
			if (!__unicode.TryGetValue(category, out uc))
			{
				uc = new List<char>();
				for (int c = Char.MinValue; c <= Char.MaxValue; ++c)
				{
					if (CharUnicodeInfo.GetUnicodeCategory((char)c) == category)
						uc.Add((char)c);
				}
				__unicode.Add(category, uc);
			}
			return that.With(CharsGenerator(minLength, maxLength, uc, filter));
		}
		private static readonly Dictionary<UnicodeCategory, List<char>> __unicode = new Dictionary<UnicodeCategory, List<char>>();

		public static RandString Range(this RandString that, char min, char max, int length, Func<char, bool> filter = null)
		{
			return Range(that, min, max, length, length, filter);
		}
		public static RandString Range(this RandString that, char min, char max, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			if (minLength < 0)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);
			if (maxLength < minLength)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);
			if (min > max)
			{
				char t = min;
				min = max;
				max = t;
			}
			return that.With((Func<string>)(filter == null ? (Func<string>)
				(() =>
				{
					char[] result = new char[minLength == maxLength ? minLength : Rand.Int(minLength, maxLength + 1)];
					for (int i = 0; i < result.Length; ++i)
					{
						result[i] = (char)Rand.Int(min, max + 1);
					}
					return new String(result);
				}) :
				(() =>
				{
					char[] result = new char[minLength == maxLength ? minLength : Rand.Int(minLength, maxLength + 1)];
					int j = 0;
					for (int i = 0; i < result.Length; ++i)
					{
						char c = (char)Rand.Int(min, max + 1);
						while (j < MaxExceptions && !filter(c))
						{
							c = (char)Rand.Int(min, max + 1);
							++j;
						}
						result[i] = c;
					}
					return new String(result);
				})));
		}

		public static RandString Any(this RandString that, int length, Func<char, bool> filter = null)
		{
			return Range(that, Char.MinValue, Char.MaxValue, length, length, filter);
		}

		public static RandString Any(this RandString that, int minLength, int maxLength, Func<char, bool> filter = null)
		{
			return Range(that, Char.MinValue, Char.MaxValue, minLength, maxLength, filter);
		}


		public static RandString Item<T>(this RandString that, params T[] values)
		{
			return that.With(() => Rand.Item(values)?.ToString());
		}

		public static RandString Item<T>(this RandString that, double p, IReadOnlyList<T> values)
		{
			return that.With(() => Rand.Item(p, values)?.ToString());
		}

		public static RandString Int(this RandString that, int lo, int hi, string format)
		{
			return that.With(() => Rand.Int(lo, hi).ToString(format));
		}

		public static RandString Int(this RandString that, int lo, int hi)
		{
			return that.With(() => Rand.Int(lo, hi).ToString());
		}
		private static Func<string> CharsGenerator(int minLength, int maxLength, IReadOnlyList<char> items, Func<char, bool> filter = null)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (items.Count == 0)
				throw new ArgumentOutOfRangeException(nameof(items.Count), items.Count, null);
			if (minLength < 0)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (maxLength < minLength)
				throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);

			return filter == null ? (Func<string>)
				(() =>
				{
					char[] result = new char[minLength == maxLength ? minLength : Rand.Int(minLength, maxLength + 1)];
					for (int i = 0; i < result.Length; ++i)
					{
						result[i] = items[Rand.Int(0, items.Count)];
					}
					return new String(result);
				}) :
				(() =>
				{
					char[] result = new char[minLength == maxLength ? minLength : Rand.Int(minLength, maxLength + 1)];
					int j = 0;
					for (int i = 0; i < result.Length; ++i)
					{
						char c = items[Rand.Int(0, items.Count)];
						while (j < MaxExceptions && !filter(c))
						{
							c = items[Rand.Int(0, items.Count)];
							++j;
						}
						result[i] = c;
					}
					return new String(result);
				});
		}

		public static RandString Lorem(this RandString that, int maxLength, IReadOnlyList<string> words)
		{
			return that.With(() => GetLorem(0, maxLength, words));
		}
		public static RandString Lorem(this RandString that, int minLength, int maxLength, IReadOnlyList<string> words)
		{
			return that.With(() => GetLorem(minLength, maxLength, words));
		}

		public const int MinLoremLength = 12;

		private static string GetLorem(int minLength, int maxLength, IReadOnlyList<string> words)
		{
			if (words == null)
				throw new ArgumentNullException(nameof(words));
			if (words.Count == 0)
				throw new ArgumentOutOfRangeException(nameof(words) + ".Count", words.Count, null);
			if (minLength < 0)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (maxLength < MinLoremLength)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (minLength > maxLength)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);

			int len = Rand.Int(Math.Max(minLength, MinLoremLength), maxLength);
			StringBuilder text = new StringBuilder();
			int point = Rand.Int(3, 15);
			int coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
			bool ucase = true;
			while (text.Length < len)
			{
				var s = Rand.Item(words);
				if (text.Length > 0)
				{
					if (--point == 0)
					{
						text.Append('.');
						point = Rand.Int(3, 15);
						coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
						ucase = true;
					}
					else if (--coma == 0)
					{
						text.Append(',');
						coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
					}
					text.Append(' ');
				}
				if (ucase)
					text.Append(Char.ToUpperInvariant(s[0])).Append(s.Substring(1));
				else
					text.Append(s);
				ucase = false;
			}
			if (text.Length >= maxLength)
				text.Length = maxLength - 1;
			if (text.Length > 0 && text[text.Length - 1] != '.')
				text.Append('.');
			return text.ToString();
		}
	}
}
#endif