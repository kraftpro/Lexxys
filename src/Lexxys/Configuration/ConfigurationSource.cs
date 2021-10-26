using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Xml;

#nullable enable

namespace Lexxys.Configuration
{
	internal class ConfigurationSource
	{
		public static IEnumerable<XmlLiteNode>? HandleInclude(string logSource, IReadOnlyCollection<string> parameters, string? directory, ref List<string>? includes, EventHandler<ConfigurationEventArgs> eventHandler)
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
			var xs = ConfigurationFactory.FindXmlSource(cl, parameters);
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
	}
}
