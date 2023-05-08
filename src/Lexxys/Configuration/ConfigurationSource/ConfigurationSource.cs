namespace Lexxys.Configuration;

using Xml;

internal static class ConfigurationSource
{
	internal static IEnumerable<XmlLiteNode>? HandleInclude(string logSource, IReadOnlyCollection<string>? parameters, string? directory, ref List<string>? includes, EventHandler<ConfigurationEventArgs> eventHandler)
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
		foreach (var constructor in Factory.Constructors(typeof(IXmlConfigurationSource), "TryCreate", __locationType2))
		{
			try
			{
				var obj = constructor.Invoke(null, arguments);
				if (obj is IXmlConfigurationSource source)
					return source;
			}
#pragma warning disable CA1031 // Ignore all the errors.
			catch (Exception flaw)
			{
				Config.LogConfigurationError($"{nameof(TryCreateXmlConfigurationSource)} from {location}", flaw);
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}
		return null;
	}
	private static readonly Type[] __locationType2 = { typeof(Uri), typeof(IReadOnlyCollection<string>) };
}
