using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Lexxys;

#nullable enable

namespace Lexxys
{

	public interface ILoggingFactory
	{
		ILogging CreateLogger(string source);
		ILogging<T> CreateLogger<T>();
	}

	public static class LoggingFactory
	{
		public static readonly ILoggingFactory Default = new DefaultFactory();

		public static ILoggingFactory Current => __current;
		private static ILoggingFactory __current = Default;

		public static void SetCurrentFactory(ILoggingFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			Interlocked.Exchange(ref __current, factory);
		}

		private class DefaultFactory: ILoggingFactory
		{
			public ILogging CreateLogger(string source) => new Logger(source);
			public ILogging<T> CreateLogger<T>() => new Logger<T>();
		}
	}
}
