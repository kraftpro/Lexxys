// <copyright file="FlagSetTest.cs" company="KRAFT Program">Copyright © 2001-2014, KRAFT Program LLC.</copyright>

//using Microsoft.Pex.Framework;
//using Microsoft.Pex.Framework.Validation;

namespace Lexxys.Tests
{
	[TestClass]
    //[PexClass(typeof(FlagSet))]
    //[PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    //[PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class FlagSetTest
    {

        //[PexMethod]
        public string ToString01(/*[PexAssumeUnderTest]*/FlagSet target)
        {
            string result = target.ToString();
            return result;
        }
    }
}
