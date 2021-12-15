// Lexxys Infrastructural library.
// file: LinguaTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Tools
{
    using Tools;


    /// <summary>
    ///This is a test class for LinguaTest and is intended
    ///to contain all LinguaTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LinguaTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
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

        private static string[] __plural =
        {
            "=>",
            "man => men",
            "bug => bugs"
        };
        [TestMethod()]
        public void PluralTest()
        {
            foreach (string testInfo in __plural)
            {
                TestInfo info = TestInfo.Parse(testInfo);
                string actual = Lingua.Plural(info.Test);
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        private static string[] __singular =
        {
            "=>",
            "men => man",
            "bugs => bug"
        };
        [TestMethod()]
        public void SingularTest()
        {
            foreach (string testInfo in __singular)
            {
                TestInfo info = TestInfo.Parse(testInfo);
                string actual = Lingua.Singular(info.Test);
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        private static string[] __writtenToWrittenOrdinal =
        {
            "zero => zeroth",
            "one => first",
            "two => second",
            "five => fifth",
            "on handred and twelve => on handred and twelfth",
            "on handred and one => on handred and first",
            "on handred and one point zero => on handred and first point zero",
        };
        [TestMethod()]
        public void OrdStringTest()
        {
            foreach (string testInfo in __writtenToWrittenOrdinal)
            {
                TestInfo info = TestInfo.Parse(testInfo);
                string actual = Lingua.Ord(info.Test);
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        private static string[] __numberToNumberOrdinal =
        {
            "0 => 0th",
            "1 => 1st",
            "2 => 2nd",
            "5 => 5th",
            "12311 => 12311th",
            "98761 => 98761st",
        };
        [TestMethod]
        public void Ord_WithDoubleArgument_Test()
        {
            foreach (string t in __numberToNumberOrdinal)
            {
                TestInfo info = TestInfo.Parse(t);
                string actual = Lingua.Ord(Int64.Parse(info.Test));
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        private static string[] __numberToNumberOrdinal1 =
        {
            "0 => 0th",
            "1 => 1st",
            "2 => 2nd",
            "5 => 5th",
            "12311 => 12311th",
            "98761 => 98761st",
            "1,234 => 1,234th",
            "1,234$ => 1,234th$",
            "1,234 $ => 1,234th $",
            "$1,234 => $1,234th",
            "$ 1,231 => $ 1,231st",
        };
        [TestMethod]
        public void Ord_WithStringArgument_Test()
        {
            foreach (string t in __numberToNumberOrdinal1)
            {
                TestInfo info = TestInfo.Parse(t);
                string actual = Lingua.Ord(info.Test);
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        private static string[] __numWordNumber =
        {
            "123 => one hundred and twenty-three"
        };
        [TestMethod]
        public void NumWordNumberTest()
        {
            foreach (string testInfo in __numWordNumber)
            {
                TestInfo info = TestInfo.Parse(testInfo);
                string actual = Lingua.NumWord(Int64.Parse(info.Test));
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }

        [TestMethod]
        public void NumWordStringTest()
        {
            foreach (string testInfo in __numWordNumber)
            {
                TestInfo info = TestInfo.Parse(testInfo);
                string actual = Lingua.NumWord(info.Test);
                Assert.AreEqual(info.ExpectedResult, actual);
            }
        }
    }
}
