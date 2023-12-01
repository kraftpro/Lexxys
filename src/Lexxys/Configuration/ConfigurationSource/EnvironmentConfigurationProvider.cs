// Lexxys Infrastructural library.
// file: EnvironmentConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Configuration;

using Xml;

class EnvironmentConfigurationProvider: IConfigSource
{
	private static readonly Uri Uri = new Uri("system:environment");

	public int Version => 1;

	public object? GetValue(string reference, Type returnType)
	{
		if (reference == null)
			throw new ArgumentNullException(nameof(reference));
		if (reference.StartsWith("env::", StringComparison.OrdinalIgnoreCase))
			reference = reference.Substring(5);
		return Strings.TryGetValue(Environment.GetEnvironmentVariable(reference), returnType, out var result) ? result: null;
	}

	public IReadOnlyList<T> GetList<T>(string reference)
	{
		if (reference == null)
			throw new ArgumentNullException(nameof(reference));

		if (reference.StartsWith("env::", StringComparison.OrdinalIgnoreCase))
			reference = reference.Substring(5);
		if (Strings.TryGetValue<T>(Environment.GetEnvironmentVariable(reference), out var value))
			return ReadOnly.Wrap(new[] { value! })!;
		return Array.Empty<T>();
	}

	bool IEquatable<IConfigSource>.Equals(IConfigSource? other) => other is EnvironmentConfigurationProvider;

#pragma warning disable CS0067
	public event EventHandler<ConfigurationEventArgs>? Changed;
}


