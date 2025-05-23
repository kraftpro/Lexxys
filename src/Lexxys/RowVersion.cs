// Lexxys Infrastructural library.
// file: RowVersion.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
namespace Lexxys;

[Serializable]
public readonly struct RowVersion: IEquatable<RowVersion>, IComparable<RowVersion>, IComparable
{
	public RowVersion(ulong value)
	{
		Value = value;
	}

	public RowVersion(byte[] bits)
	{
		if (bits is null) throw new ArgumentNullException(nameof(bits));
		Value = PackRowVersion(bits);
	}

	public ulong Value { get; }

	public byte[] ToByteArray() => UnPackRowVersion(Value);

	public static RowVersion FromByteArray(byte[] value) => new RowVersion(value ?? throw new ArgumentNullException(nameof(value)));

	public static explicit operator byte[](RowVersion value) => value.ToByteArray();

	public static explicit operator RowVersion(byte[] value) => FromByteArray(value);

	public ulong ToInt64() => Value;

	public static RowVersion FromInt64(ulong value) => new RowVersion(value);

	public static explicit operator ulong(RowVersion value) => value.Value;

	public static explicit operator RowVersion(ulong value) => new RowVersion(value);

	private static ulong PackRowVersion(byte[] value)
	{
		if (value == null) throw new ArgumentNullException(nameof(value));
		if (value.Length != sizeof(ulong)) throw new ArgumentOutOfRangeException(nameof(value) + ".Length", value.Length, null);

		var v = Unsafe.ReadUnaligned<ulong>(ref value[0]);
		return Swap64(v);
	}

	private static byte[] UnPackRowVersion(ulong value)
	{
		var v = Swap64(value);
		return BitConverter.GetBytes(v);
	}

	private static ulong Swap64(ulong value)
	{
		if (!BitConverter.IsLittleEndian) return value;

		ulong x = value << 32 | value >> 32;
		x = (x & 0xFFFF0000_FFFF0000) >> 16 | (x & 0x0000FFFF_0000FFFF) << 16;
		x = (x & 0xFF00FF00_FF00FF00) >> 8  | (x & 0x00FF00FF_00FF00FF) << 8;
		return x;
	}


	public bool Equals(RowVersion other) => Value == other.Value;

	public override bool Equals(object? obj) => obj is RowVersion version && Equals(version);

	public int CompareTo(RowVersion other) => Value.CompareTo(other.Value);

	public int CompareTo(object? obj) => obj is RowVersion version ? CompareTo(version): 2;

	public override int GetHashCode() => Value.GetHashCode();

	public override unsafe string ToString()
	{
		var v = Swap64(Value);
		return Strings.ToHexString(new ReadOnlySpan<byte>(&v, sizeof(ulong)), "0x");
	}

	public static bool operator ==(RowVersion left, RowVersion right) => left.Value == right.Value;

	public static bool operator !=(RowVersion left, RowVersion right) => left.Value != right.Value;

	public static bool operator <(RowVersion left, RowVersion right) => left.Value < right.Value;

	public static bool operator >(RowVersion left, RowVersion right) => left.Value > right.Value;

	public static bool operator <=(RowVersion left, RowVersion right) => left.Value <= right.Value;

	public static bool operator >=(RowVersion left, RowVersion right) => left.Value >= right.Value;
}