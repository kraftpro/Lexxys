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

			var l1 = Statics.TryGetLogger<ILogging<UseCases>>();
			var l2 = Statics.TryGetLogger(nameof(UseCases));
			l1?.Info("L1");
			l2?.Info("L2");
			var c1 = Statics.TryGetService<IConfigSection>();
			var v1 = c1?.GetValue<List<string>>(listNode);
			var v1v = v1?.Value;
			var v2 = c1?.GetCollection<string>(listNode);
			var v2v = v2?.Value;

			var l0a = Statics.TryGetLogger(nameof(UseCases));

			var l1a = Statics.TryGetLogger<UseCases>();

			var c1a = Statics.TryGetService<IConfigSection>();
			var c1b = Statics.TryGetService<IConfigSection>()?.GetSection(listNode);

			var c2a = Statics.TryGetService<IConfigSection>()?.GetValue<TimeSpan>(timeoutNode);
			var c2b = Statics.TryGetService<IConfigSection>()?.GetSection(settingsNode)?.GetCollection<string>("proxy");

			var cf = Statics.TryGetService<IConfigService>();
			IConfigProvider provider = new EnvironmentConfigurationProvider();
			cf?.AddConfiguration(provider, true);
		}
	}
}
