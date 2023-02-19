// Lexxys Infrastructural library.
// file: CharPosition.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Represents a position in a text.
	/// </summary>
	public readonly struct CharPosition: IEquatable<CharPosition>
	{
		public static readonly CharPosition Start;

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
		public string ToString(CultureInfo? culture)
		{
			return SR.CHR_AtPosition(culture, Line + 1, Column + 1, Position);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return ToString(null);
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			return obj is CharPosition position && this == position;
		}

		/// <inheritdoc />
		public bool Equals(CharPosition other)
		{
			return this == other;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return HashCode.Join(Position.GetHashCode(), Line.GetHashCode(), Column.GetHashCode());
		}

		public static bool operator ==(CharPosition left, CharPosition right)
		{
			return left.Position == right.Position && left.Line == right.Line && left.Column == right.Column;
		}

		public static bool operator !=(CharPosition left, CharPosition right)
		{
			return left.Position != right.Position || left.Line != right.Line || left.Column != right.Column;
		}
	}
}


