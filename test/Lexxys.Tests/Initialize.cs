using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests
{
	[TestClass]
	public static class Initialize
	{

		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			Statics.AddServices(o => o
				.AddConfigService());
		}
	}
}
