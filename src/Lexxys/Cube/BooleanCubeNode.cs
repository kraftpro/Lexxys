// Lexxys Infrastructural library.
// file: BooleanCubeNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;

#nullable enable

namespace Lexxys.Cube
{
	[DebuggerDisplay("{Hex}")]
	public struct BooleanCubeNode: IEquatable<BooleanCubeNode>
	{
		internal uint Bits;
		internal uint Holes;

		internal BooleanCubeNode(uint bits)
		{
			Bits = bits;
			Holes = 0;
		}
		internal BooleanCubeNode(uint bits, uint holes)
		{
			Bits = bits;
			Holes = holes;
		}

		public bool this[int index]
		{
			get => IsBit(index);
			set => SetBit(index, value);
		}

		public bool IsBit(int index) => (Bits & (1u << index)) != 0;

		public void SetBit(int index, bool value)
		{
			if (value)
				SetBit(index);
			else
				ResetBit(index);
		}

		public void SetBit(int index) => Bits |= 1u << index;

		public void ResetBit(int index) => Bits &= ~(1u << index);

		public void ReverseBit(int index) => Bits ^= (1u << index);

		public bool IsHole(int index) => (Holes & (1u << index)) != 0;
		
		public void SetHole(int index, bool value)
		{
			if (value)
				SetHole(index);
			else
				ResetHole(index);
		}

		public void SetHole(int index) => Holes |= 1u << index;
		
		public void ResetHole(int index) => Holes &= ~(1u << index);

		public string Hex => String.Format(CultureInfo.InvariantCulture, "{0:X}; {1:X}", Bits, Holes);

		public void Normalize() => Bits &= ~Holes;

		public void Join(BooleanCubeNode other)
		{
			Holes |= other.Holes | (Bits ^ other.Bits);
			Bits = (Bits & other.Bits) | ~(Bits | other.Bits);
		}

		public static int MaxWidth => 32;

		public void AppendDisjunctant(StringBuilder sb, string delimiter, string[] arguments)
		{
			if (sb is null)
				throw EX.ArgumentNull(nameof(sb));
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));
			if (arguments.Length >= 32)
				throw EX.ArgumentOutOfRange("arguments.Length", arguments.Length);
			uint m = 1;
			for (int i = 0; i < arguments.Length; ++i)
			{
				if ((Holes & m) == 0)
				{
					sb.Append(delimiter);
					if ((Bits & m) == 0)
						sb.Append('~');
					sb.Append(arguments[i]);
					delimiter = "&";
				}
			}
		}

		public static BooleanCubeNode Add(BooleanCubeNode left, BooleanCubeNode right)
		{
			uint holes = left.Holes | right.Holes | (left.Bits ^ right.Bits);
			return new BooleanCubeNode(left.Bits & ~holes, holes);
		}

		public static BooleanCubeNode operator +(BooleanCubeNode left, BooleanCubeNode right)
		{
			uint holes = left.Holes | right.Holes | (left.Bits ^ right.Bits);
			return new BooleanCubeNode(left.Bits & ~holes, holes);
		}

		public static bool operator ==(BooleanCubeNode left, BooleanCubeNode right)
		{
			return left.Bits == right.Bits && left.Holes == right.Holes;
		}

		public static bool operator !=(BooleanCubeNode left, BooleanCubeNode right)
		{
			return left.Bits != right.Bits || left.Holes != right.Holes;
		}

		public override bool Equals(object? obj)
		{
			return obj is BooleanCubeNode node && Equals(node);
		}

		public bool Equals(BooleanCubeNode other)
		{
			return Bits == other.Bits && Holes == other.Holes;
		}

		public override int GetHashCode()
		{
			return HashCode.Join(Bits.GetHashCode(), Holes.GetHashCode());
		}
	}
}
