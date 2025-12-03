using Lexxys;

namespace Lexxys.Con.ArgsCon;

// [CliParameters(IgnoreCase = true)]
partial class SampleOption3
{
    [CliOption(["a"], ValueName = "alpha", Description = "alpha option")]
    public float Alpha { get; init; }

    [CliOption(["b", "bt"], ValueName = "beta", Description = "beta option")]
    public float Beta { get; init; }

    [CliOption(["c", "g"], ValueName = "gamma", Description = "gamma option")]
    public float Gamma { get; init; }

	[CliOption(["i"], ValueName = "input", Description = "input file")]
	public FileInfo? Input { get; init; }

	[CliOption(["o"], ValueName = "output", Description = "output file")]
	public FileInfo? Output { get; init; }

	[CliCommand("new", Description = "Create something")]
	public CommandCreate? Create { get; init; }

	[CliCommand("del", Description = "Delete something")]
	public CommandDelete? Delete { get; init; }

	// [CliParameters]
	public partial class CommandCreate
	{
		[CliOption(["a"], Description = "alpha option")]
		public int Alpha { get; init; }

		[CliOption(["b"], Description = "beta option")]
		public int Beta { get; init; }

		[CliOption(["c"], Description = "gamma option")]
		public int Gamma { get; init; }
	}

	// [CliParameters]
	public partial class CommandDelete: ICliParameters<CommandDelete>
	{
		[CliOption("a")]
		public int Alpha { get; init; }

		[CliOption(["b", "bb", "bbb"], ValueName = "beta", Description = "bbb")]
		public int Beta { get; init; }

		[CliOption(["c", "cc", "ccc"])]
		public int Gamma { get; init; }

		static CommandDelete ICliParameters<CommandDelete>.Parse(IArguments cmd, ICollection<string>? error)
		{
			throw new NotImplementedException();
		}
	}
}



partial class SampleOption3: ICliParameters<SampleOption3>
{
	public static Arguments<SampleOption3> Parse(IEnumerable<string> args, ArgumentsBuilder? builder = null)
	{
		Arguments arguments = CreateBuilder(builder).Parse(args);
		var error = new List<string>();
		var obj = Parse(arguments, error);
		return new Arguments<SampleOption3>(arguments, obj, error);
	}

	public static SampleOption3 Parse(IArguments cmd, ICollection<string>? error)
	{
		if (cmd is null) throw new ArgumentNullException(nameof(cmd));

		return new SampleOption3
		{
			Alpha = cmd.GetValue<float>("alpha", default),
			Beta = cmd.GetValue<float>("beta", default),
			Gamma = cmd.GetValue<float>("gamma", default),
			Input = cmd.GetValue<FileInfo?>("input", default),
			Output = cmd.GetValue<FileInfo?>("output", default),
			Create = cmd.Command?.Name == "create" ? CommandCreate.Parse(cmd.Command, error): null,
			Delete = cmd.Command?.Name == "delete" ? CommandDelete.Parse(cmd.Command, error): null,
		};
	}

	public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
		.Parameter("alpha", __aliases[0], valueName: "alpha", description: "alpha option")
		.Parameter("beta", __aliases[1], valueName: "beta", description: "beta option")
		.Parameter("gamma", __aliases[2], valueName: "gamma", description: "gamma option")
		.Parameter("input", __aliases[3], valueName: "input", description: "input file")
		.Parameter("output", __aliases[4], valueName: "output", description: "output file")
		.Command("create", __aliases[5], description: "Create something")
		.Command("delete", __aliases[6], description: "Delete something");

	private static readonly string[][] __aliases = [["a"], ["b", "bt"], ["c", "g"], ["i"], ["o"], ["new"], ["del"]];

	partial class CommandCreate: ICliParameters<CommandCreate>
	{
		public static CommandCreate ParseArguments(IEnumerable<string> args) => Parse(CreateBuilder().Parse(args), null);

		public static CommandCreate Parse(IArguments c, ICollection<string>? error) => new CommandCreate
		{
			Alpha = c.GetValue<int>("alpha)", default),
			Beta = c.GetValue<int>("beta)", default),
			Gamma = c.GetValue<int>("gamma)", default),
		};

		public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", __aliases[0], description: "alpha option")
			.Parameter("beta", __aliases[1], description: "beta option")
			.Parameter("gamma", __aliases[2], description: "gamma option");

		static CommandCreate ICliParameters<CommandCreate>.Parse(IArguments cmd, ICollection<string>? error)
		{
			throw new NotImplementedException();
		}

		static ArgumentsBuilder ICliOptionBase<CommandCreate>.CreateBuilder(ArgumentsBuilder? builder)
		{
			throw new NotImplementedException();
		}

		private static readonly string[][] __aliases = [["a"], ["b"], ["c"]];
	}

	partial class CommandDelete: ICliParameters<CommandDelete>
	{
		public static CommandDelete ParseArguments(IEnumerable<string> args, ICollection<string>? error = null) => Parse(CreateBuilder().Parse(args), error);

		public static CommandDelete Parse(IArguments cmd, ICollection<string>? error = null)
		{
			if (cmd is null) throw new ArgumentNullException(nameof(cmd));

			return new CommandDelete
			{
				Alpha = cmd.GetValue<int>("alpha", default),
				Beta = cmd.GetValue<int>("beta", default),
				Gamma = cmd.GetValue<int>("gamma", default),
			};
		}

		//public static Arguments<CommandDelete> Parse(Arguments args)
		//{
		//	var error = new List<string>();
		//	var v = Parse(args, error);
		//	var r = new Arguments<CommandDelete>(args, v);
		//	foreach (var item in error)
		//	{
		//		r.Errors.Add(item);
		//	}
		//	return r;
		//}

		public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", __aliases[0])
			.Parameter("beta", __aliases[1], valueName: "beta", description: "bbb")
			.Parameter("gamma", __aliases[2]);

		private static readonly string[][] __aliases = [["a"], ["b", "bb", "bbb"], ["c", "cc", "ccc"]];
	}
}
