using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lexxys
{
#if !NETSTANDARD

	static class WriterExtension
	{
		public static void Write(this TextWriter writer, ReadOnlySpan<char> value)
		{
			for (int i = 0; i < value.Length; ++i)
			{
				writer.Write(value[i]);
			}
		}
	}

#endif
}
