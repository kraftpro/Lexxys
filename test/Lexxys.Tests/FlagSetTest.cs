using System.Reflection;
// <copyright file="FlagSetTest.cs" company="KRAFT Program">Copyright © 2001-2014, KRAFT Program LLC.</copyright>

using System;
using Lexxys;
//using Microsoft.Pex.Framework;
//using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
