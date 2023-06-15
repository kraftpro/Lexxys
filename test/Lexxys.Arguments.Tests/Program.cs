using System.Security.Cryptography.X509Certificates;

args = new[] { "-a", "1", "-b", "2", "-c", "3" };

var arguments = new ArgumentsParser<SampleOptions>(args);

var options = arguments.Parse();


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

	public static bool TryParse(string[] args, out SampleOptions? options)
	{
		options = null;
		if (args == null || args.Length == 0)
			return false;
		var parser = new ArgumentsParser<SampleOptions>(args);
		options = parser.Parse();
		return true;
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

