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
		Assert.AreEqual(0, diagnostics.Length, String.Join("; ", diagnostics));
	}


	static Compilation CreateCompilation(string source)
	=> CSharpCompilation.Create("compilation",
		new[] { CSharpSyntaxTree.ParseText(source) },
		new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
		new CSharpCompilationOptions(OutputKind.ConsoleApplication));

	private const string SeparateText = """
		[CliParameters(IgnoreCase = true)]
		partial class SampleOption
		{
		    [CliOption("a", ValueName = "alpha", Description = "alpha option")]
		    public float Alpha { get; init; }

		    [CliOption("b", "bt", ValueName = "beta", Description = "beta option")]
		    public float Beta { get; init; }

			[CliCommand("new", Description = "Create something")]
			public CommandCreate? Create { get; init; }

			[CliCommand("del", Description = "Delete something")]
			public CommandDelete? Delete { get; init; }
		}
		
		[CliParameters]
		public partial class CommandCreate
		{
			[CliOption("a", Description = "alpha option")]
			public int Alpha { get; init; }
		}
		
		[CliParameters]
		public partial class CommandDelete: ICliOption<CommandDelete>
		{
			[CliOption("a")]
			public int Alpha { get; init; }
		}
		""";

	private const string NestedText = """
		[CliParameters(IgnoreCase = true)]
		partial class SampleOptionSecond
		{
		    [CliOption("a", ValueName = "alpha", Description = "alpha option")]
		    public float Alpha { get; init; }

		    [CliOption("b", "bt", ValueName = "beta", Description = "beta option")]
		    public float Beta { get; init; }

			[CliCommand("new", Description = "Create something")]
			public CommandCreate? Create { get; init; }

			[CliCommand("del", Description = "Delete something")]
			public CommandDelete? Delete { get; init; }

			[CliParameters]
			public partial class CommandCreate
			{
				[CliOption("a", Description = "alpha option")]
				public int Alpha { get; init; }

				[CliCommand]
				public CommandCopy? Copy { get; init; }

				[CliParameters]
				public partial class CommandCopy
				{
					public int Source { get; init; }
				}
			}

			[CliParameters]
			public partial class CommandDelete: ICliOption<CommandDelete>
			{
				[CliOption("a")]
				public int Alpha { get; init; }
			}
		}
		""";
}