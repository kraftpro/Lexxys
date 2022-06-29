// Lexxys Infrastructural library.
// file: ConfigurationFactory.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Lexxys.Configuration
{
	internal static class ConfigurationFactory
	{
		internal static IConfigProvider? TryCreateProvider(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			var arguments = new object?[] { location, parameters };

			foreach (var constructor in Factory.Constructors(typeof(IConfigProvider), "Create", __locationType2))
			{
				try
				{
					var obj = constructor.Invoke(null, arguments);
					if (obj is IConfigProvider source)
						return source;
				}
				catch (Exception flaw)
				{
					Config.LogConfigurationError($"{nameof(TryCreateProvider)} from {location}", flaw);
				}
			}
			return null;
		}

		internal static IXmlConfigurationSource? TryCreateXmlConfigurationSource(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			var arguments = new object?[] { location, parameters };
			foreach (var constructor in Factory.Constructors(typeof(IXmlConfigurationSource), "Create", __locationType2))
			{
				try
				{
					var obj = constructor.Invoke(null, arguments);
					if (obj is IXmlConfigurationSource source)
						return source;
				}
				catch (Exception flaw)
				{
					Config.LogConfigurationError($"{nameof(TryCreateXmlConfigurationSource)} from {location}", flaw);
				}
			}
			return null;
		}
		private static readonly Type[] __locationType2 = { typeof(Uri), typeof(IReadOnlyCollection<string>) };
	}
}
