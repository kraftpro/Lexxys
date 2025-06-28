using System.Data.Common;

#nullable enable

// ReSharper disable all
namespace Lexxys.Tests.Usage
{
	using Lexxys.Configuration;
	using Lexxys.Data;

	internal class UseCases
	{

		public static void NullableDc(DbCommand command, IDataContext context)
		{
			{
				var i1 = Dc.ValueMapper<int>(command);
				var i2 = Dc.ValueMapperAsync<int>(command).Result;
				var s1 = Dc.ValueMapper<string>(command);
				var s2 = Dc.ValueMapperAsync<string>(command).Result;
			}
			{
				int i1 = context.Map<int>(c => default, SqlPart.Empty);
				int i2 = context.MapAsync<int>(c => Task.FromResult<int>(default), SqlPart.Empty).Result;
				string s1 = context.Map<string>(c => "", SqlPart.Empty);
				string s2 = context.MapAsync<string>(c => Task.FromResult<string>(""), SqlPart.Empty).Result;
				string? t1 = context.Map<string?>(c => default, SqlPart.Empty);
				string? t2 = context.MapAsync<string?>(c => Task.FromResult<string?>(default), SqlPart.Empty).Result;
			}
			{
				int i1 = context.GetValue<int>(SqlPart.Empty);
				int i2 = context.GetValueAsync<int>(SqlPart.Empty).Result;
				string? s1 = context.GetValue<string>(SqlPart.Empty);
				string? s2 = context.GetValueAsync<string>(SqlPart.Empty).Result;
			}
		}

		public static void StaticSvcs()
		{
			string listNode = "usage.listNode";
			string timeoutNode = "usage.timeout";
			string settingsNode = "usage.app-settings";
			//TODO: Lexxys.Logging.LogRecordsService.RegisterFactory();

			var l1 = Statics.GetLogger<ILogging<UseCases>>();
			var l2 = Statics.GetLogger(nameof(UseCases));
			l1.Info("L1");
			l2.Info("L2");
			var c1 = Statics.GetService<IConfigSection>();
			var v1 = c1.GetValue<List<string>>(listNode);
			var v1v = v1.Value;
			var v2 = c1.GetCollection<string>(listNode);
			var v2v = v2.Value;

			var l0a = Statics.TryGetLogger(nameof(UseCases));

			var l1a = Statics.TryGetLogger<UseCases>();

			var c1a = Statics.TryGetService<IConfigSection>();
			var c1b = Statics.GetService<IConfigSection>().GetSection(listNode);

			var c2a = Statics.GetService<IConfigSection>().GetValue<TimeSpan>(timeoutNode);
			var c2b = Statics.GetService<IConfigSection>().GetSection(settingsNode).GetCollection<string>("proxy");
			
			var cf = Statics.GetService<IConfigService>();
			IConfigSource provider = new EnvironmentConfigurationProvider();
			cf.AddConfiguration(provider, 0);

		}
	}
}
