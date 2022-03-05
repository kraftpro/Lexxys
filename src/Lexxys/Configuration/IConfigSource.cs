// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

#nullable enable

namespace Lexxys.Configuration
{
	public interface IConfigSource
	{
		event EventHandler<ConfigurationEventArgs>? Changed;
		int Version { get; }
		IReadOnlyList<T> GetList<T>(string key);
		object? GetValue(string key, Type objectType);
	}
}
