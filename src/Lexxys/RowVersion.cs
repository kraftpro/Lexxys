// Lexxys Infrastructural library.
// file: RowVersion.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	public struct RowVersion: IEquatable<RowVersion>, IComparable<RowVersion>, IComparable
	{
		public readonly long Value;

		public RowVersion(long value)
		{
			Value = value;
		}

		public RowVersion(byte[] bits)
		{
			Value = PackRowVersion(bits);
		}

		public byte[] GetBits()
		{
			return UnPackRowVersion(Value);
		}

		public static explicit operator byte[](RowVersion value)
		{
			return UnPackRowVersion(value.Value);
		}

		public static explicit operator RowVersion(byte[] value)
		{
			return new RowVersion(value);
		}

		public static explicit operator long(RowVersion value)
		{
			return value.Value;
		}

		public static explicit operator RowVersion(long value)
		{
			return new RowVersion(value);
		}

		private static unsafe long PackRowVersion(byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (value.Length != sizeof(long))
				throw new ArgumentOutOfRangeException(nameof(value) + ".Length", value.Length, null);

			fixed (byte* p = value)
			{
				byte* q = stackalloc byte[sizeof(long)];
				q[0] = p[7];
				q[1] = p[6];
				q[2] = p[5];
				q[3] = p[4];
				q[4] = p[3];
				q[5] = p[2];
				q[6] = p[1];
				q[7] = p[0];
				return *(long*)q;
			}
		}

		private static unsafe byte[] UnPackRowVersion(long value)
		{
			byte[] bytes = new byte[sizeof(long)];
			fixed (byte* p = bytes)
			{
				byte* q = stackalloc byte[sizeof(long)];
				*(long*)q = value;
				p[0] = q[7];
				p[1] = q[6];
				p[2] = q[5];
				p[3] = q[4];
				p[4] = q[3];
				p[5] = q[2];
				p[6] = q[1];
				p[7] = q[0];
			}
			return bytes;
		}

		public bool Equals(RowVersion other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return obj is RowVersion version && Equals(version);
		}

		public int CompareTo(RowVersion other)
		{
			return Value.CompareTo(other.Value);
		}

		public int CompareTo(object obj)
		{
			return obj is RowVersion version ? CompareTo(version): 2;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public override string ToString()
		{
			return Value.ToString("x");
		}

		public static bool operator ==(RowVersion left, RowVersion right)
		{
			return left.Value == right.Value;
		}

		public static bool operator !=(RowVersion left, RowVersion right)
		{
			return left.Value != right.Value;
		}

		public static bool operator < (RowVersion left, RowVersion right)
		{
			return left.Value < right.Value;
		}

		public static bool operator >(RowVersion left, RowVersion right)
		{
			return left.Value > right.Value;
		}

		public static bool operator <=(RowVersion left, RowVersion right)
		{
			return left.Value <= right.Value;
		}

		public static bool operator >=(RowVersion left, RowVersion right)
		{
			return left.Value >= right.Value;
		}
	}
}


