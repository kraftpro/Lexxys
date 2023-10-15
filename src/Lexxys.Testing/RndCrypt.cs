using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lexxys.Testing;

/// <summary>
/// Implementation of <see cref="IRand"/> using cryptographic random number generator.  Doesn't support <see cref="Reset(int)"/> method.
/// </summary>
public class RndCrypt: IRand
{
	private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
	private const double AlmostOne = (int.MaxValue - 1) * (1.0 / int.MaxValue);

	/// <inheritdoc/>
	public void NextBytes(byte[] buffer) => _generator.GetBytes(buffer);

	/// <inheritdoc/>
	public int NextInt()
	{
#if NET7_0_OR_GREATER
		int val = 0;
		_generator.GetBytes(MemoryMarshal.AsBytes(new Span<int>(ref val)));
		return Math.Abs(val);
#else
		var val = new byte[sizeof(int)];
		_generator.GetBytes(val);
		return Math.Abs(MemoryMarshal.Read<int>(val));
#endif
	}

	/// <inheritdoc/>
	public double NextDouble() => NextInt() * (AlmostOne / int.MaxValue);

	/// <summary>
	/// Throws <see cref="NotSupportedException"/> exception if <paramref name="seed"/> is greater than zero.
	/// </summary>
	/// <exception cref="NotSupportedException">The method is not supported</exception>
	public void Reset(int seed = 0)
	{
		if (seed > 0)
			throw new NotSupportedException($"Method {nameof(Reset)} is not supported by {nameof(RndCrypt)}.");
	}
}
