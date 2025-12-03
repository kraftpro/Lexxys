using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Reflection;
using System.Runtime.Loader;

namespace Lexxys.Argument.Generator.Tests;

public class GeneratorTests
{
	[Test]
	public async Task CanGenerate_SeparateClasses()
	{
		var (output, _) = await RunGenerator(SeparateText);
		await AssertNoCompilationErrors(output);
	}

	[Test]
	public async Task CanGenerate_NestedClasses()
	{
		var (output, s) = await RunGenerator(NestedText);
		await AssertNoCompilationErrors(output);
	}

	[Test]
	public async Task GeneratedCode_BindsNamesAliasesCollectionsAndCommands()
	{
		var (output, source) = await RunGenerator(BindingText);
		await AssertNoCompilationErrors(output);
		var assembly = await LoadAssembly(output);
		var model = assembly.GetType("GeneratedBindingModel")!;
		var parse = model.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, [typeof(IReadOnlyCollection<string>)], null)!;
		Assert.NotNull(parse);
		var parsed = parse.Invoke(null, [new[] { "r", "-c", "7", "--values=1,2,3", "--names=alpha,beta", "--force" }]);
		await Assert.That(parsed).IsNotNull();
		var parsedType = parsed!.GetType();
		await Assert.That(String.Join("; ", (IEnumerable<string>)parsedType.GetProperty("Errors")!.GetValue(parsed)!)).IsEqualTo("");

		var value = parsedType.GetProperty("Value")!.GetValue(parsed)!;
		await Assert.That(model.GetProperty("Count")!.GetValue(value)).IsEqualTo(7); //, "Count was not parsed.");
		await Assert.That(((int[])model.GetProperty("Values")!.GetValue(value)!)).IsEquivalentTo([1, 2, 3]); // , "Values were not parsed.");
		await Assert.That(((List<string>)model.GetProperty("Names")!.GetValue(value)!)).IsEquivalentTo(["alpha", "beta"]); //, "Names were not parsed.");

		var run = model.GetProperty("Run")!.GetValue(value);
		await Assert.That(run).IsNotNull();
		await Assert.That(run!.GetType().GetProperty("Force")!.GetValue(run)).EqualTo(true); //, "Command option was not parsed.");
	}

	[Test]
	public async Task GeneratedCode_UsesCliParametersSettings()
	{
		var (output, _) = await RunGenerator(SettingsText);
		await AssertNoCompilationErrors(output);
		var assembly = await LoadAssembly(output);
		var model = assembly.GetType("GeneratedSettingsModel")!;
		var parse = model.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, [typeof(IReadOnlyCollection<string>)], null)!;

		var parsed = parse.Invoke(null, [new[] { "/VALUE=11" }]);
		await Assert.That(parsed).IsNotNull();
		var parsedType = parsed!.GetType();
		await Assert.That(!(bool)parsedType.GetProperty("HasErrors")!.GetValue(parsed)!).IsTrue(); //, "Parsed result has errors.");
		var value = parsedType.GetProperty("Value")!.GetValue(parsed)!;
		await Assert.That(model.GetProperty("Value")!.GetValue(value)).EqualTo(11); //, "Value was not parsed.");
	}

	[Test]
	public async Task GeneratedCode_UsesAttributtedNamingStyle()
	{
		var (output, _) = await RunGenerator(CurrentSettingsText);
		await AssertNoCompilationErrors(output);
		var assembly = await LoadAssembly(output);
		var model = assembly.GetType("GeneratedCurrentSettingsModel")!;
		var parse = model.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, [typeof(IReadOnlyCollection<string>)], null)!;
		var cases = new (ParameterNaming NamingStyle, string Argument, int Value)[]
		{
			(ParameterNaming.KebabCase, "--output-directory=13", -1),
			(ParameterNaming.PascalCase, "--OutputDirectory=15", -1),
			(ParameterNaming.SnakeCase, "--output_directory=16", 16),
		};

		foreach (var item in cases)
		{
			var parsed = parse.Invoke(null, [new[] { item.Argument, "--Exact.Name=value" }])!;

			if (item.Value == -1)
			{
				await Assert.That(GetHasErrors(parsed)).IsTrue();
				continue;
			}
			await Assert.That(GetHasErrors(parsed)).IsFalse();
			var value = GetParsedValue(parsed);
			await Assert.That(GetProperty(value, "OutputDirectory")).IsEqualTo(item.Value);
			await Assert.That(GetProperty(value, "ExplicitName")).IsEqualTo("value");
		}
	}

	[Test]
	public async Task GeneratedCode_MatchesReflectionParser()
	{
		var (output, source) = await RunGenerator(ParityText);
		await AssertNoCompilationErrors(output);
		var assembly = await LoadAssembly(output);
		var generatedModel = assembly.GetType("GeneratedParityModel")!;
		var reflectionModel = assembly.GetType("ReflectionParityModel")!;
		string[] args = ["input.txt", "r", "-c", "7", "--values=1,2,3", "--names=alpha,beta", "--force", "-m", "5"];

		var generatedParse = generatedModel.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, [typeof(IReadOnlyCollection<string>)], null)!;
		// var generated = generatedParse.Invoke(null, [args])!;

		var parseMethod = typeof(ArgumentsBuilder).Assembly.GetType("Lexxys.Arguments")!
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(o => o is { Name: "Parse", IsGenericMethodDefinition: true } && o.GetParameters().Length == 2);

		var generated = parseMethod.MakeGenericMethod(reflectionModel).Invoke(null, [args, null])!;
		var reflected = parseMethod.MakeGenericMethod(generatedModel).Invoke(null, [args, null])!;

		await Assert.That(GetHasErrors(generated)).IsFalse();
		await Assert.That(GetHasErrors(reflected)).IsFalse();

		var generatedValue = GetParsedValue(generated);
		var reflectedValue = GetParsedValue(reflected);
		await Assert.That(generatedValue).IsNotNull();
		await Assert.That(reflectedValue).IsNotNull();

		await Assert.That(GetProperty(generatedValue, "Count")).IsEqualTo(GetProperty(reflectedValue, "Count"));
		await Assert.That(GetProperty(generatedValue, "Input")).IsEqualTo(GetProperty(reflectedValue, "Input"));
		await Assert.That((int[])GetProperty(generatedValue, "Values")!).IsEquivalentTo((int[])GetProperty(reflectedValue, "Values")!);
		await Assert.That((List<string>)GetProperty(generatedValue, "Names")!).IsEquivalentTo((List<string>)GetProperty(reflectedValue, "Names")!);

		var generatedRun = GetProperty(generatedValue, "Run");
		var reflectedRun = GetProperty(reflectedValue, "Run");
		await Assert.That(generatedRun).IsNotNull();
		await Assert.That(reflectedRun).IsNotNull();

		await Assert.That(GetProperty(generatedRun, "Force")).IsEqualTo(GetProperty(reflectedRun, "Force"));
		await Assert.That(GetProperty(generatedRun, "Mode")).IsEqualTo(GetProperty(reflectedRun, "Mode"));
	}

	static Compilation CreateCompilation(string source)
	=> CSharpCompilation.Create("compilation_" + Guid.NewGuid().ToString("N"),
		[CSharpSyntaxTree.ParseText(source)],
		GetReferences(),
		new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

	private static async Task<(Compilation Output, string GeneratedSource)> RunGenerator(string source)
	{
		var gen = new ArgumentCodeGen();
		GeneratorDriver dr = CSharpGeneratorDriver.Create(gen);
		dr = dr.RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out var output, out var diagnostics);
		await Assert.That(diagnostics).IsEmpty(); //, "Generator diagnostics: " + String.Join("; ", diagnostics));
		var runResult = dr.GetRunResult();
		await Assert.That(runResult.Results).HasSingleItem(); //, "Expected one generator result.");
		var generated = runResult.Results[0].GeneratedSources.Single(o => o.HintName == "CliGeneratorExtensions_Generated.g.cs").SourceText.ToString();
		return (output, generated);
	}

	private static async Task AssertNoCompilationErrors(Compilation output)
	{
		var errors = output.GetDiagnostics().Where(o => o.Severity == DiagnosticSeverity.Error).ToArray();
		await Assert.That(errors).IsEmpty(); //, "Compilation errors: " + String.Join(Environment.NewLine, errors.Select(o => o.ToString())));
	}

	private static async Task<Assembly> LoadAssembly(Compilation output)
	{
		using var ms = new MemoryStream();
		var result = output.Emit(ms);
		await Assert.That(result.Success).IsTrue(); //, "Emit diagnostics: " + String.Join(Environment.NewLine, result.Diagnostics.Select(o => o.ToString())));
		// await Assert.That(result).Satisfies(o => o!.Success, expression: String.Join(Environment.NewLine, result.Diagnostics.Select(o => o.ToString())));
		ms.Position = 0;
		return AssemblyLoadContext.Default.LoadFromStream(ms);
	}

	private static MetadataReference[] GetReferences()
	{
		var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator) ?? [];
		var references = trustedAssemblies.Select(o => MetadataReference.CreateFromFile(o)).ToList();
		references.Add(MetadataReference.CreateFromFile(typeof(CliParametersAttribute).GetTypeInfo().Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(ArgumentsBuilder).GetTypeInfo().Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location));
		return references.DistinctBy(o => o.Display).ToArray();
	}

	private static bool GetHasErrors(object parsed)
		=> (bool)parsed.GetType().GetProperty("HasErrors")!.GetValue(parsed)!;

	private static object GetParsedValue(object parsed)
		=> parsed.GetType().GetProperty("Value")!.GetValue(parsed)!;

	private static object? GetProperty(object value, string name)
		=> value.GetType().GetProperty(name)!.GetValue(value);

	private const string SeparateText = """
		using Lexxys;

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
		public partial class CommandDelete
		{
			[CliOption("a")]
			public int Alpha { get; init; }
		}
		""";

	private const string NestedText = """
		using Lexxys;

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
			public partial class CommandDelete
			{
				[CliOption("a")]
				public int Alpha { get; init; }
			}
		}
		""";

	private const string BindingText = """
		using Lexxys;
		using System.Collections.Generic;

		[CliParameters(NamingStyle = ParameterNaming.KebabCase, AllowSlash = true)]
		public partial class GeneratedBindingModel
		{
			[CliOption(Name = "item-count", Alias = ["c"], Required = true)]
			public int Count { get; init; }

			[CliOption]
			public int[]? Values { get; init; }

			[CliOption]
			public List<string>? Names { get; init; }

			[CliCommand(Name = "run", Alias = ["r"])]
			public RunCommand? Run { get; init; }

			[CliParameters]
			public partial class RunCommand
			{
				[CliOption]
				public bool Force { get; init; }
			}
		}
		""";

	private const string SettingsText = """
		using Lexxys;

		[CliParameters(IgnoreCase = true, AllowSlash = true)]
		public partial class GeneratedSettingsModel
		{
			[CliOption]
			public int Value { get; init; }
		}
		""";

	private const string CurrentSettingsText = """
		using Lexxys;

		[CliParameters(
			Strict = true,
			NamingStyle = ParameterNaming.SnakeCase,
			MatchingType = ParameterMatching.Strict,
			ColonSeparator = false,
			EqualSeparator = true,
			BlankSeparator = false)]
		public partial class GeneratedCurrentSettingsModel
		{
			[CliOption]
			public int OutputDirectory { get; init; }

			[CliOption(Name = "Exact.Name")]
			public string? ExplicitName { get; init; }
		}
		""";

	private const string ParityText = """
		using Lexxys;
		using System.Collections.Generic;

		[CliParameters]
		public partial class GeneratedParityModel
		{
			[CliOption(Name = "item-count", Alias = ["c"], Required = true)]
			public int Count { get; init; }

			[CliOption(Positional = true, Required = true, ValueName = "INPUT")]
			public string? Input { get; init; }

			[CliOption]
			public int[]? Values { get; init; }

			[CliOption]
			public List<string>? Names { get; init; }

			[CliCommand(Name = "run", Alias = ["r"])]
			public GeneratedRunCommand? Run { get; init; }
		}

		[CliParameters]
		public partial class GeneratedRunCommand
		{
			[CliOption]
			public bool Force { get; init; }

			[CliOption("m")]
			public int Mode { get; init; }
		}

		public class ReflectionParityModel
		{
			[CliOption(Name = "item-count", Alias = ["c"], Required = true)]
			public int Count { get; init; }

			[CliOption(Positional = true, Required = true, ValueName = "INPUT")]
			public string? Input { get; init; }

			[CliOption]
			public int[]? Values { get; init; }

			[CliOption]
			public List<string>? Names { get; init; }

			[CliCommand(Name = "run", Alias = ["r"])]
			public ReflectionRunCommand? Run { get; init; }
		}

		public class ReflectionRunCommand
		{
			[CliOption]
			public bool Force { get; init; }

			[CliOption("m")]
			public int Mode { get; init; }
		}
		""";
}
