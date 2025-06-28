using Lexxys.Tokenizer;

namespace Lexxys.Configuration;

public readonly struct FilePosition: IEquatable<FilePosition>, IComparable<FilePosition>
{
	public string? FileName { get; }
	public int Line { get; }
	public int Column { get; }

	public FilePosition(string? fileName, int line, int column)
	{
		FileName = fileName;
		Line = line;
		Column = column;
	}

	public FilePosition(int line, int column)
	{
		Line = line;
		Column = column;
	}

	public FilePosition(CharPosition position, string? fileName = null)
	{
		Line = position.Line + 1;
		Column = position.Column + 1;
	}

	public override string ToString() => FileName == null ? $"{Line}:{Column}": $"{FileName}({Line}:{Column})";

	public override int GetHashCode() => HashCode.Join(Line, Column);

	public override bool Equals(object? obj) => obj is FilePosition position && Equals(position);

	public bool Equals(FilePosition other) => Line == other.Line && Column == other.Column;

	public int CompareTo(FilePosition other) => Line == other.Line ? Column.CompareTo(other.Column): Line.CompareTo(other.Line);

	public static bool operator ==(FilePosition left, FilePosition right) => left.Equals(right);

	public static bool operator !=(FilePosition left, FilePosition right) => !(left == right);

	public static bool operator <(FilePosition left, FilePosition right) => left.CompareTo(right) < 0;

	public static bool operator <=(FilePosition left, FilePosition right) => left.CompareTo(right) <= 0;

	public static bool operator >(FilePosition left, FilePosition right) => left.CompareTo(right) > 0;

	public static bool operator >=(FilePosition left, FilePosition right) => left.CompareTo(right) >= 0;
}
