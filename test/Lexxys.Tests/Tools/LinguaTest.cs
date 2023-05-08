// Lexxys Infrastructural library.
// file: LinguaTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//

namespace Lexxys.Tests.Tools
{
	/// <summary>
	///This is a test class for LinguaTest and is intended
	///to contain all LinguaTest Unit Tests
	///</summary>
	[TestClass()]
	public class LinguaTest
	{
		///	<summary>
		///	Gets or sets the test context which provides
		///	information about and functionality for the current test run.
		///	</summary>
		public TestContext TestContext { get; set; }

		#region Additional test attributes

		// 
		//	You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//

		#endregion

		[TestMethod]
		[DataRow("", "")]
		[DataRow("man", "men")]
		[DataRow("bug", "bugs")]
		public void PluralTest(string value, string expected)
		{
			string actual = Lingua.Plural(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("", "")]
		[DataRow("men", "man")]
		[DataRow("bugs", "bug")]
		public void SingularTest(string value, string expected)
		{
			string actual = Lingua.Singular(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("zero", "zeroth")]
		[DataRow("one", "first")]
		[DataRow("two", "second")]
		[DataRow("five", "fifth")]
		[DataRow("on handred and twelve", "on handred and twelfth")]
		[DataRow("on handred and one", "on handred and first")]
		[DataRow("on handred and one point zero", "on handred and first point zero")]
		public void OrdStringTest(string value, string expected)
		{
			string actual = Lingua.Ord(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow(0, "0th")]
		[DataRow(1, "1st")]
		[DataRow(2, "2nd")]
		[DataRow(5, "5th")]
		[DataRow(12311, "12311th")]
		[DataRow(98761, "98761st")]
		public void Ord_WithDoubleArgument_Test(long value, string expected)
		{
			string actual = Lingua.Ord(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("0", "0th")]
		[DataRow("1", "1st")]
		[DataRow("2", "2nd")]
		[DataRow("5", "5th")]
		[DataRow("12311", "12311th")]
		[DataRow("98761", "98761st")]
		[DataRow("1,234", "1,234th")]
		[DataRow("1,234$", "1,234th$")]
		[DataRow("1,234 $", "1,234th $")]
		[DataRow("$1,234", "$1,234th")]
		[DataRow("$ 1,231", "$ 1,231st")]
		public void Ord_WithStringArgument_Test(string value, string expected)
		{
			string actual = Lingua.Ord(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow(123, "one hundred and twenty-three")]
		public void NumWordNumberTest(long value, string expected)
		{
			string actual = Lingua.NumWord(value);
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("123", "one hundred and twenty-three")]
		public void NumWordStringTest(string value, string expected)
		{
			string actual = Lingua.NumWord(value);
			Assert.AreEqual(expected, actual);
		}
	}
}