// Lexxys Infrastructural library.
// file: ConfigurationFactory.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.Configuration
{
	public static class ConfigurationFactory
	{
		internal static IConfigurationProvider FindProvider(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				throw EX.ArgumentNull("location");

			var arguments = new object[] { location, parameters };
			return Factory.Constructors(typeof(IConfigurationProvider), "Create", __locationType2)
				.Select(m => m.Invoke(null, arguments) as IConfigurationProvider).FirstOrDefault(o => o != null);
		}

		internal static IXmlConfigurationSource FindXmlSource(ConfigurationLocator location, IReadOnlyCollection<string> parameters)
		{
			if (location == null)
				throw EX.ArgumentNull(nameof(location));

			var arguments = new object[] { location, parameters };
			return Factory.Constructors(typeof(IXmlConfigurationSource), "Create", __locationType2)
				.Select(m => m.Invoke(null, arguments) as IXmlConfigurationSource).FirstOrDefault(o => o != null);
		}
		private static readonly Type[] __locationType2 = { typeof(ConfigurationLocator), typeof(IReadOnlyCollection<string>) };

		//internal static IConfigurationProvider FindProvider(ConfigurationLocator location)
		//{
		//	if (location == null)
		//		throw EX.ArgumentNull("location");
		//	return Find<IConfigurationProvider>(location);
		//}

		//internal static IXmlConfigurationSource FindXmlSource(ConfigurationLocator location)
		//{
		//	if (location == null)
		//		throw EX.ArgumentNull("location");
		//	return Find<IXmlConfigurationSource>(location);
		//}

		//private static T Find<T>(ConfigurationLocator parameter)
		//	where T: class
		//{
		//	var parameters = new object[] { parameter };
		//	return Factory.Constructors(typeof(T), "Create", __locationType)
		//		.Select(m => m.Invoke(null, parameters) as T).FirstOrDefault(o => o != null);
		//}
		//private static readonly Type[] __locationType = { typeof(ConfigurationLocator) };
	}
}


