namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task Parse_StateMachineWorkflow_CreatesNestedStatesTransitionsAndActions()
	{
		var nodes = YclParser.Parse(
			"""
			machine
			  name OrderApproval
			  initial Draft
			  states
			    -
			      name Draft
			      on
			        -
			          event Submit
			          target Review
			          guard "ctx.total > 0"
			          actions [validate, stamp-submission]
			    -
			      name Review
			      on
			        -
			          event Approve
			          target Approved
			          actions [reserve-credit, notify-customer]
			        -
			          event Reject
			          target Draft
			          actions [append-review-note]
			    -
			      name Approved
			      final true
			""");

		await AssertDump(nodes,
			"""
			{
			  machine = {
			    name = "OrderApproval",
			    initial = "Draft",
			    states = [
			      {
			        name = "Draft",
			        on = [
			          {
			          event = "Submit",
			          target = "Review",
			          guard = "ctx.total > 0",
			          actions = [ "validate", "stamp-submission" ]
			          }
			        ]
			      },
			      {
			        name = "Review",
			        on = [
			          { event = "Approve", target = "Approved", actions = [ "reserve-credit", "notify-customer" ] },
			          { event = "Reject", target = "Draft", actions = [ "append-review-note" ] }
			        ]
			      },
			      {
			        name = "Approved",
			        final = "true"
			      }
			    ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_StateMachineWithTypedTransitions_AppliesDefaultsAndCustomSeparator()
	{
		var nodes = YclParser.Parse(
			"""
			%!set-separator '->'
			%:transition event -> target -> guard= ("true") -> actions
			%**/transition :transition

			machine
			  name PaymentFlow
			  transitions
			    transition Authorize -> Captured -> "ctx.authorized" -> [capture, receipt]
			    transition Timeout -> Cancelled
			    transition Refund -> Refunded -> "ctx.canRefund" -> [refund, notify]
			""");

		await AssertDump(nodes,
			"""
			{
			  machine = {
			    name = "PaymentFlow",
			    transitions = {
			      transition = [
			        { event = "Authorize", target = "Captured", guard = "ctx.authorized", actions = [ "capture", "receipt" ] },
			        { event = "Timeout", target = "Cancelled", guard = [ "true" ], actions = null },
			        { event = "Refund", target = "Refunded", guard = "ctx.canRefund", actions = [ "refund", "notify" ] }
			      ]
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_StateMachineWithIncludesAndVariables_ComposesReusableFragments()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			File.WriteAllText(Path.Combine(directory, "shared-states.ycl"),
				"""
				-
				  name Failed
				  final true
				-
				  name Completed
				  final true
				""");
			File.WriteAllText(Path.Combine(directory, "retry-policy.ycl"),
				"""
				retry
				  attempts ${retry-count|3}
				  backoff exponential
				""");
			var main = Path.Combine(directory, "machine.ycl");
			File.WriteAllText(main,
				"""
				%$service = fulfillment
				%$retry-count = 5

				machine
				  name "${service}-pipeline"
				  initial Pending
				  states
				    -
				      name Pending
				      on
				        -
				          event Start
				          target Running
				          actions [lock-order, publish-${service}-started]
				    -
				      name Running
				      on
				        -
				          event Succeed
				          target Completed
				        -
				          event Fail
				          target Failed
				          actions [publish-${service}-failed]
				    %!include shared-states.ycl
				  %!include retry-policy.ycl
				""");

			var nodes = YclParser.ParseFile(main);

			await AssertDump(nodes,
				"""
				{
				  machine = {
				    name = "fulfillment-pipeline",
				    initial = "Pending",
				    states = [
				      {
				        name = "Pending",
				        on = [
				          {
				          event = "Start",
				          target = "Running",
				          actions = [ "lock-order", "publish-fulfillment-started" ]
				          }
				        ]
				      },
				      {
				        name = "Running",
				        on = [
				          { event = "Succeed", target = "Completed" },
				          { event = "Fail", target = "Failed", actions = [ "publish-fulfillment-failed" ] }
				        ]
				      },
				      { name = "Failed", final = "true" },
				      { name = "Completed", final = "true" }
				    ],
				    retry = {
				      attempts = "5",
				      backoff = "exponential"
				    }
				  }
				}
				""");
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}
}
