using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration
{
	internal static class ConfigScanner
	{
		private const string InitialLocationKey = "configuration";
		public const string AnonymousConfigurationFile = "application";

		public static List<IConfigProvider> ScanConfigFiles(bool system = false)
		{
			var configFiles = new List<IConfigProvider>();

			if (system)
			{
				configFiles.Add(new EnvironmentConfigurationProvider());
#if !NETCOREAPP
				configFiles.Add(new SystemConfigurationProvider());
#endif
			}

			var cc0 = GetInitialConfigurationsLocations();
			foreach (var c in cc0)
			{
				configFiles.AddRange(FindConfigurationFiles(c));
			}
			Lxx.OnConfigurationInitialized(null, new ConfigurationEventArgs());

			return configFiles;
		}

		private static IEnumerable<IConfigProvider> FindConfigurationFiles(string file)
		{
			var list = new List<IConfigProvider>();
			if (File.Exists(file + ".config.txt"))
				list.Add(default);
			if (File.Exists(file + ".config.xml"))
				list.Add(default);
			if (File.Exists(file + ".config.ini"))
				list.Add(default);
			if (File.Exists(file + ".config.json"))
				list.Add(default);
			return list;
		}



		private static IEnumerable<Uri> GetConfiguredLocations()
		{
#if NETCOREAPP
			return Array.Empty<Uri>();
#else
			string[] ss = ConfigurationManager.AppSettings.GetValues(InitialLocationKey);
			return ss is null || ss.Length == 0 ?
				Array.Empty<Uri>():
				ss.Select(o => new Uri(o, UriKind.RelativeOrAbsolute)).Where(o => o.IsAbsoluteUri);
#endif
		}

		private static IEnumerable<string> GetInitialConfigurationsLocations()
		{
			var cc = new OrderedSet<string>(StringComparer.OrdinalIgnoreCase);
			var appconfig = Path.Combine(AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory, AnonymousConfigurationFile);
			cc.Add(appconfig);

			cc.AddRange(Factory.DomainAssemblies
				.Select(o => o == null || o.IsDynamic ? null: o.GetName().CodeBase.TrimToNull())
				.Where(o => o != null)
				.Select(FullNameWitoutExtension));

			return cc;

			static string FullNameWitoutExtension(string name)
			{
				var s = Path.GetFullPath(name);
				var e = Path.GetExtension(s);
				return e.Length == 0 ? s: s.Substring(0, s.Length - e.Length);
			}
		}
	}
}
