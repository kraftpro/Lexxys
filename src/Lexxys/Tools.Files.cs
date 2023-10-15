using System.Diagnostics;

namespace Lexxys;

public static class Files
{
	/// <summary>
	/// Creates a new temporary file in the specified <paramref name="directory"/> and with <paramref name="suffix"/>.
	/// </summary>
	/// <param name="directory">Directory to create temporary file or null to use curren user's temporary directory</param>
	/// <param name="suffix">Temporary file suffix</param>
	/// <param name="action">Action to execute over the created file stream.</param>
	/// <returns><see cref="FileInfo"/> of the temporary file.</returns>
	public static FileInfo GetTempFile(string? directory, string? suffix = null, Action<FileStream>? action = null)
	{
		directory ??= Path.GetTempPath();

		const int LogFrequency = 5;
		const int TotalLimit = 30;
		int index = 0;
		var log = 0;
		for (;;)
		{
			try
			{
				var temp = new FileInfo(Path.Combine(directory, OrderedName() + suffix));
				using var stream = temp.Open(FileMode.CreateNew);
				action?.Invoke(stream);
				return temp;
			}
			catch (IOException flaw)
			{
				if (++index >= TotalLimit)
					throw;
				if (index >= log)
				{
					flaw.LogError();
					log += LogFrequency;
				}
			}
		}
	}

	private static string OrderedName() => SixBitsCoder.Thirty(((ulong)DateTime.UtcNow.Ticks - _base) ^ (ulong)__random.Next(32767));
	private static readonly Random __random = new Random();
	private static ulong _base = (ulong)new DateTime(DateTime.Today.Year - DateTime.Today.Year % 10, 1, 1).Ticks;
}