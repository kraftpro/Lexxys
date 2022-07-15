﻿using System;

using Microsoft.Extensions.Logging;

#nullable enable

namespace Lexxys.Configuration
{
	public interface IConfigLogger
	{
		void LogConfigurationError(string logSource, Exception exception);
		void LogConfigurationEvent(string logSource, string message);
		void SetLogger(ILogger? logger = null);
	}
}