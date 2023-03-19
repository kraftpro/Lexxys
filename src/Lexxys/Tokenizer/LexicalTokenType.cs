// Lexxys Infrastructural library.
// file: LexicalTokenType.cs
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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// The type of the lexical token
	/// </summary>
	public class LexicalTokenType
	{
		private static readonly ConcurrentDictionary<(short, short), LexicalTokenType> _lexicalTokenTypes = new();

		public static readonly LexicalTokenType EMPTY		= LexicalTokenType.Create(0, 0, "empty");
		public static readonly LexicalTokenType EOF			= LexicalTokenType.Create(0, 1, "eof");
		public static readonly LexicalTokenType NUMERIC		= LexicalTokenType.Create(1, 0, "number");
		public static readonly LexicalTokenType IDENTIFIER	= LexicalTokenType.Create(2, 0, "identifier");
		public static readonly LexicalTokenType STRING		= LexicalTokenType.Create(3, 0, "string");
		public static readonly LexicalTokenType SEQUENCE	= LexicalTokenType.Create(4, 0, "sequence");
		public static readonly LexicalTokenType COMMENT		= LexicalTokenType.Create(5, 0, "comment");
		public static readonly LexicalTokenType INDENT		= LexicalTokenType.Create(6, 0, "indent");
		public static readonly LexicalTokenType UNDENT		= LexicalTokenType.Create(7, 1, "undent");
		public static readonly LexicalTokenType WHITESPACE	= LexicalTokenType.Create(7, 0, "space");
		public static readonly LexicalTokenType CHAR		= LexicalTokenType.Create(8, 0, "char");
		public static readonly LexicalTokenType PAIR		= LexicalTokenType.Create(9, 0, "pair");
		public static readonly LexicalTokenType KEYWORD		= LexicalTokenType.Create(10, 0, "keyword");
		public static readonly LexicalTokenType IGNORE		= LexicalTokenType.Create(11, 0, "ignore");
		public static readonly LexicalTokenType NEWLINE		= LexicalTokenType.Create(12, 0, "newline");
		public static readonly LexicalTokenType NEWLINE2	= LexicalTokenType.Create(12, 1, "newline");

		/// <summary>
		/// Creates a new type of the <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="group">Group Id.</param>
		/// <param name="item">Item Id.</param>
		/// <param name="name">Type name.</param>
		private LexicalTokenType(short group, short item, string name)
		{
			Group = group;
			Item = item;
			Name = name;
		}


		/// <summary>
		/// Gets combined group an item ID.
		/// </summary>
		public int Id => Group << 16 | (ushort)Item;
		/// <summary>
		/// Gets the group ID.
		/// </summary>
		public short Group { get; }

		/// <summary>
		/// Gets the item ID.
		/// </summary>
		public short Item { get; }

		/// <summary>
		/// Gets the type name.
		/// </summary>
		public string Name { get; }

		public bool IsEmpty => Group == 0 && Item == 0;

		public static LexicalTokenType Create(short group, short item, string name)
			=> _lexicalTokenTypes.TryGetValue((group, item), out var type) ? type : _lexicalTokenTypes.GetOrAdd((group, item), new LexicalTokenType(group, item, name));

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other)
			=> Group == other.Group;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other1, LexicalTokenType other2)
			=> Group == other1.Group || Group == other2.Group;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other1, LexicalTokenType other2, LexicalTokenType other3)
			=> Group == other1.Group || Group == other2.Group || Group == other3.Group;

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one and the specified <paramref name="itemId"/>.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The item ID to test.</param>
		/// <returns></returns>
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Is(LexicalTokenType other, int itemId)
			=> Group == other.Group && Item == itemId;

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
			=> Group == other.Group && (Item == itemId || Item == itemId2);

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
			=> Group == other.Group && (Item == itemId || Item == itemId2 || Item == itemId3);

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
			=> Group == other.Group && (Item == itemId || Item == itemId2 || Item == itemId3 || Item == itemId4);

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
}
