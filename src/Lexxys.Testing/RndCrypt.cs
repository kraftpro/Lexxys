using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lexxys.Testing;

/// <summary>
/// Implementation of <see cref="IRand"/> using cryptographic random number generator.  Doesn't support <see cref="Reset(long)"/> method.
/// </summary>
public class RndCrypt: IRand
{
	private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

	/// <inheritdoc/>
#if NET
	public void NextBytes(Span<byte> buffer) => _generator.GetBytes(buffer);
#else
	public void NextBytes(Span<byte> buffer)
	{
		var bb = ArrayPool<byte>.Shared.Rent(buffer.Length);
		_generator.GetBytes(bb);
		bb.AsSpan().CopyTo(buffer);
		ArrayPool<byte>.Shared.Return(bb);
	}
#endif

	/// <inheritdoc/>
	public int NextInt32() => (int)NextUInt32() & ~Int32.MinValue;

	public long NextInt64() => (long)NextUInt64() & ~Int64.MinValue;

	public uint NextUInt32()
	{
#if NET
		Span<uint> val = stackalloc uint[1];
		_generator.GetBytes(MemoryMarshal.AsBytes(val));
		return val[0];
#else
		var val = ArrayPool<byte>.Shared.Rent(sizeof(uint));
		_generator.GetBytes(val);
		var result = MemoryMarshal.Read<uint>(val);
		ArrayPool<byte>.Shared.Return(val);
		return result;
#endif
	}

	public ulong NextUInt64()
	{
#if NET
		Span<ulong> val = stackalloc ulong[1];
		_generator.GetBytes(MemoryMarshal.AsBytes(val));
		return val[0];
#else
		var val = ArrayPool<byte>.Shared.Rent(sizeof(ulong));
		_generator.GetBytes(val);
		var result = MemoryMarshal.Read<ulong>(val);
		ArrayPool<byte>.Shared.Return(val);
		return result;
#endif
	}


	/// <inheritdoc/>
	public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1ul << 53));

	/// <summary>
	/// Throws <see cref="NotSupportedException"/> exception if <paramref name="seed"/> is greater than zero.
	/// </summary>
	/// <exception cref="NotSupportedException">The method is not supported</exception>
	public void Reset(long seed = 0)
	{
		if (seed != 0)
			throw new NotSupportedException($"Method {nameof(Reset)} is not supported by {nameof(RndCrypt)}.");
	}
}
