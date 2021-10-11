// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

#nullable enable

namespace Lexxys
{
	using Configuration;

	public class ConfigurationEventArgs : EventArgs
	{
	}

	public static class Config
	{
		public static readonly IConfigSection Default = ConfigSection.Instance;

		//public static T? GetValue<T>(string key)
		//{
		//	if (key == null || key.Length <= 0)
		//		throw new ArgumentNullException(nameof(key));
		//	return DefaultConfig.GetObjectValue(key, typeof(T)) is T value ? value : default;
		//}

		//public static T GetValue<T>(string key, T defaultValue)
		//{
		//	if (key == null || key.Length <= 0)
		//		throw new ArgumentNullException(nameof(key));
		//	return DefaultConfig.GetObjectValue(key, typeof(T)) is T value ? value : defaultValue;
		//}
		
		//public static T GetValue<T>(string key, Func<T> defaultValue)
		//{
		//	if (key == null || key.Length <= 0)
		//		throw new ArgumentNullException(nameof(key));
		//	if (defaultValue == null)
		//		throw new ArgumentNullException(nameof(defaultValue));
		//	return DefaultConfig.GetObjectValue(key, typeof(T)) is T value ? value : defaultValue();
		//}

		public static void AddConfiguration(string location)
			=> DefaultConfig.AddConfiguration(location);

		public static void LogConfigurationError(string logSource, Exception exception)
			=> DefaultConfig.LogConfigurationError(logSource, exception);

		public static void LogConfigurationEvent(string logSource, Func<string> message)
			=> DefaultConfig.LogConfigurationEvent(logSource, message);
	}
}
