using System.Buffers;
using System.Runtime.InteropServices;

namespace Lexxys.Testing;

public class RndPcg: IRand
{
#if !NET
	Random _random = new();
#endif
	private ulong _state;
	private ulong _inc;

	public RndPcg() => Reset((ulong)Environment.TickCount, 900719925);

	public RndPcg(ulong seed, ulong seq = 0) => Reset(seed, seq);

	private void Reset(ulong seed, ulong seq)
	{
		if (seed == 0 && seq == 0)
		{
#if NET
			Span<ulong> uu = stackalloc ulong[2];
			Random.Shared.NextBytes(MemoryMarshal.AsBytes(uu));
			seed = uu[0];
			seq = uu[1];
#else
			var bb = ArrayPool<byte>.Shared.Rent(2 * sizeof(ulong));
			_random.NextBytes(bb);
			var uu = MemoryMarshal.Cast<byte, ulong>(bb);
			seed = uu[0];
			seq = uu[1];
			ArrayPool<byte>.Shared.Return(bb);
#endif
		}
		_inc = (seq << 1) | 1U;
		_state = (seed + _inc) * 6364136223846793005U + _inc;
	}

	public uint NextUInt32()
	{
		ulong state = _state;
		_state = state * 6364136223846793005U + _inc;
		uint xorShifted = (uint)(((state >> 18) ^ state) >> 27);
		int rot = (int)(state >> 59);
		return (xorShifted >> rot) | (xorShifted << (32 - rot));
	}

	public ulong NextUInt64()
	{
		Span<uint> b = [NextUInt32(), NextUInt32()];
		return MemoryMarshal.Read<ulong>(MemoryMarshal.AsBytes(b));
	}

	public void NextBytes(Span<byte> buffer)
	{
		Span<byte> bb = buffer;
		if (bb.Length >= sizeof(uint))
		{
			Span<uint> uu = MemoryMarshal.Cast<byte, uint>(bb);
			for (int i = 0; i < uu.Length; ++i)
			{
				uu[i] = NextUInt32();
			}
			bb = bb.Slice(bb.Length & ~(sizeof(ulong) - 1));
		}
		if (bb.Length == 0) return;

		Span<byte> t = stackalloc byte[sizeof(uint)];
#if NET
		MemoryMarshal.AsRef<uint>(t) = NextUInt32();
#else
		MemoryMarshal.Cast<byte, uint>(t)[0] = NextUInt32();
#endif
		bb[0] = t[0];
		if (bb.Length < 2) return;
		bb[1] = t[1];
		if (bb.Length < 3) return;
		bb[2] = t[2];
	}

	public double NextDouble() => (NextUInt64() >> 5) * (1.0 / 9007199254740992.0);

	public int NextInt32() => (int)NextUInt32() & ~Int32.MinValue;

	public long NextInt64() => (long)NextUInt64() & ~Int64.MinValue;

	public void Reset(long seed = 0) => Reset((ulong)seed, 0);
}
