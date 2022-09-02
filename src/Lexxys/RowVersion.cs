// Lexxys Infrastructural library.
// file: RowVersion.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Globalization;

namespace Lexxys
{
	public readonly struct RowVersion: IEquatable<RowVersion>, IComparable<RowVersion>, IComparable
	{
		public RowVersion(long value)
		{
			Value = value;
		}

		public RowVersion(byte[] bits)
		{
			if (bits is null)
				throw new ArgumentNullException(nameof(bits));
			Value = PackRowVersion(bits);
		}

		public long Value { get; }

		public byte[] ToByteArray() => UnPackRowVersion(Value);

		public static RowVersion FromByteArray(byte[] value) => new RowVersion(value ?? throw new ArgumentNullException(nameof(value)));

		public static explicit operator byte[](RowVersion value) => value.ToByteArray();

		public static explicit operator RowVersion(byte[] value) => FromByteArray(value);

		public long ToInt64() => Value;

		public static RowVersion FromInt64(long value) => new RowVersion(value);

		public static explicit operator long(RowVersion value) => value.Value;

		public static explicit operator RowVersion(long value) => new RowVersion(value);

		private static unsafe long PackRowVersion(byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (value.Length != sizeof(long))
				#pragma warning disable CA2208 // Instantiate argument exceptions correctly
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

		public bool Equals(RowVersion other) => Value == other.Value;

		public override bool Equals(object? obj) => obj is RowVersion version && Equals(version);

		public int CompareTo(RowVersion other) => Value.CompareTo(other.Value);

		public int CompareTo(object? obj) => obj is RowVersion version ? CompareTo(version) : 2;

		public override int GetHashCode() => Value.GetHashCode();

		public override string ToString() => Value.ToString("x", CultureInfo.InvariantCulture);

		public static bool operator ==(RowVersion left, RowVersion right) => left.Value == right.Value;

		public static bool operator !=(RowVersion left, RowVersion right) => left.Value != right.Value;

		public static bool operator <(RowVersion left, RowVersion right) => left.Value < right.Value;

		public static bool operator >(RowVersion left, RowVersion right) => left.Value > right.Value;

		public static bool operator <=(RowVersion left, RowVersion right) => left.Value <= right.Value;

		public static bool operator >=(RowVersion left, RowVersion right) => left.Value >= right.Value;
	}
}


