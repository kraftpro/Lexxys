using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.Logging;

public static partial class LogWriterParameterExtensions
{
	public static ILogWriterParameters SetName(this ILogWriterParameters parameters, string? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Name = value;
		return parameters;
	}

	public static ILogWriterParameters SetLogLevel(this ILogWriterParameters parameters, LogType? logType)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.LogLevel = logType;
		return parameters;
	}

	public static ILogWriterParameters SetInclude(this ILogWriterParameters parameters, params Type[] value)
		=> SetInclude(parameters, value.Select(o => o.GetTypeName()));

	public static ILogWriterParameters SetInclude(this ILogWriterParameters parameters, params string[] value)
		=> SetInclude(parameters, (IEnumerable<string>)value);

	public static ILogWriterParameters SetInclude(this ILogWriterParameters parameters, IEnumerable<string>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Include = null;
		}
		else
		{
			var items = String.Join(",", value.Where(o => o != null));
			parameters.Include = items.TrimToNull();
		}
		return parameters;
	}

	public static ILogWriterParameters SetExclude(this ILogWriterParameters parameters, params Type[] value)
		=> SetExclude(parameters, value.Select(o => o.GetTypeName()));

	public static ILogWriterParameters SetExclude(this ILogWriterParameters parameters, params string[] value)
		=> SetExclude(parameters, (IEnumerable<string>)value);

	public static ILogWriterParameters SetExclude(this ILogWriterParameters parameters, IEnumerable<string>? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (value is null)
		{
			parameters.Exclude = null;
		}
		else
		{
			var items = String.Join(",", value.Where(o => o != null));
			parameters.Exclude = items.TrimToNull();
		}
		return parameters;
	}

	public static ILogWriterParameters SetFlushTimeout(this ILogWriterParameters parameters, TimeSpan? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.FlushTimeout = value;
		return parameters;
	}

	public static ILogWriterParameters SetFormatter(this ILogWriterParameters parameters, ILogRecordFormatterParameters? value)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Formatter = value;
		return parameters;
	}

	public static ILogWriterParameters SetFormatter(this ILogWriterParameters parameters, ILogRecordFormatter? formatter)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Formatter = formatter == null ? null: new SimpeLogRecordFormatterParameters(formatter);
		return parameters;
	}

	public static ILogWriterParameters SetTextFormat(this ILogWriterParameters parameters, string format, string? indent = null, string? section = null)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		parameters.Formatter = new LogRecordTextParameters(format, indent, section);
		return parameters;
	}

	public static ILogWriterParameters SetFilter(this ILogWriterParameters parameters, params LogWriterFilter[] value)
		=> SetFilter(parameters, (IEnumerable<LogWriterFilter>)value);

	public static ILogWriterParameters SetFilter(this ILogWriterParameters parameters, IEnumerable<LogWriterFilter>? value)
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

	public static ILogWriterParameters AddFilter(this ILogWriterParameters parameters, LogTypeFilter? logType, string? include = null, string? exclude = null)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.Rules == null)
			parameters.Rules = new List<LogWriterFilter>();
		else if (parameters.Rules.IsReadOnly)
			parameters.Rules = new List<LogWriterFilter>(parameters.Rules);
		parameters.Rules.Add(new LogWriterFilter(logType, include, exclude));
		return parameters;
	}

	private class SimpeLogRecordFormatterParameters: ILogRecordFormatterParameters
	{
		ILogRecordFormatter _formatter;

		public SimpeLogRecordFormatterParameters(ILogRecordFormatter formatter) => _formatter = formatter;

		public ILogRecordFormatter CreateFormatter() => _formatter;
	}
}
