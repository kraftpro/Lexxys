using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lexxys
{
#if NETFRAMEWORK

	static class WriterExtension
	{
		public static void Write(this TextWriter writer, ReadOnlySpan<char> value)
		{
			if (value == null || value.Length == 0)
				return;
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
	}

#endif
}
