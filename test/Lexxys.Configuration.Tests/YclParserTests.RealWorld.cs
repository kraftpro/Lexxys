namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task Parse_RealWorld_DockerComposeMonitoringStack_UsesServiceDefaultsAndArrays()
	{
		var nodes = YclParser.Parse(
			"""
			# Converted from vegasbrianc/prometheus docker-compose.yml.
			%:compose/version :string
			%:compose/volumes :string[]
			%:compose/networks :string[]
			%:compose/services/* image :string, ports :string[], volumes :string[], command :string[], networks :string[], restart :string = always, depends_on :string[], deploy (mode :string)

			%. :compose
			compose
			  version 3.7
			  volumes [prometheus_data, grafana_data]
			  networks [front-tier, back-tier]
			  services
			    prometheus
			      image prom/prometheus:v2.36.2
			      ports [9090:9090]
			      volumes [./prometheus/:/etc/prometheus/, prometheus_data:/prometheus]
			      command [
			        "--config.file=/etc/prometheus/prometheus.yml",
			        "--storage.tsdb.path=/prometheus",
			        "--web.console.libraries=/usr/share/prometheus/console_libraries",
			        "--web.console.templates=/usr/share/prometheus/consoles"
			      ]
			      depends_on [cadvisor, alertmanager]
			      networks [back-tier]
			    node-exporter
			      image quay.io/prometheus/node-exporter:latest
			      ports [9100:9100]
			      volumes [/proc:/host/proc:ro, /sys:/host/sys:ro, /:/rootfs:ro, /:/host:ro,rslave]
			      command [
			        "--path.rootfs=/host",
			        "--path.procfs=/host/proc",
			        "--path.sysfs=/host/sys",
			        "--collector.filesystem.ignored-mount-points",
			        "^/(sys|proc|dev|host|etc|rootfs/var/lib/docker/containers|rootfs/var/lib/docker/overlay2|rootfs/run/docker/netns|rootfs/var/lib/docker/aufs)($$|/)"
			      ]
			      networks [back-tier]
			      deploy
			        mode global
			    grafana
			      image grafana/grafana
			      ports [3000:3000]
			      volumes [grafana_data:/var/lib/grafana, ./grafana/provisioning/:/etc/grafana/provisioning/]
			      networks [back-tier, front-tier]
			""");

		var services = nodes["compose"].Collection!["services"].Collection!;
		await Assert.That(services["prometheus"].Collection!["restart"].Value).IsEqualTo("always");
		await Assert.That(services["prometheus"].Collection!["depends_on"].Collection![1].Value).IsEqualTo("alertmanager");
		await Assert.That(services["node-exporter"].Collection!["command"].Collection![4].Value).Contains("rootfs/var/lib/docker/overlay2");
		await Assert.That(services["node-exporter"].Collection!["deploy"].Collection!["mode"].Value).IsEqualTo("global");
		await Assert.That(services["grafana"].Collection!["networks"].Collection![1].Value).IsEqualTo("front-tier");
	}

	[Test]
	public async Task Parse_RealWorld_OpenTelemetryKubernetesCollector_UsesLocalTypeApplications()
	{
		var nodes = YclParser.Parse(
			"""
			# Converted from open-telemetry/opentelemetry-collector-contrib examples/kubernetes/otel-collector.yaml.
			kubernetes
			  manifests
			    %- apiVersion kind name
			    - v1 ConfigMap otel-collector-config
			      collector
			        receivers
			          file_log
			            include [/var/log/pods/*/*/*.log]
			            exclude [/var/log/pods/*/otel-collector/*.log]
			            start_at end
			            operators
			              %- type id
			              - router get-format
			                routes
			                  %- output expr
			                  - parser-docker 'body matches "^\\{"'
			                  - parser-crio 'body matches "^[^ Z]+ "'
			                  - parser-containerd 'body matches "^[^ Z]+Z"'
			              - regex_parser parser-crio
			                regex '^(?P<time>[^ Z]+) (?P<stream>stdout|stderr) (?P<logtag>[^ ]*) ?(?P<log>.*)$'
			                output extract_metadata_from_filepath
			                timestamp
			                  parse_from attributes.time
			                  layout_type gotime
			                  layout '2006-01-02T15:04:05.999999999Z07:00'
			              - json_parser parser-docker
			                output extract_metadata_from_filepath
			                timestamp
			                  parse_from attributes.time
			                  layout '%Y-%m-%dT%H:%M:%S.%LZ'
			        processors
			          k8sattributes
			            auth_type serviceAccount
			            passthrough false
			            extract
			              metadata [k8s.pod.name, k8s.pod.uid, k8s.deployment.name, k8s.namespace.name, k8s.node.name, k8s.pod.start_time, k8s.cluster.uid]
			              labels
			                %- tag_name key from
			                - key1 label1 pod
			                - key2 label2 pod
			            pod_association
			              -
			                sources
			                  - from resource_attribute
			                    name k8s.pod.uid
			                  - from resource_attribute
			                    name k8s.pod.ip
			              -
			                sources
			                  - from connection
			        exporters
			          debug
			            verbosity detailed
			        service
			          pipelines
			            logs
			              receivers [file_log]
			              processors [k8sattributes]
			              exporters [debug]
			    - apps/v1 DaemonSet otel-collector
			      labels [app=opentelemetry, component=otel-collector]
			      containers
			        %- name image
			        - otel-collector otel/opentelemetry-collector-contrib:0.51.0
			          resources
			            limits
			              cpu 100m
			              memory 200Mi
			            requests
			              cpu 100m
			              memory 200Mi
			          volumeMounts
			            %- mountPath name readOnly
			            - /var/log varlog true
			            - /var/lib/docker/containers varlibdockercontainers true
			            - /etc/otelcol-contrib/config.yaml data true
			              subPath config.yaml
			    - v1 Service otel-collector
			      labels [app=opentelemetry, component=otel-collector]
			      ports
			        %- name port
			        - metrics 8888
			      selector
			        component otel-collector
			""");

		var manifests = nodes["kubernetes"].Collection!["manifests"].Collection!;
		var configMap = manifests[0].Collection!;
		await Assert.That(configMap["apiVersion"].Value).IsEqualTo("v1");
		await Assert.That(configMap["kind"].Value).IsEqualTo("ConfigMap");
		await Assert.That(configMap["collector"].Collection!["receivers"].Collection!["file_log"].Collection!["operators"].Collection![0].Collection!["type"].Value).IsEqualTo("router");
		await Assert.That(configMap["collector"].Collection!["processors"].Collection!["k8sattributes"].Collection!["extract"].Collection!["metadata"].Collection![6].Value).IsEqualTo("k8s.cluster.uid");

		var daemonSet = manifests[1].Collection!;
		await Assert.That(daemonSet["containers"].Collection![0].Collection!["volumeMounts"].Collection![2].Collection!["subPath"].Value).IsEqualTo("config.yaml");
		await Assert.That(manifests[2].Collection!["ports"].Collection![0].Collection!["port"].Value).IsEqualTo("8888");
	}

	[Test]
	public async Task Parse_RealWorld_StartSpringApplicationMetadata_UsesDefaultsAndSubstitution()
	{
		var nodes = YclParser.Parse(
			""""
			# Converted from spring-io/start.spring.io start-site/src/main/resources/application.yml.
			%$bootVersion = 3.5.0

			spring
			  bootVersion ${bootVersion}
			  dependencies
			    %- name :string, id :string, groupId :string = org.springframework.boot, artifactId :string, scope :string = compile, starter :bool = true, description :string
			    - "Configuration Processor" configuration-processor
			      artifactId spring-boot-configuration-processor
			      starter false
			      description """
			        Generate metadata for developers to offer contextual help and code completion
			        when working with custom configuration keys.
			        """
			      links
			        -
			          rel reference
			          href "https://docs.spring.io/spring-boot/${bootVersion}/specification/configuration-metadata/annotation-processor.html"
			    - "Docker Compose Support" docker-compose
			      artifactId spring-boot-docker-compose
			      scope runtime
			      starter false
			      description "Provides docker compose support for enhanced development experience."
			      links
			        -
			          rel reference
			          href "https://docs.spring.io/spring-boot/${bootVersion}/reference/features/dev-services.html#features.dev-services.docker-compose"
			    - "Spring Modulith" modulith
			      groupId org.springframework.modulith
			      artifactId spring-modulith-starter-core
			      compatibilityRange "[3.5.0,4.2.0-M1)"
			"""");

		var dependencies = nodes["spring"].Collection!["dependencies"].Collection!;
		await Assert.That(dependencies[0].Collection!["groupId"].Value).IsEqualTo("org.springframework.boot");
		await Assert.That(dependencies[0].Collection!["starter"].Value).IsEqualTo("false");
		await Assert.That(dependencies[0].Collection!["description"].Value).Contains("code completion");
		await Assert.That(dependencies[0].Collection!["links"].Collection![0].Collection!["href"].Value).Contains("/3.5.0/");
		await Assert.That(dependencies[1].Collection!["scope"].Value).IsEqualTo("runtime");
		await Assert.That(dependencies[2].Collection!["groupId"].Value).IsEqualTo("org.springframework.modulith");
		await Assert.That(dependencies[2].Collection!["compatibilityRange"].Value).IsEqualTo("[3.5.0,4.2.0-M1)");
	}
}
