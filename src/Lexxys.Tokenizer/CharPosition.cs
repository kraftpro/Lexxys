// Lexxys Infrastructural library.
// file: CharPosition.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Globalization;
using System.Diagnostics.Contracts;

namespace Lexxys.Tokenizer;

/// <summary>
/// Represents a position in a text.
/// </summary>
[Serializable]
public readonly struct CharPosition: IEquatable<CharPosition>
{
	public static readonly CharPosition Start = default;

	/// <summary>
	/// Creates a new <see cref="CharPosition"/> value.
	/// </summary>
	/// <param name="position">Zero based position in text.</param>
	/// <param name="line">Zero base line number.</param>
	/// <param name="column">Zero base column number.</param>
	public CharPosition(int position, int line, int column)
	{
		Position = position;
		Line = line;
		Column = column;
	}

	/// <summary>
	/// Copy constructor for <see cref="CharPosition"/> value.
	/// </summary>
	/// <param name="position">A <see cref="CharPosition"/> value to create a copy from.</param>
	public CharPosition(CharPosition position)
	{
		Position = position.Position;
		Line = position.Line;
		Column = position.Column;
	}

	public CharPosition(ReadOnlySpan<char> text, int position, int tabSize)
	{
		if (position <= 0) return;
		Position = position > text.Length ? text.Length : position;
		(Line, Column) = OffsetBox(text.Slice(0, Position), tabSize);
	}

	/// <summary>
	/// Offset of the position from the text beginning.
	/// </summary>
	public int Position { get; }

	/// <summary>
	/// Zero based line number of the text position.
	/// </summary>
	public int Line { get; }

	/// <summary>
	/// Zero based character number of the text position.
	/// </summary>
	public int Column { get; }

	/// <summary>
	/// Converts the text position to the string representation using the specified culture-specific format information.
	/// </summary>
	/// <param name="culture">An object that supplies culture-specific formatting information.</param>
	/// <returns></returns>
	[Pure]
	public string ToString(CultureInfo? culture) => SR.CHR_AtPosition(culture, Line + 1, Column + 1, Position);

	/// <inheritdoc />
	public override string ToString() => ToString(null);

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is CharPosition position && this == position;

	/// <inheritdoc />
	public bool Equals(CharPosition other) => this == other;

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(Position, Line, Column);

	public static bool operator ==(CharPosition left, CharPosition right) => left.Position == right.Position && left.Line == right.Line && left.Column == right.Column;

	public static bool operator !=(CharPosition left, CharPosition right) => left.Position != right.Position || left.Line != right.Line || left.Column != right.Column;

	public CharPosition Next(ReadOnlySpan<char> text, int offset, int tabSize)
	{
		if (offset <= 0)
			return this;

		int start = Position;
		int end = start + offset;
		if (end > text.Length)
			end = text.Length;
		var lc = OffsetBox(text.Slice(start, end - start), tabSize);
		return new CharPosition(end, lc.Line + Line, lc.Line == 0 ? lc.Column + Column: lc.Column);
	}

	private static (int Line, int Column) OffsetBox(ReadOnlySpan<char> part, int tabSize)
	{
		int line = 0;
		var newLineTokens = new ReadOnlySpan<char>(CrLf);
		int i = part.IndexOfAny(newLineTokens);
		if (i >= 0)
		{
			do
			{
				++line;
				part = part.Slice(i + NewLineLen(part, i));
			} while ((i = part.IndexOfAny(newLineTokens)) >= 0);
		}

		if (tabSize <= 1)
			return (line, part.Length);

		int column = 0;
		while ((i = part.IndexOf(TAB)) >= 0)
		{
			var position = column + i;
			column = position - position % tabSize + tabSize;
			part = part.Slice(i + 1);
		}
		return (line, column + part.Length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int NewLineLen(ReadOnlySpan<char> part, int index) => part[index] == CR && index + 1 < part.Length && part[index + 1] == LF ? 2 : 1;
	}
	private const char CR = '\r';
	private const char LF = '\n';
	private const char TAB = '\t';
	private static readonly char[] CrLf = [LF, CR];
}
