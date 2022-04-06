using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
