// Lexxys Infrastructural library.
// file: FactoryTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexxys;

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

		public FactoryTest()
		{
			var x = Config.AddConfiguration("application.config.txt");
			Assert.IsNotNull(x);
			var y = Config.Current.GetCollection<Lexxys.Xml.XmlLiteNode>(Lexxys.Factory.ConfigurationSynonyms);
			Assert.IsNotNull(y.Value);
			Assert.IsTrue(y.Value.Count > 0);
		}

		[TestMethod()]
		public void GetTypeTest()
		{
			Dictionary<string, Type> dd = new Dictionary<string, Type>()
				{
					{ "Id",				typeof(int) },
					{ "Id?",			typeof(int?) },
					{ "int",			typeof(int) },
					{ " system.int32",	typeof(int) },
					{ "ULong ? ",		typeof(ulong?) },
					{ "date ? ",		typeof(DateTime?) },
					{ "FT",				typeof(FactoryTest) },
					{ "FT?",			null },
					{ "X",				typeof(void) },
					{ "X?",				null },
					{ "Z",				typeof(FactoryTest.ZeroElement) },
					{ "Z?",				typeof(FactoryTest.ZeroElement?) },
					{ "Z??",			null },
					{ "Z0",				typeof(FactoryTest.PrivateZeroElement) },
					{ "Z0?",			typeof(FactoryTest.PrivateZeroElement?) },
					{ "Fiction",		null },
				};

			foreach (var item in dd)
			{
				Type expected = item.Value;
				Type actual = Factory.GetType(item.Key);
				Assert.AreEqual(expected, actual, item.Key);
			}

		}

	}
}
