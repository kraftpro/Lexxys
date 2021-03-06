using System;
using System.Text.RegularExpressions;
using Lexxys;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Data
{
    /// <summary>This class contains parameterized unit tests for ConnectionStringInfo</summary>
    //[PexClass(typeof(ConnectionStringInfo))]
    //[PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    //[PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class ConnectionStringInfoTest
    {
        /// <summary>Test stub for .ctor()</summary>
        //[PexMethod]
        //public ConnectionStringInfo ConstructorTest()
        //{
        //    ConnectionStringInfo target = new ConnectionStringInfo();
        //    return target;
        //    // TODO: add assertions to method ConnectionStringInfoTest.ConstructorTest()
        //}

		[TestMethod, DoNotParallelize]
		[DataRow("Provider = SQLOLEDB.1;Data Source = source;Integrated Security = SSPI;Initial Catalog = EqtCoverage;Persist Security Info = False",
			"server=source;database=EqtCoverage;integrated security=SSPI;provider=SQLOLEDB.1;persist security info=false")]
		public void ConstructorTest(string value, string expected)
		{
			var cs = new ConnectionStringInfo(value);
			var sv = cs.GetConnectionString();
			sv = Regex.Replace(sv, @"app=.*?;", "");
			sv = Regex.Replace(sv, @"wsid=.*?;", "");
			Assert.AreEqual(Norm(expected), Norm(sv));
		}

		private string Norm(string value) => value?.ToUpperInvariant().Replace(" ", "");
    }
}
