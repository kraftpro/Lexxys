using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexxys.Testing
{
	public interface IRand
	{
		void Reset(int seed);
		int NextInt();
		double NextDouble();
		void NextBytes(byte[] buffer);
	}
}