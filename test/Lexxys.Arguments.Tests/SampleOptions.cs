using System.Diagnostics.SymbolStore;
using System.Text.RegularExpressions;

namespace Lexxys.Argument.Tests;

[CliArguments(IgnoreCase = true)]
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

	[CliArguments]
	public partial class CommandCreate
	{
		[CliParam(["a"], Description = "alpha option")]
		public int Alpha { get; init; }

		[CliParam(["b"], Description = "beta option")]
		public int Beta { get; init; }

		[CliParam(["c"], Description = "gamma option")]
		public int Gamma { get; init; }
	}

	[CliArguments]
	public partial class CommandDelete: ICliOption<CommandDelete>
	{
		[CliParam(["a"])]
		public int Alpha { get; init; }

		[CliParam(["b", "bb", "bbb"], ValueName = "beta", Description = "bbb")]
		public int Beta { get; init; }

		[CliParam(["c", "cc", "ccc"])]
		public int Gamma { get; init; }
	}
}

#if false
partial class SampleOption: ICliOption<SampleOption>
{
	public static SampleOption ParseArguments(IReadOnlyCollection<string> args) => Parse(CreateBuilder().Build(args).Container);
	public static ArgumentsBuilder CreateBuilder() => Build(new ArgumentsBuilder());
	
	public static SampleOption Parse(ArgumentCommand c) => new()
	{
		Alpha = c.Parameters.Value<int>("alpha", default),
		Beta = c.Parameters.Value<float>("beta", default),
		Gamma = c.Parameters.Value<float>("gamma", default),
		//Input = c.Parameters.Value("input") is string v1 ? new FileInfo(v1) : null,
		Input = c.Parameters.Value<FileInfo?>("input", default),
		Output = c.Parameters.Value("output") is string v2 ? new FileInfo(v2) : null,
		Create = c.Command?.Name == "Create" ? CommandCreate.Parse(c.Command): null,
		Delete = c.Command?.Name == "Delete" ? CommandDelete.Parse(c.Command): null,
	};

	public static ArgumentsBuilder Build(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
		.Parameter("alpha", __aliases[0], description: "alpha option")
		.Parameter("beta", __aliases[1], description: "beta option")
		.Parameter("gamma", __aliases[2], description: "gamma option")
		.Parameter("input", __aliases[3], description: "input file")
		.Parameter("output", __aliases[4], description: "output file")
		.Command<CommandCreate>("new", description: "Create something")
		.Command<CommandDelete>("del", description: "Delete something");
	private static readonly string[][] __aliases = [["a"], ["b", "bt"], ["c", "g"], ["i"], ["o"]];

	public partial class CommandCreate: ICliOption<CommandCreate>
	{
		public static CommandCreate Parse(ArgumentCommand c) => new()
		{
			Alpha = c.Parameters.Value<int>("a", default),
			Beta = c.Parameters.Value<int>("b", default),
			Gamma = c.Parameters.Value<int>("c", default)
		};

		public static ArgumentsBuilder Build(ArgumentsBuilder? builder = null) => (builder ?? new ArgumentsBuilder())
			.Parameter("alpha", __aliases[0], description: "alpha option")
			.Parameter("beta", __aliases[1], description: "beta option")
			.Parameter("gamma", __aliases[2], description: "gamma option");
		private static readonly string[][] __aliases = [["a"], ["b"], ["c"]];
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
			.Parameter("alpha", __aliases[0], description: "alpha option")
			.Parameter("beta", __aliases[1], description: "bbb")
			.Parameter("gamma", __aliases[2], description: "gamma option");
		private static readonly string[][] __aliases = [["a"], ["b", "bb", "bbb"], ["c", "cc", "ccc"]];
	}
}

#endif

//public interface ICliOption<T>
//{
//	static abstract ArgumentsBuilder Build(ArgumentsBuilder? builder = null);
//	static abstract T Parse(ArgumentCommand c);
//}

//public static class ICliOptionExtensions
//{
//	public static ArgumentsBuilder AddCommand<T>(this ArgumentsBuilder builder, string name, string description) where T: ICliOption<T>
//	{
//		builder.BeginCommand(name, description);
//		T.Build(builder);
//		return builder.EndCommand();
//	}
//}

