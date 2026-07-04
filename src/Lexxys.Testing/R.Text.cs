using System.Text;

namespace Lexxys.Testing;

public static partial class R
{
	private const int MinLoremLength = 12;

	/// <summary>
	/// Generates a random text using the specified collection of <paramref name="words"/> to be randomly selected for the text generator.
	/// </summary>
	/// <param name="maxLength">Maximum length of the generated text.</param>
	/// <param name="words">Random item generator for selecting the words.</param>
	/// <returns></returns>
	public static RandItem<string> Text(int maxLength, RandItem<string> words)
	{
		if (maxLength < MinLoremLength)
			throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);

		return new RandItem<string>(() => GetText(MinLoremLength, maxLength, words));
	}

	/// <summary>
	/// Generates a random text using the specified collection of <paramref name="words"/> to be randomly selected for the text generator.
	/// </summary>
	/// <param name="minLength">Minimum length of the generated text.</param>
	/// <param name="maxLength">Maximum length of the generated text.</param>
	/// <param name="words">Random item generator for selecting the words.</param>
	/// <returns></returns>
	public static RandItem<string> Text(int minLength, int maxLength, RandItem<string> words)
	{
		if (minLength < 0)
			throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
		if (maxLength < MinLoremLength)
			throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, null);
		if (minLength > maxLength)
			throw new ArgumentOutOfRangeException(nameof(minLength), minLength, null);
		if (minLength < MinLoremLength)
			minLength = MinLoremLength;

		return new RandItem<string>(() => GetText(minLength, maxLength, words));
	}

	private static string GetText(int minLength, int maxLength, RandItem<string> words)
	{
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
