// Lexxys Infrastructural library.
// file: LexicalToken.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Represents a lexical token produced by TokenScanner.
	/// </summary>
	public sealed class LexicalToken
	{
		public static readonly LexicalToken Empty = new LexicalToken(LexicalTokenType.EMPTY, "", 0);

		/// <summary>
		/// Creates a copy of the specified <paramref name="token"/>.
		/// </summary>
		/// <param name="token"></param>
		public LexicalToken(LexicalToken token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			TokenType = token.TokenType;
			Position = token.Position;
			Text = token.Text;
			CultureInfo = token.CultureInfo;
			Value = token.Value;
		}

		/// <summary>
		/// Creates a copy of the specified <paramref name="token"/> with a new object value.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="value"></param>
		public LexicalToken(LexicalToken token, object? value)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			TokenType = token.TokenType;
			Position = token.Position;
			Text = token.Text;
			CultureInfo = token.CultureInfo;
			Value = value;
		}

		/// <summary>
		/// Creates a new <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="tokenType">Type of the token</param>
		/// <param name="text">Textual value of the token</param>
		/// <param name="position">Position of the token in the text</param>
		/// <param name="cultureInfo">The culture of the token</param>
		public LexicalToken(LexicalTokenType tokenType, string text, int position, CultureInfo? cultureInfo = null)
		{
			Text = text ?? throw new ArgumentNullException(nameof(text));
			TokenType = tokenType;
			Position = position;
			Value = text;
			CultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
		}

		/// <summary>
		/// Creates a new <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="tokenType">Type of the token</param>
		/// <param name="text">Textual value of the token</param>
		/// <param name="position">Position of the token in the text</param>
		/// <param name="value">Value of the token.</param>
		/// <param name="cultureInfo">The culture of the token</param>
		public LexicalToken(LexicalTokenType tokenType, string text, int position, object? value, CultureInfo? cultureInfo = null)
		{
			Text = text ?? throw new ArgumentNullException(nameof(text));
			TokenType = tokenType;
			Position = position;
			Value = value;
			CultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
		}

		public bool IsEmpty => TokenType.Is(LexicalTokenType.EMPTY);

		public bool IsEof => TokenType == LexicalTokenType.EOF;

		/// <summary>
		/// Gets type of the token.
		/// </summary>
		public LexicalTokenType TokenType { get; }

		/// <summary>
		/// Gets textual value of the token.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets position of the token in the parsed text.
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// The token's culture.
		/// </summary>
		public CultureInfo CultureInfo { get; }

		/// <summary>
		/// The token's value.
		/// </summary>
		public object? Value { get; }

		/// <summary>
		/// The group value of the token type.
		/// </summary>
		public short Group => TokenType.Group;

		/// <summary>
		/// The item value of the token type.
		/// </summary>
		public short Item => TokenType.Item;

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

		public static LexicalToken Eof(int position) => new LexicalToken(LexicalTokenType.EOF, "", position);

		/// <inheritdoc />
		public override string ToString()
		{
			return String.Format(CultureInfo, "{0}, {1}: {2}", TokenType, Position.ToString(CultureInfo), Text == null ? "(null)" : Strings.Ellipsis(Strings.EscapeCsString(Text.Substring(0, Math.Min(Text.Length, 120))), 120, "…\""));
		}
	}
}


