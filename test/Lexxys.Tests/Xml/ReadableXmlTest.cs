// Lexxys Infrastructural library.
// file: ReadableXmlTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace Lexxys.Tests.Xml
{
	using Lexxys.Xml;

	/// <summary>
	///This is a test class for Lexxys.ReadableXml and is intended
	///to contain all Lexxys.ReadableXml Unit Tests
	///</summary>
	[TestClass()]
	public class ReadableXmlTest
	{
		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion

		/// <summary>
		///A test for GetFromXml (XmlReader)
		///</summary>
		[TestMethod()]
		[DataRow("""
			<configuration>

				<data>
					<mappers
						compile=""
						actionProcFormat="ppc.Mapper_{0}_{1}"
						>
						<mapper name="AspSession"
								compile=""
								inline="*"
						/>
					</mappers>
				</data>

				<dataContext>
					<providers>
						<provider
							name="SqlServer"
							type="TSQL"
							class="Lexxys.Data.SqlServerDataProvider"
						/>
					</providers>
					<sources>
						<source name="Backend" provider="SqlServer">
							<connection
								server="(local)"
								catalogue="School"
								>
								<option name="commandTimeout" value="60" />
							</connection>
						</source>
					</sources>
				</dataContext>
			</configuration>
			""", """
			configuration
				data
					mappers
						=compile	
						=actionProcFormat	ppc.Mapper_{0}_{1}
						mapper
							=name	AspSession
							=compile	
							=inline	*
				dataContext
					providers
						provider
							=name	SqlServer
							=type	TSQL
							=class	Lexxys.Data.SqlServerDataProvider
					sources
						source
							=name	Backend
							=provider	SqlServer
							connection
								=server	(local)
								=catalogue	School
								option
									=name	commandTimeout
									=value	60

			""")]
		[DataRow("""
			<root>
				<node1 attr='attr'>node value</node1>
				<node2 a='a' b='b'>node2 value node2 value2 node2 value3
					<node2node1 c='c' />
					<node2node2>node2node2 value</node2node2>
				</node2>
			</root>
			""", """
			root
				node1	node value
					=attr	attr
				node2	node2 value node2 value2 node2 value3
					=a	a
					=b	b
					node2node1
						=c	c
					node2node2	node2node2 value
			""")]
		public void ConvertXmlToTxtTest(string xml, string text)
		{
			using StringReader sr = new StringReader(xml);
			string actual = XmlToTextConverter.Convert(XmlReader.Create(sr)).Trim();
			string expected = text.Trim();

			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		[DataRow("""
			<easy1 item1='/1'>
				VALUE1
			</easy1>
			<easy2 item2='2'>
				VALUE2
			</easy2>
			""", """
			easy1	VALUE1
				@item1	/1	// comments
			easy2
				@item2	2
				<<
				VALUE2
				>>
			""")]
		[DataRow("""
			< stateMachines>
				<stateMachine name='Matrix'>
					<state name='Default' id='0'>
						<transition action='InPrep' target='10' />
						<transition action='ReadyForReview' target='20' />
						<transition action='FirstReview' target='30' />
						<transition action='SecondReview' target='40' />
					</state>
				</stateMachine>
			</stateMachines>
			""", """
			%*/stateMachine		name
			%**/state			name	id
			stateMachines
				%stateMachine/state/transition
				%..						action	target

				stateMachine Matrix

					. Default = 0
						. InPrep				10
						. ReadyForReview		= 20
						. FirstReview			, 30
						. SecondReview			=> 40

				/stateMachine Matrix
			""")]
		[DataRow("""
			<configuration>
				<seedgen>
					<entity		source='CostBase'		value='CostBasis' />
					<entity		source='GrantDetail'	value='GrantDetails' />

					<reference	source='Fx'		value='Fxs.ID'	type='int' />
					<reference	source='FxId'	value='Fxs.ID'	type='int' />

					<type		field='Zip'						value='zip(9)'		table='Students' />
					<type		field='Note'					value='string({0})'	table='Courses' />
					<type		field='(Phone|Fax)(?![a-z])'	value='phone({0})'	original='char' />
				</seedgen>
			</configuration>
			""", """
			configuration
				seedgen

					// Rename generated entity name
					%*			source			value			type
					entity		CostBase		CostBasis
					entity		GrantDetail		GrantDetails

					// Set reference by field name
					%*			source			value			type
					reference	Fx				Fxs.ID			int
					reference	FxId			Fxs.ID			int

					// Set fields types
					%*			field						value			original	table
					type		Zip							zip(9)			,		,	Students
					type		Note						string({0})		,		,	Courses

					type		(Phone|Fax)(?![a-z])		phone({0})		char
			""")]
		[DataRow("""
			<configuration>
				<crypto>
					<providers>
						<encriptor name='DES' class='Lexxys.Crypting.Cryptors.DesCryptor' assembly='.\Common\StdCryptors.dll' />
						<encriptor name='RSA' class='Lexxys.Crypting.Cryptors.RsaEncryptor' assembly='.\Common\StdCryptors.dll' />
						<decryptor name='DES' class='Lexxys.Crypting.Cryptors.DesCryptor' assembly='.\Common\StdCryptors.dll' />
						<decryptor name='RSA' class='Lexxys.Crypting.Cryptors.RsaDecryptor' assembly='.\Common\StdCryptors.dll' />
					</providers>
				</crypto>
			</configuration>
			""", """
			configuration
				crypto
					providers
						%encriptor	name class									assembly
						.DES 		Lexxys.Crypting.Cryptors.DesCryptor 		.\Common\StdCryptors.dll
						.RSA 		Lexxys.Crypting.Cryptors.RsaEncryptor 		.\Common\StdCryptors.dll
						%decryptor	name class									assembly
						.DES 		Lexxys.Crypting.Cryptors.DesCryptor 		.\Common\StdCryptors.dll
						.RSA 		Lexxys.Crypting.Cryptors.RsaDecryptor 		.\Common\StdCryptors.dll

			""")]
		public void ConvertTxtToXmlTest(string xml, string text)
		{
			string actual = TextToXmlConverter.Convert(text).Trim();
			string expected = xml.Trim();

			Assert.AreEqual(TrimXml(expected), TrimXml(actual));
		}

		[TestMethod]
		public void LoadDatabaseConfigurationTest()
		{
			if (File.Exists(@"test.db.config.txt"))
			{
				var p = Config.AddConfiguration(@"test.db.config.txt");
				Assert.IsNotNull(p);
				var c = Config.Current.GetValue<Lexxys.Data.ConnectionStringInfo>("database.connection", () => null).Value;
				if (c == null)
					Debugger.Break();
				Assert.IsNotNull(c);
			}
		}

		private string TrimXml(string source)
		{
			source = source.Replace('\'', '"');
			source = Regex.Replace(source, @"(?<![a-zA-Z0-9\s])\s+", "");
			source = Regex.Replace(source, @"\s+(?![a-zA-Z0-9\s])", "");
			source = Regex.Replace(source, @"(?<=[a-zA-Z0-9])\s+", " ");
			source = Regex.Replace(source, @"\s+(?![a-zA-Z0-9])", " ");
			return source;
		}
	}
}
