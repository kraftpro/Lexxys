using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;


#nullable enable

namespace Lexxys.Configuration
{
	using Xml;

	internal class ConfigurationSource
	{
		internal static IEnumerable<XmlLiteNode>? HandleInclude(string logSource, IReadOnlyCollection<string> parameters, string? directory, ref List<string>? includes, EventHandler<ConfigurationEventArgs> eventHandler)
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
			var xs = ConfigurationFactory.TryCreateXmlConfigurationSource(inc, parameters);
			if (xs == null)
				return null;

			xs.Changed += eventHandler;

			if (includes == null)
				includes = new List<string>();
			if (!includes.Contains(inc.ToString()))
			{
				includes.Add(inc.ToString());
			}
			Config.LogConfigurationEvent(logSource, SR.ConfigurationFileIncluded(inc.ToString()));
			return xs.Content;
		}

		internal static string GetContent(Uri location, string? currentDirectory = null)
		{
			if (!location.IsAbsoluteUri || location.IsFile)
			{
				string path = location.IsAbsoluteUri ? location.LocalPath : location.OriginalString;
				return File.ReadAllText(String.IsNullOrEmpty(currentDirectory) ? path: Path.Combine(currentDirectory, path));
			}

#if NETFRAMEWORK
			using (var c = new WebClient())
			{
				return c.DownloadString(location);
			}
#else
			if (location.Scheme == Uri.UriSchemeHttp || location.Scheme == Uri.UriSchemeHttps)
			{
				using var http = new System.Net.Http.HttpClient();
				return http.GetStringAsync(location).GetAwaiter().GetResult();
			}
			throw new NotSupportedException($"Specified url \"{location}\" is not supported");
#endif
		}
	}
}
