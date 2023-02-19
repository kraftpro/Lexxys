// Lexxys Infrastructural library.
// file: DataValidatorTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Data
{
	using Lexxys.Data;
	using Lexxys.Testing;

	/// <summary>
	/// Summary description for DataValidatorTest
	/// </summary>
	[TestClass]
	public class DataValidatorTest
	{
		private readonly string[] goodURLs =
		{
			"http://www.lostand.com:8081/master/login?name=Ivan&pass=12%34_Sdf$#top",
			"HttpS://admin:pass@lostand.uz",
			"foo.com",
			"foo.com/",
			"www.foo-bar.kg/%20dir%21/here",
			"www.foo.com?",
			"www.foo.com/?",
			"www.foo.com/?argument",
			"www.foo.com/?arg=value",
			"www.foo.com?arg=value&more=data",
			"www.foo.com////folder./folder..//file.xyz?arg=value&more=data",
			"21.1.0.254////folder./folder..//file.xyz?arg=value&more=data",
			"192.169.1.1",
			"172.15.254.254",
			"172.32.0.1",
		};

		private readonly string[] badURLs =
		{
			"http://www.lostandcontoso.com:80810/Chapter/login?name=Ivan&pass=12%34_Sdf$#top",
			"HttpS://admin:pass@lostandcontoso.u",
			"admin:pass@contoso.com",
			"foo..com",
			"foo.com\\/",
			"www.foo-bar.kg/%2Odir%21/here",
			"www.foo.com ?",
			"www.foo.com /?",
			"www.foo$.com/?arg%",
			"www.foo.com?arg=%2",
			"www.foo.com?arg=%0 &more=data",
			"www.foo.com////fo\nlder./folder..//file.xyz?arg=value&more=data",
			"127.1.0.254/folder/folder/file.xyz?arg=value&more=data",
			"192.168.1.1",
			"172.16.0.1",
			"172.31.0.1",
			"1.1.1.255",
			"255.0.0.0",
			"23.3.43.400",
			"22",
			"121.65.77.1234",
			"121.65.a.10",
		};

		[TestMethod]
		public void TestHttpUrl()
		{
			//well-formed URLs
			foreach (string url in goodURLs)
			{
				if (!UrlValueValidator.IsLegalHttpUrl(url))
					Assert.Fail("Well-formed URL was not recognized as valid. URL = " + url);
			}

			//bad URLS
			//well-formed URLs
			foreach (string url in badURLs)
			{
				if (UrlValueValidator.IsLegalHttpUrl(url))
					Assert.Fail("Invalid URL was interpreted as well-formed one. URL = " + url);
			}
		}

		[TestMethod]
		public void TestSsn()
		{
			const string IsSsn = "value {0} is valid SSN";
			const string IsNotSsn = "value {0} is not valid SSN";
			string v;
			v = "666456789"; Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
			v = "123456789"; Assert.IsTrue(Check.SsnCode(v, false), IsNotSsn, v);
			
			v = "0000"; Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
			v = "0001"; Assert.IsTrue(Check.SsnCode(v, false), IsNotSsn, v);
			v = "6664"; Assert.IsTrue(Check.SsnCode(v, false), IsNotSsn, v);
			v = "9876"; Assert.IsTrue(Check.SsnCode(v, false), IsNotSsn, v);

			v = R.Digit(4+1); Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
			v = R.Digit(4-1); Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
			v = R.Digit(9+1); Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
			v = R.Digit(9-1); Assert.IsFalse(Check.SsnCode(v, false), IsSsn, v);
		}
	}
}
