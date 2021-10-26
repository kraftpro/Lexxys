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
		internal static IConfigurationProvider? FindProvider(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			var arguments = new object?[] { location, parameters };
			return Factory.Constructors(typeof(IConfigurationProvider), "Create", __locationType2)
				.Select(m => m.Invoke(null, arguments) as IConfigurationProvider).FirstOrDefault(o => o != null);
		}

		internal static IXmlConfigurationSource? FindXmlSource(Uri location, IReadOnlyCollection<string>? parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			var arguments = new object?[] { location, parameters };
			return Factory.Constructors(typeof(IXmlConfigurationSource), "Create", __locationType2)
				.Select(m => m.Invoke(null, arguments) as IXmlConfigurationSource).FirstOrDefault(o => o != null);
		}
		private static readonly Type[] __locationType2 = { typeof(Uri), typeof(IReadOnlyCollection<string>) };
	}
}
