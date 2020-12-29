// Lexxys Infrastructural library.
// file: LexicalToken.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys.Tokenizer2
{
	/// <summary>
	/// Represents a lexical token produced by TokenScanner.
	/// </summary>
	public readonly struct LexicalTokenT: IEquatable<LexicalTokenT>
	{
		/// <summary>
		/// Creates a new <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="tokenType">Type of the token</param>
		/// <param name="text">Textual value of the token</param>
		/// <param name="position">Position of the token in the text</param>
		/// <param name="culture">The culture of the token</param>
		public LexicalTokenT(LexicalTokenType tokenType, int position, int length, CultureInfo culture = default)
		{
			TokenType = tokenType;
			Position = position;
			Length = length;
			Value = default;
			Culture = culture ?? CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Creates a new <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="tokenType">Type of the token</param>
		/// <param name="text">Textual value of the token</param>
		/// <param name="position">Position of the token in the text</param>
		/// <param name="value">Value of the token.</param>
		/// <param name="culture">The culture of the token</param>
		public LexicalTokenT(LexicalTokenType tokenType, int position, int length, object value, CultureInfo culture = default)
		{
			TokenType = tokenType;
			Position = position;
			Length = length;
			Value = value;
			Culture = culture ?? CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Gets type of the token.
		/// </summary>
		public readonly LexicalTokenType TokenType;

		/// <summary>
		/// Gets position of the token in the parsed text.
		/// </summary>
		public readonly int Position;

		public readonly int Length;

		public ReadOnlySpan<T> Text<T>(ReadOnlySpan<T> value)
			=> value.Slice(Position, Length);

		public ReadOnlySpan<T> Text<T>(CharStream<T> value) where T: IEquatable<T>
			=> value.Slice(Position, Length);

		/// <summary>
		/// The token's culture.
		/// </summary>
		public readonly CultureInfo Culture;

		/// <summary>
		/// The token's value.
		/// </summary>
		public readonly object Value;

		/// <summary>
		/// The group value of the token type.
		/// </summary>
		public short Group => TokenType.Group;

		/// <summary>
		/// The item value of the token type.
		/// </summary>
		public short Item => TokenType.Item;

		public bool IsEmpty => TokenType == default && Position == default && Length == default && Value is null;

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other) => TokenType.Is(other);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and the specified <paramref name="itemId"/>.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The item ID to test.</param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other, int itemId) => TokenType.Is(other, itemId);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other, int itemId, int itemId2) => TokenType.Is(other, itemId, itemId2);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <param name="itemId3">The third item ID to test.</param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3) => TokenType.Is(other, itemId, itemId2, itemId3);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <param name="itemId3">The third item ID to test.</param>
		/// <param name="itemId4">The forth item ID to test.</param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3, int itemId4) => TokenType.Is(other, itemId, itemId2, itemId3, itemId4);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="items">Collection of the items IDs to test.</param>
		/// <returns></returns>
		public bool Is(LexicalTokenType other, params int[] items) => TokenType.Is(other, items);

		/// <inheritdoc />
		public override string ToString()
		{
			return String.Format(Culture, "{0}, {1},{2}: {3}", TokenType, Position, Length, Value ?? "(null)");
		}

		public override int GetHashCode()
		{
			return HashCode.Join(TokenType.GetHashCode(), Position, Length, Value?.GetHashCode() ?? 0, Culture.GetHashCode());
		}

		public override bool Equals(object obj) => obj is LexicalTokenT token ? Equals(token) : false;

		public bool Equals(LexicalTokenT other)
		{
			return TokenType == other.TokenType && Position == other.Position && Length == other.Length && Object.Equals(Value, other.Value) && Culture == other.Culture;
		}

		public static bool operator ==(LexicalTokenT left, LexicalTokenT right) => left.Equals(right);
		
		public static bool operator !=(LexicalTokenT left, LexicalTokenT right) => !left.Equals(right);
	}
}
