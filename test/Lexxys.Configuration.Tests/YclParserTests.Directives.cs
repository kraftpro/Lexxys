namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task Parse_Variables_SubstitutesValuesAndDefaults()
	{
		var nodes = YclParser.Parse(
			"""
			%$user = Gerry

			user
			  name ${user}
			  checksum "/sha/user-${user}-sha${sha-type|256}.bin"
			""");

		await AssertDump(nodes,
			"""
			{
			  user = {
			    name = "Gerry",
			    checksum = "/sha/user-Gerry-sha256.bin"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_Variables_SubstitutesInsideVariableDeclarations()
	{
		var nodes = YclParser.Parse(
			"""
			%$env = prod
			%$service = billing
			%$base = "/${env}/${service|api}"
			%$endpoint = "${base}/${region|us-east}"

			app
			  path ${endpoint}
			""");

		await AssertDump(nodes,
			"""
			{
			  app = {
			    path = "/prod/billing/us-east"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_Variables_LazilySubstitutesVariableDeclarations()
	{
		var nodes = YclParser.Parse(
			"""
			%$endpoint = "${base}/${region|us-east}"
			%$base = "/${env}/${service}"
			%$env = prod
			%$service = billing

			app
			  path ${endpoint}
			""");

		await AssertDump(nodes,
			"""
			{
			  app = {
			    path = "/prod/billing/us-east"
			  }
			}
			""");
	}

	[Test]
	public async Task ParseResult_Variables_WarnsOnRecursiveSubstitution()
	{
		var result = YclParser.ParseResult(
			"""
			%$a = ${b}
			%$b = ${a}
			value ${a}
			""");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Warnings.Count).IsEqualTo(1);
		await Assert.That(result.Warnings[0].Message).Contains("Recursive substitution");
		await Assert.That(result.Value!["value"].Value).IsEqualTo("${a}");
	}

	[Test]
	public async Task Parse_SubstitutionSources_UseEnvironmentArgumentsAndExternalConfiguration()
	{
		const string environmentName = "LEXXYS_YCL_TEST_ENV";
		var previous = Environment.GetEnvironmentVariable(environmentName);
		try
		{
			Environment.SetEnvironmentVariable(environmentName, "from-env");
			var configuration = new ConfigNodeCollection(
			[
				new KeyValuePair<string?, ConfigNode>("service", new ConfigNode(new ConfigNodeCollection(
				[
					new KeyValuePair<string?, ConfigNode>("name", new ConfigNode("billing"))
				])))
			]);

			var nodes = YclParser.Parse(
				"""
				env ${env:LEXXYS_YCL_TEST_ENV}
				arg ${arg:region}
				config ${config:service/name}
				custom ${vault:secret}
				missing ${env:LEXXYS_YCL_TEST_MISSING|fallback}
				""",
				parser =>
				{
					parser.SetArguments(new Dictionary<string, string> { ["region"] = "us-east" });
					parser.SetConfigurationSource(configuration);
					parser.SetSubstitutionSource("vault", context => context.Reference == "secret" ? "from-vault": null);
				});

			await Assert.That(nodes["env"].Value).IsEqualTo("from-env");
			await Assert.That(nodes["arg"].Value).IsEqualTo("us-east");
			await Assert.That(nodes["config"].Value).IsEqualTo("billing");
			await Assert.That(nodes["custom"].Value).IsEqualTo("from-vault");
			await Assert.That(nodes["missing"].Value).IsEqualTo("fallback");
		}
		finally
		{
			Environment.SetEnvironmentVariable(environmentName, previous);
		}
	}

	[Test]
	public async Task Parse_SubstitutionSources_UseCurrentParsedDocument()
	{
		var nodes = YclParser.Parse(
			"""
			root
			  name demo
			value ${:root/name}
			""");

		await Assert.That(nodes["value"].Value).IsEqualTo("demo");
	}

	[Test]
	public async Task Parse_SubstitutionSources_LeavesMissingSourceExpressionUnchanged()
	{
		var nodes = YclParser.Parse(
			"""
			value ${secret:name}
			""");

		await Assert.That(nodes["value"].Value).IsEqualTo("${secret:name}");
	}

	[Test]
	public async Task Parse_Directives_AllowSpacesAfterDirectiveSpecialCharacters()
	{
		var nodes = YclParser.Parse(
			"""
			% $value = 123
			% ! set-escape = '\'
			% : pair left, right
			% pair : pair

			pair "${value}" "text \"quoted\""
			""");

		await AssertDump(nodes,
			"""
			{
			  pair = {
			    left = "123",
			    right = "text \"quoted\""
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_Directives_CanAppearAfterIndentedScalarValues()
	{
		var nodes = YclParser.Parse(
			"""
			root
			  name demo
			    % $suffix = api
			    % ! set-separator = '->'
			  values [one -> two]
			    % ! set-escape = '^'
			  quoted "Hello ^"YCL^""
			  service "${suffix}"
			    %:pair left right
			    %pair :pair
			  pair alpha beta
			""");

		await AssertDump(nodes,
			"""
			{
			  root = {
			    name = "demo",
			    values = [ "one", "two" ],
			    quoted = "Hello \"YCL\"",
			    service = "api",
			    pair = { left = "alpha", right = "beta" }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_Directives_AffectOnlyFollowingLines()
	{
		var nodes = YclParser.Parse(
			"""
			root
			  beforeSeparator [one->two]
			    %!set-separator '->'
			  afterSeparator [one -> two]
			  beforeEscape "a ^t"
			    %!set-escape '^'
			  afterEscape "a ^t"
			  beforeVariable ${name}
			    %$name = next
			  afterVariable ${name}
			  plain alpha beta
			    %:pair left right
			    %typed :pair
			  typed alpha beta
			""");

		var root = nodes["root"].Collection!;
		await Assert.That(root["beforeSeparator"].Collection![0].Value).IsEqualTo("one->two");
		await Assert.That(root["afterSeparator"].Collection![0].Value).IsEqualTo("one");
		await Assert.That(root["afterSeparator"].Collection![1].Value).IsEqualTo("two");
		await Assert.That(root["beforeEscape"].Value).IsEqualTo("a ^t");
		await Assert.That(root["afterEscape"].Value).IsEqualTo("a \t");
		await Assert.That(root["beforeVariable"].Value).IsEqualTo("${name}");
		await Assert.That(root["afterVariable"].Value).IsEqualTo("next");
		await Assert.That(root["plain"].Value).IsEqualTo("alpha beta");
		await Assert.That(root["typed"].Collection!["left"].Value).IsEqualTo("alpha");
		await Assert.That(root["typed"].Collection!["right"].Value).IsEqualTo("beta");
	}

	[Test]
	public void Parse_MetaStatements_RejectUnknownNames()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%!unknown value
			node value
			"""));
	}

	[Test]
	public void Parse_MetaStatements_RequireExactNames()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%!set-escape-extra '^'
			node value
			"""));
	}

	[Test]
	public async Task Parse_MetaStatements_AllowCustomHandlers()
	{
		var nodes = YclParser.Parse(
			"""
			%!generated user
			""",
			parser => parser.SetMetaStatementHandler("generated", context =>
			{
				var name = context.ReadRequiredScalarValue("Generated node name expected.");
				return new ConfigNodeCollection(
				[
					new KeyValuePair<string?, ConfigNode>(name, new ConfigNode("created", context.Location))
				], context.Location);
			}));

		await Assert.That(nodes["user"].Value).IsEqualTo("created");
		await Assert.That(nodes["user"].Location.Line).IsEqualTo(1);
	}

	[Test]
	public async Task Parse_Directives_SetSeparatorAndSetEscapeWithoutValueResetDefaults()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-separator '->'
			before [one -> two]
			%!set-separator
			after [one->two]

			%!set-escape '^'
			quoted-before "A ^t"
			%!set-escape
			quoted-after "A `t"
			""");

		await Assert.That(nodes["before"].Collection![0].Value).IsEqualTo("one");
		await Assert.That(nodes["before"].Collection![1].Value).IsEqualTo("two");
		await Assert.That(nodes["after"].Collection![0].Value).IsEqualTo("one->two");
		await Assert.That(nodes["quoted-before"].Value).IsEqualTo("A \t");
		await Assert.That(nodes["quoted-after"].Value).IsEqualTo("A \t");
	}

	[Test]
	public async Task Parse_Variables_SubstitutesComplexFlowValues()
	{
		var nodes = YclParser.Parse(
			"""
			%$item = { street: "1212 Oneway Rd", city: "Small town" }

			customer
			  name Gerry
			  address ${item}
			""");

		await AssertDump(nodes,
			"""
			{
			  customer = {
			    name = "Gerry",
			    address = {
			      street = "1212 Oneway Rd",
			      city = "Small town"
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_Variables_LeavesMissingSubstitutionExpressionsUnchanged()
	{
		var nodes = YclParser.Parse(
			"""
			config
			  plain /srv/${missing}/data
			  quoted "value-${missing}"
			  fallback ${missing|default-value}
			""");

		await AssertDump(nodes,
			"""
			{
			  config = {
			    plain = "/srv/${missing}/data",
			    quoted = "value-${missing}",
			    fallback = "default-value"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_SetEscape_ChangesQuotedStringEscapeCharacter()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-escape '\'
			message "Hello \"YCL\""
			""");

		await AssertDump(nodes,
			"""
			{
			  message = "Hello \"YCL\""
			}
			""");
	}

	[Test]
	public async Task Parse_SetEscape_BacktickZeroDisablesQuotedStringEscaping()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-escape: '`0'
			text "a `n `t `0 # // /* */ <# #>"
			""");

		await Assert.That(nodes["text"].Value).IsEqualTo("a `n `t `0 # // /* */ <# #>");
	}

	[Test]
	public async Task Parse_SetEscape_BackslashZeroDisablesQuotedStringEscaping()
	{
		var nodes = YclParser.Parse(
			"""
			% ! set-escape = '\'
			% ! set-escape = '\0'
			text "a \n \t \0 # // /* */ <# #>"
			""");

		await Assert.That(nodes["text"].Value).IsEqualTo(@"a \n \t \0 # // /* */ <# #>");
	}

	[Test]
	public async Task Parse_SetEscape_QuotedStringsWithCommentSequencesUseConfiguredEscape()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-escape '\'
			values
			  hash "value \"# not comment\""
			  slash "value \"// not comment\""
			  cstyle "value \"/* not comment */\" after"
			  powershell "value \"<# not comment #>\" after"
			  flow ["\"# item\"", "\"// item\"", "\"/* item */\"", "\"<# item #>\""]
			""");

		await AssertDump(nodes,
			"""
			{
			  values = {
			    hash = "value \"# not comment\"",
			    slash = "value \"// not comment\"",
			    cstyle = "value \"/* not comment */\" after",
			    powershell = "value \"<# not comment #>\" after",
			    flow = [ "\"# item\"", "\"// item\"", "\"/* item */\"", "\"<# item #>\"" ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_SetEscape_RandomEscapeSymbolWithCommentSequencesUseConfiguredEscape()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-escape '^'
			values
			  hash "value ^"# not comment^""
			  slash "value ^"// not comment^""
			  cstyle "value ^"/* not comment */^" after"
			  powershell "value ^"<# not comment #>^" after"
			  flow ["^"# item^"", "^"// item^"", "^"/* item */^"", "^"<# item #>^""]
			""");

		await AssertDump(nodes,
			"""
			{
			  values = {
			    hash = "value \"# not comment\"",
			    slash = "value \"// not comment\"",
			    cstyle = "value \"/* not comment */\" after",
			    powershell = "value \"<# not comment #>\" after",
			    flow = [ "\"# item\"", "\"// item\"", "\"/* item */\"", "\"<# item #>\"" ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_SetSeparator_AddsCustomSeparator()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-separator '->'
			values [1, 2 -> 3]

			%:pair left -> right
			%pair :pair
			pair alpha -> beta
			""");

		await AssertDump(nodes,
			"""
			{
			  values = [ "1", "2", "3" ],
			  pair = { left = "alpha", right = "beta" }
			}
			""");
	}

	[Test]
	public async Task Parse_SetSeparator_CustomSeparatorsRequireSurroundingWhitespace()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-separator '->'
			compact [left->right]
			spaced [left -> right]
			""");

		await AssertDump(nodes,
			"""
			{
			  compact = [ "left->right" ],
			  spaced = [ "left", "right" ]
			}
			""");
	}

	[Test]
	public async Task Parse_PercentSetSeparator_AddsCustomSeparator()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-separator '|'
			values [1 | 2 | 3]
			""");

		await AssertDump(nodes,
			"""
			{
			  values = [ "1", "2", "3" ]
			}
			""");
	}
}
