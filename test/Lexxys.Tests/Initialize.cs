namespace Lexxys.Tests
{
	[TestClass]
	public class Initialize
	{

		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			Statics.AddServices(o => o
				.AddConfigService());
		}
	}
}
