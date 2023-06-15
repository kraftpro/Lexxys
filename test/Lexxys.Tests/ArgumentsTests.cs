namespace Lexxys.Tests
{
	[TestClass]
	public class ArgumentsTests
	{
		private static readonly string[] Args1 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

		[TestMethod]
		public void ArgsTest()
		{
			var a = new Arguments(null);
			Assert.IsNotNull(a.Args);
			Assert.AreEqual(0, a.Args.Count);
			a = new Arguments(Args1);
			CollectionAssert.AreEqual(Args1, a.Args.ToList());
		}

		[TestMethod]
		public void SwitchTest()
		{
			var a = new Arguments(Args1);
			Assert.IsTrue(a.Switch("ca"));
			Assert.IsTrue(a.Switch("category"));
			Assert.IsTrue(a.Switch("cross across"));
			Assert.IsFalse(a.Switch("data base"));
			Assert.IsFalse(a.Switch("database"));
			Assert.IsFalse(a.Switch("c"));
			Assert.IsFalse(a.Switch("cat balance"));
		}

		[TestMethod]
		public void StringValueTest()
		{
			var a = new Arguments(Args1);
			Assert.AreEqual("default", a.Value("ca", "default"));
			Assert.AreEqual("default", a.Value("xx", "default"));
			Assert.AreEqual("D", a.Value("db", "default"));
		}

		[TestMethod]
		public void IntValueTest()
		{
			var args = Args1.ToList();
			args.Add("-i:123");
			args.Add("-j:");
			args.Add("234");
			var a = new Arguments(args);
			Assert.AreEqual(-1, a.Value("ca", -1));
			Assert.AreEqual(-1, a.Value("xx", -1));
			Assert.AreEqual(-1, a.Value("db", -1));
			Assert.AreEqual(123, a.Value("i", -1));
			Assert.AreEqual(234, a.Value("j", -1));
		}

		[TestMethod]
		public void DecimalValueTest()
		{
			var args = Args1.ToList();
			args.Add("-io:123.11");
			args.Add("-j:");
			args.Add("234");
			var a = new Arguments(args);
			Assert.AreEqual(-1m, a.Value("ca", -1m));
			Assert.AreEqual(-1m, a.Value("xx", -1m));
			Assert.AreEqual(-1m, a.Value("db", -1m));
			Assert.AreEqual(123.11m, a.Value("index of", -1m));
			Assert.AreEqual(234, a.Value("j", default(decimal?)));
		}

		[TestMethod]
		public void DateTimeValueTest()
		{
			var args = Args1.ToList();
			args.Add("-io:2011-11-11");
			args.Add("-j:");
			args.Add("20111122");
			var a = new Arguments(args);
			Assert.AreEqual(default, a.Value("ca", default(DateTime)));
			Assert.AreEqual(default, a.Value("xx", default(DateTime)));
			Assert.AreEqual(default, a.Value("db", default(DateTime)));
			Assert.AreEqual(new DateTime(2011, 11, 11), a.Value<DateTime?>("index of"));
			Assert.AreEqual(new DateTime(2011, 11, 22), a.Value("june", DateTime.MinValue));
		}

		[TestMethod]
		public void FirstTest()
		{
			var args = Args1.ToList();
			args.Insert(0, "Y");
			args.Insert(0, "X");
			args.Insert(0, "-xx:");
			var a = new Arguments(args);
			Assert.AreEqual("Y", a.First());
		}

		[TestMethod]
		public void PositionalTest()
		{
			var args = Args1.ToList();
			args.Add("-xx:");
			args.Add("X");
			args.Add("Y");
			var a = new Arguments(args);
			CollectionAssert.AreEqual(new [] { "", "a", "b", "C", "Y" }, a.Positional.ToList());
		}

		public static ArgumentsBuilder LsParameters()
		{
			return new ArgumentsBuilder()
			   .Switch("all", "a", "do not ignore entries starting with .")
			   .Switch("almost-all", new[] { "A" }, "do not list implied . and ..")
			   .Switch("author", description: "with -l, print the author of each file")
			   .Switch("escape", "b", description: "print C-style escapes for nongraphic characters")
			   .Parameter("block-size", valueName: "SIZE", description: "with -l, scale sizes by SIZE when printing them; e.g.,\n'--block-size=M'; see SIZE format below")
			   .Switch("ignore-backups", "B", description: "do not list implied entries ending with ~")
			   .Switch("c", description: "with -lt: sort by, and show, ctime (time of last\nmodification of file status information); with -l: show\nctime and sort by name; otherwise: sort by ctime, newest\nfirst")
			   .Switch("C", description: "list entries by columns")
			   .Parameter("color", valueName: "WHEN", description: "color the output WHEN; more info below")
			   .Switch("directory", "d", description: "list directories themselves, not their contents")
			   .Switch("dired", "D", description: "generate output designed for Emacs' dired mode")
			   .Switch("f", description: "list all entries in directory order")
			   .Parameter("classify", "F", valueName: "WHEN", description: "append indicator (one of */=>@|) to entries WHEN")
			   .Switch("file-type", description: "likewise, except do not append '*'")
			   .Parameter("format", valueName: "WORD", description: "across -x, commas -m, horizontal -x, long -l,\nsingle-column -1, verbose -l, vertical -C")
			   .Switch("full-time", description: "like -l --time-style=full-iso")
			   .Switch("g", description: "like -l, but do not list owner")
			   .Switch("group-directories-first", description: "group directories before files; can be augmented with a\n--sort option, but any use of --sort=none (-U) disables\ngrouping")
			   .Switch("no-group", "G", description: "in a long listing, don't print group names")
			   .Switch("human-readable", "h", description: "with -l and -s, print sizes like 1K 234M 2G etc.")
			   .Switch("si", description: "likewise, but use powers of 1000 not 1024")
			   .Switch("dereference-command-line", "H", description: "follow symbolic links listed on the command line")
			   .Switch("dereference-command-line-symlink-to-dir", description: "follow each command line symbolic link that points to a\ndirectory")
			   .Parameter("hide", valueName: "PATTERN", description: "do not list implied entries matching shell PATTERN\n(overridden by -a or -A)")
			   .Parameter("hyperlink", valueName: "WHEN", description: "hyperlink file names WHEN")
			   .Parameter("indicator-style", valueName: "WORD", description: "append indicator with style WORD to entry names: none\n(default), slash (-p), file-type (--file-type), classify\n(-F)")
			   .Switch("inode", "i", description: "print the index number of each file")
			   .Parameter("ignore", "I", valueName: "PATTERN", description: "do not list implied entries matching shell PATTERN")
			   .Switch("kibibytes", "k", description: "default to 1024-byte blocks for file system usage; used\nonly with -s and per directory totals")
			   .Switch("l", description: "use a long listing format")
			   .Switch("dereference", "L", description: "when showing file information for a symbolic link, show\ninformation for the file the link references rather than\nfor the link itself")
			   .Switch("m", description: "fill width with a comma separated list of entries")
			   .Switch("numeric-uid-gid", "n", description: "like -l, but list numeric user and group IDs")
			   .Switch("literal", "N", description: "print entry names without quoting")
			   .Switch("o", description: "like -l, but do not list group information")
			   .Parameter("indicator-style", "p", valueName: "slash", description: "append / indicator to directories")
			   .Switch("hide-control-chars", "q", description: "print ? instead of nongraphic characters")
			   .Switch("show-control-chars", description: "show nongraphic characters as-is (the default, unless\nprogram is 'ls' and output is a terminal)")
			   .Switch("quote-name", "Q", description: "enclose entry names in double quotes")
			   .Parameter("quoting-style", valueName: "WORD", description: "use quoting style WORD for entry names: literal, locale,\nshell, shell-always, shell-escape, shell-escape-always, c,\nescape (overrides QUOTING_STYLE environment variable)")
			   .Switch("reverse", "r", description: "reverse order while sorting")
			   .Switch("recursive", "R", description: "list subdirectories recursively")
			   .Switch("size", "s", description: "print the allocated size of each file, in blocks")
			   .Switch("S", description: "sort by file size, largest first")
			   .Parameter("sort", valueName: "WORD", description: "sort by WORD instead of name: none (-U), size (-S), time\n(-t), version (-v), extension (-X), width")
			   .Parameter("time", valueName: "WORD", description: "change the default of using modification times; access\ntime (-u): atime, access, use; change time (-c): ctime,\nstatus; birth time: birth, creation;\n\nwith -l, WORD determines which time to show; with\n--sort=time, sort by WORD (newest first)")
			   .Parameter("time-style", valueName: "TIME_STYLE", description: "time/date format with -l; see TIME_STYLE below")
			   .Switch("t", description: "sort by time, newest first; see --time")
			   .Parameter("tabsize", "T", valueName: "COLS", description: "assume tab stops at each COLS instead of 8")
			   .Switch("u", description: "with -lt: sort by, and show, access time; with -l: show\naccess time and sort by name; otherwise: sort by access\ntime, newest first")
			   .Switch("U", description: "do not sort; list entries in directory order")
			   .Switch("v", description: "natural sort of (version) numbers within text")
			   .Parameter("width", "w", valueName: "COLS", description: "set output width to COLS.  0 means no limit")
			   .Switch("x", description: "list entries by lines instead of by columns")
			   .Switch("X", description: "sort alphabetically by entry extension")
			   .Switch("context", "Z", description: "print any security context of each file")
			   .Switch("zero", description: "end each output line with NUL, not newline")
			   .Switch("1", description: "list one file per line")
			   .Switch("help", description: "display this help and exit")
			   .Switch("version", description: "output version information and exit")
			   .Array("FILE", positional: true, description: "files to list")
			   ;
		}

		private static readonly string[] Args2 = new[] { "", "a", "b", "-ca", "C", "/db:D" };

		[TestMethod]
		public void UnixSwitchTest()
		{
			var lsa = LsParameters()
				.UnixStyle();
			var a = lsa.Build(Args2);
		}
//			var param = new Parameters()
//				.Switch("test")
//				.Parameter<decimal>("percent", "percent value")
//				.Parameter<string>("output file", "path to the output file")
//				.Parameter<string>("input file");
	}
}
