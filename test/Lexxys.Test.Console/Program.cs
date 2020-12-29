// Lexxys Infrastructural library.
// file: Program.cs
//
// Copyright (c) 2001-2014, KRAFT Program LLC.
// You may use this code under the terms of the LGPLv3 license (https://www.gnu.org/copyleft/lesser.html)
//
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

using System.Xml.XPath;

using System.Web;

using Lexxys;
using Lexxys.Configuration;
using Lexxys.Crypting;
using Lexxys.Data;
using Lexxys.Logging;
using Lexxys.Testing;
using Lexxys.Xml;
using System.Windows.Markup;

namespace Lexxys.Test.Con
{

	static class Program
	{
		private static Logger Log => _logger ??= new Logger("Program");
		private static Logger _logger;

		private static Regex _crlRex = new Regex(@"^\s*((?<schema>file|https?|ftps?|database|enums)\s*:\s*).*?(?<params>\?.*?)?(<root>\[.*?\])?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
#if DEBUG
		private static int Count = 1000000;
#else
		private static int Count = 1000000;
#endif
		private static bool NoLogging = false;
		private static bool Clear = false;
		private static int Ths = 10;

		enum Abc { A, B, C }

		private static readonly byte[] TokenKey = { 12, 115, 134, 178, 31, 105, 236, 141, 56, 114, 139, 115, 61, 122, 38, 129, 180, 222, 150, 177, 162, 250, 177, 112 };

		static void Main(string[] args)
		{
			try
			{

				var a = new Arguments(args);
				string testCode = a.First("ChArr");

				Console.WriteLine(testCode);
				switch (testCode)
				{
					case "ChArr":
						{
							Speed.CharArr.Go();
							break;
						}
					case "Cfg":
						{
							Config.AddConfiguration(@"C:\Application\Config\fsadmin.config.txt");
							List<SubscriptionGroup> cg = Config.GetValue("FsAdmin.email.subscription.groups", new List<SubscriptionGroup>());
							Console.WriteLine(DumpWriter.ToString(cg));
							break;
						}

					case "Xml2":
						Core.Xml2.Test.Go();
						return;

					case "Tokenizer-1":
						Tokenizer2Test.Go();
						return;

					case "Dump1":
						{
							var lb = new List<byte>() { 155 };
							var ab = new byte[] { 234 };
							Console.WriteLine(Ba(lb));
							Console.WriteLine(Ba(ab));
							static string Ba(object value)
							{
								char[] HexChar = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
								var text = new StringBuilder();
								switch (value)
								{
									case IEnumerable<byte> colbytes:
										text.Append("0x");
										foreach (var b in colbytes)
										{
											text.Append(HexChar[b >> 4]).Append(HexChar[b & 15]);
										}
										return text.ToString();
									default:
										return value?.ToString();
								}
							}
							return;
						}

					case "Xml-Speed-1":
						Core.Xml.ParseTest.Go();
						return;

					case "Xml-Speed-2":
						Core.Xml.ParseTest.Go3(100000);
						return;

					case "Xml-Speed-A":
						Speed.ToXmlLite.Go();
						return;

					case "Utf8":
						Js.Utf8Test.SetStringValue("\u0017\u0017\f7Ok\u0015$;vZ\u0015i\r;W}Au\"[\u0004\u0003\u0001J\u001bM*+Nml$-i\u0003\u0017$;aq\"2[nNA;\u0011\u001f=\rHxu\u0013\u0013\nm4\u001dkxl\bmq;#8?X\u0005\u001418R(\u0013{Y\u0012\u0019\f\u0011`#+7Uo\f6\aC\u0006)ES\0]\u0005\u0017X`d\a4F<AkwrZ\vRl['IE\u0003NH2\"}P+\u000f\fT\u0012\u001eSz@+,Xod{\u0014\u0004\u0014D-73<j+7\u0017\u0002I\u0010-B\tc\u001b-4\u0016eS\u0006\u0010\u0011@/OZ`\b9C\u001d]P\u0019wR\")[\aQ1/~iO\vIM`n/p&&\"{:\\\\6y\u000fL\u000fe9J$[\u0011l\nL4_aBE\0O\u0017;YUnl)rUVgM~PX!cn\u0004\u0015@%qN\u0001$#qG@9u\\\f\u001e>6v!x\u000fXN\u0005*\u001f\v|\\=~S@j\u000frI\nTIH7*\u0015\u001cqqq2\u0019klM\f\u0006gVl$b\u0019'\u0015\nAe5r},t,\"X/W.9h8-\u0019IBR\u0011\u00063c'\u001b]fq0!\"\u001fS7<\u007ft\u0006%?Ju\\/9\u001c\u0003l4\u007f^R8/_\u0002\u000eU\u0010\u0016F0'\u000e4\u000ee1|.%C\u001d\u0013G\u0011V3n\u007fYy=0BOg#W:p\u001eu\u000ey\u0019MNz*6Rp\u0006Z\u001a5%hN+[f\u0011=LswKd'BYA=\u0006M\u000e\u001a-C\b9CH\a\t6V'\u001aA;\t\u000e\u00157dQ,&V\u0002\u0015\u0003ty.ITM'\v:\n\"!#H6B=!#Je\aO3'Nh<m\am\u0006#@S\r[m\u0002Zr6\0\u007fls\u0014\u001b\u001bjh5^K\u001em){!\u0005ej\u001e3\u001c\u001c~bu(gO\u001eIW/\u0006PF49L!\u0002\u001e8'W!fc`L|u\b\u0017MbOt1&r\u0002\u0018\u0016\u0015sofY4lL\u0018I\u001b\u0014dt&@\u000eD\a\u0003\"TO\u00179nv?\a%.\0\u0005\"\u0012d2\u007f\n+_0\\?|9~[Mq\u0018.эКъаТлЦхЮЦвщННЗасбЖЗЭнТшыИНАчНшЛЖАВшЧианйПцЩцфТрХЮьЕсДщРшБдЙЮЦэСЭЦтНЛПЙИАЩЮНцщВЖхЬкЩСтЩБЖтяЦДЦОТСеыЯВЕеЖФуЕШжмЩяюЭФБлгякэрЖЩПуДкКЛыСэгДрчдЕЭнБЫЩШАЧжБМзнжРбСюЛчапЪНЫрЗБЪШгКЖЮбьыХнАОЬЭиэгОкнБутлнднУфяйРяьНЭВаПДУжыСЛиДТсПчвЬФЩесЮЩщфйЗЦГпПЯПКыГюжЩЫшааДаЫбдЗЗфлгимдЖХРкЯеНЫЩеВзФфгцъэнСюФЩШЗвОьыгызЮЧьйЦпьТУДлыЩъРУгэреыЪесычвьЧСахйЪИэчъЯЛБЪэщйЮяНТъЩэуЦмГсЛнЯМяыВдхСюсТЬйЮдгаЬ");
						Js.Utf8Test.Go();
						return;

					case "XmlNode":
						Xml.XmlNodeTest.Go();
						return;

					case "Resources":
						ResourcesTest.Go();
						return;

					case "DeEnCode":
						DeEnCode.Go();
						return;

					case "Json":
						JsonTest.Go();
						return;

					case "Validators":
						{
							var ii = new[] { new ErrorAttrib("min", 1), new ErrorAttrib("max", 2), new ErrorAttrib("value", 123) };
							string xx = ErrorInfo.FormatMessage(new[] {
										"Fiscal year end should be a number between {min}.",
										"Fiscal year end  is out of range.",
										"Fiscal year end should be a number between {min} and {max} at {bound}.",
										"Fiscal year end should be a number between {min} and {max}.",
										}, ii);
							var ii2 = new[] { new ErrorAttrib("size", 33), new ErrorAttrib("value", "aaaaaa") };
							string x1 = ErrorInfo.FormatMessage("{field} should not be longer than {size} characters.", ii2, field: "Foundation name");
							var a1 = Lexxys.Data.FieldValidator.EmailAddress("abs@aaa", 5, "EM");
							var b1 = ValidationResults.Create("TEST2");
							var c1 = a1 && b1;
							var d1 = Lexxys.Data.FieldValidator.UsStateCode("DE__x", "aa", true);
							return;
						}

					case "Log":
						{
							Count = a.Value("count", Count);
							Ths = a.Value("ths", Ths);
							NoLogging = a.Exists("no logging", NoLogging);
							Clear = a.Exists("clear", Clear);

							if (NoLogging)
							{
								Console.WriteLine("NoLogging");
								LoggingContext.Disable();
							}

							Console.WriteLine("Log {0:N0} records by {1} thread(s).", Count, Ths);
							GC.Collect();
							double mem = GC.GetTotalMemory(false);
							Console.WriteLine("Starting {0:N1} MB.", mem / (1024 * 1024));

							long x = WatchTimer.Start();
							LoggingTest.Test(Count, Ths);
							double t1 = WatchTimer.ToSeconds(WatchTimer.Query(x));

							if (Clear)
							{
								LoggingContext.ClearBuffers();
								Console.WriteLine("ClearBuffers");
							}

							Console.WriteLine("Running{0,10:F6} sec.", t1);
							double mem2 = GC.GetTotalMemory(false);
							Console.WriteLine("Allocated {0:N1} MB.", (mem2 - mem) / (1024 * 1024));
							long y = WatchTimer.Start();
							LoggingContext.Stop();
							double t2 = WatchTimer.ToSeconds(WatchTimer.Query(y));
							Console.WriteLine("Finish {0,10:F6} sec.", t2);
							Console.WriteLine("Total  {0,10:F6} sec.", t1 + t2);
							return;
						}

					case "Cached":
						Caches.Go();
						return;

					case "XmlBuilder":
						XmlBuilderTest.Go();
						return;

					case "Dc":
						Dat.TestDc();
						return;
				}

				// Core.Xml.StrCmp.Go();
				// return;
				return;

			}
			catch (Exception flaw)
			{
				Log.Error("Main:", flaw.Add(nameof(args), args));
				throw;
			}

			// return;
			// var b = SixBitsCoder.Decode("YmVlYWExZTctYTcwZi00ZGJmLTk3NGQtOTBkYzYyZjEzMTIysqlq6PECx0DoRAuyXSodf6vKjTRWudGXoTCcOZby1pRugLwwvegnWjxDHMhs09agLA_LbFurEgrNoMiYvi2XYw");
			// string s = Crypto.Decryptor("DES3", TokenKey).DecryptString(b, Encoding.UTF8);
			// Js.Utf8Test.Go();
			// return;
			// var sec = Config.GetValue("FsAdmin.rabbitMq.email", XmlLiteNode.Empty);
			// Console.WriteLine(sec.Value);
			// Js.Utf8Test.Go();
			// return;
			// ConfigTest.Go();
			// return;
			// 
			// Js.Jsb.Test();
			// //Speed.ToCsStr.Go();
			// //return;
			// ConfigText.Go();
			// var config = TextToXmlConverter.ConvertLite(File.ReadAllText(@"C:\Application\Config\fsadmin.config.txt"));
			// Console.WriteLine(config.Count);
			// return;

			//Dump.Test();
			//return;

			//TokenTest.Go();
			//return;
			//Strings2.Go();
			//return;
			//FileTest.Go();
			//return;
			//var code = DecodeGo("a53eHSrk");
			//ConfigText.Go();
			//SpeedTest.TestCollectionExtensions(1);
			//return;
			//for (int i = 0; i < 1000; ++i)
			//{
			//	var days = Rand.Items(Rand.GetInteger(1, 7), new[] { DayOfWeek.Saturday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday });
			//	var period = Rand.GetInteger(1, 5);
			//	var start = new DateTime(2017, 1, 1) + TimeSpan.FromDays(Rand.GetInteger(200));
			//	var t = new WeeklySchedule(period, days);
			//	var count = Rand.GetInteger(10, 100);
			//	DateTime? d1 = null, d2 = null;
			//	for (int j = 0; j < count; ++j)
			//	{
			//		var r1 = t.Next(start, d1);
			//		var r2 = t.Next2(start, d2);
			//		if (r1 != r2)
			//		{
			//			Console.WriteLine($"start: {start:d}, last: {d1:d}, next: {r2:D}; {r1:D}");
			//			break;
			//		}
			//		d1 = d2 = r2;
			//	}
			//
			//}

			// ScheduleTest.Go();
			// return;

			// return;

			//var zz = Dc.GetList(new { id = 0 }, "select top 100 id from users");
			//var yy = Dc.GetList<decimal?>("select top 100 cast(id as money) from users");
			//Console.WriteLine(zz.Count);
			//Console.WriteLine(yy.Count);
			//Cnstrctr.Go();
			//return;

			//Dat.Run();

			//var xx = ValueValidator.IsEmail("ab@fs.com");
			//var a1 = Lingua.Plural("genus");
			//var a2 = Lingua.Plural("opus");
			//var a3 = Lingua.Plural("magnum opus");

			//UrlValueValidator.IsLegalHttpUrl("WxLLdeHKDwPFNhibdWRpmNHFZDnCiTNpGSmShmpFXO");
			//Cryp.Test();

			//DictionaryTest.Test(args);
			//return;

			//Test9();
			//Regex rex = new Regex(@"^\s*((?<schema>file|https?|ftps?|database|enums)\s*:\s*)(?<body>.*?)\s*(\?\s*(?<params>.*?))?(\s*\[\s*(?<root>([^[]|\[\[)*?)\s*\])?\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
			//Args(args);
			//Test8();


			//var a = new Arguments(args);
			//Count = XmlTools.GetInt32(a.First(), Count);
			//NoLogging = a.Exists("no logging", NoLogging);
			//Clear = a.Exists("clear", Clear);
			//Ths = a.Value("ths", Ths);
			//
			//if (NoLogging)
			//{
			//	Console.WriteLine("NoLogging");
			//	LoggingContext.Disable();
			//}
			//
			//Console.WriteLine("Log {0:N0} records by {1} thread(s).", Count, Ths);
			//GC.Collect();
			//double mem = GC.GetTotalMemory(false);
			//Console.WriteLine("Starting {0:N1} MB.", mem / (1024*1024));
			//
			//long x = WatchTimer.Start();
			//LoggingTest.Test(Count, Ths);
			//double t1 = WatchTimer.ToSeconds(WatchTimer.Query(x));
			//
			//if (Clear)
			//{
			//	LoggingContext.ClearBuffers();
			//	Console.WriteLine("ClearBuffers");
			//}
			//
			//Console.WriteLine("Running{0,10:F6} sec.", t1);
			//double mem2 = GC.GetTotalMemory(false);
			//Console.WriteLine("Allocated {0:N1} MB.", (mem2 - mem) / (1024*1024));
			//long y = WatchTimer.Start();
			//LoggingContext.Stop();
			//double t2 = WatchTimer.ToSeconds(WatchTimer.Query(y));
			//Console.WriteLine("Finish {0,10:F6} sec.", t2);
			//Console.WriteLine("Total  {0,10:F6} sec.", t1 + t2);


			// double mem3 = GC.GetTotalMemory(false);
			// Console.WriteLine("Allocated {0:N1} MB.", (mem3 - mem2) / (1024*1024));
			// GC.Collect();
			// Console.WriteLine("Collected {0:N1} MB.", (mem3 - GC.GetTotalMemory(false)) / (1024*1024));
			// Console.WriteLine("Finishing {0:N1} MB.", (double)GC.GetTotalMemory(false) / (1024*1024));

			//			Console.WriteLine("Exception ...");
			//			long r;
			//			Math.DivRem(5, 0, out r);
		}

		private const ulong EncodeMask = 982451653;
		private const ulong EncodeMult = 15485863;

		private static int DecodeGo(string value)
		{
			ulong code;
			if (value == null || !value.StartsWith("a") || (code = SixBitsCoder.Sixty(value.Substring(1))) == 0)
				return 0;
			code = code ^ EncodeMask;
			if (code % EncodeMult != 0)
				return 0;
			code /= EncodeMult;
			return code > (ulong)int.MaxValue ? 0 : (int)code;
		}

		private static string EncodeGo(int value)
		{
			if (value < 0)
				throw EX.ArgumentOutOfRange(nameof(value), value);
			return "a" + SixBitsCoder.Sixty((ulong)value * EncodeMult ^ EncodeMask);
		}

		private static void Test9()
		{
			string name = Config.GetValue("stateMachines.stateMachine:name", "nothing");
		}

		private static void Args(string[] args)
		{
			foreach (var arg in args)
			{
				int n;
				if (Int32.TryParse(arg, out n))
					Count = n;
				else if (arg.Equals("nolog", StringComparison.OrdinalIgnoreCase))
					NoLogging = true;
				else if (arg.Equals("clear", StringComparison.OrdinalIgnoreCase))
					Clear = true;
			}
		}

		static XPathNavigator ResetContent(string xml, string root)
		{
			XPathNavigator navigator = null;
			if (xml != null)
			{
				using (var stream = new StringReader(xml))
				{
					navigator = new XPathDocument(stream).CreateNavigator();
					if (root != null && root.Length > 0)
						navigator = navigator.SelectSingleNode(root);
				}
			}
			return navigator;
		}

		static void Test7()
		{
			//DecimalField a = new DecimalField();
			//DecimalField b = null;
			//DecimalField c = 6;
			//if (ValueValidator.IsInRange(b, 1, 8))
			//    Debug.Print("aaa");
			//if (ValueValidator.IsInRange(b, a, c, true))
			//    Debug.Print("aaa");
			//if (ValueValidator.IsInRange(c, 1, null))
			//    Debug.Print("aaa");
		}

		static void EncodeDecodeTest()
		{
			Random r = new Random();
			for (int i = 0; i < 10000; ++i)
			{
				int n = r.Next(128);
				byte[] expected = new byte[n];
				r.NextBytes(expected);
				try
				{
					string value = SixBitsCoder.Encode(expected);
					byte[] actual = SixBitsCoder.Decode(value);
					if (expected.Length != actual.Length)
						throw EX.InvalidOperation();
					for (int j = 0; j < n; ++j)
					{
						if (expected[j] != actual[j])
							throw EX.InvalidOperation();
					}
				}
				catch (Exception e)
				{
					throw EX.InvalidOperation("problems", e);
				}
			}
		}

		static void Test8()
		{
			Hasher h1 = Crypto.Hasher("SHA");
			Hasher h5 = Crypto.Hasher("SHA5");
			Hasher hm = Crypto.Hasher("MD5");
		}

		static string _charLine = "abcdefghijklmnopqrstuvwxyz@ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
		static string NewID()
		{
			byte[] a = Guid.NewGuid().ToByteArray();
			StringBuilder result = new StringBuilder(24);
			int rest = Encode(a, result);
			Debug.Assert((rest & ~3) == 0x0200);
			int q = ((result.ToString().GetHashCode() & 0xFFFF) << 2) | (rest & 3);
			result.Append(SixToChar(q));
			result.Append(SixToChar(q >> 6));
			result.Append(SixToChar(q >> 12));
			bool x = IsID(result.ToString());
			return result.ToString();
		}

		static bool IsID(string id)
		{
			if (id == null || id.Length != 24)
				return false;
			int q = (id.Substring(0, 21).GetHashCode() & 0xFFFF);
			int k = (CharToSix(id[21]) | (CharToSix(id[22]) << 6) | (CharToSix(id[23]) << 12));
			if ((k >> 2) != q)
				return false;
			byte[] x = Decode(id.Substring(0, 22));
			return (x != null);
		}

		static char SixToChar(int index)
		{
			return _charLine[index & 0x3F];
		}

		static int CharToSix(char c)
		{
			if (c >= 'a')
				if (c <= 'z')
					return c - 'a' + (0);
				else
					return -1;
			else if (c >= '@')
				if (c <= 'Z')
					return c - '@' + ('z' - 'a' + 1);
				else
					return -1;
			else if (c >= '0')
				if (c <= '9')
					return c - '0' + ('z' - 'a' + 'Z' - '@' + 2);
				else
					return -1;
			else if (c == '$')
				return 63;
			else
				return -1;
		}

		static string Encode(byte[] bits)
		{
			StringBuilder result = new StringBuilder((bits.Length * 4 + 2) / 3);
			int rest = Encode(bits, result);
			if (rest > 0)
				result.Append(SixToChar(rest));
			return result.ToString();
		}

		static int Encode(byte[] bits, StringBuilder result)
		{
			int k = 0;
			int n = 0;
			for (int i = 0; i < bits.Length; ++i)
			{
				k |= (int)bits[i] << n;
				n += 8;
				while (n > 6)
				{
					result.Append(SixToChar(k));
					k >>= 6;
					n -= 6;
				}
			}
			return (n << 8) | k;
		}

		static byte[] Decode(string value)
		{
			int rest;
			return Decode(value, out rest);
		}

		static byte[] Decode(string value, out int rest)
		{
			rest = 0;
			byte[] bits = new byte[(value.Length * 3) / 4];
			int eight = 0;
			int n = 0;
			int j = 0;
			foreach (char c in value)
			{
				int six = CharToSix(c);
				if (six == -1)
					return null;
				eight |= six << n;
				n += 6;
				if (n >= 8)
				{
					bits[j++] = (byte)(eight & 0xFF);
					eight >>= 8;
					n -= 8;
				}
			}
			Debug.Assert(j == bits.Length);
			rest = (n << 8) | eight;
			return bits;
		}

		//static void Test5()
		//{
		//	var list =
		//		from x in Config.GetList<EnumsRecord>("Enums/Countries")
		//		orderby x.Name descending
		//		select x;

		//	foreach (var rec in list)
		//	{
		//		Console.WriteLine("{0}\t{1}", rec.Abbrev, rec.Name);
		//	}
		//	EnumsRecord b = Config.GetValue("Enums/Transaction/Type[@Id=555]", EnumsRecord.Empty);
		//	EnumsRecord c = Config.GetValue("Enums/Transaction/Type[@Id=55]", EnumsRecord.Empty);
		//}

		static void Test2()
		{
			string s = "5599999999999";
			int i = ParseSid(s);
		}

		static int ParseSid(string sid)
		{
			if (sid == null || sid.Length == 0)
				return 0;
			sid = sid.Trim();
			if (sid.Length == 0)
				return 0;
			for (int i = 0; i < sid.Length; i++)
			{
				if (i > 9 || sid[i] < '0' && sid[i] > '9')
					return -1;
			}
			return int.Parse(sid);
		}

		static void Test1()
		{
			string value = "staging123";
			string seed = "55CDA470335F80A194685FDA4C0A3153A";
			Console.WriteLine();
			Console.WriteLine(seed);
			Console.WriteLine();
			var en = new[]
				{
					Encoding.ASCII,
					Encoding.BigEndianUnicode,
					Encoding.Unicode,
					Encoding.UTF32,
					Encoding.UTF7,
					Encoding.UTF8,
				};

			foreach (Encoding e in en)
			{
				MD5CryptoServiceProvider hasher = new MD5CryptoServiceProvider();
				byte[] bvalue = e.GetBytes(value);
				//hasher.InputBlockSize = bvalue.Length;
				int offset = hasher.TransformBlock(bvalue, 0, bvalue.Length - 1, bvalue, 0);
				hasher.TransformFinalBlock(bvalue, offset, 1);
				byte[] bits = hasher.Hash;

				MD5CryptoServiceProvider hasher2 = new MD5CryptoServiceProvider();
				byte[] bits2 = hasher2.ComputeHash(bvalue);

				string hash = "5" + new String(Lexxys.Strings.ToHexCharArray(bits));
				string hash2 = "5" + new String(Lexxys.Strings.ToHexCharArray(bits2));
				Console.WriteLine(hash);
				Console.WriteLine(hash2);
				if (hash == seed)
					Console.WriteLine("here");
			}
		}

		public class SubscriptionGroup
		{
			public SubscriptionGroup(string name, IReadOnlyCollection<int> items = null)
			{
				Name = name;
				Items = items;
			}

			public string Name { get; }
			public IReadOnlyCollection<int> Items { get; set; }
		}

	}
}
