using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Xml;
using System.IO;

#nullable enable

namespace Lexxys.Configuration
{
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
			var cl = Config.LocateFile(include!, String.IsNullOrEmpty(directory) ? null: new[] { directory! }, null);
			if (cl == null)
			{
				Config.LogConfigurationEvent(logSource, SR.OptionIncludeFileNotFound(include, directory));
				return null;
			}
			var xs = ConfigurationFactory.TryCreateXmlConfigurationSource(cl, parameters);
			if (xs == null)
				return null;

			xs.Changed += eventHandler;

			if (includes == null)
				includes = new List<string>();
			if (!includes.Contains(cl.AbsoluteUri))
			{
				includes.Add(cl.AbsoluteUri);
			}
			Config.LogConfigurationEvent(logSource, SR.ConfigurationFileIncluded(cl.AbsoluteUri));
			return xs.Content;
		}

		internal static string GetContent(Uri location, string? currentDirectory = null)
		{
			if (!location.IsAbsoluteUri || location.IsFile)
			{
				string path = location.IsAbsoluteUri ? location.LocalPath : location.OriginalString;
				return File.ReadAllText(String.IsNullOrEmpty(currentDirectory) ? path: Path.Combine(currentDirectory, path));
			}

#if !NETFRAMEWORK
			if (location.Scheme == Uri.UriSchemeHttp || location.Scheme == Uri.UriSchemeHttps)
				return new System.Net.Http.HttpClient().GetStringAsync(location).GetAwaiter().GetResult();
			throw new NotSupportedException($"Specified url \"{location}\" is not supported");
#else
			using (var c = new WebClient())
			{
				return c.DownloadString(location);
			}
#endif
		}
	}
}
