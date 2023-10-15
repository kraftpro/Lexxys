namespace Lexxys.Testing;

/// <summary>
/// Implementation of <see cref="IRand"/> using regular pseudo-random number generator.
/// </summary>
public class RndSys: IRand
{
	private Random _r = new Random();

	/// <inheritdoc/>
	public void Reset(int seed = 0) => _r = seed <= 0 ? new Random(): new Random(seed);

	/// <inheritdoc/>
	public int NextInt() => _r.Next();

	/// <inheritdoc/>
	public double NextDouble() => _r.NextDouble();

	/// <inheritdoc/>
	public void NextBytes(byte[] buffer) => _r.NextBytes(buffer);
}
