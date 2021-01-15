using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Lexxys.Xml;

namespace Lexxys.Test.Con.Speed
{
	public class FileValue
	{
		public string File { get; }
		public string Name { get; }
		public string Value { get; }

		public FileValue(string file)
		{
			File = file;
			Name = Path.GetFileNameWithoutExtension(file);
			Value = System.IO.File.ReadAllText(file);
		}

		public override string ToString() => Name;
	}

	public class ToXmlLite
	{
		private static FileValue A { get; } = new FileValue(@"C:\Temp\Gotestt.xml");
		private static FileValue B { get; } = new FileValue(@"C:\Temp\netstandard.xml");
		private static FileValue C { get; } = new FileValue(@"C:\Temp\proforma.xml");
		private static FileValue D { get; } = new FileValue(@"C:\Temp\Sessions.xml");
		private static FileValue E { get; } = new FileValue(@"C:\Temp\Windows.UniversalApiContract.xml");
		private static FileValue F { get; } = new FileValue(@"C:\Temp\database.config.xml");

		public static FileValue[] Abcd => new[] { B };

		[ParamsSource(nameof(Abcd))]
		public FileValue T;

		public static void Go()
		{
			BenchmarkRunner.Run<ToXmlLite>();
		}

		public static int RunLite(int count)
		{
			var x = new ToXmlLite();
			int c = 0;
			for (int i = 0; i < count; ++i)
			{
				c += (x.UseXmlLite() == null) ? 0 : 1;
			}
			return c;
		}

		public static int RunLite2(int count)
		{
			var x = new ToXmlLite();
			int c = 0;
			for (int i = 0; i < count; ++i)
			{
				c += (x.UseXmlLite() == null) ? 0 : 1;
			}
			return c;
		}

		public static void Try()
		{
			var x = Try1();
			var y = Try2();
			Console.WriteLine(x.ToString(true));
			Console.WriteLine();
			Console.WriteLine(y.ToString(true));
			Console.WriteLine();
		}

		public static XmlLiteNode Try1()
		{
			return XmlLiteNode.FromXml(F.Value);
		}

		public static XmlLiteNode Try2()
		{
			using (var reader = XmlReader.Create(new StringReader(F.Value)))
			{
				return XmlLiteNode.FromXml(reader, StringComparer.Ordinal);
			}
		}

		[Benchmark]
		public object UseXmlLite()
		{
			return XmlLiteNode.FromXml(T.Value);
		}

		[Benchmark]
		public object UseXmlLinq()
		{
			return System.Xml.Linq.XDocument.Parse(T.Value);
		}

		//[Benchmark]
		public object UseXmlDoc()
		{
			var doc = new System.Xml.XmlDocument();
			doc.LoadXml(T.Value);
			return doc;
		}
	}
}
