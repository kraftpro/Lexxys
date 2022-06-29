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
using System.Text.RegularExpressions;

#nullable enable

namespace Lexxys
{
	using Configuration;

	public class ConfigurationEventArgs: EventArgs
	{
		public static new readonly ConfigurationEventArgs Empty = new ConfigurationEventArgs();
	}

	public static class Config
	{
		public static IConfigSection Current => StaticServices.Create<IConfigSection>();

		public static T? GetValue<T>(string key)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			return Current.GetValue<T?>(key).Value;
		}

		public static T GetValue<T>(string key, T defaultValue)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			return Current.GetValue<T>(key, defaultValue).Value;
		}

		public static T GetValue<T>(string key, Func<T> defaultValue)
		{
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));
			if (defaultValue == null)
				throw new ArgumentNullException(nameof(defaultValue));
			return Current.GetValue<T>(key, defaultValue).Value;
		}

		public static bool AddConfiguration(string location, bool tail = false)
		{
			var p = SplitOptions(location);
			return StaticServices.Create<IConfigService>().AddConfiguration(new Uri(p.Location, UriKind.RelativeOrAbsolute), p.Parameters, tail);
		}

		public static bool AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null)
			=> StaticServices.Create<IConfigService>().AddConfiguration(location, parameters);

		public static void LogConfigurationError(string logSource, Exception exception)
			=> StaticServices.Create<IConfigLogger>().LogConfigurationError(logSource, exception);

		public static void LogConfigurationEvent(string logSource, string message)
			=> StaticServices.Create<IConfigLogger>().LogConfigurationEvent(logSource, message);

		internal static Uri[] GetLocalFiles(string path, IEnumerable<string>? directories)
		{
			var matched = __configExtRex.IsMatch(path);
			if (Path.IsPathRooted(path) || directories == null)
			{
				if (matched)
					return File.Exists(path) ? new[] { new Uri(path) }: Array.Empty<Uri>();
				return CollectFiles(path) ?? Array.Empty<Uri>();
			}

			foreach (var dir in directories)
			{
				if (matched)
				{
					if (File.Exists(Path.Combine(dir, path)))
						return new[] { new Uri(Path.Combine(dir, path)) };
				}
				else
				{
					var files = CollectFiles(Path.Combine(dir, path));
					if (files?.Length > 0)
						return files;
				}
			}
			return Array.Empty<Uri>();

			static Uri[]? CollectFiles(string path)
			{
				int i = path.LastIndexOf('\\');
				if (i < 0)
					i = path.LastIndexOf('/');
				if (i < 0)
					return null;
				if (Directory.Exists(path.Substring(0, i)))
					return Array.ConvertAll(Directory.GetFiles(path.Substring(0, i), path.Substring(i + 1) + ".config.*"), o => new Uri(o));
				return null;
			}
		}
		private static readonly Regex __configExtRex = new Regex(@"\.config\.[^.]+\z", RegexOptions.IgnoreCase);

		private static (string Location, IReadOnlyCollection<string>? Parameters) SplitOptions(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			value = value.Trim();
			var xx = value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
			if (xx.Length == 1)
				return (value, null);
			if (xx.Length == 2)
				return (xx[0], new[] { xx[1] });
			return (xx[0], xx.Skip(1).ToList());
		}
		private static readonly char[] SpaceSeparator = new[] { ' ' };

	}
}
