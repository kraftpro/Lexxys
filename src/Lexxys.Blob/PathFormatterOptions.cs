namespace Lexxys;

public class PathFormatterOptions
{
	public int DirectoryCount { get; set; } = 100;
	public int FileCount { get; set; } = 1000;
	public int Radix { get; set; } = 10;
	public bool Flat { get; set; } = false;
	public char PathSeparator { get; set; } = default;
	public string? TemporaryFolder { get; set; } = null;
	public DirectoryGenerationMode Mode { get; set; } = default;
}