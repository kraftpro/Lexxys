// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;

namespace Lexxys
{
	using Configuration;

	public static class IConfigSectionExtenstions
	{
#nullable disable

		public static IValue<T> GetValue<T>(this IConfigSection config, string key, T defaultValue)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetValue(key, () => defaultValue);
		}

		//public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, Func<T> defaultValue) where T : class, new()
		//{
		//	if (config == null)
		//		throw new ArgumentNullException(nameof(config));

		//	var section = config.GetSection(key, defaultValue);
		//	return new OptOut<T>(() => section.Value);
		//}

		//public static IOptions<List<T>> GetOptionsList<T>(this IConfigSection config, string key)
		//{
		//	if (config == null)
		//		throw new ArgumentNullException(nameof(config));
		//	if (key == null || key.Length <= 0)
		//		throw new ArgumentNullException(nameof(key));

		//	var actual = new ConfigSectionValue<List<T>>(
		//		value: () =>
		//		{
		//			var value = config.GetSectionList<T>(key).Value;
		//			return value as List<T> ?? new List<T>(value);
		//		},
		//		version: () => config.Version);
		//	return new OptOut<List<T>>(() => actual.Value);
		//}

		//public static IOptions<T> GetOptions<T>(this IConfigSection config, string key) where T : class, new()
		//	=> config.GetOptions<T>(key, () => default);
		//public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, T defaultValue) where T : class, new()
		//	=> config.GetOptions<T>(key, () => defaultValue);

		public static T GetValueInRange<T>(this IConfigSection config, string key, T minValue, T maxValue, T defaultValue)
			where T : IComparable<T>
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			bool def = false;
			var value = config.GetValue(key, () => { def = true; return default(T); }).Value;
			return def || value is null ? defaultValue :
				value.CompareTo(minValue) <= 0 ? minValue :
				value.CompareTo(maxValue) >= 0 ? maxValue : value;
		}
	}
}
