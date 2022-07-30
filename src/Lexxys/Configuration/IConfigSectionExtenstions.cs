// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys
{
	using Configuration;

	public static class IConfigSectionExtenstions
	{
		public static IValue<T> GetValue<T>(this IConfigSection section)
			=> (section ?? throw new ArgumentNullException(nameof(section))).GetValue<T>(null, null);

		public static IValue<T> GetValue<T>(this IConfigSection section, Func<T> defaultValue)
			=> (section ?? throw new ArgumentNullException(nameof(section))).GetValue<T>(null, defaultValue);

		public static IValue<IReadOnlyList<T>> GetCollection<T>(this IConfigSection section)
			=> (section ?? throw new ArgumentNullException(nameof(section))).GetCollection<T>(null);

		public static IValue<T> GetValue<T>(this IConfigSection config, string key, T defaultValue)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));
			return config.GetValue(key, () => defaultValue);
		}

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
