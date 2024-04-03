using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lexxys.Testing;

/// <summary>
/// Implementation of <see cref="IRand"/> using cryptographic random number generator.  Doesn't support <see cref="Reset(long)"/> method.
/// </summary>
public class RndCrypt: IRand
{
	private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
	private const long SignificantValue = long.MaxValue >> 13;
	private const double ToDoubleMult = (double)(SignificantValue - 1) / SignificantValue / long.MaxValue;

	/// <inheritdoc/>
	public void NextBytes(byte[] buffer) => _generator.GetBytes(buffer);

	/// <inheritdoc/>
	public int NextInt32()
	{
		return (int)NextUInt32() & ~Int32.MinValue;;
	}

	public long NextInt64()
	{
		return (long)NextUInt64() & ~Int64.MinValue;;
	}

	public uint NextUInt32()
	{
#if NET6_0_OR_GREATER
		Span<uint> val = stackalloc uint[1];
		_generator.GetBytes(MemoryMarshal.AsBytes(val));
		return val[0];
#else
		var val = new byte[sizeof(uint)];
		_generator.GetBytes(val);
		return MemoryMarshal.Read<uint>(val);
#endif
	}

	public ulong NextUInt64()
	{
#if NET6_0_OR_GREATER
		Span<ulong> val = stackalloc ulong[1];
		_generator.GetBytes(MemoryMarshal.AsBytes(val));
		return val[0];
#else
		var val = new byte[sizeof(ulong)];
		_generator.GetBytes(val);
		return MemoryMarshal.Read<ulong>(val);
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
