namespace Lexxys.Configuration;

public readonly record struct ConfigSourceLocation(string? FileName, int Line, int Column)
{
	public bool IsEmpty => FileName is null && Line == 0 && Column == 0;

	public override string ToString()
	{
		if (IsEmpty)
			return String.Empty;
		var location = Line > 0 ? Column > 0 ? $"{Line}:{Column}": Line.ToString(): String.Empty;
		if (String.IsNullOrEmpty(FileName))
			return location ?? String.Empty;
		return String.IsNullOrEmpty(location) ? FileName ?? String.Empty: $"{FileName}:{location}";
	}
}
