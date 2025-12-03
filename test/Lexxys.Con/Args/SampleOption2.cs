namespace Lexxys.Con.ArgsCon;

[CliParameters]
internal partial class SampleOption2
{
	[CliOption(["a"], ValueName = "alpha-value", Description = "alpha option")]
	public float Alpha { get; init; }

	[CliOption(["b", "bt"], ValueName = "beta-value", Description = "beta option")]
	public float Beta { get; init; }

	[CliOption(["c", "g"], ValueName = "gamma-value", Description = "gamma option")]
	public float Gamma { get; init; }

	[CliOption(["i"], ValueName = "input-file", Description = "input file")]
	public FileInfo? Input { get; init; }

	[CliOption(["o"], ValueName = "output-file", Description = "output file")]
	public FileInfo? Output { get; init; }

	[CliCommand("new", Description = "Create something")]
	public CommandCreate? Create { get; init; }

	[CliCommand("del", Description = "Delete something")]
	public CommandDelete? Delete { get; init; }

	[CliParameters]
	public partial class CommandCreate
	{
		[CliOption(["a"], Description = "alpha option")]
		public int Alpha { get; init; }

		[CliOption(["b"], Description = "beta option")]
		public int Beta { get; init; }

		[CliOption(["c"], Description = "gamma option")]
		public int Gamma { get; init; }
	}

	[CliParameters]
	public partial class CommandDelete //: ICliOption<CommandDelete>
	{
		[CliOption(["a"])]
		public int Alpha { get; init; }

		[CliOption(["b", "bb", "bbb"], ValueName = "beta", Description = "bbb")]
		public int Beta { get; init; }

		[CliOption(["c", "cc", "ccc"])]
		public int Gamma { get; init; }
	}
}
