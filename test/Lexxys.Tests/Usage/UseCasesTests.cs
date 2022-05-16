using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Usage
{
	[TestClass]
	public class UseCasesTests
	{
		[TestMethod]
		public void UseCase1()
		{
			UseCases.StaticSvcs();
		}
	}
}
