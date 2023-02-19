// Lexxys Infrastructural library.
// file: FactoryTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Tools
{
	[TestClass()]
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
			var x = Config.AddConfiguration("application.config.txt");
			if (!x)
				Debugger.Break();
			Assert.IsTrue(x);
			var y = Config.Current.GetCollection<Lexxys.Xml.XmlLiteNode>(Lexxys.Factory.ConfigurationSynonyms);
			if (y.Value is null || y.Value.Count == 0)
				Debugger.Break();
			Assert.IsNotNull(y.Value);
			Assert.IsTrue(y.Value.Count > 0);
		}

		[TestMethod()]
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
