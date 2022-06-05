using System.Collections.Generic;

#nullable enable

namespace Lexxys.Logging
{
	public interface ILogWriter
	{
		string Name { get; }
		string Target { get; }
		void Open();
		void Close();
		bool Accepts(string? source, LogType type);
		void Write(IEnumerable<LogRecord> records);
	}
}