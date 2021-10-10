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

namespace Lexxys.Configuration
{
	public interface IConfigSection
	{
		event EventHandler<ConfigurationEventArgs> Changed;

		int Version { get; }

		IConfigSection Section(string key);

		IVersionedValue<T> GetSection<T>(string key, Func<T> defaultValue);
		IVersionedValue<IReadOnlyList<T>> GetSectionList<T>(string key);
	}
}
