namespace Lexxys.Testing;

public static partial class R
{
	/// <summary>
	/// Loads lines from a file, optionally applying a filter to each line, and returns a list of non-null lines.
	/// Consecutive blank lines in the file are represented as single empty strings in the result.
	/// Leading and trailing lines that are blank are excluded from the result.
	/// </summary>
	/// <remarks>The returned list preserves the order of lines in the file, excluding lines filtered out by the
	/// <paramref name="filter"/> function. If multiple consecutive blank lines are present in the file, only a single
	/// empty string is included in the result for that sequence.</remarks>
	/// <param name="path">The path to the file to load. Cannot be null.</param>
	/// <param name="filter">An optional function to process or filter each line. If provided, the function is called for each line; lines for
	/// which the function returns null are excluded from the result. If null, lines are trimmed and empty or
	/// whitespace-only lines are excluded.</param>
	/// <returns>A list of strings containing the filtered lines from the file. Blank lines are represented as empty strings, and
	/// consecutive blank lines are collapsed into a single empty string. The list will be empty if the file contains no
	/// non-filtered lines.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
	public static IList<string> LoadFile(string path, Func<string, string?>? filter = null)
	{
		if (path == null) throw new ArgumentNullException(nameof(path));
		var lines = new List<string>();
		int blanks = 0;
		filter ??= o => (o = o.Trim()).Length == 0 ? null : o;

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
	/// Loads and parses the lines of a text file into a list of values of type T.
	/// </summary>
	/// <remarks>Empty or whitespace-only lines are ignored. Only lines for which the parser returns a successful
	/// result are included in the returned list.</remarks>
	/// <typeparam name="T">The type of value to parse from each line of the file.</typeparam>
	/// <param name="path">The path to the text file to read. Cannot be null.</param>
	/// <param name="parser">A delegate that attempts to parse a line of text into a value of type T. Cannot be null.</param>
	/// <returns>A list containing all successfully parsed values from the file. Lines that cannot be parsed are skipped.</returns>
	/// <exception cref="ArgumentNullException">Thrown if path or parser is null.</exception>
	public static IList<T> LoadFile<T>(string path, TryParse<T> parser)
	{
		if (path == null) throw new ArgumentNullException(nameof(path));
		if (parser == null) throw new ArgumentNullException(nameof(parser));
		var result = new List<T>();
		foreach (var item in File.ReadLines(path))
		{
			var line = item.Trim();
			if (line.Length == 0 || !parser(line, out var value))
				continue;
			result.Add(value);
		}
		return result;
	}
}
