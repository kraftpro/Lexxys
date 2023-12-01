// Lexxys Infrastructural library.
// file: SystemConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace Lexxys.Configuration;

using Xml;

[DebuggerDisplay("[System.Configuration]")]
public sealed class SystemConfigurationProvider: IConfigSource
{
	private static readonly Uri Uri = new Uri("system:configuration");

	public int Version => 1;

	public object? GetValue(string key, Type objectType)
	{
		if (objectType == null)
			throw new ArgumentNullException(nameof(objectType));
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		string value = ConfigurationManager.AppSettings[key];
		if (value != null && Strings.TryGetValue(value, objectType, out object? result))
			return result;
		if (key.StartsWith("connection.", StringComparison.OrdinalIgnoreCase) && objectType == typeof(string))
			return ConfigurationManager.ConnectionStrings[key.Substring(11)];
		result = ConfigurationManager.GetSection(key);
		if (objectType.IsInstanceOfType(result))
			return result;
		return null;
	}

	public IReadOnlyList<T> GetList<T>(string key)
	{
		if (key == null)
			throw new ArgumentNullException(nameof(key));

		string[]? values = ConfigurationManager.AppSettings.GetValues(key);
		if (values != null)
		{
			var result = new List<T>();
			foreach (string value in values)
			{
				if (Strings.TryGetValue(value, typeof(T), out object? x))
				{
					result.Add((T)x!);
				}
			}
			if (result.Count > 0)
				return ReadOnly.Wrap(result)!;
		}
		if (key.StartsWith("connection.", StringComparison.OrdinalIgnoreCase))
		{
			var cc = ConfigurationManager.ConnectionStrings;
			if (cc.Count > 0)
			{
				if (typeof(T) == typeof(string))
				{
					var result = new List<T>();
					for (int i = 0; i < cc.Count; ++i)
					{
						result.Add(Tools.Cast<T>(cc[i].ConnectionString));
					}
					return ReadOnly.Wrap(result)!;
				}
				if (typeof(T) == typeof(ConnectionStringSettings))
				{
					var result = new List<T>();
					for (int i = 0; i < cc.Count; ++i)
					{
						result.Add(Tools.Cast<T>(cc[i]));
					}
					return ReadOnly.Wrap(result)!;
				}
				if (typeof(T) == typeof(Data.ConnectionStringInfo))
				{
					var result = new List<T>();
					for (int i = 0; i < cc.Count; ++i)
					{
						result.Add(Tools.Cast<T>(new Data.ConnectionStringInfo(cc[i].ConnectionString)));
					}
					return ReadOnly.Wrap(result)!;
				}
				return Array.Empty<T>();
			}
		}
		if (ConfigurationManager.GetSection(key) is List<T> list)
			return ReadOnly.Wrap(list)!;
		if (ConfigurationManager.GetSection(key) is IEnumerable<T> ienum)
			return ReadOnly.WrapCopy(ienum)!;
		return Array.Empty<T>();
	}

	bool IEquatable<IConfigSource>.Equals(IConfigSource other) => other is SystemConfigurationProvider;

	public event EventHandler<ConfigurationEventArgs>? Changed
	{
		add { }
		remove { }
	}
}
#endif
