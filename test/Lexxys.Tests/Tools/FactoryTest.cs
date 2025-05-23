// Lexxys Infrastructural library.
// file: FactoryTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Diagnostics;

namespace Lexxys.Tests.Tools
{
	[TestClass]
	[DeploymentItem("application.config.txt")]
	public class FactoryTest
	{
		public struct ZeroElement
		{
		}

		private struct PrivateZeroElement
		{
		}

		static FactoryTest()
		{
			Config.Current.SetCollection(Factory.ConfigurationSynonyms, new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("Cardinal", "long"),
				new KeyValuePair<string, string>("ID", "int"),
				new KeyValuePair<string, string>("X", "void"),
				new KeyValuePair<string, string>("FT", typeof(FactoryTest).FullName),
				new KeyValuePair<string, string>("Z", typeof(ZeroElement).FullName),
				new KeyValuePair<string, string>("Z0", typeof(PrivateZeroElement).FullName)
			});
		}

		[TestMethod]
		[DataRow("Id", typeof(int))]
		[DataRow("Id?", typeof(int?))]
		[DataRow("int", typeof(int))]
		[DataRow(" system.int32", typeof(int))]
		[DataRow("ULong ? ", typeof(ulong?))]
		[DataRow("date ? ", typeof(DateTime?))]
		[DataRow("FactoryTest", typeof(FactoryTest))]
		[DataRow("FactoryTest?", typeof(FactoryTest))]
		[DataRow("X", typeof(void))]
		[DataRow("X?", typeof(void))]
		[DataRow("FT", typeof(FactoryTest))]
		[DataRow("FT?", typeof(FactoryTest))]
		[DataRow("Z", typeof(FactoryTest.ZeroElement))]
		[DataRow("Z?", typeof(FactoryTest.ZeroElement?))]
		[DataRow("Z??", typeof(FactoryTest.ZeroElement?))]
		[DataRow("Z0", typeof(FactoryTest.PrivateZeroElement))]
		[DataRow("Z0?", typeof(FactoryTest.PrivateZeroElement?))]
		[DataRow("Fiction", null)]
		public void GetTypeTest(string typeName, Type expected)
		{
			Type actual = Factory.GetType(typeName);
			Assert.AreEqual(expected, actual);
		}

	}
}
