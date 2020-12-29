using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexxys;
using Lexxys.Configuration;

namespace Lexxys.Test.Con
{
	using Lexxys.Tokenizer2.Xml;
    using System.IO;

    class Tokenizer2Test
	{
		private readonly FileInfo _file;

		public static void Go()
		{
			var source = @"C:\Application\Config\fsadmin.config.txt";
			TextToXmlConverter.Convert(File.ReadAllText(source), OptionHandler, source);
		}

		private static IEnumerable<Lexxys.Xml.XmlLiteNode> OptionHandler(string option, IReadOnlyCollection<string> parameters)
		{
			if (option != "include")
			{
				return null;
			}
			var directory = @"C:\Application\Config";
			var include = parameters?.FirstOrDefault();
			if (String.IsNullOrEmpty(include))
				return null;
			var cl = new ConfigurationLocator(include).Locate(String.IsNullOrEmpty(directory) ? null : new[] { directory }, null);
			if (!cl.IsLocated)
				return null;
			var xs = ConfigurationFactory.FindXmlSource(cl, parameters);
			return xs?.Content;
		}

	}
}
