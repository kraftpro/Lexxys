using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Lexxys.Tests
{
	public class UsageTests
	{

		public interface ILoggingProvider: ILoggerProvider
		{
			new ILogging CreateLogger(string categoryName);
		}

		public interface ILoggingFactory: ILoggerFactory
		{
			void AddProvider(ILoggingProvider provider);
			new ILogging CreateLogger(string categoryName);
		}


		class LoggingFactory: ILoggingFactory
		{
			public void AddProvider(ILoggingProvider provider)
			{
				throw new NotImplementedException();
			}

			public void AddProvider(ILoggerProvider provider)
			{
				throw new NotImplementedException();
			}

			public ILogging CreateLogger(string categoryName)
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
				throw new NotImplementedException();
			}

			ILogger ILoggerFactory.CreateLogger(string categoryName) => CreateLogger(categoryName);
		}

		class LoggingProvider: ILoggingProvider
		{
			public ILogging CreateLogger(string categoryName)
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
				throw new NotImplementedException();
			}

			ILogger ILoggerProvider.CreateLogger(string categoryName) => CreateLogger(categoryName);
		}

		public class LoggingMap: ILogging
		{
			private readonly ILogger _logger;

			public LoggingMap(ILogger logger)
			{
				_logger = logger;
			}

			public string Source => "";

			public IDisposable Enter(LogType logType, string sectionName, IDictionary args) => _logger.Enter(logType, sectionName, args);

			public bool IsEnabled(LogType logType) => _logger.IsEnabled(logType);

			public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			{
				throw new NotImplementedException();
			}

			public IDisposable BeginScope<TState>(TState state)
			{
				throw new NotImplementedException();
			}

			public void Log(LogType logType, int eventId, string source, string message, Exception exception, IDictionary args)
			{
				throw new NotImplementedException();
			}

			public IDisposable Timing(LogType logType, string description, TimeSpan threshold) => _logger.Timing(logType, description, threshold);
		}

		public void LoggingUsageTest()
		{

		}

		public void ConfigUsageTest()
		{

		}
	}
}
