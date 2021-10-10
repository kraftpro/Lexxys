// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Threading;

using Lexxys;

#nullable disable

namespace Lexxys
{
	public class ConfigurationEventArgs : EventArgs
	{
	}

	public interface ISectionValue<out T>: IValue<T>
	{
		int Version { get; }
	}

	public interface IConfigSection
	{
		event EventHandler<ConfigurationEventArgs> Changed;

		int Version { get; }

		IConfigSection Section(string key);

		ISectionValue<T> GetSection<T>(string key, Func<T> defaultValue);
		ISectionValue<IReadOnlyList<T>> GetSectionList<T>(string key);

		//IOptions<T> GetOptions<T>(string key, Func<T> defaultValue) where T: class, new();
		//IOptions<List<T>> GetOptionsList<T>(string key) where T: class, new();
	}

	public static class ConfigSectionExtenstions
	{
		public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, Func<T> defaultValue) where T : class, new()
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			var section = config.GetSection(key, defaultValue);
			return new OptOut<T>(() => section.Value);
		}

		public static IOptions<List<T>> GetOptionsList<T>(this IConfigSection config, string key)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			if (key == null || key.Length <= 0)
				throw new ArgumentNullException(nameof(key));

			var actual = new ConfigSectionValue<List<T>>(
				value: () =>
				{
					var value = config.GetSectionList<T>(key).Value;
					return value as List<T> ?? new List<T>(value);
				},
				version: () => config.Version);
			return new OptOut<List<T>>(() => actual.Value);
		}

		public static IValue<T?> GetSection<T>(this IConfigSection config, string key)
			=> config.GetSection<T?>(key, () => default);
		public static IValue<T> GetSection<T>(this IConfigSection config, string key, T defaultValue)
			=> config.GetSection<T>(key, () => defaultValue);

		public static IOptions<T?> GetOptions<T>(this IConfigSection config, string key) where T : class, new()
			=> config.GetOptions<T?>(key, () => default);
		public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, T defaultValue) where T : class, new()
			=> config.GetOptions<T>(key, () => defaultValue);

		public static T? GetValue<T>(this IConfigSection config, string key)
			=> config.GetSection<T?>(key, () => default).Value;
		public static T GetValue<T>(this IConfigSection config, string key, T defaultValue)
			=> config.GetSection<T>(key, () => defaultValue).Value;
		public static T GetValue<T>(this IConfigSection config, string key, Func<T> defaultValue)
			=> config.GetSection<T>(key, defaultValue).Value;

		public static IReadOnlyList<T> GetList<T>(this IConfigSection config, string key)
			=> config.GetSectionList<T>(key).Value;

		public static T GetValue<T>(this IConfigSection config, string key, T minValue, T maxValue, T defaultValue)
			where T: IComparable<T>
		{
			bool def = false;
			var value = config.GetSection(key, () => { def = true; return defaultValue; }).Value;
			return def ? value :
				value.CompareTo(minValue) <= 0 ? minValue :
				value.CompareTo(maxValue) >= 0 ? maxValue : value;
		}
	}
}
