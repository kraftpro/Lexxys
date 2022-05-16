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
			var l1 = StaticServices.Create<ILogging<UseCases>>();
			var l2 = StaticServices.Create<ILogging>(nameof(UseCases));
			l1.Info("L1");
			l2.Info("L2");
			var c1 = StaticServices.Create<IConfigSection>();
			var v1 = c1.GetValue<List<string>>("node");
			var v1v = v1.Value;
			var v2 = c1.GetCollection<string>("node");
			var v2v = v2.Value;

			var d1 = StaticServices.Create<string>();


			var l0a = StaticServices.Create<ILogging>(nameof(UseCases));
			var l1a = StaticServices.GetLogger(nameof(UseCases));
			var l2b = StaticServices.Logger.Create(nameof(UseCases));

			var l0b = StaticServices.Create<ILogging<UseCases>>();
			var l1b = StaticServices.GetLogger<UseCases>();
			var l2a = StaticServices.Logger.Create<UseCases>();

			var c1a = StaticServices.GetConfig();
			var c1b = StaticServices.GetConfig("node");

			var c2a = StaticServices.GetConfig().GetValue<TimeSpan>("timeout");
			var c2b = StaticServices.GetConfig("app-settings").GetCollection<string>("proxy");
			
			var cf = StaticServices.GetConfigFactory();
			IConfigProvider provider = null!;
			cf.AddConfigurationProvider(provider, true);

			StaticServices.ConfigFactory.AddConfigurationProvider(provider);

			var c3a = StaticServices.Config;

		}
	}
}
