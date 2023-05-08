// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

// ReSharper disable CheckNamespace
using Microsoft.Extensions.Options;

namespace Lexxys;

using Configuration;

public static class ConfigSectionExtensions
{
	public static IValue<T> GetValue<T>(this IConfigSection section)
		=> (section ?? throw new ArgumentNullException(nameof(section))).GetValue<T>(null);

	public static IValue<T> GetValue<T>(this IConfigSection section, Func<T> defaultValue)
		=> (section ?? throw new ArgumentNullException(nameof(section))).GetValue(null, defaultValue);

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

	public static IValue<IReadOnlyList<T>> GetCollection<T>(this IConfigSection section)
		=> (section ?? throw new ArgumentNullException(nameof(section))).GetCollection<T>(null);

	public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, Func<T>? defaultValue = null) where T: class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<T>(() => config.GetValue(key, defaultValue).Value);
	}

	public static IOptions<T> GetOptions<T>(this IConfigSection config) where T : class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<T>(() => config.GetValue<T>(null).Value);
	}

	public static IOptions<T> GetOptions<T>(this IConfigSection config, Func<T>? defaultValue) where T : class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<T>(() => config.GetValue(null, defaultValue).Value);
	}

	public static IOptions<T> GetOptions<T>(this IConfigSection config, string key, T defaultValue) where T : class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<T>(() => config.GetValue(key, () => defaultValue).Value);
	}

	public static IOptions<IReadOnlyList<T>> GetOptionCollections<T>(this IConfigSection config, string key) where T : class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<IReadOnlyList<T>>(() => config.GetCollection<T>(key).Value);
	}

	public static IOptions<IReadOnlyList<T>> GetOptionCollections<T>(this IConfigSection config) where T : class
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));
		return new OptOut<IReadOnlyList<T>>(() => config.GetCollection<T>(null).Value);
	}
}
