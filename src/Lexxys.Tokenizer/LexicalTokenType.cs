// Lexxys Infrastructural library.
// file: LexicalTokenType.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;

namespace Lexxys.Tokenizer;

/// <summary>
/// Represents a type of lexical token used in tokenization or lexical analysis processes.
/// </summary>
/// <remarks>
/// Use the predefined static instances, such as <see cref="LexicalTokenType.NUMERIC"/>, <see
/// cref="LexicalTokenType.IDENTIFIER"/>, and others, to identify common token types. Custom token types can be created
/// using the <see cref="Create(short, short, string)"/> method. Instances of this class are immutable and can be compared by group and item
/// identifiers to determine token category relationships.
/// </remarks>
[Serializable]
public class LexicalTokenType
{
	private static readonly ConcurrentDictionary<(short, short), LexicalTokenType> _lexicalTokenTypes = [];

	public static readonly LexicalTokenType EMPTY		= LexicalTokenType.Create(0, 0, "empty");
	public static readonly LexicalTokenType EOF			= LexicalTokenType.Create(0, 1, "eof");
	public static readonly LexicalTokenType ERROR		= LexicalTokenType.Create(0, 3, "error");
	public static readonly LexicalTokenType NUMERIC		= LexicalTokenType.Create(1, 0, "number");
	public static readonly LexicalTokenType IDENTIFIER	= LexicalTokenType.Create(2, 0, "identifier");
	public static readonly LexicalTokenType STRING		= LexicalTokenType.Create(3, 0, "string");
	public static readonly LexicalTokenType SEQUENCE	= LexicalTokenType.Create(4, 0, "sequence");
	public static readonly LexicalTokenType COMMENT		= LexicalTokenType.Create(5, 0, "comment");
	public static readonly LexicalTokenType INDENT		= LexicalTokenType.Create(6, 0, "indent");
	public static readonly LexicalTokenType UNDENT		= LexicalTokenType.Create(6, 1, "undent");
	public static readonly LexicalTokenType WHITESPACE	= LexicalTokenType.Create(7, 0, "space");
	public static readonly LexicalTokenType CHAR		= LexicalTokenType.Create(8, 0, "char");
	public static readonly LexicalTokenType PAIR		= LexicalTokenType.Create(9, 0, "pair");
	public static readonly LexicalTokenType KEYWORD		= LexicalTokenType.Create(10, 0, "keyword");
	public static readonly LexicalTokenType IGNORE		= LexicalTokenType.Create(11, 0, "ignore");
	public static readonly LexicalTokenType NEWLINE		= LexicalTokenType.Create(12, 0, "newline");
	public static readonly LexicalTokenType NEWLINE2	= LexicalTokenType.Create(NEWLINE, 1);

	/// <summary>
	/// Initializes a new instance of the LexicalTokenType class with the specified group, item, and name values.
	/// </summary>
	/// <param name="group">The group identifier for the lexical token type.</param>
	/// <param name="item">The item identifier within the group for the lexical token type.</param>
	/// <param name="name">The display name or description of the lexical token type.</param>
	private LexicalTokenType(short group, short item, string name)
	{
		Group = group;
		Item = item;
		Name = name;
	}


	/// <summary>
	/// Gets the unique identifier that combines the group and item values into a single integer.
	/// </summary>
	/// <remarks>The identifier is constructed by shifting the group value 16 bits to the left and combining it with
	/// the item value. This allows both values to be represented in a single 32-bit integer.</remarks>
	public int Id => Group << 16 | (ushort)Item;

	/// <summary>
	/// Gets the group identifier associated with the current token type.
	/// </summary>
	public short Group { get; }

	/// <summary>
	/// Gets the value of the item identifier within the group for the current token type.
	/// </summary>
	public short Item { get; }

	/// <summary>
	/// Gets the name associated with the current token type.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets a value indicating whether the current instance has no associated group.
	/// </summary>
	public bool IsEmpty => Group == 0;

	/// <summary>
	/// Retrieves an existing lexical token type for the specified group and item, or creates a new one if it does not
	/// exist.
	/// </summary>
	/// <remarks>
	/// This method ensures that only one instance of a lexical token type exists for each unique
	/// combination of group and item. Subsequent calls with the same group and item values will return the same instance,
	/// regardless of the name provided.
	/// </remarks>
	/// <param name="group">The group identifier for the lexical token type.</param>
	/// <param name="item">The item identifier within the specified group for the lexical token type.</param>
	/// <param name="name">The display name to assign if a new lexical token type is created. This value is ignored if the token type already exists.</param>
	/// <returns>A <see cref="LexicalTokenType"/> instance corresponding to the specified group and item. If a matching token type
	/// already exists, it is returned; otherwise, a new instance is created and returned.</returns>
	public static LexicalTokenType Create(short group, short item, string name)
		=> _lexicalTokenTypes.TryGetValue((group, item), out var type) ? type: _lexicalTokenTypes.GetOrAdd((group, item), new LexicalTokenType(group, item, name));

	public static LexicalTokenType Create(LexicalTokenType token, short item)
		=> _lexicalTokenTypes.TryGetValue((token.Group, item), out var type) ? type : _lexicalTokenTypes.GetOrAdd((token.Group, item), new LexicalTokenType(token.Group, item, token.Name));

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other)
		=> Group == (other ?? throw new ArgumentNullException(nameof(other))).Group;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other1, LexicalTokenType other2)
		=>	Group == (other1 ?? throw new ArgumentNullException(nameof(other1))).Group ||
			Group == (other2 ?? throw new ArgumentNullException(nameof(other2))).Group;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other1, LexicalTokenType other2, LexicalTokenType other3)
		=>	Group == (other1 ?? throw new ArgumentNullException(nameof(other1))).Group ||
			Group == (other2 ?? throw new ArgumentNullException(nameof(other2))).Group ||
			Group == (other3 ?? throw new ArgumentNullException(nameof(other3))).Group;

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one and the specified <paramref name="itemId"/>.
	/// </summary>
	/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
	/// <param name="itemId">The item ID to test.</param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other, int itemId)
		=> Group == (other ?? throw new ArgumentNullException(nameof(other))).Group && Item == itemId;

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
	/// </summary>
	/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
	/// <param name="itemId">The first item ID to test.</param>
	/// <param name="itemId2">The second item ID to test.</param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other, int itemId, int itemId2)
		=> Group == (other ?? throw new ArgumentNullException(nameof(other))).Group && (Item == itemId || Item == itemId2);

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
	/// </summary>
	/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
	/// <param name="itemId">The first item ID to test.</param>
	/// <param name="itemId2">The second item ID to test.</param>
	/// <param name="itemId3">The third item ID to test.</param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3)
		=> Group == (other ?? throw new ArgumentNullException(nameof(other))).Group && (Item == itemId || Item == itemId2 || Item == itemId3);

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
	/// </summary>
	/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
	/// <param name="itemId">The first item ID to test.</param>
	/// <param name="itemId2">The second item ID to test.</param>
	/// <param name="itemId3">The third item ID to test.</param>
	/// <param name="itemId4">The forth item ID to test.</param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3, int itemId4)
		=> Group == (other ?? throw new ArgumentNullException(nameof(other))).Group && (Item == itemId || Item == itemId2 || Item == itemId3 || Item == itemId4);

	/// <summary>
	/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
	/// </summary>
	/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
	/// <param name="items">Collection of the items IDs to test.</param>
	/// <returns></returns>
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Is(LexicalTokenType other, params int[] items)
	{
		if (other is null)
			throw new ArgumentNullException(nameof(other));
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		if (Group != other.Group)
			return false;
		for (int i = 0; i < items.Length; ++i)
		{
			if (Item == items[i])
				return true;
		}
		return false;
	}

	/// <summary>
	/// Creates a new <see cref="LexicalTokenType"/> with the specified item ID value.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	[Pure]
	public LexicalTokenType WithItem(int item) => LexicalTokenType.Create(Group, (short)item, Name);

	/// <inheritdoc />
	public override string ToString() => $"{Name}:{Group} ({Item})";
}
