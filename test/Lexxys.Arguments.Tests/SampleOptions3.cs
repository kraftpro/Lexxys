using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;

namespace Lexxys.Argument.Tests.Template;

// [CliArguments(IgnoreCase = true)]
partial class SampleOption: ICliOption<SampleOption>
{
    [CliParam(["a"], ValueName = "alpha", Description = "alpha option")]
    public float Alpha { get; init; }

    [CliParam(["b", "bt"], ValueName = "beta", Description = "beta option")]
    public float Beta { get; init; }

    [CliParam(["c", "g"], ValueName = "gamma", Description = "gamma option")]
    public float Gamma { get; init; }

	[CliParam(["i"], ValueName = "input", Description = "input file")]
	public FileInfo? Input { get; init; }

	[CliParam(["o"], ValueName = "output", Description = "output file")]
	public FileInfo? Output { get; init; }

	[CliCommand("new", Description = "Create something")]
	public CommandCreate? Create { get; init; }

	[CliCommand("del", Description = "Delete something")]
	public CommandDelete? Delete { get; init; }

	// [CliArguments]
	public partial class CommandCreate
	{
		[CliParam(["a"], Description = "alpha option")]
		public int Alpha { get; init; }

		[CliParam(["b"], Description = "beta option")]
		public int Beta { get; init; }

		[CliParam(["c"], Description = "gamma option")]
		public int Gamma { get; init; }
	}

	// [CliArguments]
	public partial class CommandDelete: ICliOption<CommandDelete>
	{
		[CliParam("a")]
		public int Alpha { get; init; }

		[CliParam(["b", "bb", "bbb"], ValueName = "beta", Description = "bbb")]
		public int Beta { get; init; }

		[CliParam(["c", "cc", "ccc"])]
		public int Gamma { get; init; }
	}
}



partial class SampleOption: ICliOption<SampleOption>
{
	public static SampleOption ParseArguments(IReadOnlyCollection<string> args) => Parse(CreateBuilder().Build(args));

	public static SampleOption Parse(IArgumentCommand c) => new SampleOption
	{
		Alpha = c.Parameters.Value<float>("alpha)", default),
		Beta = c.Parameters.Value<float>("beta)", default),
		Gamma = c.Parameters.Value<float>("gamma)", default),
		Input = c.Parameters.Value<FileInfo?>("input)", default),
		Output = c.Parameters.Value<FileInfo?>("output)", default),
		Create = c.Command?.Name == "create" ? CommandCreate.Parse(c.Command) : null,
		Delete = c.Command?.Name == "delete" ? CommandDelete.Parse(c.Command) : null,
	};

	public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
		.Parameter("alpha", __aliases[0], valueName: "alpha", description: "alpha option")
		.Parameter("beta", __aliases[1], valueName: "beta", description: "beta option")
		.Parameter("gamma", __aliases[2], valueName: "gamma", description: "gamma option")
		.Parameter("input", __aliases[3], valueName: "input", description: "input file")
		.Parameter("output", __aliases[4], valueName: "output", description: "output file")
		.Command<CommandCreate>("create", __aliases[5], description: "Create something")
		.Command<CommandDelete>("delete", __aliases[6], description: "Delete something");

	private static readonly string[][] __aliases = [["a"], ["b", "bt"], ["c", "g"], ["i"], ["o"], ["new"], ["del"]];

	partial class CommandCreate: ICliOption<CommandCreate>
	{
		public static CommandCreate ParseArguments(IReadOnlyCollection<string> args) => Parse(CreateBuilder().Build(args).Root);

		public static CommandCreate Parse(IArgumentCommand c) => new CommandCreate
		{
			Alpha = c.Parameters.Value<int>("alpha)", default),
			Beta = c.Parameters.Value<int>("beta)", default),
			Gamma = c.Parameters.Value<int>("gamma)", default),
		};

		public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", __aliases[0], description: "alpha option")
			.Parameter("beta", __aliases[1], description: "beta option")
			.Parameter("gamma", __aliases[2], description: "gamma option");

		private static readonly string[][] __aliases = [["a"], ["b"], ["c"]];
	}

	partial class CommandDelete: ICliOption<CommandDelete>
	{
		public static CommandDelete ParseArguments(IReadOnlyCollection<string> args) => Parse(CreateBuilder().Build(args).Root);

		public static CommandDelete Parse(IArgumentCommand c) => new CommandDelete
		{
			Alpha = c.Parameters.Value<int>("alpha)", default),
			Beta = c.Parameters.Value<int>("beta)", default),
			Gamma = c.Parameters.Value<int>("gamma)", default),
		};

		public static Arguments<CommandDelete> Parse2(IArgumentCommand c) => new CommandDelete
		{
			Alpha = c.Parameters.Value<int>("alpha)", default),
			Beta = c.Parameters.Value<int>("beta)", default),
			Gamma = c.Parameters.Value<int>("gamma)", default),
		};

		public static ArgumentsBuilder CreateBuilder(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", __aliases[0])
			.Parameter("beta", __aliases[1], valueName: "beta", description: "bbb")
			.Parameter("gamma", __aliases[2]);

		private static readonly string[][] __aliases = [["a"], ["b", "bb", "bbb"], ["c", "cc", "ccc"]];
	}
}
