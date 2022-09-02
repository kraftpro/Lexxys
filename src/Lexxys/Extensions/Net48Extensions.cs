using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace Lexxys
{
#if !NET5_0_OR_GREATER

	static class TextWriterExtension
	{
		public static void Write(this TextWriter writer, ReadOnlySpan<char> value)
		{
			if (value.Length == 0)
				return;
			if (value.Length == 1)
			{
				writer.Write(value[0]);
				return;
			}
			char[] array = ArrayPool<char>.Shared.Rent(value.Length);
			try
			{
				value.CopyTo(new Span<char>(array));
				writer.Write(array, 0, value.Length);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}

		public static void WriteLine(this TextWriter writer, ReadOnlySpan<char> value)
		{
			if (value.Length == 0)
			{
				writer.WriteLine();
				return;
			}
			if (value.Length == 1)
			{
				writer.WriteLine(value[0]);
				return;
			}
			char[] array = ArrayPool<char>.Shared.Rent(value.Length);
			try
			{
				value.CopyTo(new Span<char>(array));
				writer.WriteLine(array, 0, value.Length);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
	}

#endif
}
