namespace Lexxys.Argument.Tests;

[CliArguments]
internal partial class SampleOption2
{
	[CliParam(["a"], ValueName = "alpha-value", Description = "alpha option")]
	public float Alpha { get; init; }

	[CliParam(["b", "bt"], ValueName = "beta-value", Description = "beta option")]
	public float Beta { get; init; }

	[CliParam(["c", "g"], ValueName = "gamma-value", Description = "gamma option")]
	public float Gamma { get; init; }

	[CliParam(["i"], ValueName = "input-file", Description = "input file")]
	public FileInfo? Input { get; init; }

	[CliParam(["o"], ValueName = "output-file", Description = "output file")]
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
