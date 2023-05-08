#if NET6_0_OR_GREATER

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

	public LoggingInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, LogType logType, out bool handlerIdValid)
	{
		if (logger == null)
			throw new ArgumentNullException(nameof(logger));
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

	public LoggingTraceInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Trace))
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

//.?$X = above("LogType.Trace", "Trace");
#endregion

#region Debug
//.#back($X, "LogType.Debug", "Debug")

[InterpolatedStringHandler]
public ref struct LoggingDebugInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingDebugInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Debug))
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
//.#back($X, "LogType.Information", "Info")

[InterpolatedStringHandler]
public ref struct LoggingInfoInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingInfoInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Information))
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
//.#back($X, "LogType.Warning", "Warning")

[InterpolatedStringHandler]
public ref struct LoggingWarningInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingWarningInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Warning))
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
//.#back($X, "LogType.Error", "Error")

[InterpolatedStringHandler]
public ref struct LoggingErrorInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingErrorInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Error))
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
//.#back($X, "LogType.Output", "Write")

[InterpolatedStringHandler]
public ref struct LoggingWriteInterpolatedStringHandler
{
	private DefaultInterpolatedStringHandler _handler;
	private bool _isValid;

	public LoggingWriteInterpolatedStringHandler(int literalLength, int formattedCount, ILogging logger, out bool handlerIdValid)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));
		if (logger.IsEnabled(LogType.Output))
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