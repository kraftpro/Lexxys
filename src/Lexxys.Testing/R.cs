using Lexxys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Testing
{
	public static class R
	{
		public static RandItem<T> Any<T>(T first, T second, params T[] rest)
		{
			if (rest == null || rest.Length == 0)
			{
				var fs = new[] { first, second };
				return new RandItem<T>(() => Rand.Item(fs));
			}
			var vv = new T[rest.Length + 2];
			vv[0] = first;
			vv[0] = second;
			Array.Copy(rest, 0, vv, 1, rest.Length);
			return new RandItem<T>(() => Rand.Item(vv));
		}

		public static RandItem<T> Any<T>(IEnumerable<T> items)
		{
			if (items == null)
				return RandItem<T>.Empty;
			if (items is ICollection<T> col)
			{
				if (col.Count == 0)
					return RandItem<T>.Empty;
				if (col.Count == 1)
				{
					var v = col.FirstOrDefault();
					return new RandItem<T>(1, v);
				}
				var vv = new T[col.Count];
				col.CopyTo(vv, 0);
				return new RandItem<T>(() => Rand.Item(vv));
			}
			else
			{
				var vv = items.ToList();
				if (vv.Count == 0)
					return RandItem<T>.Empty;
				if (vv.Count == 1)
				{
					var v = vv[0];
					return new RandItem<T>(1, v);
				}
				return new RandItem<T>(() => Rand.Item(vv));
			}
		}
		
		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/> with weight = 1.0
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="generator">Item value generator</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(Func<T> generator) => new RandItem<T>(generator);
		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="weight">Weight of the item</param>
		/// <param name="generator">Item value generator</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(float weight, Func<T> generator) => new RandItem<T>(weight, generator);
		/// <summary>
		/// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="pairs"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="pairs">Collection of values</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(params IWeightValuePair<T>[] pairs) => new RandItem<T>(pairs);
		/// <summary>
		/// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="pairs"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="pairs">Collection of values</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(IEnumerable<IWeightValuePair<T>> pairs) => new RandItem<T>(pairs);

		private const int MaxTries = 9999;

		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="generator">Item value generator</param>
		/// <param name="filter">Predicate on item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(Func<T> generator, Func<T, bool> filter)
		{
			if (generator == null)
				throw new ArgumentNullException(nameof(generator));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return new RandItem<T>(() =>
			{
				T t;
				int i = 0;
				while (++i <= MaxTries)
				{
					t = generator();
					if (filter(t))
						return t;
				}
				return default;
			});
		}

		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="weight">Weight of the item</param>
		/// <param name="generator">Item value generator</param>
		/// <param name="filter">Predicate on item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(float weight, Func<T> generator, Func<T, bool> filter)
		{
			if (generator == null)
				throw new ArgumentNullException(nameof(generator));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return new RandItem<T>(weight, () =>
			{
				T t;
				int i = 0;
				while (++i <= MaxTries)
				{
					t = generator();
					if (filter(t))
						return t;
				}
				return default;
			});
		}

		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="generator">Item value generator</param>
		/// <param name="filter">Predicate on item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(RandItem<T> generator, Func<T, bool> filter) => new RandItem<T>(() =>
		{
			if (generator == null)
				throw new ArgumentNullException(nameof(generator));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			T t;
			int i = 0;
			while (++i <= MaxTries)
			{
				t = generator.NextValue();
				if (filter(t))
					return t;
			}
			return default;
		});

		/// <summary>
		/// Creates a new <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="weight">Weight of the item</param>
		/// <param name="generator">Item value generator</param>
		/// <param name="filter">Predicate on item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> I<T>(float weight, RandItem<T> generator, Func<T, bool> filter)
		{
			if (generator == null)
				throw new ArgumentNullException(nameof(generator));
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return new RandItem<T>(weight, () =>
			{
				T t;
				int i = 0;
				while (++i <= MaxTries)
				{
					t = generator.NextValue();
					if (filter(t))
						return t;
				}
				return default;
			});
		}

		/// <summary>
		/// Creates a constant value <see cref="RandItem{T}"/> with weight = 1.0
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="value">Item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> V<T>(T value) => new RandItem<T>(1, value);
		/// <summary>
		/// Creates a constant value <see cref="RandItem{T}"/>
		/// </summary>
		/// <typeparam name="T">Type of item value</typeparam>
		/// <param name="weight">Weight of the item</param>
		/// <param name="value">Item value</param>
		/// <returns>new <see cref="RandItem{T}"/></returns>
		public static RandItem<T> V<T>(float weight, T value) => new RandItem<T>(weight, value);
		///// <summary>
		///// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="values"/>
		///// </summary>
		///// <typeparam name="T">Type of item value</typeparam>
		///// <param name="values">Collection of values</param>
		///// <returns>new <see cref="RandItem{T}"/></returns>
		//public static RandItem<T> I<T>(params T[] values) => new RandItem<T>(values);
		///// <summary>
		///// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="values"/>
		///// </summary>
		///// <typeparam name="T">Type of item value</typeparam>
		///// <param name="values">Collection of values</param>
		///// <returns>new <see cref="RandItem{T}"/></returns>
		//public static RandItem<T> I<T>(IEnumerable<T> values) => new RandItem<T>(values);
		///// <summary>
		///// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="values"/>
		///// </summary>
		///// <typeparam name="T">Type of item value</typeparam>
		///// <param name="values">Collection of values</param>
		///// <returns>new <see cref="RandItem{T}"/></returns>
		//public static RandItem<T> I<T>(IReadOnlyList<T> values) => new RandItem<T>(values);

		public static RandItem<int> Int(int min, int max) => new RandItem<int>(() => Rand.Int(min, max));
		public static RandItem<int> Int(int max) => new RandItem<int>(() => Rand.Int(max));
		public static RandItem<string> Int(int min, int max, string format) => new RandItem<string>(() => Rand.Int(min, max).ToString(format));
		public static RandItem<string> Int(int min, int max, RandItem<string> format) => new RandItem<string>(() => Rand.Int(min, max).ToString(format));

		public static RandItem<long> Int(long min, long max) => new RandItem<long>(() => Rand.Long(min, max));
		public static RandItem<long> Int(long max) => new RandItem<long>(() => Rand.Long(max));
		public static RandItem<string> Int(long min, long max, string format) => new RandItem<string>(() => Rand.Long(min, max).ToString(format));
		public static RandItem<string> Int(long min, long max, RandItem<string> format) => new RandItem<string>(() => Rand.Long(min, max).ToString(format));

		public static RandItem<decimal> Dec(decimal min, decimal max) => new RandItem<decimal>(() => Rand.Dec(min, max));
		public static RandItem<decimal> Dec(decimal max) => new RandItem<decimal>(() => Rand.Dec(max));
		public static RandItem<string> Dec(decimal min, decimal max, string format) => new RandItem<string>(() => Rand.Dec(min, max).ToString(format));
		public static RandItem<string> Dec(decimal min, decimal max, RandItem<string> format) => new RandItem<string>(() => Rand.Dec(min, max).ToString(format));

		public static RandItem<double> Dbl(double min, double max) => new RandItem<double>(() => Rand.Dbl(min, max));
		public static RandItem<double> Dbl(double max) => new RandItem<double>(() => Rand.Dbl(max));
		public static RandItem<string> Dbl(double min, double max, string format) => new RandItem<string>(() => Rand.Dbl(min, max).ToString(format));
		public static RandItem<string> Dbl(double min, double max, RandItem<string> format) => new RandItem<string>(() => Rand.Dbl(min, max).ToString(format));

		public static RandItem<string> Concat(params RandItem<string>[] items) => new RandItem<string>(() => String.Join("", items.Select(o => o.NextValue())));
		public static RandItem<string> Concat(IEnumerable<RandItem<string>> items) => new RandItem<string>(() => String.Join("", items.Select(o => o.NextValue())));

		public static RandItem<string> Concat<T>(params RandItem<T>[] items) => new RandItem<string>(() => String.Join("", items.Select(o => o.ToString())));
		public static RandItem<string> Concat<T>(IEnumerable<RandItem<T>> items) => new RandItem<string>(() => String.Join("", items.Select(o => o.ToString())));

		public static RandItem<string> Concat<T>(Func<T, string> convert, params RandItem<T>[] items) => new RandItem<string>(() => String.Join("", items.Select(o => convert(o))));
		public static RandItem<string> Concat<T>(Func<T, string> convert, IEnumerable<RandItem<T>> items) => new RandItem<string>(() => String.Join("", items.Select(o => convert(o))));

		public static RandItem<char> DigitChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, 10) + '0'));
		public static RandItem<char> LowerChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, 'z' - 'a' + 1) + 'a'));
		public static RandItem<char> UpperChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, 'Z' - 'A' + 1) + 'A'));
		public static RandItem<char> LetterChar { get; } = new RandItem<char>(() => { int i = Rand.Int(0, ('z'-'a'+1) * 2); return (char)(i < ('z'-'a'+1) ? i + 'a': i + ('z'-'a'+1) + 'A'); });
		public static RandItem<char> LetterOrDigitChar { get; } = new RandItem<char>(() => { int i = R.Int(0, ('z' - 'a' + 1) * 2 + 10); return (char)(i < 10 ? i + '0': i < 10 + ('z' - 'a' + 1) ? i - 10 + 'a' : i - 10 - ('z' - 'a' + 1) + 'A'); });
		public static RandItem<char> AsciiChar { get; } = new RandItem<char>(() => (char)Rand.Int(' ', 127));
		public static RandItem<char> Ascii0Char { get; } = new RandItem<char>(() => (char)Rand.Int(0, 127 + 1));
		public static RandItem<char> Chr(int min, int max) => new RandItem<char>(() => (char)Rand.Int(min, max + 1));
		public static RandItem<string> Digit(int length) => new RandItem<string>(() => new String(DigitChar.Collect(length)));
		public static RandItem<string> Lower(int length) => new RandItem<string>(() => new String(LowerChar.Collect(length)));
		public static RandItem<string> Upper(int length) => new RandItem<string>(() => new String(UpperChar.Collect(length)));
		public static RandItem<string> Ascii(int length) => new RandItem<string>(() => new String(AsciiChar.Collect(length)));
		public static RandItem<string> Ascii0(int length) => new RandItem<string>(() => new String(Ascii0Char.Collect(length)));
		public static RandItem<string> Letter(int length) => new RandItem<string>(() => new String(LetterChar.Collect(length)));
		public static RandItem<string> LetterOrDigit(int length) => new RandItem<string>(() => new String(LetterOrDigitChar.Collect(length)));
		public static RandItem<string> Str(RandItem<char> ci, int length) => new RandItem<string>(() => new String(ci.Collect(length)));

		public static List<string> LoadFile(string path, Func<string, string> filter = null)
		{
			var lines = new List<string>();
			int blanks = 0;
			if (filter == null)
				filter = o => o.TrimToNull();

			foreach (var item in File.ReadLines(path))
			{
				var line = filter(item);
				if (line == null)
					continue;
				if (line.Length == 0)
				{
					if (lines.Count > 0)
						++blanks;
					continue;
				}
				while (blanks > 0)
				{
					lines.Add(String.Empty);
					--blanks;
				}
				lines.Add(line);
			}
			return lines;
		}

		public static List<T> LoadFile<T>(string path, Func<string, T> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));
			return File.ReadLines(path).Select(filter).Where(o => !Object.Equals(o, default(T))).ToList();
		}

		public static RandSeq<T> Sequance<T>(params RandItem<T>[] items) => new RandSeq<T>(items);

		public static RandItem<string> Pic(double weight, string picture) => new RandItem<string>(Picture(weight, picture));
		public static RandItem<string> Pic(params string[] picture) => Pic((IEnumerable<string>)picture);
		public static RandItem<string> Pic(IEnumerable<string> picture) => new RandItem<string>(picture.Select(o => Picture(1, o)));
		public static RandItem<string> Pic(params IWeightValuePair<string>[] picture) => Pic((IEnumerable<IWeightValuePair<string>>)picture);
		public static RandItem<string> Pic(IEnumerable<IWeightValuePair<string>> picture) => new RandItem<string>(picture.Select(o => Picture(o.Weight, o.Value)));

		public static IWeightValuePair<T> P<T>(double weight, T value) => WeightValuePair.Create(weight, value);
		public static IWeightValuePair<T> P<T>(T value) => WeightValuePair.Create(value);
		public static IWeightValuePair<T> P<T>(double weight, Func<T> value) => WeightValuePair.Create(weight, value);
		public static IWeightValuePair<T> P<T>(Func<T> value) => WeightValuePair.Create(value);
		public static IWeightValuePair<T> P<T>(double weight, RandItem<T> value) => WeightValuePair.Create(weight, () => value.NextValue());
		public static IWeightValuePair<T> P<T>(RandItem<T> value) => WeightValuePair.Create(() => value.NextValue());

		private static IWeightValuePair<string> Picture(double weight, string picture)
		{
			var r = RandSeq<string>.Empty;
			int l = 0;
			var rs = __pic.Replace(picture, Evaluator);
			var result = r;
			if (rs == picture)
				return new WeightValuePair<string>(weight, rs);
			if (l < picture.Length)
				result |= V(picture.Substring(l));
			return new WeightFunctionPair<string>(weight, () => result.ToString());

			string Evaluator(Match m)
			{
				string pad = null;
				if (m.Index > l)
				{
					string txt = picture.Substring(l, m.Index - l);
					if (txt.EndsWith(" "))
					{
						pad = " ";
						txt = txt.Substring(0, txt.Length - 1);
					}
					if (txt.Length > 0)
						r |= V(txt);
				}
				l = m.Index + m.Length;
				string value = m.Value;
				string s = value;
				RandItem<string> item;
				if (!s.StartsWith("{"))
				{
					int len = s.Length;
					item = I(() => pad + new String(R.DigitChar.Collect(len)));
				}
				else
				{
					s = s.Substring(1, s.Length - 2);
					int i = s.IndexOf(':');
					string f = null;
					double p = 1.0;
					if (i >= 0)
					{
						f = s.Substring(i + 1);
						s = s.Substring(0, i);
					}
					i = s.IndexOf('|');
					if (i >= 0 && double.TryParse(s.Substring(0, i), out p))
						s = s.Substring(0, i + 1);
					item = I(() => Pad(pad, GetResourceItem(p, s, f)));
				}
				r |= item;
				return "";
			}
		}
		private static readonly Regex __pic = new Regex("#+|{[^}]*}");

		private static string GetResourceItem(double probability, string name, string format)
		{
			return Rand.Dbl() >= probability ? "":
				!Resources.Resource.TryGetValue(name, out var val) || val == null ? "":
				format == null ? val.ToString():
				(val as IFormattable)?.ToString(format, null) ?? val.ToString();
		}

		private static string Pad(string pad, string value)
		{
			if (value == null)
				return "";
			value = value.Trim();
			return value.Length == 0 ? value: pad + value;
		}

		public const int MinLoremLength = 12;

		public static RandItem<string> Lorem(int maxLength, RandItem<string> words) => new RandItem<string>(() => GetLorem(0, maxLength, words));
		public static RandItem<string> Lorem(int minLength, int maxLength, RandItem<string> words) => new RandItem<string>(() => GetLorem(minLength, maxLength, words));

		private static string GetLorem(int minLength, int maxLength, RandItem<string> words)
		{
			if (words == null)
				throw new ArgumentNullException(nameof(words));
			if (minLength < 0)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (maxLength < MinLoremLength)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
			if (minLength > maxLength)
				throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);

			int len = Rand.Int(Math.Max(minLength, MinLoremLength), maxLength);
			var text = new StringBuilder();
			int point = Rand.Int(3, 15);
			int coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
			bool ucase = true;
			while (text.Length < len)
			{
				var s = words.NextValue();
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
