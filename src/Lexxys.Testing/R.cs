using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable CA1720 // Identifier contains type name

namespace Lexxys.Testing;

/// <summary>
/// Static class with methods to generate random values.
/// </summary>
public static class R
{
	private const int DefaultMaxTries = 9999;

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Collection of items.</param>
	/// <returns></returns>
	public static RandItem<T> Any<T>(params T[] items)
	{
		return new RandItem<T>(() => Rand.Item(items));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Collection of items.</param>
	/// <returns></returns>
	public static RandItem<T> Any<T>(IEnumerable<T>? items)
	{
		if (items == null)
			return RandItem<T>.Empty;
		if (items is ICollection<T> col)
		{
			if (col.Count == 0)
				return RandItem<T>.Empty;
			if (col.Count == 1)
			{
				var v = col.FirstOrDefault()!;
				return new RandItem<T>(v);
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
				return new RandItem<T>(v);
			}
			return new RandItem<T>(() => Rand.Item(vv));
		}
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with single value and a weight of 1.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="value">The value</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(T value) => new RandItem<T>(value);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with single value and the specified weight.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="weight">Weight of the value</param>
	/// <param name="value">The value</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(double weight, T value) => new RandItem<T>(weight, value);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with the specified value generator and weight = 1.0
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="generator">Value generator</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(Func<T> generator) => new RandItem<T>(generator);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with the specified value generator and the specified weight.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="weight">Weight of the value</param>
	/// <param name="generator">Value generator</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(double weight, Func<T> generator) => new RandItem<T>(weight, generator);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> randomly returning item from provided collection of <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="items">Collection of <see cref="RandItem{T}"/>s.</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(params RandItem<T>[] items) => new RandItem<T>(items);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> randomly returning item from provided collection of <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="items">Collection of <see cref="RandItem{T}"/>s.</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(IEnumerable<RandItem<T>> items) => new RandItem<T>(items);

	/// <summary>
	/// Create new <see cref="RandItem{T}"/> randomly returning item from provided collection <paramref name="pairs"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value</typeparam>
	/// <param name="pairs">Collection of weight-value pairs.</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(params (double Weight, T Value)[] pairs) => new RandItem<T>(Array.ConvertAll(pairs, o => new RandItem<T>(o.Weight, o.Value)), false);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> with the specified weight.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="weight">Weight of the value</param>
	/// <param name="value">The <see cref="RandItem{T}"/></param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(double weight, RandItem<T> value) => value.WithWeight(weight);

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/>
	/// </summary>
	/// <typeparam name="T">Type of the item value.</typeparam>
	/// <param name="generator">Item value generator.</param>
	/// <param name="filter">Predicate to filter out the item values.</param>
	/// <param name="maxTries">Max number of tries</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(Func<T> generator, Func<T, bool> filter, int maxTries = 0)
	{
		if (generator == null)
			throw new ArgumentNullException(nameof(generator));
		if (filter == null)
			throw new ArgumentNullException(nameof(filter));
		if (maxTries <= 0)
			maxTries = DefaultMaxTries;

		return new RandItem<T>(() =>
		{
			for (int i = 0; i != maxTries; ++i)
			{
				var t = generator();
				if (filter(t))
					return t;
			}
			throw new InvalidOperationException($"Cannot find an appropriate item value in {maxTries} tries.");
		});
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> based on the specifies <paramref name="generator"/> and the specified <paramref name="filter"/>.
	/// </summary>
	/// <typeparam name="T">Type of the item value</typeparam>
	/// <param name="generator">Random item values generator.</param>
	/// <param name="filter">Predicate to filter out generated item values.</param>
	/// <param name="maxTries">Max number of tries</param>
	/// <returns>new <see cref="RandItem{T}"/></returns>
	public static RandItem<T> I<T>(RandItem<T> generator, Func<T, bool> filter, int maxTries = 0)
	{
		if (generator == null)
			throw new ArgumentNullException(nameof(generator));
		if (filter == null)
			throw new ArgumentNullException(nameof(filter));
		return I(generator.NextValue, filter, maxTries);
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random integer in range <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number to be returned.</param>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	public static RandItem<int> Int(int min, int max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<int>(() => Rand.Int(min, max));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random integer in range 0 (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is negative.</exception>
	public static RandItem<int> Int(int max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<int>(() => Rand.Int(max));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random long numbers in range <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number to be returned.</param>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	public static RandItem<long> Int(long min, long max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<long>(() => Rand.Long(min, max));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random long numbers in range 0 (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is negative.</exception>
	public static RandItem<long> Int(long max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<long>(() => Rand.Long(max));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random decimal numbers in range <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number to be returned.</param>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
	/// <returns></returns>
	public static RandItem<decimal> Dec(decimal min, decimal max)
	{
		if (min < 0) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<decimal>(() => Rand.Dec(min, max));
	}

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> returning random decimal numbers in range 0 (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is negative.</exception>
	public static RandItem<decimal> Dec(decimal max)
	{
		if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<decimal>(() => Rand.Dec(max));
	}

	/// <summary>
	/// Returns a new <see cref="RandItem{T}"/> returning random double numbers in range <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="min">The inclusive lower bound of the random number to be returned.</param>
	/// <param name="max">The exclusive upper bound of the random number to be returned.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or infinity or NaN or <paramref name="max"/> is less than <paramref name="min"/> or infinity or NaN.</exception>
	/// <returns></returns>
	public static RandItem<double> Dbl(double min, double max)
	{
		if (min < 0 || double.IsInfinity(min) || double.IsNaN(min)) throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max < min || double.IsInfinity(max) || double.IsNaN(max)) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<double>(() => Rand.Dbl(min, max));
	}

	/// <summary>
	/// Returns a new <see cref="RandItem{T}"/> returning random double numbers in range 0 (inclusive) to <paramref name="max"/> (exclusive)
	/// </summary>
	/// <param name="max"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is negative or infinity or NaN.</exception>
	public static RandItem<double> Dbl(double max)
	{
		if (max < 0 || double.IsInfinity(max) || double.IsNaN(max)) throw new ArgumentOutOfRangeException(nameof(max), max, null);
		return new RandItem<double>(() => Rand.Dbl(max));
	}

	/// <summary>
	/// Returns a new <see cref="RandItem{T}">RandItem&lt;<see cref="string"/>&gt;</see> returning the formatted string value of the <paramref name="item"/> using <paramref name="format"/> and the specified <paramref name="formatProvider"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item">The item to format.</param>
	/// <param name="format">Format string.</param>
	/// <param name="formatProvider">Format provider.</param>
	/// <returns></returns>
	public static RandItem<string> Fmt<T>(RandItem<T> item, string format, IFormatProvider? formatProvider = null) => new RandItem<string>(() => item.ToString(format, formatProvider));

	/// <summary>
	/// Returns a new <see cref="RandItem{T}">RandItem&lt;<see cref="string"/>&gt;</see> returning the formatted string value of the <paramref name="item"/> using randomly selected <paramref name="format"/> and the specified <paramref name="formatProvider"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item">The item to format.</param>
	/// <param name="format">Format string generator.</param>
	/// <param name="formatProvider">Format provider.</param>
	/// <returns></returns>
	public static RandItem<string> Fmt<T>(RandItem<T> item, RandItem<string> format, IFormatProvider? formatProvider = null) => new RandItem<string>(() => item.ToString(format, formatProvider));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing values of the specified <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(params RandItem<T>[] items) => new RandItem<string>(() => String.Join(null, items));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(IReadOnlyCollection<RandItem<T>> items) => new RandItem<string>(() => String.Join(null, items));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/> converted to string using the specified <paramref name="convert"/> function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="convert">Conversion function.</param>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(Func<T, string> convert, params RandItem<T>[] items) => new RandItem<string>(() => String.Join(null, items.Select(o => convert(o!))));

	/// <summary>
	/// Creates a new <see cref="RandItem{T}"/> containing concatenated values of the specified <paramref name="items"/> converted to string using the specified <paramref name="convert"/> function.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="convert">Conversion function.</param>
	/// <param name="items">Items to concatenate.</param>
	/// <returns></returns>
	public static RandItem<string> Concat<T>(Func<T, string> convert, IReadOnlyCollection<RandItem<T>> items) => new RandItem<string>(() => String.Join(null, items.Select(o => convert(o!))));

	private const int NC = 'z' - 'a' + 1;
	private const int NC2 = NC + NC;
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range '0' to '9'.
	/// </summary>
	public static RandItem<char> DigitChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, 10) + '0'));
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range 'a' to 'z'.
	/// </summary>
	public static RandItem<char> LowerChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, NC) + 'a'));
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range 'A' to 'Z'.
	/// </summary>
	public static RandItem<char> UpperChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, NC) + 'A'));
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range 'a' to 'z' or 'A' to 'Z'.
	/// </summary>
	public static RandItem<char> LetterChar { get; } = new RandItem<char>(() => { int i = Rand.Int(0, NC * 2); return (char)(i < NC ? i + 'a' : i + ('A' - NC)); });
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range 'a' to 'z' or 'A' to 'Z' or '0' to '9'.
	/// </summary>
	public static RandItem<char> LetterOrDigitChar { get; } = new RandItem<char>(() => { int i = R.Int(0, NC2 + 10); return (char)(i < NC ? i + 'a' : i < NC2 ? i + ('A' - NC) : i + ('0' - NC2)); });
	/// <summary>
	/// A <see cref="RandItem{T}"/> that returns a random char in range ' ' to '~'.
	/// </summary>
	public static RandItem<char> AsciiChar { get; } = new RandItem<char>(() => (char)Rand.Int(' ', 127));
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random char in range <paramref name="min"/> to <paramref name="max"/> (inclusive).
	/// </summary>
	/// <param name="min">Minimum char code.</param>
	/// <param name="max">Maximum char code.</param>
	/// <returns></returns>
	public static RandItem<char> Chr(int min, int max) => new RandItem<char>(() => (char)Rand.Int(min, max + 1));
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random char from the specified collection of characters.
	/// </summary>
	/// <param name="chars">Collection of characters.</param>
	/// <returns></returns>
	public static RandItem<char> Chr(char[] chars) => new RandItem<char>(() => chars[Rand.Int(0, chars.Length)]);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random char from the specified string.
	/// </summary>
	/// <param name="chars">String of characters.</param>
	/// <returns></returns>
	public static RandItem<char> Chr(string chars) => new RandItem<char>(() => chars[Rand.Int(0, chars.Length)]);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from ' ' to '~'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Ascii(int minLength, int maxLength) => Str(AsciiChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from ' ' to '~'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Ascii(int length) => Str(AsciiChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from '0' to '9'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Digit(int minLength, int maxLength) => Str(DigitChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from '0' to '9'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Digit(int length) => Str(DigitChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Lower(int minLength, int maxLength) => Str(LowerChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Lower(int length) => Str(LowerChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'A' to 'Z'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Upper(int minLength, int maxLength) => Str(UpperChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'A' to 'Z'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Upper(int length) => Str(UpperChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z' or 'A' to 'Z'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Letter(int minLength, int maxLength) => Str(LetterChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z' or 'A' to 'Z'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> Letter(int length) => Str(LetterChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z' or 'A' to 'Z' or '0' to '9'.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> LetterOrDigit(int minLength, int maxLength) => Str(LetterOrDigitChar, minLength, maxLength);
	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters in range from 'a' to 'z' or 'A' to 'Z' or '0' to '9'.
	/// </summary>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	public static RandItem<string> LetterOrDigit(int length) => Str(LetterOrDigitChar, length);

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters from the specified random character generator.
	/// </summary>
	/// <param name="ci">Character generator.</param>
	/// <param name="minLength">Minimum length of the generated string.</param>
	/// <param name="maxLength">Maximum length of the generated string.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static RandItem<string> Str(RandItem<char> ci, int minLength, int maxLength = 0)
	{
		if (minLength < 0) throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
		if (maxLength < minLength) throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);

		if (minLength == maxLength)
			return Str(ci, minLength);

		int min = minLength;
		int max = maxLength + 1;
		return new RandItem<string>(() =>
		{
			var text = new char[Rand.Int(min, max)];
			for (int i = 0; i < text.Length; ++i)
				text[i] = ci.NextValue();
			return new String(text);
		});
	}

	/// <summary>
	/// Returns a <see cref="RandItem{T}"/> that returns a random string of characters from the specified random character generator.
	/// </summary>
	/// <param name="ci">Character generator.</param>
	/// <param name="length">Length of the generated string.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static RandItem<string> Str(RandItem<char> ci, int length)
	{
		if (length == 0) return new RandItem<string>(() => String.Empty);
		if (length == 1) return new RandItem<string>(() => Char.ToString(ci.NextValue()));
		return new RandItem<string>(() =>
		{
			var text = new char[length];
			for (int i = 0; i < text.Length; ++i)
				text[i] = ci.NextValue();
			return new String(text);
		});
	}

	/// <summary>
	/// Loads a file and returns a list strings. Leading and trailing empty lines are removed.
	/// </summary>
	/// <param name="path">Path to the file.</param>
	/// <param name="filter">Filter function that is called for each line. If it returns null, the line is ignored.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IList<string> LoadFile(string path, Func<string, string?>? filter = null)
	{
		if (path == null) throw new ArgumentNullException(nameof(path));
		var lines = new List<string>();
		int blanks = 0;
		filter ??= o => o.TrimToNull();

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

	/// <summary>
	/// Genetic value parser declaration.
	/// </summary>
	/// <typeparam name="T">Result type.</typeparam>
	public delegate bool TryParse<T>(string text, out T value);

	/// <summary>
	/// Loads a file and returns a list of parsed values. All empty lines are ignored.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="path">Path to the file.</param>
	/// <param name="parser">Parser function that is called for each line to construct the value. If it returns false, the line is ignored.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IList<T> LoadFile<T>(string path, TryParse<T> parser)
	{
		if (path == null) throw new ArgumentNullException(nameof(path));
		if (parser == null) throw new ArgumentNullException(nameof(parser));
		var result = new List<T>();
		foreach (var item in File.ReadLines(path))
		{
			var line = item.TrimToNull();
			if (line == null || !parser(line, out var value))
				continue;
			result.Add(value);
		}
		return result;
	}

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified <paramref name="weight"/> and the specified <paramref name="picture"/> for the item generator.
	/// </summary>
	/// <param name="weight">Weight of the item.</param>
	/// <param name="picture">The picture for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(double weight, string picture) => new RandItem<string>(GetPicture(weight, picture ?? throw new ArgumentNullException(nameof(picture))));

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="picture"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="picture">Collection of the pictures for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(IEnumerable<string> picture) => new RandItem<string>(picture.Select(o => GetPicture(1, o)));

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="picture"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="picture">Collection of the pictures for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(params string[] picture) => new RandItem<string>(Array.ConvertAll(picture, o => GetPicture(1, o)), false);

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified collection of <paramref name="items"/>s to be randomly selected for the item generator.
	/// </summary>
	/// <param name="items">Collection of the weight-pictures pairs for the item generator.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static RandItem<string> Picture(params (double Weight, string Picture)[] items) => new RandItem<string>(Array.ConvertAll(items, o => GetPicture(o.Weight, o.Picture)), false);

	/// <summary>
	/// Creates a <see cref="RandItem{T}"/> using the specified random item generator <paramref name="item"/> for selecting the picture.
	/// </summary>
	/// <param name="item">Random item generator for selecting the picture.</param>
	/// <returns></returns>
	public static RandItem<string> Picture(RandItem<string> item) => new RandItem<string>(() => ParsePicture(1, item.NextValue()).NextValue());

	private static RandItem<string> GetPicture(double weight, string picture) => ParsePicture(weight, picture);

	private static RandItem<string> ParsePicture(double weight, string picture)
	{
		var items = new List<RandItem<string>>();
		int l = 0;
		var rs = __pic.Replace(picture, Evaluator);
		if (rs == picture)
			return new RandItem<string>(weight, picture);
		if (l < picture.Length)
			items.Add(I(picture.Substring(l)));
		var array = items.ToArray();
		return new RandItem<string>(weight, () => String.Join(null, array));

		string Evaluator(Match m)
		{
			string pad = String.Empty;
			if (m.Index > l)
			{
				string txt = picture.Substring(l, m.Index - l);
				if (txt.EndsWith(" ", StringComparison.InvariantCulture))
				{
					pad = " ";
					txt = txt.Substring(0, txt.Length - 1);
				}
				if (txt.Length > 0)
					items.Add(I(txt));
			}
			l = m.Index + m.Length;
			string value = m.Value;
			string s = value;
			RandItem<string> item;
			if (!s.StartsWith("{", StringComparison.InvariantCulture))
			{
				int len = s.Length;
				item = I(() => pad + new String(R.DigitChar.Collect(len)));
			}
			else
			{
				s = s.Substring(1, s.Length - 2);
				int i = s.IndexOf(':');
				string? f = null;
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
			items.Add(item);
			return "";
		}
	}
	private static readonly Regex __pic = new Regex(@"#+|\{[^}]*}");

	private static string GetResourceItem(double probability, string name, string? format)
	{
		return Rand.Dbl() >= probability ? "" :
			!Resources.Resource.TryGetValue(name, out var val) ? "" :
			format == null ? val.ToString() :
			val.ToString(format, null);
	}

	private static string Pad(string pad, string? value)
	{
		if (value == null)
			return "";
		value = value.Trim();
		return value.Length == 0 ? value : pad + value;
	}

	private const int MinLoremLength = 12;

	/// <summary>
	/// Generates a random text using the specified collection of <paramref name="words"/> to be randomly selected for the text generator.
	/// </summary>
	/// <param name="maxLength">Maximum length of the generated text.</param>
	/// <param name="words">Random item generator for selecting the words.</param>
	/// <returns></returns>
	public static RandItem<string> Text(int maxLength, RandItem<string> words) => new RandItem<string>(() => GetText(0, maxLength, words));
	
	/// <summary>
	/// Generates a random text using the specified collection of <paramref name="words"/> to be randomly selected for the text generator.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated text.</param>
	/// <param name="maxLength">Maximum length of the generated text.</param>
	/// <param name="words">Random item generator for selecting the words.</param>
	/// <returns></returns>
	public static RandItem<string> Text(int minLength, int maxLength, RandItem<string> words) => new RandItem<string>(() => GetText(minLength, maxLength, words));

	private static string GetText(int minLength, int maxLength, RandItem<string> words)
	{
		if (minLength < 0)
			throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
		if (maxLength < MinLoremLength)
			throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);
		if (minLength > maxLength)
			throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);

		int len = Rand.Int(Math.Max(minLength, MinLoremLength), maxLength);
		var text = new StringBuilder();
		int point = Rand.Int(3, 15);
		int coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
		bool upper = true;
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
					upper = true;
				}
				else if (--coma == 0)
				{
					text.Append(',');
					coma = point < 5 ? 0 : Rand.Case(0.4, 0, Rand.Int(3, point));
				}
				text.Append(' ');
			}
			if (upper)
				text.Append(Char.ToUpperInvariant(s[0])).Append(s.AsSpan(1));
			else
				text.Append(s);
			upper = false;
		}
		if (text.Length >= maxLength)
			text.Length = maxLength - 1;
		if (text.Length > 0 && text[text.Length - 1] != '.')
			text.Append('.');
		return text.ToString();
	}
}
