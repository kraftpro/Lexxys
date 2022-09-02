using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
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
	string? LogLevel { get; set; }
	string? Include { get; set; }
	string? Exclude { get; set; }
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


public static class LoggingServicesExtenstions
{
	public static IServiceCollection AddLoggingService(this IServiceCollection services, Action<LoggingParameters>? configure = default)
	{
		services.AddSingleton(typeof(ILogger<>), typeof(Lexxys.Logging.Logger<>));
		if (configure != null)
		{
			var parameters = new LoggingParameters();
			configure(parameters);
		}
		return services;
	}
}

public static class LoggingParametersExtenstions
{
	public static void AddConsole(this LoggingParameters parameters, Action<LoggingConsoleParameters> config)
	{
	}

	public static void AddFile(this LoggingParameters parameters, Action<LoggingFileParameters> config)
	{
	}

	public static void AddEventLog(this LoggingParameters parameters, Action<LoggingEventParameters> config)
	{
	}
}

public class LoggingParameters
{
	public string? Name;
	public TimeSpan? LogoffTimeout { get; set; }
	public int? MaxQueueSize { get; set; }
	public string[]? Exclude { get; set; }
	public ILogRecordFormatter? Formatter { get; set; }
}

public class LogWriterParameters
{
	public string[]? Include { get; set; }
	public string[]? Exclude { get; set; }
	public ILogRecordFormatter? Formatter { get; set; }
	public LogWriterRuleConfig[]? Rules { get; set; }
}

public class LoggingConsoleParameters : LogWriterParameters
{
}

public class LoggingFileParameters : LogWriterParameters
{
	public string? Path { get; set; }
	public TimeSpan? Timeout { get; set; }
	public bool Overwrite { get; set; }
}

public class LoggingEventParameters : LogWriterParameters
{
	public string? LogName { get; set; }
}
