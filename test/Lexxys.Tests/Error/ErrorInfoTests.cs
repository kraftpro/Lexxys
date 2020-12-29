using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Error
{
	[TestClass]
	public class ErrorInfoTests
	{

		[TestMethod]
		[DataRow("hello there")]
		[DataRow("parameters {param}")]
		[DataRow("parameters {0}")]
		[DataRow("parameters {}")]
		[DataRow("parameters {} {1}")]
		public void NoParametersTest(string template)
		{
			var message = ErrorInfo.FormatMessage(template, null);
			Assert.AreEqual(template, message);
		}

		[TestMethod]
		[DataRow("hello there {one}", "hello there 1")]
		[DataRow("parameters", "parameters")]
		[DataRow("parameters {param}", "parameters {param}")]
		[DataRow("parameters {{{one}{{} {param} {two} {one:00}", "parameters {1{} {param} 2 01")]
		public void PartialExpand(string template, string expected)
		{
			var message = ErrorInfo.FormatMessage(template, new[] { new ErrorAttrib("one", 1), new ErrorAttrib("two", 2) });
			Assert.AreEqual(expected, message);
		}
	}
}
