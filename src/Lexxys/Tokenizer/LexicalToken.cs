// Lexxys Infrastructural library.
// file: LexicalToken.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// Represents a lexical token produced by TokenScanner.
	/// </summary>
	public readonly struct LexicalToken
	{
		public delegate object? Getter(LexicalTokenType token, ReadOnlySpan<char> buffer);
        
		public static readonly LexicalToken Empty = new();

		private readonly Getter? _getter;

        public LexicalToken()
        {
			TokenType = LexicalTokenType.EMPTY;
        }

        /// <summary>
        /// Creates a new <see cref="LexicalToken"/>.
        /// </summary>
        /// <param name="type">Type of the token</param>
        /// <param name="position">Position of the token in the text</param>
        /// <param name="length">Length of the token</param>
        public LexicalToken(LexicalTokenType type, int position, int length)
        {
			TokenType = type;
            Position = position;
            Length = length;
        }

		/// <summary>
		/// Creates a new <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="type">Type of the token</param>
		/// <param name="position">Position of the token in the text</param>
		/// <param name="length">Length of the token</param>
		/// <param name="getter">Function to extract token value from the stream</param>
		public LexicalToken(LexicalTokenType type, int position, int length, Getter getter)
        {
			TokenType = type;
            Position = position;
            Length = length;
            _getter = getter;
        }

		/// <summary>
		/// Gets type of the token.
		/// </summary>
		public LexicalTokenType TokenType { get; }

        /// <summary>
        /// Gets position of the token in the parsed text.
        /// </summary>
        public int Position { get; }

        public int Length { get; }

		/// <summary>
		/// The group value of the token type.
		/// </summary>
		public short Group => TokenType.Group;

		/// <summary>
		/// The item value of the token type.
		/// </summary>
		public short Item => TokenType.Item;

		public bool IsEmpty => TokenType?.IsEmpty ?? true;

		public bool IsEof => TokenType?.Is(LexicalTokenType.EOF) ?? true;

		public bool HasValue => _getter != null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? GetValue(ReadOnlySpan<char> buffer) => _getter is null ? buffer.Slice(Position, Length).ToString(): _getter(TokenType, buffer);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? GetValue(in CharStream stream) => _getter is null ? stream.Chunk(Position, Length).ToString(): _getter(TokenType, stream.Chunk(0, stream.Capacity));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetString(ReadOnlySpan<char> buffer) => _getter is null ? buffer.Slice(Position, Length).ToString(): _getter(TokenType, buffer)?.ToString() ?? String.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetString(in CharStream stream) => _getter is null ? stream.Chunk(Position, Length).ToString(): _getter(TokenType, stream.Chunk(0, stream.Capacity))?.ToString() ?? String.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetSpan(ReadOnlySpan<char> buffer) => buffer.Slice(Position, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> GetSpan(in CharStream stream) => stream.Chunk(Position, Length);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other) => TokenType.Is(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, LexicalTokenType other2) => TokenType.Is(other, other2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, LexicalTokenType other2, LexicalTokenType other3) => TokenType.Is(other, other2, other3);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and the specified <paramref name="itemId"/>.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The item ID to test.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, int itemId) => TokenType.Is(other, itemId);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, int itemId, int itemId2) => TokenType.Is(other, itemId, itemId2);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <param name="itemId3">The third item ID to test.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3, int itemId4) => TokenType.Is(other, itemId, itemId2, itemId3, itemId4);

		/// <summary>
		/// Tests if type of this token has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="items">Collection of the items IDs to test.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, params int[] items) => TokenType.Is(other, items);

		/// <inheritdoc />
		public override string ToString() => $"{TokenType} ({Position}:{Length})";

		public static implicit operator bool(LexicalToken token) => !token.Is(LexicalTokenType.EMPTY);
	}
}
