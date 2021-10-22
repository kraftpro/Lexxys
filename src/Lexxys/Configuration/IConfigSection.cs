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
	public interface IConfigSection
	{
		event EventHandler<ConfigurationEventArgs> Changed;

		int Version { get; }

		IConfigSection GetSection(string key);

		void MapPath(string key, string value);

		void DefineValue<T>(string key, T value);
		#nullable disable
		IValue<T> GetValue<T>(string key, Func<T> defaultValue = null);
		#nullable enable

		void DefineCollection<T>(string key, IReadOnlyList<T> value);
		IValue<IReadOnlyList<T>> GetCollection<T>(string key);
	}
}
