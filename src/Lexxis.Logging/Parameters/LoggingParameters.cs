using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Lexxys.Logging;

public class LoggingParameters: ILoggingParameters
{
	private List<ILogWriterParameters> _parameters;

	public LoggingParameters(IServiceCollection services)
	{
		Services = services ?? throw new ArgumentNullException(nameof(services));
		_parameters = new List<ILogWriterParameters>();
	}

	public IServiceCollection Services { get; }
	public ICollection<string>? Exclude { get; set; }
	public LogType? LogLevel { get; set; }
	public ICollection<LogWriterFilter>? Rules { get; set; }

	public int Count => _parameters.Count;
	public bool IsReadOnly => false;

	public LoggingParameters WithServices(IServiceCollection services)
	{
		var result = new LoggingParameters(services);
		result._parameters.AddRange(_parameters);
		return result;
	}

	public void Add(ILogWriterParameters item) => _parameters.Add(item);

	public void Clear() => _parameters.Clear();

	public bool Contains(ILogWriterParameters item) => _parameters.Contains(item);

	public void CopyTo(ILogWriterParameters[] array, int arrayIndex) => _parameters.CopyTo(array, arrayIndex);

	public IEnumerator<ILogWriterParameters> GetEnumerator() => _parameters.GetEnumerator();

	public bool Remove(ILogWriterParameters item) => _parameters.Remove(item);

	IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
}
