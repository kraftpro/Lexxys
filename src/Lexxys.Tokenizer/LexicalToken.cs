// Lexxys Infrastructural library.
// file: LexicalToken.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Tokenizer;

/// <summary>
/// Represents a lexical token identified during text parsing, including its type, position, length, and optional value
/// extraction logic.
/// </summary>
/// <remarks>
/// A LexicalToken encapsulates information about a segment of text recognized by a lexer, such as its
/// classification (token type), location within the source, and length. It may also provide access to the token's value
/// via a custom getter function. LexicalToken is an immutable value type and is typically used in lexical analysis or
/// tokenization scenarios. The struct provides utility members for checking token type, extracting the token's text,
/// and retrieving associated values. The static Empty field represents a default, empty token.
/// </remarks>
[Serializable]
public readonly struct LexicalToken
{
	public delegate object? Getter(LexicalToken token, ReadOnlySpan<char> buffer);
		
	public static readonly LexicalToken Empty = new();

	private readonly Getter? _getter;

	public LexicalToken()
	{
		TokenType = LexicalTokenType.EMPTY;
	}

	/// <summary>
	/// Initializes a new instance of the LexicalToken class with the specified token type, position, and length.
	/// </summary>
	/// <param name="type">The type of the lexical token to create.</param>
	/// <param name="position">The zero-based position in the source text where the token begins.</param>
	/// <param name="length">The length, in characters, of the token in the source text. Must be non-negative.</param>
	public LexicalToken(LexicalTokenType type, int position, int length)
	{
		TokenType = type;
		Position = position;
		Length = length;
	}

	/// <summary>
	/// Initializes a new instance of the LexicalToken class with the specified token type, position, length, and value
	/// getter.
	/// </summary>
	/// <param name="type">The type of the lexical token to create.</param>
	/// <param name="position">The zero-based position in the source text where the token begins.</param>
	/// <param name="length">The length, in characters, of the token in the source text. Must be non-negative.</param>
	/// <param name="getter">A delegate used to retrieve the value or text of the token. Cannot be null.</param>
	public LexicalToken(LexicalTokenType type, int position, int length, Getter getter)
	{
		TokenType = type;
		Position = position;
		Length = length;
		_getter = getter;
	}

	/// <summary>
	/// Gets the type of the lexical token represented by this instance.
	/// </summary>
	public LexicalTokenType TokenType { get; }

	/// <summary>
	/// Gets the position of the lexical token in the parsed text.
	/// </summary>
	public int Position { get; }

	/// <summary>
	/// Gets the number of characters in the lexical token.
	/// </summary>
	public int Length { get; }

	/// <summary>
	/// Gets the group identifier for this token type.
	/// </summary>
	public short Group => TokenType.Group;

	/// <summary>
	/// Gets the item identifier for this token type.
	/// </summary>
	public short Item => TokenType.Item;

	/// <summary>
	/// Gets a value indicating whether the current instance has no associated token type or represents an empty token type.
	/// </summary>
	public bool IsEmpty => TokenType?.IsEmpty ?? true;

	/// <summary>
	/// Gets a value indicating whether the current token represents the end of the input stream.
	/// </summary>
	public bool IsEof => TokenType?.Is(LexicalTokenType.EOF) ?? true;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public object? GetValue(ReadOnlySpan<char> buffer) => _getter is null ? GetSpan(buffer).ToString(): _getter(this, GetSpan(buffer));
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public object? GetValue(in CharStream stream) => _getter is null ? GetSpan(stream).ToString(): _getter(this, GetSpan(stream));
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetString(ReadOnlySpan<char> buffer) => _getter is null ? GetSpan(buffer).ToString(): _getter(this, GetSpan(buffer))?.ToString() ?? String.Empty;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetString(in CharStream stream) => _getter is null ? GetSpan(stream).ToString(): _getter(this, GetSpan(stream))?.ToString() ?? String.Empty;

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
}
