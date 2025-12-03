using System.Buffers;

namespace Lexxys.Testing;

/// <summary>
/// Implementation of <see cref="IRand"/> using regular pseudo-random number generator.
/// </summary>
public class RndSys: IRand
{
	private Random _r = new Random();

	/// <inheritdoc/>
	public void Reset(long seed = 0) => _r = seed == 0 ? new Random(): new Random((int)seed);

	/// <inheritdoc/>
	public int NextInt32() => _r.Next();

	/// <inheritdoc/>
#if NET
	public long NextInt64() => _r.NextInt64();
#else
	public long NextInt64() => ((long)_r.Next() << 31) | (long)_r.Next();
#endif

	/// <inheritdoc/>
	public double NextDouble() => _r.NextDouble();

	/// <inheritdoc/>
#if NET
	public void NextBytes(Span<byte> buffer) => _r.NextBytes(buffer);
#else
	public void NextBytes(Span<byte> buffer)
	{
		var bb = ArrayPool<byte>.Shared.Rent(buffer.Length);
		_r.NextBytes(bb);
		bb.AsSpan().CopyTo(buffer);
		ArrayPool<byte>.Shared.Return(bb);
	}
#endif
}
