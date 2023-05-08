using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.Logging;

public static partial class LoggingParametersExtensions
{
	public static ILoggingParameters AddConsole(this ILoggingParameters parameters, Action<LoggingConsoleParameters>? config = null)
	{
		var param = (LoggingConsoleParameters?)parameters.FirstOrDefault(o => o is LoggingConsoleParameters);
		param ??= new LoggingConsoleParameters
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

	public static ILoggingParameters AddFile(this ILoggingParameters parameters, Action<LoggingFileParameters>? config = null)
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

	public static ILoggingParameters AddEventLog(this ILoggingParameters parameters, Action<LoggingEventParameters>? config = null)
	{
		var param = (LoggingEventParameters?)parameters.FirstOrDefault(o => o is LoggingEventParameters);
		param ??= new LoggingEventParameters
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

	public static ILoggingParameters AddDebugLog(this ILoggingParameters parameters, Action<LoggingDebugParameters>? config = null)
	{
		var param = (LoggingDebugParameters?)parameters.FirstOrDefault(o => o is LoggingDebugParameters);
		param ??= new LoggingDebugParameters
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

	public static ILoggingParameters AddTraceLog(this ILoggingParameters parameters, Action<LoggingTraceParameters>? config = null)
	{
		var param = (LoggingTraceParameters?)parameters.FirstOrDefault(o => o is LoggingTraceParameters);
		param ??= new LoggingTraceParameters
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
