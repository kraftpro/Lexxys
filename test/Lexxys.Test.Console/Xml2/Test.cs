using Lexxys;
using Lexxys.Xml;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lexxys.Test.Con.Core.Xml2
{
	class Test
	{
		public static void Go()
		{
			Lexxys.Config.AddConfiguration(@"C:\Application\Config\fsadmin.config.txt");
			var node = Lexxys.Config.GetValue<XmlLiteNode>("FsAdmin");
			//var node = Lexxys.Xml.TextToXmlConverter.ConvertLite(File.ReadAllText(@"C:\Application\Config\fsadmin.config.txt"), @"C:\Application\Config\fsadmin.config.txt", true)
			//	.FirstOrDefault();
			var config0 = Config.GetValue("FsAdmin", () => new FoundationSource.Admin.Configuration());
			//File.WriteAllText(@"c:\tmp\confing0.txt", DumpWriter.Create().Pretty(tab: "  ").Dump(config0).ToString());
			var config = XmlTools.GetValue<FoundationSource.Admin.Configuration>(node, null);
			File.WriteAllText(@"c:\tmp\confing1.txt", DumpWriter.Create().Pretty(tab: "  ").Dump(config).ToString());
			var web = XmlLiteNode.FromXml(File.ReadAllText(@"C:\Projects\FSAdmin\src\FsAdminWeb.root\FsAdminWeb\Web.config"));
			var rtm = XmlTools.GetValue<HttpRuntimeSection>(web.Element("system.web").Element("httpRuntime"), null);

			var ext = TextToXmlConverter.ConvertLite(File.ReadAllText(@"C:\Projects\FSAdmin\src\Externs.root\Externs\Externs.config.txt")).FirstOrDefault();
			var map = XmlTools.GetValue(ext.Element("externs"), new Dictionary<string, string>());

			var app = config.Application;
			Console.WriteLine("app:");
			Console.WriteLine(DumpWriter.Create().Pretty(tab:"  ").Dump(app));
		}
	}


	public sealed class HttpRuntimeSection
	{
		public TimeSpan ExecutionTimeout { get; set; }
		public int MaxRequestLength { get; set; }
		public int RequestLengthDiskThreshold { get; set; }
		public bool UseFullyQualifiedRedirectUrl  { get; set; }
		public int MinFreeThreads { get; set; }
		public int MinLocalRequestFreeThreads { get; set; }
		public int AppRequestQueueLimit { get; set; }
		public bool EnableKernelOutputCache { get; set; }
		public bool EnableVersionHeader { get; set; }
		public bool ApartmentThreading { get; set; }
		public bool RequireRootedSaveAsPath { get; set; }
		public bool Enable { get; set; }
		public string TargetFramework { get; set; }
		public bool SendCacheControlHeader { get; set; }
		public TimeSpan DefaultRegexMatchTimeout { get; set; }
		public TimeSpan ShutdownTimeout { get; set; }
		public TimeSpan DelayNotificationTimeout { get; set; }
		public int WaitChangeNotification { get; set; }
		public int MaxWaitChangeNotification { get; set; }
		public bool EnableHeaderChecking { get; set; }
		public string EncoderType { get; set; }
		public Version RequestValidationMode { get; set; }
		public string RequestValidationType { get; set; }
		public string RequestPathInvalidCharacters { get; set; }
		public int MaxUrlLength { get; set; }
		public int MaxQueryStringLength { get; set; }
		public bool RelaxedUrlToFileSystemMapping { get; set; }
		public bool AllowDynamicModuleRegistration { get; set; }
		internal int MaxRequestLengthBytes { get; }
		internal int RequestLengthDiskThresholdBytes { get; }
		internal string VersionHeader { get; }

		static HttpRuntimeSection()
		{
		}
	}

}
