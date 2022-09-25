using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lexxys.Logging;

#region Config

public class LoggingConfig
{
	public TimeSpan? LogoffTimeout { get; set; }
	public string? Exclude { get; set; }
	public int? MaxQueueSize { get; set; }
	public ILogRecordFormatter? Formatter { get; set; }

}

public class LogWriterConfig
{
	public ILogRecordFormatter? Formatter { get; set; }
	public LogWriterRuleConfig[]? Rules { get; set; }
}

public class LogWriterRuleConfig
{
	public string? LogLevel { get; set; }
	public string? Include { get; set; }
	public string? Exclude { get; set; }
}

public class LogWriterFileConfig: LogWriterConfig
{
	string? Path { get; set; }
	TimeSpan? Timeout { get; set; }
	bool Overwrite { get; set; }
}

public class LogWriterEventConfig: LogWriterConfig
{
	public string? LogName { get; set; }
}

#endregion


public interface ILoggingService
{
	ILogRecordWriter GetLogWriter(string source);
	//void AddWriters(IEnumerable<ILogWriter?> writers);
	//void SetWriters(IEnumerable<ILogWriter?> writers);
	//void Start();
	//void Stop(bool force = false);
	//int LockLogging();
	//int UnlockLogging();
}

//public class LoggingService: ILoggingService
//{
//	private LoggingParameters _parameters;

//	public LoggingService(LoggingParameters parameters)
//	{
//		_parameters = parameters;
//	}

//	public ILogRecordWriter GetLogWriter(string source) => default!;
//}

public static class LoggingServicesExtenstions
{
	public static IServiceCollection AddLoggingService(this IServiceCollection services, Action<ILoggingParameters>? configure = default)
	{
		// TODO: Read configuration and add Loggers into the parameters
		var parameters = new LoggingParameters(services);

		configure?.Invoke(parameters);

		// TODO: Register Logging services
		// services.AddSingleton(typeof(ILogger<>), typeof(Lexxys.Logging.Logger<>));

		services.TryAddSingleton<ILoggingService>(new LogRecordService(parameters));
		services.AddSingleton(typeof(ILogger<>), typeof(Lexxys.Logging.Logger<>));
		services.AddSingleton(typeof(ILogging<>), typeof(Lexxys.Logging.Logger<>));

		return services;
	}

	public static void RegisterStatics(this IServiceCollection services)
	{
		if (services is null)
			throw new ArgumentNullException(nameof(services));
		Statics.AddServices(services);
	}
}

public static partial class LoggingParametersExtenstions
{
	public static ILoggingParameters AddConsole(this ILoggingParameters parameters, Action<LoggingConsoleParameters> config)
	{
		var param = (LoggingConsoleParameters?)parameters.FirstOrDefault(o => o is LoggingConsoleParameters);
		if (param == null)
			param = new LoggingConsoleParameters
			{
				Name = "Console",
				Rules = new List<LogWriterFilter>
				{
					new LogWriterFilter { LogType = LogTypeFilter.Information }
				}
			};
		config?.Invoke(param);
		parameters.Add(param);
		return parameters;
	}

	public static ILoggingParameters AddFile(this ILoggingParameters parameters, Action<LoggingFileParameters> config)
	{
		var param = new LoggingFileParameters
		{
			Name = "./logs/YMD.log",
			Path = "./logs/{YMD}.log",
			Formatter = new LogRecordTextParameters(),
			Rules = new List<LogWriterFilter>
			{
				new LogWriterFilter { LogType = LogTypeFilter.Information }
			}
		};
		config?.Invoke(param);
		parameters.Add(param);
		return parameters;
	}

	public static ILoggingParameters AddEventLog(this ILoggingParameters parameters, Action<LoggingEventParameters> config)
	{
		var param = (LoggingEventParameters?)parameters.FirstOrDefault(o => o is LoggingEventParameters);
		if (param == null)
			param = new LoggingEventParameters
			{
				Name = "EventLog",
				Rules = new List<LogWriterFilter>
				{
					new LogWriterFilter { LogType = LogTypeFilter.Information }
				}
			};
		config?.Invoke(param);
		parameters.Add(param);
		return parameters;
	}

	public static ILoggingParameters AddDebugerLog(this ILoggingParameters parameters, Action<LoggingDebugParameters> config)
	{
		var param = (LoggingDebugParameters?)parameters.FirstOrDefault(o => o is LoggingDebugParameters);
		if (param == null)
			param = new LoggingDebugParameters
			{
				Name = "Debugger",
				Rules = new List<LogWriterFilter>
				{
					new LogWriterFilter { LogType = LogTypeFilter.Debug }
				}
			};
		config?.Invoke(param);
		parameters.Add(param);
		return parameters;
	}

	public static ILoggingParameters AddTraceLog(this ILoggingParameters parameters, Action<LoggingTraceParameters> config)
	{
		var param = (LoggingTraceParameters?)parameters.FirstOrDefault(o => o is LoggingTraceParameters);
		if (param == null)
			param = new LoggingTraceParameters
			{
				Name = "Trace",
				Rules = new List<LogWriterFilter>
				{
					new LogWriterFilter { LogType = LogTypeFilter.Trace }
				}
			};
		config?.Invoke(param);
		parameters.Add(param);
		return parameters;
	}
}

public static partial class LoggingParametersExtenstions
{
	public static ILoggingParameters SetExclude(this ILoggingParameters parameters, params Type[] value)
		=> SetExclude(parameters, value.Select(o => o.GetTypeName()));

	public static ILoggingParameters SetExclude(this ILoggingParameters parameters, params string[] value)
		=> SetExclude(parameters, (IEnumerable<string>)value);

	public static ILoggingParameters SetExclude(this ILoggingParameters parameters, IEnumerable<string>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Exclude = null;
		}
		else
		{
			var items = new List<string>(value.Where(o => o != null));
			parameters.Exclude = items.Count == 0 ? null : items;
		}
		return parameters;
	}
}

public static partial class LogWriterParameterExtensions
{
	public static ILogWriterParameters SetName(ILogWriterParameters parameters, string? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Name = value;
		return parameters;
	}

	public static ILogWriterParameters SetInclude(ILogWriterParameters parameters, params Type[] value)
		=> SetInclude(parameters, value.Select(o => o.GetTypeName()));

	public static ILogWriterParameters SetInclude(ILogWriterParameters parameters, params string[] value)
		=> SetInclude(parameters, (IEnumerable<string>)value);

	public static ILogWriterParameters SetInclude(ILogWriterParameters parameters, IEnumerable<string>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Include = null;
		}
		else
		{
			var items = new List<string>(value.Where(o => o != null));
			parameters.Include = items.Count == 0 ? null : items;
		}
		return parameters;
	}

	public static ILogWriterParameters SetExclude(ILogWriterParameters parameters, params Type[] value)
		=> SetExclude(parameters, value.Select(o => o.GetTypeName()));

	public static ILogWriterParameters SetExclude(ILogWriterParameters parameters, params string[] value)
		=> SetExclude(parameters, (IEnumerable<string>)value);

	public static ILogWriterParameters SetExclude(ILogWriterParameters parameters, IEnumerable<string>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Exclude = null;
		}
		else
		{
			var items = new List<string>(value.Where(o => o != null));
			parameters.Exclude = items.Count == 0 ? null : items;
		}
		return parameters;
	}
	
	public static ILogWriterParameters SetFlushTimeout(ILogWriterParameters parameters, TimeSpan? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.FlushTimeout = value;
		return parameters;
	}
	
	public static ILogWriterParameters SetFormatter(ILogWriterParameters parameters, ILogRecordFormatterParameters? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Formatter = value;
		return parameters;
	}
	
	public static ILogWriterParameters SetRules(ILogWriterParameters parameters, params LogWriterFilter[] value)
		=> SetRules(parameters, (IEnumerable<LogWriterFilter>)value);

	public static ILogWriterParameters SetRules(ILogWriterParameters parameters, IEnumerable<LogWriterFilter>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Rules = null;
		}
		else
		{
			var rules = new List<LogWriterFilter>(value.Where(o => o != null));
			parameters.Rules = rules.Count == 0 ? null : rules;
		}
		return parameters;
	}
}

public interface ILoggingParameters: ICollection<ILogWriterParameters>
{
	IServiceCollection Services { get; }
	ICollection<string>? Exclude { get; set; }
	LogType LogType { get; set; }
}

public interface ILogWriterParameters
{
	string? Name { get; set; }
	ICollection<string>? Include { get; set; }
	ICollection<string>? Exclude { get; set; }
	LogType LogType { get; set; }
	int? MaxQueueSize { get; set; }
	TimeSpan? FlushTimeout { get; set; }
	ILogRecordFormatterParameters? Formatter { get; set; }
	ICollection<LogWriterFilter>? Rules { get; set; }
	ILogWriter CreateWriter();
}

public static partial class LogWriterParametersExtensions
{
	public static ILogWriterParameters SetName(this ILogWriterParameters parameters, string name)
	{
		parameters.Name = name;
		return parameters;
	}

	public static ILogWriterParameters SetIncludes(this ILogWriterParameters parameters, params string[] value)
	{
		parameters.Include = value;
		return parameters;
	}

	public static ILogWriterParameters SetIncludes(this ILogWriterParameters parameters, params Type[] value)
	{
		parameters.Include = value.Select(o => o.GetTypeName()).ToList();
		return parameters;
	}

	public static ILogWriterParameters SetExcludes(this ILogWriterParameters parameters, params string[] value)
	{
		parameters.Exclude = value;
		return parameters;
	}

	public static ILogWriterParameters SetExcludes(this ILogWriterParameters parameters, params Type[] value)
	{
		parameters.Exclude = value.Select(o => o.GetTypeName()).ToList();
		return parameters;
	}

	public static ILogWriterParameters SetLogType(this ILogWriterParameters parameters, LogType logType)
	{
		parameters.LogType = logType;
		return parameters;
	}

	public static ILogWriterParameters SetFormatter(this ILogWriterParameters parameters, ILogRecordFormatterParameters formatter)
	{
		parameters.Formatter = formatter;
		return parameters;
	}

	public static ILogWriterParameters SetFormatter(this ILogWriterParameters parameters, ILogRecordFormatter formatter)
	{
		parameters.Formatter = new SimpeLogRecordFormatterParameters(formatter);
		return parameters;
	}

	public static ILogWriterParameters SetTextFormat(this ILogWriterParameters parameters, string format, string? indent = null, string? section = null)
	{
		parameters.Formatter = new LogRecordTextParameters(format, indent, section);
		return parameters;
	}

	public static ILogWriterParameters SetFilter(this ILogWriterParameters parameters, params LogWriterFilter[] rules)
	{
		parameters.Rules = rules;
		return parameters;
	}

	public static ILogWriterParameters AddFilter(this ILogWriterParameters parameters, LogTypeFilter logType, string? include = null, string? exclude = null)
	{
		if (parameters.Rules == null)
		{
			parameters.Rules = new List<LogWriterFilter>();
		}
		else if (parameters.Rules.IsReadOnly)
		{
			parameters.Rules = new List<LogWriterFilter>(parameters.Rules);
		}
		parameters.Rules.Add(new LogWriterFilter { LogType = logType, Include = include, Exclude = exclude });
		return parameters;
	}

	private class SimpeLogRecordFormatterParameters: ILogRecordFormatterParameters
	{
		ILogRecordFormatter _formatter;

		public SimpeLogRecordFormatterParameters(ILogRecordFormatter formatter)
		{
			_formatter = formatter;
		}

		public ILogRecordFormatter CreateFormatter() => _formatter;
	}
}

public class LogWriterFilter
{
	public LogTypeFilter LogType { get; set; } = LogTypeFilter.Information;
	public string? Include { get; set; }
	public string? Exclude { get; set; }
}

public class LoggingParameters: ILoggingParameters
{
	private List<ILogWriterParameters> _parameters;

	public LoggingParameters(IServiceCollection? services)
	{
		Services = services ?? new ServiceCollection(); // throw new ArgumentNullException(nameof(services));
		_parameters = new List<ILogWriterParameters>();
	}

	public IServiceCollection Services { get; }
	public int Count => _parameters.Count;
	public bool IsReadOnly => false;

	public ICollection<string>? Exclude { get; set; }
	public int MaxQueueSize { get; set; }
	public TimeSpan? FlushTimeout { get; set; }
	public LogType LogType { get; set; }

	public void Add(ILogWriterParameters item) => _parameters.Add(item);

	public void Clear() => _parameters.Clear();

	public bool Contains(ILogWriterParameters item) => _parameters.Contains(item);

	public void CopyTo(ILogWriterParameters[] array, int arrayIndex) => _parameters.CopyTo(array, arrayIndex);

	public IEnumerator<ILogWriterParameters> GetEnumerator() => _parameters.GetEnumerator();

	public bool Remove(ILogWriterParameters item) => _parameters.Remove(item);

	IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
}


public abstract class LogWriterParameters: ILogWriterParameters
{
	public string? Name { get; set; }
	public ICollection<string>? Include { get; set; }
	public ICollection<string>? Exclude { get; set; }
	public TimeSpan? FlushTimeout { get; set; }
	public int? MaxQueueSize { get; set; }
	public ILogRecordFormatterParameters? Formatter { get; set; }
	public ICollection<LogWriterFilter>? Rules { get; set; }
	public LogType LogType { get; set; }

	protected LogWriterParameters()
	{
	}

	public abstract ILogWriter CreateWriter();
}

public class LoggingConsoleParameters: LogWriterParameters
{
	public LoggingConsoleParameters()
	{
	}

	public override ILogWriter CreateWriter() => new ConsoleLogWriter(this);
}

public class LoggingFileParameters: LogWriterParameters
{
	public string? Path { get; set; }
	public TimeSpan? Timeout { get; set; }
	public bool Overwrite { get; set; }

	public LoggingFileParameters()
	{
	}

	public LoggingFileParameters SetFile(string? path = default, bool? overwrite = default, TimeSpan? timeout = default)
	{
		if (path != null)
			Path = path;
		if (overwrite != null)
			Overwrite = overwrite.GetValueOrDefault();
		if (timeout != null)
			Timeout = timeout;
		return this;
	}

	public override ILogWriter CreateWriter() => new FileLogWriter(this);
}

public class LoggingEventParameters: LogWriterParameters
{
	public string? EventSource { get; set; }
	public string? LogName { get; set; }

	public LoggingEventParameters()
	{
	}

	public override ILogWriter CreateWriter() => new EventLogLogWriter(this);
}

public class LoggingDebugParameters: LogWriterParameters
{
	public override ILogWriter CreateWriter() => new DebuggerLogWriter(this);
}

public class LoggingTraceParameters: LogWriterParameters
{
	public override ILogWriter CreateWriter() => new TraceLogWriter(this);
}


public interface ILogRecordFormatterParameters
{
	ILogRecordFormatter CreateFormatter();
}

public class LogRecordTextParameters: ILogRecordFormatterParameters
{
	private const string DefaultFormat = @"{MachineName}:{ProcessID:X4}{ThreadID:X4}.{SeqNumber:X4} {TimeStamp:yyyyMMddTHH:mm:ss.fffff}[{Type:3}] {IndentMark}{Source}{EventId:\.0}: {Message}";

	public string? Format { get; set; }
	public string? Indent { get; set; }
	public string? Section { get; set; }

	public LogRecordTextParameters(string? format = null, string? indent = null, string? section = null)
	{
		Format = format;
		Indent = indent;
		Section = section;
	}

	public ILogRecordFormatter CreateFormatter() => new LogRecordTextFormatter(new TextFormatSetting(Format ?? DefaultFormat, Indent ?? "  ", Section));
}

public class LogRecordJsonParameters: ILogRecordFormatterParameters
{
	public NamingCaseRule Naming { get; set; }

	public ILogRecordFormatter CreateFormatter() => new LogRecordJsonFormatter(Naming);
}