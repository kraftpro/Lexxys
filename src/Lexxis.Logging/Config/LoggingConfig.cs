using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Lexxys.Logging.Config;

public class LoggingConfig
{
	public string? Exclude { get; set; }
	
	public LogType LogLevel { get; set; }

	public LogWriterFilter[]? Filters { get; set; }

	public LogWriterConfig[]? Writers { get; set; }

	//public LoggingConfig(): base(FakeServiceCollection.Instance)
	//{
	//}

	//class FakeServiceCollection: IServiceCollection
	//{
	//	public static readonly IServiceCollection Instance = new FakeServiceCollection();

	//	private FakeServiceCollection()
	//	{
	//	}

	//	public ServiceDescriptor this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	//	public int Count => 0;
	//	public bool IsReadOnly => true;

	//	public void Add(ServiceDescriptor item) => throw new NotImplementedException();

	//	public void Clear() => throw new NotImplementedException();

	//	public bool Contains(ServiceDescriptor item) => throw new NotImplementedException();

	//	public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => throw new NotImplementedException();

	//	public IEnumerator<ServiceDescriptor> GetEnumerator() => throw new NotImplementedException();

	//	public int IndexOf(ServiceDescriptor item) => throw new NotImplementedException();

	//	public void Insert(int index, ServiceDescriptor item) => throw new NotImplementedException();

	//	public bool Remove(ServiceDescriptor item) => throw new NotImplementedException();

	//	public void RemoveAt(int index) => throw new NotImplementedException();

	//	IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
	//}
}

public abstract class LogWriterConfig
{
	public string? Name { get; set; }
	public string? Include { get; set; }
	public string? Exclude { get; set; }
	public TimeSpan? FlushTimeout { get; set; }
	public int? MaxQueueSize { get; set; }
	public LogFormatConfig? Formatter { get; set; }
	public ICollection<LogWriterFilter>? Rules { get; set; }
	public LogType? LogLevel { get; set; }

	public LogWriterParameters Configure()
	{
		var parameters = CreateParameters();
		parameters.Name = Name;
		parameters.Include = Include;
		parameters.Exclude = Exclude;
		parameters.FlushTimeout = FlushTimeout;
		parameters.MaxQueueSize = MaxQueueSize;
		parameters.Formatter = Formatter?.Configure();
		parameters.Rules = Rules;
		parameters.LogLevel = LogLevel;
		return parameters;
	}
	
	public abstract LogWriterParameters CreateParameters();
}

public enum LogFormatType
{
	Text,
	Json
}

public class LogFormatConfig
{
	public LogFormatType Type { get; set; }
	public NamingCaseRule? Naming { get; set; }
	public string? Format { get; set; }
	public string? Indent { get; set; }
	public string? Section { get; set; }

	public virtual ILogRecordFormatterParameters Configure()
		=> Type == LogFormatType.Json ?
			new LogRecordJsonParameters { Naming = Naming ?? NamingCaseRule.PreferPascalCase } :
			new LogRecordTextParameters { Format = Format, Indent = Indent, Section = Section };
}

public class LogConsoleConfig: LogWriterConfig
{
	public override LogWriterParameters CreateParameters()
		=> new LoggingConsoleParameters();
}

public class LogDatabaseConfig: LogWriterConfig
{
	public string? Connection { get; set; }
	public string? Schema { get; set; }
	public string? Table { get; set; }

	public override LogWriterParameters CreateParameters()
		=> new LoggingDatabaseParameters { Connection = Connection, Schema = Schema, Table = Table };
}

public class LogDebugConfig: LogWriterConfig
{
	public override LogWriterParameters CreateParameters()
		=> new LoggingDebugParameters();
}

public class LogFileConfig: LogWriterConfig
{
	public string? Path { get; set; }
	public TimeSpan? Timeout { get; set; }
	public bool Overwrite { get; set; }

	public override LogWriterParameters CreateParameters()
		=> new LoggingFileParameters { Path = Path, Timeout = Timeout, Overwrite = Overwrite };
}

public class LogTraceConfig: LogWriterConfig
{
	public override LogWriterParameters CreateParameters()
		=> new LoggingTraceParameters();
}

public class LogEventConfig: LogWriterConfig
{
	public string? EventSource { get; set; }
	public string? LogName { get; set; }

	public override LogWriterParameters CreateParameters()
		=> new LoggingEventParameters { EventSource = EventSource, LogName = LogName };
}