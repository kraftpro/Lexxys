// Lexxys Infrastructural library.
// file: IRand.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
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