// Lexxys Infrastructural library.
// file: IRand.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexxys.Testing;

/// <summary>
/// Represents a random number generator.
/// </summary>
public interface IRand
{
	/// <summary>
	/// Resets the random number generator to generate a repeated sequence.
	/// </summary>
	/// <param name="seed">Seed value for pseudo-random numbers sequence.</param>
	/// <exception cref="NotSupportedException">The method is not supported</exception>
	void Reset(int seed = 0);
	/// <summary>
	/// Returns a non-negative random integer.
	/// </summary>
	/// <returns>A 32-bit signed integer that is greater than or equal to 0.</returns>
	int NextInt();
	/// <summary>
	/// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
	/// </summary>
	/// <returns>
	/// A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.
	/// </returns>
	double NextDouble();
	/// <summary>
	/// Fills the elements of a specified array of bytes with random numbers.
	/// </summary>
	/// <param name="buffer">An array of bytes to contain random numbers.</param>
	/// <exception cref="ArgumentNullException">buffer is null</exception>
	void NextBytes(byte[] buffer);
}