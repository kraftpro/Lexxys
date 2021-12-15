// Lexxys Infrastructural library.
// file: ConfigurationTest.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Lexxys;
using Lexxys.Configuration;

namespace Lexxys.Tests.Configuration
{
	[TestClass]
	[DeploymentItem("test.config.txt")]
	public class ConfiguraitionTest
	{
		private IValue<object> SharedConfiguration => __config ??= Config.Current.GetValue<object>("scattergories.lists");
		private IValue<object> __config;

		[TestInitialize]
		public void Initializw()
		{
			string configFile = "test.config.txt";
			if (!File.Exists(configFile))
				throw new ArgumentOutOfRangeException(nameof(configFile), configFile, null);
			var provider = Config.AddConfiguration(configFile);
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));
			var nodes = Config.Current.GetValue<System.Xml.XmlNode[]>("scattergories.lists");
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));
			if (nodes.Value == null)
				throw new ArgumentNullException("nodes.Value");
		}


		[TestMethod, DoNotParallelize]
		public void GetSestionMultiThread()
		{
			Enumerable.Range(1, 10).AsParallel().ForAll(i =>
			{
				((System.Xml.XmlNode[])SharedConfiguration.Value).FirstOrDefault(x => x.Attributes.Count > 0);
			});
		}
	}

	#if false
	/// <summary>
	/// Summary description for TestConfiguration
	/// </summary>
	[TestClass]
	public class ConfigurationTest
	{
		string _tempFileBase;
		string _tempFileDir;
		List<ConfigurationSource> _initialSources;
		string[] _configText = new string[]
			{
@"s
<root>
	<node ai='5' ab='true' ad='2004-01-05' />
	<file pathA='fileA.txt' pathB='c:\ooo\fileB.txt' />
	<sval
		int='-2345'
		uint='22222'
		decimal='1.000000000000001'
		datetime='2099-12-31'
		timespan='1d12h15m59s'
		timespan2='1.5'
		timespan3='1.5 0.5'
		string='&lt;&gt;'
		char='a'
		enum='Int3'
		enumBits='Bit2, Bit3'
	/>
</root>
",
@"f
<root>
	<nodeF ai='5' ab='true' ad='2004-02-06'>
		Node Value
	</nodeF>
	<fileF pathA='fileAF.txt' pathB='c:\ooo\fileBF.txt' />
</root>
"
			};

		public ConfigurationTest()
		{
			_tempFileBase = Path.GetTempFileName();
			FileInfo fi = new FileInfo(_tempFileBase);
			_tempFileDir = fi.DirectoryName;
			_initialSources = new List<ConfigurationSource>();
		}

		[TestInitialize()]
		public void Initialize()
		{
			StreamWriter f;
			ConfigurationSource config;
			for (int i = 0; i < _configText.Length; i++)
			{
				string s = i.ToString();
				string root = "/root";
				if (_configText[i][0] == 'f')
				{
					using (f = File.CreateText(_tempFileBase + "." + i.ToString() + ".config.xml"))
					{
						f.Write(_configText[i].Substring(1));
					}
					config = new XmlFileConfigurationSource(_tempFileBase + "." + s + ".config.xml");
				}
				else if (_configText[i][0] == 's')
				{
					config = new XmlStringConfigurationSource(_configText[i].Substring(1), "String." + s, "C:\\Config");
				}
				else
				{
					config = new XmlStringConfigurationSource(_configText[i], "String." + s, "C:\\Config");
				}
				Config.AddConfigurationSource(config);
				_initialSources.Add(config);
			}
		}

		[TestCleanup()]
		public void Cleanup()
		{
			Config.Clear();
			_initialSources.Clear();
		}

		enum SampleEnum
		{
			Int1,
			Int2,
			Int3
		}

		[Flags]
		enum SampleEnumFlags
		{
			Bit1 = 1,
			Bit2 = 2,
			Bit3 = 4
		}

		[TestMethod]
		public void SimpleValues()
		{
			int i = Config.GetValue("node/@ai", 3);
			Assert.AreEqual(5, i);
			bool b = Config.GetValue("node/@ab", false);
			Assert.AreEqual(true, b);
			DateTime d = Config.GetValue("node/@ad", DateTime.MaxValue);
			Assert.AreEqual(new DateTime(2004, 1, 5), d);
			DateTime now = DateTime.Now;
			object d2 = Config.GetValue("node/@ad2", now);
			Assert.AreEqual(now, d2);
		
			Assert.AreEqual(-2345,						Config.GetValue<int>("sval/@int"));
			Assert.AreEqual((uint)22222,				Config.GetValue<uint>("sval/@uint"));
			Assert.AreEqual(1.000000000000001m,			Config.GetValue<decimal>("sval/@decimal"));
			Assert.AreEqual(new DateTime(2099, 12, 31),	Config.GetValue<DateTime>("sval/@datetime"));
			Assert.AreEqual(new TimeSpan(1, 12, 15, 59), Config.GetValue<TimeSpan>("sval/@timespan"));
			Assert.AreEqual(new TimeSpan(0, 0, 0, 1, 500), Config.GetValue<TimeSpan>("sval/@timespan2"));
			Assert.AreEqual(new TimeSpan(1, 12, 30, 0), Config.GetValue<TimeSpan>("sval/@timespan3"));
			Assert.AreEqual("<>", 						Config.GetValue<string>("sval/@string"));
			Assert.AreEqual(SampleEnum.Int3,			Config.GetValue<SampleEnum>("sval/@enum"));
			Assert.AreEqual(SampleEnumFlags.Bit2 | SampleEnumFlags.Bit3, Config.GetValue<SampleEnumFlags>("sval/@enumBits"));
		}

		public struct node
		{
			[XmlAttribute("ai")]
			public int IntAttrib;
			[XmlAttribute("ab")]
			public bool BoolAttrib;
			[XmlAttribute("ad")]
			public DateTime DateAttrib;
		}

		[TestMethod]
		public void XmlSerializer()
		{
			// Test flat structure
			object x = Config.GetValue<node>("node");
			Assert.AreNotEqual(x, null);
			node node = (node)x;
			Assert.AreEqual(5, node.IntAttrib);
			Assert.AreEqual(true, node.BoolAttrib);
			Assert.AreEqual(new DateTime(2004, 1, 5), node.DateAttrib);
		}

#pragma warning disable 649
		private struct PrivateNode
		{
			public int Ai;
			public bool Ab;
			[XmlAttribute("ad")]
			public DateTime AdValue;
		}
#pragma warning restore 649

		[TestMethod]
		public void FlatStructure()
		{
			// Test flat structure
			object x = Config.GetValue<PrivateNode>("node");
			Assert.AreNotEqual(x, null);
			PrivateNode node = (PrivateNode)x;
			Assert.AreEqual(5, node.Ai);
			Assert.AreEqual(true, node.Ab);
			Assert.AreEqual(new DateTime(2004, 1, 5), node.AdValue);
		}

		[TestMethod]
		public void FilePath()
		{
			string s1, s2, s3;

			s1 = Config.GetFilePath("file/@pathA");
			s2 = Config.GetFilePath("file/@pathB");
			s3 = Config.GetFilePath("file/@pathC", "c:\\xxx\\fileC.txt");
			Assert.IsTrue(0 == string.Compare(s1, "c:\\config\\fileA.txt", true));
			Assert.IsTrue(0 == string.Compare(s2, "c:\\ooo\\fileB.txt", true));
			Assert.IsTrue(0 == string.Compare(s3, "c:\\xxx\\fileC.txt", true));

			s1 = Config.GetFilePath("fileF/@pathA");
			s2 = Config.GetFilePath("fileF/@pathB");
			s3 = Config.GetFilePath("fileF/@pathC", "c:\\xxx\\fileCF.txt");
			Assert.IsTrue(0 == string.Compare(s1, _tempFileDir + "\\fileAF.txt", true));
			Assert.IsTrue(0 == string.Compare(s2, "c:\\ooo\\fileBF.txt", true));
			Assert.IsTrue(0 == string.Compare(s3, "c:\\xxx\\fileCF.txt", true));
		}

		[TestMethod]
		public void Cache()
		{
			long timer;

			timer = WatchTimer.Start();
			for (int i = 0; i < 10000; i++)
			{
				Config.GetValue<string>("node/i" + i.ToString());
			}
			long t1 = WatchTimer.Stop(timer);

			timer = WatchTimer.Start();
			for (int i = 0; i < 10000; i++)
			{
				Config.GetValue<string>("node/i" + i.ToString());
			}
			long t2 = WatchTimer.Stop(timer);

			Assert.IsTrue(t1 > t2);
		}

		[TestMethod]
		public void MultipleProviders()
		{
			Config.Clear();
			ConfigurationSource cp1 = new XmlStringConfigurationSource("<database><server>DB-1</server><catalogue>CP-1</catalogue></database>", "MultipleProviders1", "C:\\");
			Config.AddConfigurationSource(cp1);
			string s1 = Config.GetValue("database/server", "?s1");
			string c1 = Config.GetValue("database/catalogue", "?c1");
			Assert.AreEqual<string>("DB-1", s1);
			Assert.AreEqual<string>("CP-1", c1);

			ConfigurationSource cp2 = new XmlStringConfigurationSource("<database><server>DB-2</server><catalogue>CP-2</catalogue></database>", "MultipleProviders2", "C:\\");
			Config.AddConfigurationSource(cp2);
			string s2 = Config.GetValue("database/server", "?s2");
			string c2 = Config.GetValue("database/catalogue", "?c2");
			Assert.AreEqual<string>("DB-2", s2);
			Assert.AreEqual<string>("CP-2", c2);

			Config.AddConfigurationSource(cp1);
			string s3 = Config.GetValue("database/server", "?s3");
			string c3 = Config.GetValue("database/catalogue", "?c3");

			Assert.AreEqual<string>(s1, s3);
			Assert.AreEqual<string>(c1, c3);
		}

	}
	#endif
}