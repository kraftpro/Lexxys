using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;

namespace Lexxys.Argument.Tests;

class SampleOption
{
    [CliParam("a", ValueName = "alpha", Description = "alpha option")]
    public float Alpha { get; set; }

    [CliParam("b", "bt", ValueName = "beta", Description = "beta option")]
    public float Beta { get; set; }

    [CliParam("c", "g", ValueName = "gamma", Description = "gamma option")]
    public float Gamma { get; set; }

	[CliParam("i", ValueName = "file", Description = "input file")]
	public FileInfo? Input { get; set; }

	[CliParam("o", ValueName = "file", Description = "output file")]
	public FileInfo? Output { get; set; }

	[CliCommand("new", Description = "Create something")]
	public CommandCreate? Create { get; set; }

	[CliCommand("del", Description = "Delete something")]
	public CommandDelete? Delete { get; set; }

	//public static bool TryParse(IReadOnlyList<string> args, out SampleOption? options)
	//{
	//	int iDiv = args.FindIndex("--");
	//	int cmd = 0;
	//	int iCmd = -1;
	//	int i = CmdIndex(args, o => o is "n" or "new", iCmd, iDiv);
	//	if (i >= 0) { cmd = 1; iCmd = i; }
	//	i = CmdIndex(args, o => o is "d" or "del", iCmd, iDiv);
	//	if (i >= 0) { cmd = 2; iCmd = i; }
	//	i = CmdIndex(args, o => o is "u" or "update", iCmd, iDiv);
	//	if (i >= 0) { cmd = 3; iCmd = i; }

	//	var iCmd1 = CmdIndex(args, o => o is "n" or "new", -1, iDiv);
	//	int iCmd2 = CmdIndex(args, o => o is "d" or "del", -1, iDiv);
	//	if (iCmd >= 0 && iCmd2 >= 0)
	//		if (iCmd1 < iCmd2)
	//			iCmd2 = -1;
	//		else
	//			iCmd1 = -1;



	//	options = null;
	//	if (args == null || args.Count == 0)
	//		return false;
	//	//var parser = new ArgumentsParser<SampleOptions>(args);
	//	//options = parser.Parse();
	//	return true;

	//	static int CmdIndex(IReadOnlyList<string> args, Predicate<string> predicate, int iCmd, int iDiv)
	//	{
	//		int i = args.FindIndex(predicate);
	//		return i < 0 || i > iDiv ? -1 : i;
	//	}
	//}

	//private int ParseParameter(IReadOnlyList<string> args, int index, SampleOption options)
	//{
	//	var s = args[index].AsSpan();
	//	if (s.Length > 0 && (s[0] == '-' || s[0] == '/'))
	//	{
	//		if (s.Length == 1)
	//			return 0;
	//		s = s[0] == '-' && s[1] == '-' ? s.Slice(2) : s.Slice(1);
	//		if (s.Length == 0)
	//			return 0;
	//		var i = s.IndexOf('=');
	//		var j = s.IndexOf(':');
	//		if (i < 0 || (j >= 0 && j < i))
	//			i = j;
	//		j = s.IndexOf(' ');
	//		if (i < 0 || (j >= 0 && j < i))
	//			i = j;
	//		var v = ReadOnlySpan<char>.Empty;
	//		int len = 1;
	//		if (i >= 0)
	//		{
	//			v = s.Slice(0, i);
	//			s = s.Slice(i + 1);
	//		}
	//		else
	//		{
	//			len = 2;
	//			v = args[index + 1].AsSpan();
	//		}
	//		if (s == "a" || s == "alpha")
	//		{
	//			if (int.TryParse(v, out var value))
	//			{
	//				options.Alpha = value;
	//				return len;
	//			}
	//			return -len;
	//		}
	//		if (s == "b" || s == "bt" || s == "beta")
	//		{
	//			if (int.TryParse(v, out var value))
	//			{
	//				options.Beta = value;
	//				return len;
	//			}
	//			return -len;
	//		}
	//	}

	//	return 0;
	//}

	public class CommandCreate
	{
		[CliParam("a", Description = "alpha option")]
		public int Alpha { get; set; }

		[CliParam("b", Description = "beta option")]
		public int Beta { get; set; }

		[CliParam("c", Description = "gamma option")]
		public int Gamma { get; set; }

		public static CommandCreate Parse(ArgumentCommand c)
		{
            var x = new CommandCreate();
            x.Alpha = c.Parameters.Value<int>("a", default);
            x.Beta = c.Parameters.Value<int>("b", default);
            x.Gamma = c.Parameters.Value<int>("c", default);
            return x;
        }
	}

	public class CommandDelete
	{
		[CliParam("a")]
		public int Alpha { get; set; }

		[CliParam("b", "bb", "bbb", ValueName = "beta", Description = "bbb")]
		public int Beta { get; set; }

		[CliParam("c", "cc", "ccc")]
		public int Gamma { get; set; }

		public static CommandDelete Parse(ArgumentCommand c)
		{
            var x = new CommandDelete();
            x.Alpha = c.Parameters.Value<int>("a", default);
            x.Beta = c.Parameters.Value<int>("b", default);
            x.Gamma = c.Parameters.Value<int>("x", default);
            return x;
        }
	}

	public static SampleOption Parse(Arguments c)
	{
		var x = new SampleOption();
        x.Alpha = c.Parameters.Value<int>("alpha", default);
		x.Beta = c.Parameters.Value<float>("beta", default);
		x.Gamma = c.Parameters.Value<float>("gamma", default);
		x.Input = null;
		x.Output = null;
		if (c.Command?.Name == "new")
            x.Create = CommandCreate.Parse(c.Command);
		if (c.Command?.Name == "del")
            x.Delete = CommandDelete.Parse(c.Command);
		return x;
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class CliCommandAttribute : Attribute
{
	public string[]? Alias { get; init; }
	public string? Description { get; init; }

	public CliCommandAttribute()
	{
	}

	public CliCommandAttribute(params string[]? alias)
	{
		Alias = alias;
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class CliParamAttribute: Attribute
{
	public string[]? Alias { get; init; }
	public string? ValueName { get; init; }
	public string? Description { get; init; }
    public bool Required { get; init; }
	public int Position { get; init; }
    public bool Positional => Position > 0;

	public CliParamAttribute() 
	{ }

    public CliParamAttribute(params string[]? alias) => Alias = alias;

    public CliParamAttribute(int position) => Position = position;
}
