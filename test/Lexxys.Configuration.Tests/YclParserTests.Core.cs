namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task Parse_AssignsSourceLocationsToNodesAndCollections()
	{
		var nodes = YclParser.Parse(
			"""
			root:
			  child one
			""");

		await Assert.That(nodes.Location.Line).IsEqualTo(1);
		await Assert.That(nodes.Location.Column).IsEqualTo(1);
		await Assert.That(nodes["root"].Location.Line).IsEqualTo(1);
		await Assert.That(nodes["root"].Location.Column).IsEqualTo(1);
		await Assert.That(nodes["root"].Collection!.Location.Line).IsEqualTo(2);
		await Assert.That(nodes["root"].Collection!.Location.Column).IsEqualTo(3);
		await Assert.That(nodes["root"].Collection!["child"].Location.Line).IsEqualTo(2);
		await Assert.That(nodes["root"].Collection!["child"].Location.Column).IsEqualTo(3);
	}

	[Test]
	public async Task Parse_BlockSeparatorAtEndOfLine_CreatesCollectionWithOptionalComments()
	{
		var nodes = YclParser.Parse(
			"""
			root:
			  first one
			commented: # comment after collection marker
			  second two
			plain
			  third three
			""");

		await AssertDump(nodes,
			"""
			{
			  root = { first = "one" },
			  commented = { second = "two" },
			  plain = { third = "three" }
			}
			""");
	}

	[Test]
	public async Task Parse_IndentedUnnamedItems_CreatesNestedCollections()
	{
		var nodes = YclParser.Parse(
			"""
			users
			  -
			    id   1214
			    name John Travolta # comments
			    age=  31
			    courses
			      -   1
			      -   8
			      -   12
			""");

		await AssertDump(nodes,
			"""
			{
			  users = [
			    {
			      id = "1214",
			      name = "John Travolta",
			      age = "31",
			      courses = [ "1", "8", "12" ]
			    }
			  ]
			}
			""");
	}

	[Test]
	public async Task Parse_MixedTabsAndSpaces_UsesTabStopsOfFour()
	{
		var nodes = YclParser.Parse(
			"root\n" +
			"\tfirst one\n" +
			"    second two\n" +
			"\titems\n" +
			"\t  - alpha\n" +
			"      - beta\n");

		await AssertDump(nodes,
			"""
			{
			  root = {
			    first = "one",
			    second = "two",
			    items = [ "alpha", "beta" ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_BlankLineBetweenParentAndIndentedChild_KeepsChildNested()
	{
		var nodes = YclParser.Parse(
			"""
			entitlement

			  queue one
			""");

		await AssertDump(nodes,
			"""
			{
			  entitlement = {
			    queue = "one"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_FlowCollections_CreatesMapsArraysAndMixedCollections()
	{
		var nodes = YclParser.Parse(
			"""
			users: [
			  {
			    "id": 1214,
			    "name": "John Travolta",
			    "age": 31,
			    "courses": [1, 8, 12]
			  },
			  {
			    id = 1213; name = "Alfred J. Peterson"; age = 23; courses = [5, 8, 12]
			  }
			  ( id: 1212, name: "John Lennon", age: 23, courses: (3, 5, 8), extra-value: true )
			]
			""");

		await AssertDump(nodes,
			"""
			{
			  users = [
			    { id = "1214", name = "John Travolta", age = "31", courses = [ "1", "8", "12" ] },
			    { id = "1213", name = "Alfred J. Peterson", age = "23", courses = [ "5", "8", "12" ] },
			    { id = "1212", name = "John Lennon", age = "23", courses = [ "3", "5", "8" ], extra-value = "true" }
			  ]
			}
			""");
	}

	[Test]
	public async Task Parse_FlowCollections_RequireWhitespaceAroundSeparators()
	{
		var nodes = YclParser.Parse(
			"""
			volumes [prom_data:/prom, ./prom/:/etc/prom]
			labels [app=opentelemetry, component=otel-collector]
			series [1, 2,, 4]
			compact [1,2,3]
			object { name: demo, enabled = true }
			""");

		await AssertDump(nodes,
			"""
			{
			  volumes = [ "prom_data:/prom", "./prom/:/etc/prom" ],
			  labels = [ "app=opentelemetry", "component=otel-collector" ],
			  series = [ "1", "2", null, "4" ],
			  compact = [ "1,2,3" ],
			  object = { name = "demo", enabled = "true" }
			}
			""");
	}

	[Test]
	public async Task ParseResult_ReturnsErrorsInsteadOfThrowing()
	{
		var result = YclParser.ParseResult(
			"""
			root
			    child one
			  bad two
			""");

		await Assert.That(result.IsFailure).IsTrue();
		await Assert.That(result.Value).IsNull();
		await Assert.That(result.Errors.Count).IsEqualTo(1);
		await Assert.That(result.Errors[0].Message).Contains("Unexpected indentation");
	}

	[Test]
	public async Task ParseResult_WarnsWhenRepeatedNodesAreSplit()
	{
		var result = YclParser.ParseResult(
			"""
			node-A one
			node-B two
			node-A three
			""");

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Warnings.Count).IsEqualTo(1);
		await Assert.That(result.Warnings[0].Message).Contains("Repeated node 'node-A'");
		await Assert.That(result.Value!["node-A"].Collection![0].Value).IsEqualTo("one");
		await Assert.That(result.Value!["node-A"].Collection![1].Value).IsEqualTo("three");
	}

	[Test]
	public async Task Parse_Values_CanStartEndAndContainColonAndEqual()
	{
		var nodes = YclParser.Parse(
			"""
			values
			  starts-colon :abc
			  starts-equal =abc
			  ends-colon abc:
			  ends-equal abc=
			  contains-colon abc:def
			  contains-equal abc=def
			  block-colon: :abc:
			  block-equal = =abc=
			  quoted ":=:"
			  flow [":start", "end=", "a:b=c", { token = "=value:" }]
			""");

		await AssertDump(nodes,
			"""
			{
			  values = {
			    starts-colon = ":abc",
			    starts-equal = "=abc",
			    ends-colon = "abc:",
			    ends-equal = "abc=",
			    contains-colon = "abc:def",
			    contains-equal = "abc=def",
			    block-colon = ":abc:",
			    block-equal = "=abc=",
			    quoted = ":=:",
			    flow = [ ":start", "end=", "a:b=c", { token = "=value:" } ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_MultilineValues_UseTripleQuotesAndLegacyMarkers()
	{
		var nodes = YclParser.Parse(
			""""
			article
			  title Multiline
			  raw """
			    first line

			    second: line = value
			    final line
			    """
			  legacy <<
			    first line
			    second: line = value
			    final line
			    >>
			  after done
			"""");

		var article = nodes["article"].Collection!;
		await Assert.That(article["title"].Value).IsEqualTo("Multiline");
		await Assert.That(article["raw"].Value).IsEqualTo("first line\n\nsecond: line = value\nfinal line");
		await Assert.That(article["legacy"].Value).IsEqualTo("first line\nsecond: line = value\nfinal line");
		await Assert.That(article["after"].Value).IsEqualTo("done");
	}

	[Test]
	public async Task Parse_MultilineValues_LongerLegacyMarkersAllowShorterCloseSequenceInText()
	{
		var nodes = YclParser.Parse(
			"""
			script <<<
			  line with >> inside
			  final line
			  >>>
			after done
			""");

		await Assert.That(nodes["script"].Value).IsEqualTo("line with >> inside\nfinal line");
		await Assert.That(nodes["after"].Value).IsEqualTo("done");
	}

	[Test]
	public async Task Parse_MultilineValues_PreserveCommentSequencesInsideText()
	{
		var nodes = YclParser.Parse(
			""""
			before 1 # outside comment
			literal """
			  # hash text
			  // slash text
			  /* c-style text */
			  <# powershell text #>
			  """
			legacy <<<
			  # hash text
			  // slash text
			  /* c-style text */
			  <# powershell text #>
			  >>>
			after 2 // outside comment
			"""");

		await Assert.That(nodes["before"].Value).IsEqualTo("1");
		await Assert.That(nodes["literal"].Value).IsEqualTo("# hash text\n// slash text\n/* c-style text */\n<# powershell text #>");
		await Assert.That(nodes["legacy"].Value).IsEqualTo("# hash text\n// slash text\n/* c-style text */\n<# powershell text #>");
		await Assert.That(nodes["after"].Value).IsEqualTo("2");
	}

	[Test]
	public async Task Parse_QuotedStrings_UseBacktickAsDefaultEscape()
	{
		var nodes = YclParser.Parse(
			"""
			message "Hello `"YCL`""
			path 'C:``temp'
			""");

		await AssertDump(nodes,
			"""
			{
			  message = "Hello \"YCL\"",
			  path = "C:`temp"
			}
			""");
	}

	[Test]
	public async Task Parse_QuotedStrings_TreatCommentSequencesAsRegularCharacters()
	{
		var nodes = YclParser.Parse(
			"""
			values
			  hash "value # not comment"
			  slash "value // not comment"
			  cstyle "value /* not comment */ after"
			  powershell "value <# not comment #> after"
			  flow ["# item", "// item", "/* item */", "<# item #>"]
			""");

		await AssertDump(nodes,
			"""
			{
			  values = {
			    hash = "value # not comment",
			    slash = "value // not comment",
			    cstyle = "value /* not comment */ after",
			    powershell = "value <# not comment #> after",
			    flow = [ "# item", "// item", "/* item */", "<# item #>" ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_MultilineComments_IgnoresBlockComments()
	{
		var nodes = YclParser.Parse(
			"""
			before 1
			/* this block comment spans
			   multiple lines
			   ignored 0
			*/
			after 2
			<# another block comment
			   ignored 1
			#>
			done 3
			""");

		await AssertDump(nodes,
			"""
			{
			  before = "1",
			  after = "2",
			  done = "3"
			}
			""");
	}

	[Test]
	public async Task Parse_MultilineComments_IgnoresCommentMarkersInsideComments()
	{
		var nodes = YclParser.Parse(
			"""
			before 1
			/*
			   # line comment marker inside block comment
			   // another line comment marker inside block comment
			   <# powershell-style opener inside c-style comment #>
			*/
			after 2
			<#
			   # hash comment inside powershell-style block
			   // slash comment inside powershell-style block
			   /* c-style opener inside powershell-style block */
			#>
			done 3
			""");

		await AssertDump(nodes,
			"""
			{
			  before = "1",
			  after = "2",
			  done = "3"
			}
			""");
	}
}
