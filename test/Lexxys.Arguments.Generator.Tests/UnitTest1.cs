using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Reflection;

namespace Lexxys.Arguments.Generator.Tests;

[TestClass]
public class UnitTest1
{
	[TestMethod]
	public void CanGenerate_SeparateClasses()
	{
		var gen = new ArgumentCodeGen();
		var dr = CSharpGeneratorDriver.Create(gen);
		var result = dr.RunGeneratorsAndUpdateCompilation(CreateCompilation(SeparateText), out var output, out var diagnostics);
		Assert.AreEqual(0, diagnostics.Length);
	}

	[TestMethod]
	public void CanGenerate_NestedClasses()
	{
		var gen = new ArgumentCodeGen();
		var dr = CSharpGeneratorDriver.Create(gen);
		var result = dr.RunGeneratorsAndUpdateCompilation(CreateCompilation(NestedText), out var output, out var diagnostics);
		Assert.AreEqual(0, diagnostics.Length);
	}


	static Compilation CreateCompilation(string source)
	=> CSharpCompilation.Create("compilation",
		new[] { CSharpSyntaxTree.ParseText(source) },
		new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
		new CSharpCompilationOptions(OutputKind.ConsoleApplication));

	private const string SeparateText = """
		[CliArguments(IgnoreCase = true)]
		partial class SampleOption
		{
		    [CliParam("a", ValueName = "alpha", Description = "alpha option")]
		    public float Alpha { get; init; }

		    [CliParam("b", "bt", ValueName = "beta", Description = "beta option")]
		    public float Beta { get; init; }

			[CliCommand("new", Description = "Create something")]
			public CommandCreate? Create { get; init; }

			[CliCommand("del", Description = "Delete something")]
			public CommandDelete? Delete { get; init; }
		}
		
		[CliArguments]
		public partial class CommandCreate
		{
			[CliParam("a", Description = "alpha option")]
			public int Alpha { get; init; }
		}
		
		[CliArguments]
		public partial class CommandDelete: ICliOption<CommandDelete>
		{
			[CliParam("a")]
			public int Alpha { get; init; }
		}
		""";

	private const string NestedText = """
		[CliArguments(IgnoreCase = true)]
		partial class SampleOptionSecond
		{
		    [CliParam("a", ValueName = "alpha", Description = "alpha option")]
		    public float Alpha { get; init; }

		    [CliParam("b", "bt", ValueName = "beta", Description = "beta option")]
		    public float Beta { get; init; }

			[CliCommand("new", Description = "Create something")]
			public CommandCreate? Create { get; init; }

			[CliCommand("del", Description = "Delete something")]
			public CommandDelete? Delete { get; init; }

			[CliArguments]
			public partial class CommandCreate
			{
				[CliParam("a", Description = "alpha option")]
				public int Alpha { get; init; }

				[CliCommand]
				public CommandCopy? Copy { get; init; }

				[CliArguments]
				public partial class CommandCopy
				{
					public int Source { get; init; }
				}
			}

			[CliArguments]
			public partial class CommandDelete: ICliOption<CommandDelete>
			{
				[CliParam("a")]
				public int Alpha { get; init; }
			}
		}
		""";
}