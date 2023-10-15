#if NETCOREAPP

using System.Buffers;
using System.Text;

namespace Lexxys;

public static class BufferWriterExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write(this IBufferWriter<byte> writer, byte value)
	{
		writer.GetSpan(1)[0] = value;
		writer.Advance(1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write(this IBufferWriter<byte> writer, byte? value)
	{
		if (value == null)
		{
			writer.GetSpan(1)[0] = 0;
			writer.Advance(1);
			return;
		}
		var span = writer.GetSpan(2);
		span[0] = 1;
		span[1] = value.GetValueOrDefault();
		writer.Advance(2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write(this IBufferWriter<byte> writer, string? value)
	{
		if (value == null)
		{
			writer.Write((byte)0);
			return;
		}
		var len = (uint)Encoding.UTF8.GetByteCount(value.AsSpan());
		writer.WritePacked((uint)len + 1);
		Encoding.UTF8.GetBytes(value, writer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WritePacked(this IBufferWriter<byte> writer, ulong value)
	{
		var span = writer.GetSpan(sizeof(uint) + 1);
		int i = 0;
		while (value > 0x7F)
		{
			span[i++] = (byte)(value | 0x80);
			value >>= 7;
		}
		span[i++] = (byte)value;
		writer.Advance(i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WritePacked(this IBufferWriter<byte> writer, long value)
	{
		WritePacked(writer, value < 0 ? ((ulong)-value << 1) + 1 : (ulong)value << 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WritePacked(this IBufferWriter<byte> writer, uint value)
	{
		var span = writer.GetSpan(sizeof(uint) + 1);
		int i = 0;
		while (value > 0x7F)
		{
			span[i++] = (byte)(value | 0x80);
			value >>= 7;
		}
		span[i++] = (byte)value;
		writer.Advance(i);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WritePacked(this IBufferWriter<byte> writer, int value)
	{
		WritePacked(writer, value < 0 ? ((uint)-value << 1) + 1: (uint)value << 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write<T>(this IBufferWriter<byte> writer, scoped in T value) where T : unmanaged
	{
		int size = Unsafe.SizeOf<T>();
		var span = writer.GetSpan(size);
		Unsafe.WriteUnaligned(ref span.GetPinnableReference(), value);
		writer.Advance(size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write<T>(this IBufferWriter<byte> writer, scoped in T? value) where T: unmanaged
	{
		if (value == null)
		{
			writer.GetSpan(1)[0] = 0;
			writer.Advance(1);
			return;
		}
		int size = Unsafe.SizeOf<T>() + 1;
		var span = writer.GetSpan(size);
		span[0] = 1;
		Unsafe.WriteUnaligned(ref span.Slice(1).GetPinnableReference(), value.GetValueOrDefault());
		writer.Advance(size);
	}
}

#endif
