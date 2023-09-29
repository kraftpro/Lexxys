using System.Diagnostics;

namespace Lexxys;

public static class Files
{
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