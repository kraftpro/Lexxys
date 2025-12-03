namespace Lexxys.Argument.Tests;

public class CustomGeneratedSignatureModel
{
	public int Marker { get; init; }

	public static Arguments<CustomGeneratedSignatureModel> Parse(IReadOnlyCollection<string> args, ArgumentsConfig settings)
		=> new Arguments<CustomGeneratedSignatureModel>(Arguments.Parse(args, settings), new CustomGeneratedSignatureModel { Marker = settings.IgnoreCase ? 42: -1 });
}

[CliParameters]
public partial class GeneratedBuilderMetadataModel
{
	[CliOption(Positional = true, Required = true, ValueName = "FILE")]
	public string? File { get; init; }

	[CliOption(Required = true)]
	public int Count { get; init; }

	[CliOption]
	public int[]? Values { get; init; }

	[CliOption]
	public List<int>? ListValues { get; init; }

	[CliOption]
	public IReadOnlyList<int>? ReadOnlyValues { get; init; }
}

[CliParameters(
	Strict = true,
	NamingStyle = ParameterNaming.SnakeCase,
	MatchingType = ParameterMatching.Strict)]
public partial class GeneratedNamingModel
{
	[CliOption]
	public int OutputDirectory { get; init; }

	[CliOption(Name = "Exact.Name")]
	public string? ExplicitName { get; init; }
}

public class GeneratorIntegrationTests
{
	//[Test]
	//public async Task SeparatePositionalParameters_EnablesSplitFlag()
	//{
	//	var builder = new ArgumentsBuilder().SeparatePositionalParameters();

	//	await Assert.That(builder.SplitPositional).IsTrue();
	//}

	[Test]
	public async Task ParseGeneric_UsesIReadOnlyCollectionSettingsOverload()
	{
		var parsed = CustomGeneratedSignatureModel.Parse(["-x"], new ArgumentsConfig { IgnoreCase = true });

		await Assert.That(parsed.Errors).IsEmpty();
		await Assert.That(parsed.Value.Marker).IsEqualTo(42);
	}

	[Test]
	public async Task GeneratedCreateBuilder_EmitsPositionalRequiredAndCollectionMetadata()
	{
		var builder = GeneratedBuilderMetadataModel.CreateBuilder();

		var file = await Assert.That(builder.Root.Parameters).HasSingleItem(o => o.Name == "file");
		var count = await Assert.That(builder.Root.Parameters).HasSingleItem(o => o.Name == "count");
		var values = await Assert.That(builder.Root.Parameters).HasSingleItem(o => o.Name == "values");
		var listValues = await Assert.That(builder.Root.Parameters).HasSingleItem(o => o.Name == "list-values");
		var readOnlyValues = await Assert.That(builder.Root.Parameters).HasSingleItem(o => o.Name == "read-only-values");

		await Assert.That(file.IsPositional).IsTrue();
		await Assert.That(file.IsRequired).IsTrue();
		await Assert.That(file.IsCollection).IsFalse();

		await Assert.That(count.IsRequired).IsTrue();
		await Assert.That(count.IsPositional).IsFalse();

		await Assert.That(values.IsCollection).IsTrue();
		await Assert.That(listValues.IsCollection).IsTrue();
		await Assert.That(readOnlyValues.IsCollection).IsTrue();
	}

	[Test]
	public async Task GeneratedParse_ReportsMissingRequiredOption()
	{
		var parsed = GeneratedBuilderMetadataModel.Parse(["input.txt"]);

		await Assert.That(parsed.HasErrors).IsTrue();
		await Assert.That(parsed.Errors).Contains(o => o.Contains("Missing required parameter", StringComparison.Ordinal));
	}

	[Test]
	public async Task ParseGeneric_DispatchesToGeneratedParser()
	{
		// Routes through Arguments.Parse<T> -> the ICliParameters<T> interface dispatch (not reflection binding).
		var parsed = Arguments.Parse<GeneratedBuilderMetadataModel>(["input.txt", "--count=3", "--values=1,2,3"]);

		await Assert.That(parsed.Errors).IsEmpty();
		await Assert.That(parsed.Value.File).IsEqualTo("input.txt");
		await Assert.That(parsed.Value.Count).IsEqualTo(3);
		await Assert.That(parsed.Value.Values).IsEquivalentTo([1, 2, 3]);
	}

	[Test]
	public async Task GeneratedNaming_UsesAttributeConfig()
	{
		var attributeBuilder = GeneratedNamingModel.CreateBuilder();
		await Assert.That(attributeBuilder.Root.Parameters.Select(o => o.Name)).IsEquivalentTo(["output_directory", "Exact.Name"]);

		var fromAttribute = GeneratedNamingModel.Parse(["--output_directory=17", "--Exact.Name=value"]);
		await Assert.That(fromAttribute.Errors).IsEmpty();
		await Assert.That(fromAttribute.Value.OutputDirectory).IsEqualTo(17);
		await Assert.That(fromAttribute.Value.ExplicitName).IsEqualTo("value");
	}
}
