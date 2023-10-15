using System.Runtime.InteropServices;
using System.Text;

namespace Lexxys;

public static class ReadOnlySpanByteExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ReadByte(this scoped ref ReadOnlySpan<byte> span)
	{
		var x = span[0];
		span = span.Slice(1);
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte? ReadNullableByte(this scoped ref ReadOnlySpan<byte> span) => span.ReadByte() == 0 ? null: span.ReadByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Read<T>(this scoped ref ReadOnlySpan<byte> span) where T : unmanaged
	{
		if (span.Length < Unsafe.SizeOf<T>())
			throw new ArgumentOutOfRangeException(nameof(span), span.Length, $"Expected at least {Unsafe.SizeOf<T>()} bytes.");
		var value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
		span = span.Slice(Unsafe.SizeOf<T>());
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? ReadNullable<T>(this scoped ref ReadOnlySpan<byte> span) where T : unmanaged => span.ReadByte() == 0 ? null: span.Read<T>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe string? ReadString(this scoped ref ReadOnlySpan<byte> span)
	{
		var length = (int)span.ReadPackedUInt() - 1;
		if (length < 0)
			return null;
#if NET5_0_OR_GREATER
		var value = Encoding.UTF8.GetString(span.Slice(0, length));
		span = span.Slice(length);
		return value;
#else
		string value;
		fixed (byte* p = &span.GetPinnableReference())
		{
			value = Encoding.UTF8.GetString(p, length);
		}
		span = span.Slice(length);
		return value;
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ReadPackedUInt(this scoped ref ReadOnlySpan<byte> span)
	{
		uint value = 0;
		int i = 0;
		int h = 0;
		while (span[i] > 0x7F)
		{
			var b = (uint)span[i++] & 0x7F;
			value |= b << h;
			h += 7;
		}
		value |= (uint)span[i++] << h;
		span = span.Slice(i);
        return value;
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadPackedInt(this scoped ref ReadOnlySpan<byte> span)
	{
		uint value = span.ReadPackedUInt();
        return (value & 1) == 1 ? -(int)(value >> 1): (int)(value >> 1);
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ReadPackedULong(this scoped ref ReadOnlySpan<byte> span)
	{
		ulong value = 0;
		int i = 0;
		int h = 0;
		while (span[i] > 0x7F)
		{
			var b = (ulong)span[i++] & 0x7F;
			value |= b << h;
			h += 7;
		}
		value |= (ulong)span[i++] << h;
		span = span.Slice(i);
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadPackedLong(this scoped ref ReadOnlySpan<byte> span)
	{
		ulong value = span.ReadPackedULong();
		return (value & 1) == 1 ? -(long)(value >> 1) : (long)(value >> 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> Read<T>(this scoped ref ReadOnlySpan<byte> span, out T value) where T : unmanaged
	{
		if (span.Length < Unsafe.SizeOf<T>())
			throw new ArgumentOutOfRangeException(nameof(span), span.Length, $"Expected at least {Unsafe.SizeOf<T>()} bytes.");
		value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
		span = span.Slice(Unsafe.SizeOf<T>());
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> Read<T>(this scoped ref ReadOnlySpan<byte> span, out T? value) where T : unmanaged
	{
		if (span.ReadByte() == 0)
		{
			value = null;
			return span;
		}
		if (span.Length < Unsafe.SizeOf<T>())
			throw new ArgumentOutOfRangeException(nameof(span), span.Length, $"Expected at least {Unsafe.SizeOf<T>()} bytes.");
		value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
		span = span.Slice(Unsafe.SizeOf<T>());
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ReadPacked(this scoped ref ReadOnlySpan<byte> span, out ulong value)
	{
		value = span.ReadPackedULong();
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ReadPacked(this scoped ref ReadOnlySpan<byte> span, out long value)
	{
		value = span.ReadPackedLong();
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ReadPacked(this scoped ref ReadOnlySpan<byte> span, out uint value)
	{
		value = span.ReadPackedUInt();
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> ReadPacked(this scoped ref ReadOnlySpan<byte> span, out int value)
	{
		value = span.ReadPackedInt();
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> Read(this scoped ref ReadOnlySpan<byte> span, out string? value)
	{
		value = span.ReadString();
		return span;
	}
}