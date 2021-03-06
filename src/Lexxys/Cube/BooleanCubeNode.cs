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

namespace Lexxys.Cube
{
	[DebuggerDisplay("{Hex}")]
	public struct BooleanCubeNode
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
			get { return ((Bits & (1u << index)) != 0); }
			set
			{
				if (value)
					Bits |= 1u << index;
				else
					Bits &= ~(1u << index);
			}
		}

		public bool IsBit(int index)
		{
			return ((Bits & (1u << index)) != 0);
		}
		public void SetBit(int index, bool value)
		{
			if (value)
				Bits |= 1u << index;
			else
				Bits &= ~(1u << index);
		}
		public void SetBit(int index)
		{
			Bits |= 1u << index;
		}
		public void ResetBit(int index)
		{
			Bits &= ~(1u << index);
		}
		public void ReverseBit(int index)
		{
			Bits ^= (1u << index);
		}

		public bool IsHole(int index)
		{
			return ((Holes & (1u << index)) != 0);
		}
		public void SetHole(int index, bool value)
		{
			if (value)
				Bits |= 1u << index;
			else
				Bits &= ~(1u << index);
		}
		public void SetHole(int index)
		{
			Holes |= 1u << index;
		}
		public void ResetHole(int index)
		{
			Holes &= ~(1u << index);
		}

		public string Hex
		{
			get
			{
				return String.Format(CultureInfo.InvariantCulture, "{0:X}; {1:X}", Bits, Holes);
			}
		}

		public void Normalize()
		{
			Bits &= ~Holes;
		}

		public void Join(BooleanCubeNode other)
		{
			Holes |= other.Holes | (Bits ^ other.Bits);
			Bits = (Bits & other.Bits) | ~(Bits | other.Bits);
		}

		public static int MaxWidth
		{
			get { return 32; }
		}

		public void AppendDisjunctant(StringBuilder sb, string delimiter, string[] arguments)
		{
			if (sb == null)
				throw EX.ArgumentNull(nameof(sb));
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

		public override bool Equals(object obj)
		{
			return obj is BooleanCubeNode node && Bits == node.Bits && Holes == node.Holes;
		}

		public override int GetHashCode()
		{
			return HashCode.Join(Bits.GetHashCode(), Holes.GetHashCode());
		}
	}
}
