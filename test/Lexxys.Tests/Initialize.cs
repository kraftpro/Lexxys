using System.Runtime.CompilerServices;

namespace Lexxys.Tests
{
	internal static class Initialize
	{
		[ModuleInitializer]
		public static void ModuleInitialize()
		{
			Statics.AddServices(o => o
				.AddConfigService());
		}
	}
}
