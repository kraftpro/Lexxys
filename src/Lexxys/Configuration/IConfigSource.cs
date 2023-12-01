// Lexxys Infrastructural library.
// file: Config.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Configuration;

public interface IConfigSource: IEquatable<IConfigSource>
{
	event EventHandler<ConfigurationEventArgs>? Changed;
	int Version { get; }

	object? GetValue(string key, Type objectType);

	IReadOnlyList<T> GetList<T>(string key);
}
