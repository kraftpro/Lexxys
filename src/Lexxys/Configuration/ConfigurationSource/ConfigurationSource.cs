using System.Reflection;

namespace Lexxys.Configuration;

using Xml;

internal static class ConfigurationSource
{
	internal static IEnumerable<IXmlReadOnlyNode>? HandleInclude(string logSource, IReadOnlyCollection<string>? parameters, string? directory, ref List<string>? includes, EventHandler<ConfigurationEventArgs> eventHandler)
	{
		var include = parameters?.FirstOrDefault();
		if (String.IsNullOrEmpty(include))
		{
			Config.LogConfigurationEvent(logSource, SR.OptionIncludeFileNotFound(null, directory));
			return null;
		}
		var incs = Config.GetLocalFiles(include!, String.IsNullOrEmpty(directory) ? null: new[] { directory! });
		if (incs.Length != 1)
		{
			Config.LogConfigurationEvent(logSource, SR.OptionIncludeFileNotFound(include, directory));
			return null;
		}
		var inc = incs[0];
		var xs = TryCreateXmlConfigurationSource(inc, parameters);
		if (xs == null)
			return null;

		xs.Changed += eventHandler;

		includes ??= new List<string>();
		if (!includes.Contains(inc.ToString()))
		{
			includes.Add(inc.ToString());
		}
		Config.LogConfigurationEvent(logSource, SR.ConfigurationFileIncluded(inc.ToString()));
		return xs.Content;
	}

	internal static IXmlConfigurationSource? TryCreateXmlConfigurationSource(Uri location, IReadOnlyCollection<string>? parameters)
	{
		if (location == null)
			throw new ArgumentNullException(nameof(location));

		var arguments = new object?[] { location, parameters };
		return TryCreate(location, "TryCreate", arguments) ?? TryCreate(location, "Create", arguments);

		static IXmlConfigurationSource? TryCreate(Uri location, string methodName, object?[] arguments)
		{
			foreach (var constructor in Factory.Constructors(typeof(IXmlConfigurationSource), methodName, __locationType2))
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
	}
	private static readonly Type[] __locationType2 = [typeof(Uri), typeof(IReadOnlyCollection<string>)];
}
