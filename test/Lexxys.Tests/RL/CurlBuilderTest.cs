// Lexxys Infrastructural library.
// file: CurlBuilderTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using Lexxys.RL;
//using Microsoft.Pex.Framework;
//using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.RL
{
	/// <summary>This class contains parameterized unit tests for CurlBuilder</summary>
	//[PexClass(typeof(CurlBuilder))]
	//[PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
	//[PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
	[TestClass]
	public partial class CurlBuilderTest
	{
		/// <summary>Test stub for .ctor()</summary>
		//[PexMethod]
		public CurlBuilder Constructor()
		{
			CurlBuilder target = new CurlBuilder();
			return target;
			// TODO: add assertions to method CurlBuilderTest.Constructor()
		}

		/// <summary>Test stub for SetHost(String, Boolean, Boolean)</summary>
		//[PexMethod(MaxRunsWithoutNewTests = 200, MaxConstraintSolverTime = 8, Timeout = 240)]
		public void SetHost(
			//[PexAssumeUnderTest]
			CurlBuilder target,
			string value,
			bool user,
			bool port
		)
		{
			target.SetHost(value, user, port);
			// TODO: add assertions to method CurlBuilderTest.SetHost(CurlBuilder, String, Boolean, Boolean)
		}

		/// <summary>Test stub for SetPath(String, Boolean, Boolean)</summary>
		//[PexMethod]
		public void SetPath(
			//[PexAssumeUnderTest]
			CurlBuilder target,
			string value,
			bool query,
			bool fragment
		)
		{
			target.SetPath(value, query, fragment);
			// TODO: add assertions to method CurlBuilderTest.SetPath(CurlBuilder, String, Boolean, Boolean)
		}
	}
}
