using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys;

public class JsonDumpWriterOptions: DumpWriterOptions
{
	public JsonDumpWriterOptions()
	{
		EqualChar = ':';
		FieldSeparator = ',';
		IncludeObjectType = false;
		Base64Binary = true;
	}

	public JsonDumpWriterOptions(DumpWriterOptions options): base(options)
	{
		if (options is not JsonDumpWriterOptions jsonOptions)
		{
			EqualChar = ':';
			FieldSeparator = ',';
			IncludeObjectType = false;
			Base64Binary = true;
		}
	}
}
