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

namespace Lexxys.Tokenizer
{
	/// <summary>
	/// The type of the lexical token
	/// </summary>
	public readonly struct LexicalTokenType: IEquatable<LexicalTokenType>
	{
		#pragma warning disable CA1720 // Identifier contains type name
		public static readonly LexicalTokenType EMPTY		= new LexicalTokenType(0, 0, "empty");
		public static readonly LexicalTokenType BOF			= new LexicalTokenType(0, 1, "bof");
		public static readonly LexicalTokenType EOF			= new LexicalTokenType(0, 2, "eof");
		public static readonly LexicalTokenType NUMERIC		= new LexicalTokenType(1, 0, "number");
		public static readonly LexicalTokenType IDENTIFIER	= new LexicalTokenType(2, 0, "identifier");
		public static readonly LexicalTokenType STRING		= new LexicalTokenType(3, 0, "string");
		public static readonly LexicalTokenType SEQUENCE	= new LexicalTokenType(4, 0, "sequence");
		public static readonly LexicalTokenType COMMENT		= new LexicalTokenType(5, 0, "comment");
		public static readonly LexicalTokenType INDENT		= new LexicalTokenType(6, 0, "indent");
		public static readonly LexicalTokenType UNDENT		= new LexicalTokenType(6, 1, "undent");
		public static readonly LexicalTokenType WHITESPACE	= new LexicalTokenType(7, 0, "space");
		public static readonly LexicalTokenType CHAR		= new LexicalTokenType(8, 0, "char");
		public static readonly LexicalTokenType PAIR		= new LexicalTokenType(9, 0, "pair");
		public static readonly LexicalTokenType KEYWORD		= new LexicalTokenType(10, 0, "keyword");
		public static readonly LexicalTokenType IGNORE		= new LexicalTokenType(11, 0, "ignore");
		public static readonly LexicalTokenType NEWLINE		= new LexicalTokenType(12, 0, "newline");

		/// <summary>
		/// Creates a new type of the <see cref="LexicalToken"/>.
		/// </summary>
		/// <param name="group">Group Id.</param>
		/// <param name="item">Item Id.</param>
		/// <param name="name">Type name.</param>
		public LexicalTokenType(short group, short item, string name)
		{
			Group = group;
			Item = item;
			Name = name;
		}

		/// <summary>
		/// Creates a new type of the <see cref="LexicalToken"/> with zero item Id.
		/// </summary>
		/// <param name="group">Group Id.</param>
		/// <param name="name">Type name.</param>
		public LexicalTokenType(short group, string name)
			: this(group, 0, name)
		{
		}

		/// <summary>
		/// Gets combyned group an item ID.
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

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[Pure]
		public bool Is(LexicalTokenType other)
		{
			return Group == other.Group;
		}

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one and the specified <paramref name="itemId"/>.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The item ID to test.</param>
		/// <returns></returns>
		[Pure]
		public bool Is(LexicalTokenType other, int itemId)
		{
			return Group == other.Group && Item == itemId;
		}

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <returns></returns>
		[Pure]
		public bool Is(LexicalTokenType other, int itemId, int itemId2)
		{
			return Group == other.Group && (Item == itemId || Item == itemId2);
		}

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="itemId">The first item ID to test.</param>
		/// <param name="itemId2">The second item ID to test.</param>
		/// <param name="itemId3">The third item ID to test.</param>
		/// <returns></returns>
		[Pure]
		public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3)
		{
			return Group == other.Group && (Item == itemId || Item == itemId2 || Item == itemId3);
		}

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
		public bool Is(LexicalTokenType other, int itemId, int itemId2, int itemId3, int itemId4)
		{
			return Group == other.Group && (Item == itemId || Item == itemId2 || Item == itemId3 || Item == itemId4);
		}

		/// <summary>
		/// Tests if this type has the same group ID as the <paramref name="other"/> one and one of the specified items IDs.
		/// </summary>
		/// <param name="other"><see cref="LexicalTokenType"/> to test group ID.</param>
		/// <param name="items">Collection of the items IDs to test.</param>
		/// <returns></returns>
		[Pure]
		public bool Is(LexicalTokenType other, params int[] items)
		{
			if (Group == other.Group)
			{
				if (items == null)
					return false;
				for (int i = 0; i < items.Length; i++)
				{
					if (Item == items[i])
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Creates a new <see cref="LexicalTokenType"/> with the specified item ID value.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		[Pure]
		public LexicalTokenType WithItem(int item)
		{
			return new LexicalTokenType(Group, (short)item, Name);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return String.Format(CultureInfo.InvariantCulture, "{1}:{2} ({0})", Name, Group, Item);
		}

		public static bool operator==(LexicalTokenType left, LexicalTokenType right)
		{
			return left.Group == right.Group && left.Item == right.Item;
		}

		public static bool operator!=(LexicalTokenType left, LexicalTokenType right)
		{
			return left.Group != right.Group || left.Item != right.Item;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return 320581 + HashCode.Join(Group, Item);
		}

		/// <inheritdoc />
		public override bool Equals(object? obj)
		{
			return obj is LexicalTokenType ltt && this == ltt;
		}

		public bool Equals(LexicalTokenType other) => this == other;
	}
}
