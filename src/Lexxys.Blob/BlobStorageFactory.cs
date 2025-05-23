// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, ANN, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
// Re Sharper disable ConditionIsAlwaysTrueOrFalse

namespace Lexxys;

/// <summary>
/// Provides methods to retrieve and register blob storage services based on their URI schemes.
/// It allows for service lookup and management.
/// </summary>
public class BlobStorageFactory: IBlobStorageFactory
{
	private readonly Dictionary<string, List<IBlobStorageService>> _schemes = [];
	private readonly List<IBlobStorageService> _providers = [];

	/// <summary>
	/// Get <see cref="IBlobStorageService"/> for the specified <paramref name="location"/>.
	/// </summary>
	/// <param name="location">Blob location</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public IBlobStorageService? TryGetService(Uri location)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		lock (_schemes)
		{
			if (!_schemes.TryGetValue(location.Scheme, out var providers))
				providers = _providers;

			return providers.FirstOrDefault(o => o.CanOpen(location));
		}
	}

	/// <summary>
	/// Registers a blob storage service.
	/// </summary>
	/// <param name="service">Blob storage service</param>
	/// <exception cref="ArgumentNullException"><paramref name="service"/> is null</exception>
	public void Register(IBlobStorageService service)
	{
		if (service == null) throw new ArgumentNullException(nameof(service));

		lock (_schemes)
		{
			foreach (var scheme in service.SupportedSchemes)
			{
				if (!_schemes.TryGetValue(scheme, out var list))
					_schemes.Add(scheme, list = []);
				list.Add(service);
			}
			_providers.Add(service);
		}
	}
}


