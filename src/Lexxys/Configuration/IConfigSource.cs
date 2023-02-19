// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.Configuration
{
	public interface IConfigSource
	{
		int Version { get; }
		event EventHandler<ConfigurationEventArgs>? Changed;

		object? GetValue(string key, Type objectType);
		IReadOnlyList<T> GetList<T>(string key);
	}

	public interface IDisposableConfigSource: IConfigSource, IDisposable
	{
	}
}
