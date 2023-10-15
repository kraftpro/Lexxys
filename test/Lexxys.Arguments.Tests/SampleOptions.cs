using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;

namespace Lexxys.Argument.Tests;

[CliArguments(IgnoreCase = true)]
partial class SampleOption
{
    [CliParam("a", ValueName = "alpha", Description = "alpha option")]
    public float Alpha { get; init; }

    [CliParam("b", "bt", ValueName = "beta", Description = "beta option")]
    public float Beta { get; init; }

    [CliParam("c", "g", ValueName = "gamma", Description = "gamma option")]
    public float Gamma { get; init; }

	[CliParam("i", ValueName = "input", Description = "input file")]
	public FileInfo? Input { get; init; }

	[CliParam("o", ValueName = "output", Description = "output file")]
	public FileInfo? Output { get; init; }

	[CliCommand("new", Description = "Create something")]
	public CommandCreate? Create { get; init; }

	[CliCommand("del", Description = "Delete something")]
	public CommandDelete? Delete { get; init; }

	[CliArguments]
	public partial class CommandCreate
	{
		[CliParam("a", Description = "alpha option")]
		public int Alpha { get; init; }

		[CliParam("b", Description = "beta option")]
		public int Beta { get; init; }

		[CliParam("c", Description = "gamma option")]
		public int Gamma { get; init; }
	}

	[CliArguments]
	public partial class CommandDelete: ICliOption<CommandDelete>
	{
		[CliParam("a")]
		public int Alpha { get; init; }

		[CliParam("b", "bb", "bbb", ValueName = "beta", Description = "bbb")]
		public int Beta { get; init; }

		[CliParam("c", "cc", "ccc")]
		public int Gamma { get; init; }
	}
}

partial class SampleOption: ICliOption<SampleOption>
{
	public static SampleOption ParseArguments(IReadOnlyCollection<string> args) => Parse(CreateBuilder().Build(args).Container);
	public static ArgumentsBuilder CreateBuilder() => Build(new ArgumentsBuilder());
	
	public static SampleOption Parse(ArgumentCommand c) => new()
	{
		Alpha = c.Parameters.Value<int>("alpha", default),
		Beta = c.Parameters.Value<float>("beta", default),
		Gamma = c.Parameters.Value<float>("gamma", default),
		Input = c.Parameters.Value("input") is string v1 ? new FileInfo(v1): null,
		Output = c.Parameters.Value("output") is string v2 ? new FileInfo(v2) : null,
		Create = c.Command?.Name == "new" ? CommandCreate.Parse(c.Command): null,
		Delete = c.Command?.Name == "del" ? CommandDelete.Parse(c.Command): null,
	};

	public static ArgumentsBuilder Build(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
		.Parameter("alpha", new[] { "a" }, description: "alpha option")
		.Parameter("beta", new[] { "b", "bt" }, description: "beta option")
		.Parameter("gamma", new[] { "c", "g" }, description: "gamma option")
		.Parameter("input", new[] { "i" }, description: "input file")
		.Parameter("output", new[] { "o" }, description: "output file")
		.AddCommand<CommandCreate>("new", description: "Create something")
		.AddCommand<CommandDelete>("del", description: "Delete something");

	public partial class CommandCreate: ICliOption<CommandCreate>
	{
		public static CommandCreate Parse(ArgumentCommand c) => new()
		{
			Alpha = c.Parameters.Value<int>("a", default),
			Beta = c.Parameters.Value<int>("b", default),
			Gamma = c.Parameters.Value<int>("c", default)
		};

		public static ArgumentsBuilder Build(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", new[] { "a" }, description: "alpha option")
			.Parameter("beta", new[] { "b" }, description: "beta option")
			.Parameter("gamma", new[] { "c" }, description: "gamma option");
	}

	public partial class CommandDelete
	{
		public static CommandDelete Parse(ArgumentCommand c) => new()
		{
			Alpha = c.Parameters.Value<int>("a", default),
			Beta = c.Parameters.Value<int>("b", default),
			Gamma = c.Parameters.Value<int>("x", default)
		};

		public static ArgumentsBuilder Build(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", new[] { "a" }, description: "alpha option")
			.Parameter("beta", new[] { "b", "bb", "bbb" }, description: "bbb")
			.Parameter("gamma", new[] { "c", "cc", "ccc" }, description: "gamma option");
	}
}
