// Lexxys Infrastructural library.
// file: IReadWriteSync.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	public interface IReadWriteSync: IDisposable
	{
		bool IsReaderLockHeld { get; }
		bool IsWriterLockHeld { get; }
		IDisposable Read(int timeout, string source, int timingThreshold);
		IDisposable Read();
		IDisposable Read(int timeout);
		IDisposable Read(string source);
		IDisposable Read(int timeout, string source);
		IDisposable Write(int timeout, string source, int timingThreshold);
		IDisposable Write();
		IDisposable Write(int timeout);
		IDisposable Write(string source);
		IDisposable Write(int timeout, string source);
	}
}


