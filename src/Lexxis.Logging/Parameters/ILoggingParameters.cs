using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Lexxys.Logging;

public interface ILoggingParameters: ICollection<ILogWriterParameters>
{
	IServiceCollection Services { get; }
	ICollection<string>? Exclude { get; set; }
	LogType? LogLevel { get; set; }
	ICollection<LogWriterFilter>? Rules { get; set; }
}
