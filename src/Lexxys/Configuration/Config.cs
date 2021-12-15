// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable enable

namespace Lexxys
{
	using Configuration;

	public class ConfigurationEventArgs : EventArgs
	{
	}

	public static class Config
	{
		public static IConfigSection Current => ConfigSection.Instance;

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

		public static IConfigurationProvider? AddConfiguration(string location)
			=> DefaultConfig.AddConfiguration(location);

		public static IConfigurationProvider? AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null)
			=> DefaultConfig.AddConfiguration(location, parameters);

		public static void LogConfigurationError(string logSource, Exception exception)
			=> DefaultConfig.LogConfigurationError(logSource, exception);

		public static void LogConfigurationEvent(string logSource, Func<string> message)
			=> DefaultConfig.LogConfigurationEvent(logSource, message);

		internal static Uri? LocateFile(string filePath, IEnumerable<string>? directory, IReadOnlyCollection<string>? extension)
		{
			if (File.Exists(filePath))
				return new Uri(Path.GetFullPath(filePath));

			if (extension != null)
			{
				foreach (string ext in extension)
				{
					int i = ext.LastIndexOf('.');
					string e = (i > 0) ? ext.Substring(i) : ext;
					if (filePath.EndsWith(e, StringComparison.OrdinalIgnoreCase))
					{
						extension = null;
						break;
					}
				}
			}

			if (filePath[0] == System.IO.Path.DirectorySeparatorChar || filePath[0] == System.IO.Path.AltDirectorySeparatorChar)
				directory = null;
			else if (ContainsVolume(filePath))
				directory = null;

			if (directory == null)
				return extension?
					.Where(ext => File.Exists(filePath + ext))
					.Select(ext => new Uri(Path.GetFullPath(filePath + ext)))
					.FirstOrDefault();

			if (extension == null)
				return directory
					.Select(dir => System.IO.Path.Combine(dir, filePath))
					.Where(File.Exists)
					.Select(file => new Uri(Path.GetFullPath(file)))
					.FirstOrDefault();

			return directory
				.SelectMany(_ => extension, (dir, ext) => System.IO.Path.Combine(dir, filePath + ext))
				.Where(File.Exists)
				.Select(file => new Uri(Path.GetFullPath(file)))
				.FirstOrDefault();

			static bool ContainsVolume(string path)
			{
				if (System.IO.Path.DirectorySeparatorChar == System.IO.Path.VolumeSeparatorChar)
					return false;
				var i = path.IndexOf(System.IO.Path.VolumeSeparatorChar);
				return i > 0 && path.Substring(0, i).All(c => Char.IsLetter(c));
			}
		}

	}
}
