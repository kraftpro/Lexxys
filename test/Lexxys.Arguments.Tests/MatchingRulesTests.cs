namespace Lexxys.Argument.Tests;

public class MatchingRulesModel
{
	public string? OutputPath { get; init; }
}

public class MatchingRulesTests
{
	[Test]
	public async Task Parameter_CanMatchNameAndAlias()
	{
		var builder = new ArgumentsBuilder()
			.Parameter("input", ["i", "in"], collection: true);

		var arguments = Arguments.Parse(["-i", "one.txt", "--input", "two.txt"], builder);

		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.GetCollection<string>("input")).IsEquivalentTo(["one.txt", "two.txt"]);
	}

	[Test]
	public async Task Parameter_CanMatchNameAndAbbrevs()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { AllowUnknown = true })
			.Parameter("input", collection: true);

		var arguments = Arguments.Parse(["-i", "one.txt", "--input", "two.txt", "-i", "three.txt"], builder);

		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.GetCollection("input")).IsEquivalentTo(["one.txt", "two.txt", "three.txt"]);
	}


	[Test]
	public async Task TypedParse_ConvertsMemberNameToCliName()
	{
		var parsed = Arguments.Parse<MatchingRulesModel>(["--output-path", "out.txt"]);

		await Assert.That(parsed.Errors).IsEmpty();
		await Assert.That(parsed.Value.OutputPath).IsEqualTo("out.txt");
	}

	[Test]
	public async Task IgnoreNameSeparators_MatchesEquivalentSeparatedNames()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { MatchingType = ParameterMatching.Fluent })
			.Parameter("input-file");

		var arguments = Arguments.Parse(["--inputfile", "in.txt"], builder);

		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.GetValue("input-file", "")).IsEqualTo("in.txt");
	}

	[Test]
	public async Task DefaultMatching_RequiresSeparatorsToBePresent()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { MatchingType = ParameterMatching.Flexible })
			.Parameter("input-file");

		var arguments = Arguments.Parse(["--inputfile", "in.txt"], builder);

		await Assert.That(arguments.Errors).IsNotEmpty();
		await Assert.That(arguments.Errors).Contains(o => o.Contains("Unknown parameter", StringComparison.Ordinal));
	}

	[Test]
	public async Task IgnoreCase_MatchesOptionAndCommandNamesCaseInsensitively()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { IgnoreCase = true })
			.Parameter("output")
			.BeginCommand("run", ["r"])
				.Switch("verbose", "v")
			.EndCommand();

		var arguments = Arguments.Parse(["--OUTPUT", "out.txt", "RUN", "-V"], builder);

		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.GetValue("output", "")).IsEqualTo("out.txt");
		await Assert.That(arguments.Command).IsNotNull();
		await Assert.That(arguments.Command!.Name).IsEqualTo("run");
		await Assert.That(arguments.Command.GetValue<bool>("verbose")).IsTrue();
	}

	[Test]
	public async Task StrictDoubleDash_RejectsSingleDashLongNameButAllowsShortAlias()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { Strict = true })
			.Parameter("mode", "m");

		var invalid = Arguments.Parse(["-mode", "fast"], builder);
		var valid = Arguments.Parse(["-m", "fast"], builder);

		await Assert.That(invalid.HasErrors).IsTrue();
		await Assert.That(invalid.Errors).Contains(o => o.Contains("Unknown parameter", StringComparison.Ordinal));
		await Assert.That(valid.Errors).IsEmpty();
		await Assert.That(valid.GetValue("mode", "")).IsEqualTo("fast");
	}

	[Test]
	public async Task CommandParameter_IsMatchedOnlyAfterCommandIsSelected()
	{
		var builder = new ArgumentsBuilder()
			.BeginCommand("run", ["r"])
				.Parameter("mode", "m")
			.EndCommand();

		var withoutCommand = Arguments.Parse(["--mode", "fast"], builder);
		var withCommand = Arguments.Parse(["r", "--mode", "fast"], builder);

		await Assert.That(withoutCommand.HasErrors).IsTrue();
		await Assert.That(withoutCommand.Errors).Contains(o => o.Contains("Unknown parameter", StringComparison.Ordinal));
		await Assert.That(withCommand.Errors).IsEmpty();
		await Assert.That(withCommand.Command).IsNotNull();
		await Assert.That(withCommand.Command!.GetValue("mode", "")).IsEqualTo("fast");
	}

	[Test]
	public async Task PositionalParameters_MatchInDeclarationOrder()
	{
		var builder = new ArgumentsBuilder(new ArgumentsConfig { AllowUnknown = true });

		var arguments = Arguments.Parse(["in.txt", "out.txt"], builder);

		await Assert.That(arguments.Errors).IsEmpty();
		await Assert.That(arguments.Positional).IsEquivalentTo(["in.txt", "out.txt"]);
	}
}
