// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
// Re Sharper disable ConditionIsAlwaysTrueOrFalse

namespace Lexxys;

/// <summary>
/// 
/// </summary>
public class BlobStorage: IBlobStorage
{
	private readonly Dictionary<string, List<IBlobStorageProvider>> _schemes = new Dictionary<string, List<IBlobStorageProvider>>();
	private readonly List<IBlobStorageProvider> _providers = new List<IBlobStorageProvider>();

	/// <summary>
	/// Get <see cref="IBlobStorageProvider"/> for the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">Blob location</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public IBlobStorageProvider? TryGetProvider(Uri location)
	{
		if (location is null)
			throw new ArgumentNullException(nameof(location));

		if (!_schemes.TryGetValue(location.Scheme, out var providers))
			providers = _providers;

		return providers.FirstOrDefault(o => o.CanOpen(location));
	}

	/// <summary>
	/// Registers a blob storage provider.
	/// </summary>
	/// <param name="provider">Blob storage provider</param>
	/// <exception cref="ArgumentNullException"><paramref name="provider"/> is null</exception>
	public void Register(IBlobStorageProvider provider)
	{
		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		foreach (var scheme in provider.SupportedSchemes)
		{
			if (!_schemes.TryGetValue(scheme, out var list))
				_schemes.Add(scheme, list = new List<IBlobStorageProvider>());
			list.Add(provider);
		}
	}
}


