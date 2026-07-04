namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task Parse_TypeDeclarations_AppliesPositionalFieldsAndDefaults()
	{
		var nodes = YclParser.Parse(
			"""
			%:user id, name, age, courses
			%* :user
			user 1211 "John Doe" 55
			  courses (11, 12)
			user 2232 "Katerin Smoke" 18 [2, 13]
			%- :user
			- 2200, , 67

			%:contact name, email, phone = ("n/a")
			%contact :contact
			contact "Gerry" "gerry@example.com"
			""");

		await AssertDump(nodes,
			"""
			{
			  user = [
			    { id = "1211", name = "John Doe", age = "55", courses = [ "11", "12" ] },
			    { id = "2232", name = "Katerin Smoke", age = "18", courses = [ "2", "13" ] },
			    { id = "2200", name = null, age = "67", courses = null }
			  ],
			  contact = { name = "Gerry", email = "gerry@example.com", phone = [ "n/a" ] }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_StarMatchesEachSibling()
	{
		var nodes = YclParser.Parse(
			"""
			%:pair left right
			%* :pair

			a one two
			b one two
			a three four
			""");

		await AssertDump(nodes,
			"""
			{
			  a = [
			    { left = "one", right = "two" },
			    { left = "three", right = "four" }
			  ],
			  b = { left = "one", right = "two" }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_NamesUnnamedNodeFromLiteralPattern()
	{
		var nodes = YclParser.Parse(
			"""
			%xx name value
			- Petter 10
			""");

		await AssertDump(nodes,
			"""
			{
			  xx = {
			    name = "Petter",
			    value = "10"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_NamesUnnamedNodeFromReferencedType()
	{
		var nodes = YclParser.Parse(
			"""
			%:nv name value
			%* :nv
			- Petter 10
			""");

		await AssertDump(nodes,
			"""
			{
			  nv = {
			    name = "Petter",
			    value = "10"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_MatchesDashForUnnamedNodes()
	{
		var nodes = YclParser.Parse(
			"""
			%:item name value
			%- :item

			- alpha 1
			- beta 2
			""");

		await AssertDump(nodes,
			"""
			{
			  item = [
			    { name = "alpha", value = "1" },
			    { name = "beta", value = "2" }
			  ]
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_MatchesDeepPathsWithDoubleStar()
	{
		var nodes = YclParser.Parse(
			"""
			%:item name value
			%root/**/entry :item

			root
			  group
			    nested
			      entry alpha 1
			      entry beta 2
			other
			  group
			    nested
			      entry gamma 3
			""");

		await AssertDump(nodes,
			"""
			{
			  root = {
			    group = {
			      nested = {
			        entry = [
			          { name = "alpha", value = "1" },
			          { name = "beta", value = "2" }
			        ]
			      }
			    }
			  },
			  other = {
			    group = {
			      nested = {
			        entry = "gamma 3"
			      }
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_AppliesNestedAnonymousTypes()
	{
		var nodes = YclParser.Parse(
			"""
			root
				%*/param/- key val
				%nodeA nameA valueA

				nodeA A 1
					extra: 2
					param
						- p1 v1
			""");

		await AssertDump(nodes,
			"""
			{
			  root = {
			    nodeA = {
			      nameA = "A",
			      valueA = "1",
			      extra = "2",
			      param = [
			        {
			          key = "p1",
			          val = "v1"
			        }
			      ]
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeApplicationPattern_SkipsUnmatchedSiblingApplication()
	{
		var nodes = YclParser.Parse(
			"""
			root
			%*/param/- key val
			%nodeA nameA valueA
			%nodeB nameB valueB
			nodeA A 1
				extra: 2
				param
					- p1 v1
			""");

		await AssertDump(nodes,
			"""
			{
			  root = null,
			  nodeA = {
			    nameA = "A",
			    valueA = "1",
			    extra = "2",
			    param = [
			      {
			        key = "p1",
			        val = "v1"
			      }
			    ]
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_StructuredTypeFields_MapsNestedPositionalValues()
	{
		var nodes = YclParser.Parse(
			"""
			%:building name, address (street, city, zip), area = 0
			%building :building
			building "North Tower" ("1 Main St.", "Montgomery", 32432)
			""");

		await AssertDump(nodes,
			"""
			{
			  building = {
			    name = "North Tower",
			    address = { street = "1 Main St.", city = "Montgomery", zip = "32432" },
			    area = "0"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeDeclarations_UseColonPrefixForParameterTypes()
	{
		var nodes = YclParser.Parse(
			"""
			%:building name :string, address (street :string, city :string, zip :int), area :int = 0
			%building :building
			building "North Tower" ("1 Main St.", "Montgomery", 32432)
			""");

		await AssertDump(nodes,
			"""
			{
			  building = {
			    name = "North Tower",
			    address = { street = "1 Main St.", city = "Montgomery", zip = "32432" },
			    area = "0"
			  }
			}
			""");
	}

	[Test]
	public void Parse_TypeDeclarations_RejectColonPrefixForStructuredTypes()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%:building address :(street :string, city :string)
			%building :building
			building ("1 Main St.", "Montgomery")
			"""));
	}

	[Test]
	public async Task Parse_TypeDeclarations_ApplyDefaultsAfterChildrenAreMerged()
	{
		var nodes = YclParser.Parse(
			"""
			%:service name :string = default-name, replicas :int = 1
			%service :service
			service
			  name Orders
			""");

		await AssertDump(nodes,
			"""
			{
			  service = {
			    name = "Orders",
			    replicas = "1"
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_StructuralTypeDeclarations_ApplyNestedSchemaPathsAutomatically()
	{
		var nodes = YclParser.Parse(
			"""
			%:rmq-broadcast prefix :string, route :string
			%:rmq-broadcast/priority :int = 100
			%:rmq-broadcast/group/queue name :string, durable :bool, auto-delete :bool
			%:././exchange name :string, type :string, durable :bool
			%:./././auto-delete :bool
			%:././bind exchange :string, queue :string
			%:./././routing-key :string
			%:rmq-broadcast/group/*/parameters

			%. :rmq-broadcast
			broadcast
			  prefix dpa-broadcast-
			  route r#transact
			  -
			    exchange exch fanout true
			      auto-delete false
			    queue que
			      durable true
			      auto-delete false
			      parameters
			        x-message-ttl 60000
			    bind exch que
			      routing-key orders.created

			broadcast untouched
			""");

		await AssertDump(nodes,
			"""
			{
			  broadcast = [
			    {
			      prefix = "dpa-broadcast-",
			      route = "r#transact",
			      priority = "100",
			      group = {
			        queue = {
			          name = "que",
			          durable = "true",
			          auto-delete = "false",
			          parameters = { x-message-ttl = "60000" }
			        },
			        exchange = {
			          name = "exch",
			          type = "fanout",
			          durable = "true",
			          auto-delete = "false",
			          parameters = null
			        },
			        bind = {
			          exchange = "exch",
			          queue = "que",
			          routing-key = "orders.created",
			          parameters = null
			        }
			      }
			    },
			    "untouched"
			  ]
			}
			""");
	}

	[Test]
	public async Task Parse_RealWorldOpenTelemetryCollectorConfiguration_UsesStructuralDefaults()
	{
		var nodes = YclParser.Parse(
			"""
			# Shape based on the OpenTelemetry Collector receivers/processors/exporters/service model.
			%:otel-collector/receivers/otlp/protocols/grpc/endpoint :string = 0.0.0.0:4317
			%:otel-collector/receivers/otlp/protocols/http/endpoint :string = 0.0.0.0:4318
			%:otel-collector/receivers/zipkin/endpoint :string = 0.0.0.0:9411
			%:otel-collector/receivers/hostmetrics/collection_interval :period = 30s
			%:otel-collector/processors/memory_limiter check_interval :period = 5s, limit_mib :int = 512
			%:otel-collector/processors/batch timeout :period = 5s, send_batch_size :int = 8192
			%:otel-collector/exporters/otlphttp endpoint :string, compression :string = gzip
			%:otel-collector/exporters/prometheus endpoint :string
			%:otel-collector/exporters/debug verbosity :string = normal
			%:otel-collector/service/extensions :string[] = [health_check]
			%:otel-collector/service/pipelines/* receivers :string[], processors :string[] = [memory_limiter, batch], exporters :string[]

			%. :otel-collector
			collector
			  receivers
			    otlp
			      protocols
			        grpc
			        http
			    zipkin
			    hostmetrics
			      scrapers
			        cpu
			        memory
			        filesystem
			  processors
			    memory_limiter
			      limit_mib 768
			    batch
			      timeout 10s
			  exporters
			    otlphttp "https://otel.example.com/v1/traces"
			    prometheus 0.0.0.0:8889
			    debug detailed
			  service
			    pipelines
			      traces
			        receivers [otlp, zipkin]
			        exporters [otlphttp, debug]
			      metrics
			        receivers [otlp, hostmetrics]
			        processors [memory_limiter, batch]
			        exporters [prometheus]
			      logs
			        receivers [otlp]
			        exporters [debug]
			""");

		var collector = nodes["collector"].Collection!;
		await Assert.That(collector["receivers"].Collection!["otlp"].Collection!["protocols"].Collection!["grpc"].Collection!["endpoint"].Value).IsEqualTo("0.0.0.0:4317");
		await Assert.That(collector["receivers"].Collection!["otlp"].Collection!["protocols"].Collection!["http"].Collection!["endpoint"].Value).IsEqualTo("0.0.0.0:4318");
		await Assert.That(collector["processors"].Collection!["memory_limiter"].Collection!["check_interval"].Value).IsEqualTo("5s");
		await Assert.That(collector["processors"].Collection!["memory_limiter"].Collection!["limit_mib"].Value).IsEqualTo("768");
		await Assert.That(collector["processors"].Collection!["batch"].Collection!["timeout"].Value).IsEqualTo("10s");
		await Assert.That(collector["processors"].Collection!["batch"].Collection!["send_batch_size"].Value).IsEqualTo("8192");
		await Assert.That(collector["exporters"].Collection!["otlphttp"].Collection!["compression"].Value).IsEqualTo("gzip");
		await Assert.That(collector["exporters"].Collection!["debug"].Collection!["verbosity"].Value).IsEqualTo("detailed");

		var pipelines = collector["service"].Collection!["pipelines"].Collection!;
		await Assert.That(pipelines["traces"].Collection!["processors"].Collection![0].Value).IsEqualTo("memory_limiter");
		await Assert.That(pipelines["traces"].Collection!["processors"].Collection![1].Value).IsEqualTo("batch");
		await Assert.That(pipelines["metrics"].Collection!["receivers"].Collection![1].Value).IsEqualTo("hostmetrics");
		await Assert.That(pipelines["logs"].Collection!["exporters"].Collection![0].Value).IsEqualTo("debug");
	}

	[Test]
	public async Task Parse_ExpandedSchemaTypeDeclaration_AppliesDefaultsNestedFieldsAndValidation()
	{
		var nodes = YclParser.Parse(
			"""
			%:service
			%:./name :string required
			%:./replicas :int = 1
			%:./enabled :bool = true
			%:./tags :string[] = [api, public]
			%:./endpoint (host :string required, port :int = 443)

			%service :service
			service Orders 3
			  endpoint api.internal
			""");

		await AssertDump(nodes,
			"""
			{
			  service = {
			    name = "Orders",
			    replicas = "3",
			    enabled = "true",
			    tags = [ "api", "public" ],
			    endpoint = {
			      host = "api.internal",
			      port = "443"
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_TypeDeclarations_LazilySubstitutesDefaults()
	{
		var nodes = YclParser.Parse(
			"""
			%:service
			%:./name string = "${service-name}"
			%:./url string = "${base-url}/${service-name}"
			%:./endpoint (host string = "${service-name}.${domain}", port int = "${port|443}") = ()

			%$base-url = "https://${domain}"
			%$service-name = orders
			%$domain = example.test

			%service :service
			service
			""");

		await AssertDump(nodes,
			"""
			{
			  service = {
			    name = "orders",
			    url = "https://example.test/orders",
			    endpoint = {
			      host = "orders.example.test",
			      port = "443"
			    }
			  }
			}
			""");
	}

	[Test]
	public void Parse_ExpandedSchemaTypeDeclaration_RequiresRequiredFields()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%:service
			%:./name string required

			%service :service
			service
			"""));
	}

	[Test]
	public void Parse_ExpandedSchemaTypeDeclaration_ValidatesScalarTypes()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%:service
			%:./name string required
			%:./replicas int required

			%service :service
			service Orders many
			"""));
	}

	[Test]
	public void Parse_ExpandedSchemaTypeDeclaration_ValidatesArrayTypes()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%:service
			%:./name string required
			%:./tags string[] required

			%service :service
			service Orders api
			"""));
	}

	[Test]
	public async Task Parse_ExpandedSchemaTypeDeclaration_UsesPathSyntaxForNestedFields()
	{
		var nodes = YclParser.Parse(
			"""
			%:service
			%:./name string required
			%:./endpoint/host string required
			%:./endpoint/port int = 443

			%service :service
			service Billing
			  endpoint billing.internal
			""");

		await AssertDump(nodes,
			"""
			{
			  service = {
			    name = "Billing",
			    endpoint = {
			      host = "billing.internal",
			      port = "443"
			    }
			  }
			}
			""");
	}

	[Test]
	public async Task Parse_ExpandedSchemaTypeDeclaration_ValidatesDateTimeAndPeriodTypes()
	{
		var nodes = YclParser.Parse(
			"""
			%:job
			%:./name string required
			%:./start-date date required
			%:./started-at date-time required
			%:./timeout period = 5s

			%job :job
			job Import 2026-06-27 2026-06-27T10:15:30Z
			""");

		await AssertDump(nodes,
			"""
			{
			  job = {
			    name = "Import",
			    start-date = "2026-06-27",
			    started-at = "2026-06-27T10:15:30Z",
			    timeout = "5s"
			  }
			}
			""");
	}

	[Test]
	public void Parse_ExpandedSchemaTypeDeclaration_RejectsInvalidDateTimeAndPeriodTypes()
	{
		Assert.ThrowsExactly<SyntaxException>(() => YclParser.Parse(
			"""
			%:job
			%:./start-date date required
			%:./timeout period required

			%job :job
			job 2026-06-27T10:15 nope
			"""));
	}
}
