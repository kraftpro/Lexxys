using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Configuration;
using Lexxys.Data;

#nullable enable

namespace Lexxys.Tests.Usage
{
	internal class UseCases
	{

		public static void NullableDc(DbCommand command, IDataContext context)
		{
			{
				int i1 = Dc.ValueMapper<int>(command);
				int i2 = Dc.ValueMapperAsync<int>(command).Result;
				string? s1 = Dc.ValueMapper<string>(command);
				string? s2 = Dc.ValueMapperAsync<string>(command).Result;
			}
			{
				int i1 = context.Map<int>(c => default, "");
				int i2 = context.MapAsync<int>(c => Task.FromResult<int>(default), "").Result;
				string s1 = context.Map<string>(c => "", "");
				string s2 = context.MapAsync<string>(c => Task.FromResult<string>(""), "").Result;
				string? t1 = context.Map<string?>(c => default, "");
				string? t2 = context.MapAsync<string?>(c => Task.FromResult<string?>(default), "").Result;
			}
			{
				int i1 = context.GetValue<int>("");
				int i2 = context.GetValueAsync<int>("").Result;
				string? s1 = context.GetValue<string>("");
				string? s2 = context.GetValueAsync<string>("").Result;
			}
		}

		public static void StaticSvcs()
		{
			string listNode = "usage.listNode";
			string timeoutNode = "usage.timeout";
			string settingsNode = "usage.app-settings";
			Lexxys.Logging.LogRecordsService.RegisterFactory();

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
