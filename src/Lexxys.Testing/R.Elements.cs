#if NET
using System.Text.Unicode;
#endif

namespace Lexxys.Testing;

public static partial class R
{
	private const int NC = 'z' - 'a' + 1;
	private const int NC2 = NC + NC;

	/// <summary>
	/// Creates a random item generator that selects a character at random from the specified string.
	/// </summary>
	/// <param name="chars">The string containing the set of characters to select from. Must not be null or empty.</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;char&gt;</see> that produces a random character from the provided string each time it is invoked.</returns>
	/// <exception cref="ArgumentException">Thrown if chars is null or an empty string.</exception>
	public static RandItem<char> Any(string chars)
	{
		if (String.IsNullOrEmpty(chars))
			throw new ArgumentException("The character string must contain at least one character.", nameof(chars));
		return new RandItem<char>(() => chars[Rand.Int(0, chars.Length)]);
	}
	///// <summary>
	///// Returns a <see cref="RandItem{T}"/> that returns a random char from the specified string.
	///// </summary>
	///// <param name="chars">String of characters.</param>
	///// <returns></returns>

	/// <summary>
	/// Creates a random character generator that produces characters within the specified Unicode code point range.
	/// </summary>
	/// <remarks>The specified range must not consist solely of surrogate code points, as these do not represent
	/// valid standalone UTF-16 characters.</remarks>
	/// <param name="min">The minimum Unicode code point value, inclusive. Must be between 0 and 65535 (0xFFFF).</param>
	/// <param name="max">The maximum Unicode code point value, inclusive. Must be between 0 and 65535 (0xFFFF).</param>
	/// <returns>A <see cref="RandItem{T}">RandItem&lt;char&gt;</see> that produces a randomly selected character within the specified code point range.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if min or max is outside the range 0 to 65535, if min is greater than max, or if the specified range
	/// consists entirely of surrogate code points (0xD800 to 0xDFFF).</exception>
	public static RandItem<char> Chr(int min, int max)
	{
		if (min is < 0 or > 0xFFFF)
			throw new ArgumentOutOfRangeException(nameof(min), min, null);
		if (max is < 0 or > 0xFFFF || max < min)
			throw new ArgumentOutOfRangeException(nameof(max), max, null);
		if (min >= 0xD800 && max <= 0xDFFF)
			throw new ArgumentOutOfRangeException(nameof(min), "The specified range consists entirely of surrogate code points, which do not correspond to valid UTF-16 characters.");
		++max;
		return new RandItem<char>(
			max < 0xD800 || min > 0xDFFF ?
				() => (char)Rand.Int(min, max) :
				() => NextUnicodeChar(min, max));

		static char NextUnicodeChar(int min, int max)
		{
			int i;
			do
			{
				i = Rand.Int(min, max);
			} while (i is >= 0xD800 and <= 0xDFFF); // Skip surrogate code points
			return (char)i;
		}
	}

#if NET

	/// <summary>
	/// Creates a random character generator that produces characters within the specified Unicode range.
	/// </summary>
	/// <remarks>If the specified range includes surrogate pairs, the generator will handle them to ensure only
	/// valid UTF-16 characters are produced.</remarks>
	/// <param name="range">The UnicodeRange that defines the range of valid Unicode code points from which to generate random characters. The
	/// range must be valid and must not exceed the maximum allowed Unicode code point.</param>
	/// <returns>A RandItem&lt;char&gt; that generates a random character within the specified Unicode range.</returns>

	public static RandItem<char> Chr(UnicodeRange range) => Chr(range.FirstCodePoint, range.FirstCodePoint + range.Length - 1);

#endif


	/// <summary>
	/// Gets a random item generator that produces a single digit character from '0' to '9'.
	/// </summary>
	/// <remarks>Use this property to obtain random numeric characters, for example when generating random codes or
	/// test data. Each generated character is uniformly distributed in the range '0' through '9'.</remarks>
	public static RandItem<char> DigitChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, 10) + '0'));

	/// <summary>
	/// Gets a random item generator that produces lowercase alphabetic characters ('a' through 'z').
	/// </summary>
	public static RandItem<char> LowerChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, NC) + 'a'));

	/// <summary>
	/// Gets a random item generator that produces uppercase alphabetic characters ('A' through 'Z').
	/// </summary>
	public static RandItem<char> UpperChar { get; } = new RandItem<char>(() => (char)(Rand.Int(0, NC) + 'A'));

	/// <summary>
	/// Gets a random item generator that produces lowercase or uppercase English alphabetic characters.
	/// </summary>
	/// <remarks>The generated character is selected uniformly at random from the set of 'a'-'z' and 'A'-'Z'.</remarks>
	public static RandItem<char> LetterChar { get; } = new RandItem<char>(() => { int i = Rand.Int(0, NC * 2); return (char)(i < NC ? i + 'a' : i + ('A' - NC)); });

	/// <summary>
	/// Gets a random item generator that produces a random lowercase letter, uppercase letter, or digit character each
	/// time it is accessed.
	/// </summary>
	/// <remarks>The generated character is selected uniformly at random from the ranges 'a'-'z', 'A'-'Z', and '0'-'9'.</remarks>
	public static RandItem<char> LetterOrDigitChar { get; } = new RandItem<char>(() => { int i = R.Int(0, NC2 + 10); return (char)(i < NC ? i + 'a' : i < NC2 ? i + ('A' - NC) : i + ('0' - NC2)); });

	/// <summary>
	/// Gets a random item generator that produces ASCII characters in the printable range.
	/// </summary>
	/// <remarks>The generated characters include all printable ASCII characters from space (' ', code 32) to tilde ('~', code 126), inclusive.</remarks>
	public static RandItem<char> AsciiChar { get; } = new RandItem<char>(() => (char)Rand.Int(' ', 127));

	/// <summary>
	/// Generates a random string consisting of ASCII characters within the specified length range.
	/// </summary>
	/// <param name="minLength">The minimum length of the generated string. Must be greater than or equal to 0 and less than or equal to <paramref name="maxLength"/>.</param>
	/// <param name="maxLength">The maximum length of the generated string. Must be greater than or equal to <paramref name="minLength"/>.</param>
	/// <returns>A <see cref="RandItem{String}">RandItem&lt;String&gt;</see> that produces random ASCII strings with lengths between <paramref
	/// name="minLength"/> and <paramref name="maxLength"/>, inclusive.</returns>
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
	public static RandItem<string> Str(RandItem<char> ci, int minLength, int maxLength)
	{
		if (minLength < 0) throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
		if (maxLength < minLength) throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);

		if (minLength == maxLength)
			return Str(ci, minLength);

		int min = minLength;
		int max = maxLength + 1;
		return new RandItem<string>(() =>
		{
			int length = Rand.Int(min, max);
			Span<char> text = length < 4096 ? stackalloc char[length]: new char[length];
			for (int i = 0; i < text.Length; ++i)
				text[i] = ci.NextValue();
			return text.ToString();
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
			Span<char> text = length < 2048 ? stackalloc char[length]: new char[length];
			for (int i = 0; i < text.Length; ++i)
				text[i] = ci.NextValue();
			return text.ToString();
		});
	}
}
