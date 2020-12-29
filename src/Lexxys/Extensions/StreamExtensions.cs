// Lexxys Infrastructural library.
// file: StreamExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys
{
	public static class StreamExtensions
	{
		public static void Write(this Stream stream, byte value)
		{
			stream.WriteByte(value);
		}

		public static void Write(this Stream stream, byte[] value)
		{
			stream.Write(value, 0, value.Length);
		}
	}
}


