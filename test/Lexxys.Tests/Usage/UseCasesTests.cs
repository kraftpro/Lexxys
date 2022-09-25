using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Usage
{
	using Lexxys.Configuration;

	[TestClass]
	public class UseCasesTests
	{
		[TestMethod]
		public void UseCase1()
		{
			string listNode = "usage.listNode";
			string timeoutNode = "usage.timeout";
			string settingsNode = "usage.app-settings";
			//TODO: Lexxys.Logging.LogRecordsService.RegisterFactory();

			var l1 = StaticServices.Create<ILogging<UseCases>>();
			var l2 = StaticServices.Create<ILogging>(nameof(UseCases));
			l1.Info("L1");
			l2.Info("L2");
			var c1 = StaticServices.Create<IConfigSection>();
			var v1 = c1.GetValue<List<string>>(listNode);
			var v1v = v1.Value;
			var v2 = c1.GetCollection<string>(listNode);
			var v2v = v2.Value;

			var l0a = StaticServices.Create<ILogging>(nameof(UseCases));
			var l1a = StaticServices.GetLogger(nameof(UseCases));
			var l2b = StaticServices.Logger.Create(nameof(UseCases));

			var l0b = StaticServices.Create<ILogging<UseCases>>();
			var l1b = StaticServices.GetLogger<UseCases>();
			var l2a = StaticServices.Logger.Create<UseCases>();

			var c1a = StaticServices.Config();
			var c1b = StaticServices.Config().GetSection(listNode);

			var c2a = StaticServices.Config().GetValue<TimeSpan>(timeoutNode);
			var c2b = StaticServices.Config().GetSection(settingsNode).GetCollection<string>("proxy");

			var cf = StaticServices.ConfigService();
			IConfigProvider provider = new EnvironmentConfigurationProvider();
			cf.AddConfiguration(provider, true);

			var c3a = StaticServices.Config;
		}
	}
}
