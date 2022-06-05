//#define TraceFlushing
// Lexxys Infrastructural library.
// file: LogRecordsListener.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Logging;

public interface ILogRecordWriter
{
	void Write(LogRecord? record);
	bool IsEnabled(LogType logType);
}
