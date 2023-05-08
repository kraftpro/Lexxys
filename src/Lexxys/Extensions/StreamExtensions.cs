// Lexxys Infrastructural library.
// file: StreamExtensions.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

public static class StreamExtensions
{
	public static void Write(this Stream stream, byte value)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		stream.WriteByte(value);
	}

	public static void Write(this Stream stream, byte[]? value)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (value == null)
			return;
		stream.Write(value, 0, value.Length);
	}

#if !NET5_0_OR_GREATER
	public static void Write(this Stream stream, ReadOnlySpan<byte> value)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		stream.Write(value.ToArray(), 0, value.Length);
	}
#endif
}


