using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;

namespace Lexxys.Arguments.Tests;

class SampleOptions
{
    [ParamAttrubute("a", ValueName = "alpha", Description = "alpha option")]
    public float Alpha { get; set; }

    [ParamAttrubute("b", "bt", ValueName = "beta", Description = "beta option")]
    public float Beta { get; set; }

    [ParamAttrubute("c", "g", ValueName = "gamma", Description = "gamma option")]
    public float Gamma { get; set; }

	[ParamAttrubute("i", ValueName = "file", Description = "input file")]
	public FileInfo? Input { get; set; }

	[ParamAttrubute("o", ValueName = "file", Description = "output file")]
	public FileInfo? Output { get; set; }

	[CommandAttrubute("new", Description = "Create something")]
	public CommandCreate? Create { get; set; }

	[CommandAttrubute("del", Description = "Delete something")]
	public CommandDelete? Delete { get; set; }

	public static bool TryParse(IReadOnlyList<string> args, out SampleOptions? options)
	{
		int iDiv = args.FindIndex("--");
		int cmd = 0;
		int iCmd = -1;
		int i = CmdIndex(args, o => o is "n" or "new", iCmd, iDiv);
		if (i >= 0) { cmd = 1; iCmd = i; }
		i = CmdIndex(args, o => o is "d" or "del", iCmd, iDiv);
		if (i >= 0) { cmd = 2; iCmd = i; }
		i = CmdIndex(args, o => o is "u" or "update", iCmd, iDiv);
		if (i >= 0) { cmd = 3; iCmd = i; }

		var iCmd1 = CmdIndex(args, o => o is "n" or "new", -1, iDiv);
		int iCmd2 = CmdIndex(args, o => o is "d" or "del", -1, iDiv);
		if (iCmd >= 0 && iCmd2 >= 0)
			if (iCmd1 < iCmd2)
				iCmd2 = -1;
			else
				iCmd1 = -1;



		options = null;
		if (args == null || args.Length == 0)
			return false;
		//var parser = new ArgumentsParser<SampleOptions>(args);
		//options = parser.Parse();
		return true;

		static int CmdIndex(IReadOnlyList<string> args, Predicate<string> predicate, int iCmd, int iDiv)
		{
			int i = args.FindIndex(predicate);
			return i < 0 || i > iDiv ? -1 : i;
		}
	}

	private int ParseParameter(IReadOnlyList<string> args, int index, SampleOptions options)
	{
		var s = args[index].AsSpan();
		if (s.Length > 0 && (s[0] == '-' || s[0] == '/'))
		{
			if (s.Length == 1)
				return 0;
			s = s[0] == '-' && s[1] == '-' ? s.Slice(2) : s.Slice(1);
			if (s.Length == 0)
				return 0;
			var i = s.IndexOf('=');
			var j = s.IndexOf(':');
			if (i < 0 || (j >= 0 && j < i))
				i = j;
			j = s.IndexOf(' ');
			if (i < 0 || (j >= 0 && j < i))
				i = j;
			var v = ReadOnlySpan<char>.Empty;
			int len = 1;
			if (i >= 0)
			{
				v = s.Slice(0, i);
				s = s.Slice(i + 1);
			}
			else
			{
				len = 2;
				v = args[index + 1].AsSpan();
			}
			if (s == "a" || s == "alpha")
			{
				if (int.TryParse(v, out var value))
				{
					options.Alpha = value;
					return len;
				}
				return -len;
			}
			if (s == "b" || s == "bt" || s == "beta")
			{
				if (int.TryParse(v, out var value))
				{
					options.Beta = value;
					return len;
				}
				return -len;
			}
		}

		return 0;
	}

	public class CommandCreate
	{
		[ParamAttrubute("a", Description = "alpha option")]
		public int Alpha { get; set; }

		[ParamAttrubute("b", Description = "beta option")]
		public int Beta { get; set; }

		[ParamAttrubute("c", Description = "gamma option")]
		public int Gamma { get; set; }
	}

	public class CommandDelete
	{
		[ParamAttrubute("a")]
		public int Alpha { get; set; }

		[ParamAttrubute("b", "bb", "bbb", ValueName = "beta", Description = "bbb")]
		public int Beta { get; set; }

		[ParamAttrubute("c", "cc", "ccc")]
		public int Gamma { get; set; }
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class CommandAttrubute : Attribute
{
	public string[]? Alias { get; init; }
	public string? Description { get; init; }

	public CommandAttrubute()
	{
	}

	public CommandAttrubute(params string[]? alias)
	{
		Alias = alias;
	}
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class ParamAttrubute: Attribute
{
	public string[]? Alias { get; init; }
	public string? ValueName { get; init; }
	public string? Description { get; init; }
    public bool Required { get; init; }
	public int Position { get; init; }
    public bool Positional => Position > 0;

	public ParamAttrubute() 
	{ }

    public ParamAttrubute(params string[]? alias) => Alias = alias;

    public ParamAttrubute(int position) => Position = position;
}
