using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Testing;

class RndSys: IRand
{
	private Random _r;

	public RndSys()
	{
		_r = new Random();
	}

	public RndSys(int seed)
	{
		_r = new Random(seed);
	}

	public void Reset(int seed)
	{
		_r = seed < 0 ? new Random(): new Random(seed == 0 ? 314159: seed);
	}

	public int NextInt()
	{
		return _r.Next();
	}

	public double NextDouble()
	{
		return _r.NextDouble();
	}

	public void NextBytes(byte[] buffer)
	{
		_r.NextBytes(buffer);
	}
}
