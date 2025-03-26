#if NET6_0_OR_GREATER

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys;

[InterpolatedStringHandler]
public ref struct LoggingInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, LogType logType, out bool handlerIdValid)
	{
		if (logger == null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(logType))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

#region Trace
//.?

[InterpolatedStringHandler]
public ref struct LoggingTraceInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingTraceInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Trace))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.?$X = above("LogLevel.Trace", "Trace");
#endregion

#region Debug
//.#back($X, "LogLevel.Debug", "Debug")

[InterpolatedStringHandler]
public ref struct LoggingDebugInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingDebugInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Debug))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.=cut
#endregion

#region Info
//.#back($X, "LogLevel.Information", "Info")

[InterpolatedStringHandler]
public ref struct LoggingInfoInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingInfoInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Information))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.=cut
#endregion

#region Warning
//.#back($X, "LogLevel.Warning", "Warning")

[InterpolatedStringHandler]
public ref struct LoggingWarningInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingWarningInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Warning))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.=cut
#endregion

#region Error
//.#back($X, "LogLevel.Error", "Error")

[InterpolatedStringHandler]
public ref struct LoggingErrorInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingErrorInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Error))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.=cut
#endregion

#region Write
//.#back($X, "LogLevel.Critical", "Write")

[InterpolatedStringHandler]
public ref struct LoggingWriteInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingWriteInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIdValid)
	{
		if (logger is null) throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogLevel.Critical))
		{
			_isValid = true;
			_handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
			handlerIdValid = true;
		}
		else
		{
			handlerIdValid = false;
		}
	}

	public void AppendLiteral(string value)
	{
		if (_isValid) _handler.AppendLiteral(value);
	}

	public void AppendFormatted<T>(T value)
	{
		if (_isValid) _handler.AppendFormatted(value);
	}

	public string ToStringAndClear() => _isValid ? _handler.ToStringAndClear(): String.Empty;
}

//.=cut
#endregion

#endif