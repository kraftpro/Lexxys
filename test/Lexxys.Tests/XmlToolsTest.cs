using Lexxys;
// <copyright file="XmlToolsTest.cs" company="KRAFT Program">Copyright © 2001-2014, KRAFT Program LLC.</copyright>

using System;
using Lexxys.Xml;
//using Microsoft.Pex.Framework;
//using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Xml.Tests
{
	/// <summary>This class contains parameterized unit tests for XmlTools</summary>
	[TestClass]
	//[PexClass(typeof(XmlTools))]
	//[PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
	//[PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
	public partial class XmlToolsTest
	{

		/// <summary>Test stub for GetTernary(String)</summary>
		//[PexMethod]
		//[PexAllowedExceptionFromTypeUnderTest(typeof(FormatException))]
		public Ternary GetTernaryTest(string value)
		{
			string[] xtrue = new string[] { "TRUE", "ON", "YES", "1", "GRANT" };
			string[] xfalse = new string[] { "FALSE", "OFF", "NO", "0", "DENY" };

			Ternary result = XmlTools.GetTernary(value);

			if (value == null)
				Assert.AreEqual(Ternary.Unknown, result);
			else if (Array.IndexOf(xtrue, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.True, result);
			else if (Array.IndexOf(xfalse, value.Trim().ToUpperInvariant()) >= 0)
				Assert.AreEqual(Ternary.False, result);
			else
				Assert.AreEqual(Ternary.Unknown, result);

			return result;
		}
	}
}
