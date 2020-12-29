using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Xml;

namespace FoundationSource.Admin.RabbitMq
{
	public interface IRabbitMqConsumerConfig
	{
		RabbitMqActionsConfig Actions { get; }
		RabbitMqActionsConfig.ConsumerAction Consumer { get; }
		int DumpMessageLimit { get; }
	}

	public interface IRabbitMqProducerConfig
	{
		RabbitMqActionsConfig Actions { get; }
		int DumpMessageLimit { get; }
		RabbitMqActionsConfig.ProducerAction Producer { get; }
	}

	public class RabbitMqConsumerConfig: IRabbitMqConsumerConfig
	{
		public static RabbitMqConsumerConfig Empty = new RabbitMqConsumerConfig();

		public RabbitMqActionsConfig.ConsumerAction Consumer { get; }
		public RabbitMqActionsConfig Actions { get; }
		public int DumpMessageLimit { get; }

		private RabbitMqConsumerConfig(int? dumpMessageLimit = null, RabbitMqActionsConfig actions = null, RabbitMqActionsConfig.ConsumerAction consumer = null)
		{
			DumpMessageLimit = dumpMessageLimit ?? 1024;
			Consumer = consumer ?? RabbitMqActionsConfig.ConsumerAction.Empty;
			Actions = actions ?? RabbitMqActionsConfig.Empty;
		}

		public static RabbitMqConsumerConfig FromXml(XmlLiteNode node)
		{
			return node is null || node.IsEmpty ? Empty: new RabbitMqConsumerConfig(
				dumpMessageLimit: node["dumpMessageLimit"].AsInt32(null),
				actions: RabbitMqActionsConfig.FromXml(node),
				consumer: node.Element("consumer").AsValue<RabbitMqActionsConfig.ConsumerAction>(null));
		}
	}

	public class RabbitMqProducerConfig: IRabbitMqProducerConfig
	{
		public static RabbitMqProducerConfig Empty = new RabbitMqProducerConfig();

		public RabbitMqActionsConfig.ProducerAction Producer { get; }
		public RabbitMqActionsConfig Actions { get; }
		public int DumpMessageLimit { get; }

		private RabbitMqProducerConfig(int? dumpMessageLimit = null, RabbitMqActionsConfig actions = null, RabbitMqActionsConfig.ProducerAction producer = null)
		{
			DumpMessageLimit = dumpMessageLimit ?? 1024;
			Producer = producer ?? RabbitMqActionsConfig.ProducerAction.Empty;
			Actions = actions ?? RabbitMqActionsConfig.Empty;
		}

		public static RabbitMqProducerConfig FromXml(XmlLiteNode node)
		{
			if (node is null || node.IsEmpty)
				return Empty;
			var producer = node.Element("producer");
			if (producer.IsEmpty)
				return Empty;

			return new RabbitMqProducerConfig(
				dumpMessageLimit: node["dumpMessageLimit"].AsInt32(null),
				actions: RabbitMqActionsConfig.FromXml(node),
				producer: producer.AsValue<RabbitMqActionsConfig.ProducerAction>(null));
		}
	}

	public class RabbitMqConfig: IRabbitMqConsumerConfig, IRabbitMqProducerConfig
	{
		public static RabbitMqConfig Empty = new RabbitMqConfig();

		public RabbitMqActionsConfig.ConsumerAction Consumer { get; }
		public RabbitMqActionsConfig.ProducerAction Producer { get; }
		public RabbitMqActionsConfig Actions { get; }
		public int DumpMessageLimit { get; }

		private RabbitMqConfig(int? dumpMessageLimit = null, RabbitMqActionsConfig actions = null, RabbitMqActionsConfig.ConsumerAction consumer = null, RabbitMqActionsConfig.ProducerAction producer = null)
		{
			Actions = actions ?? RabbitMqActionsConfig.Empty;
			Consumer = consumer ?? RabbitMqActionsConfig.ConsumerAction.Empty;
			Producer = producer ?? RabbitMqActionsConfig.ProducerAction.Empty;
		}

		public static RabbitMqConfig FromXml(XmlLiteNode node)
		{
			return node is null || node.IsEmpty ? Empty: new RabbitMqConfig(
				dumpMessageLimit: node["dumpMessageLimit"].AsInt32(null),
				actions: RabbitMqActionsConfig.FromXml(node),
				consumer: node.Element("consumer").AsValue<RabbitMqActionsConfig.ConsumerAction>(null),
				producer: node.Element("producer").AsValue<RabbitMqActionsConfig.ProducerAction>(null));
		}
	}

	public class RabbitMqConnectionConfig: IDump
	{
		public string Server { get; }
		public string User { get; }
		public string Password { get; }
		public string Environment { get; }

		public RabbitMqConnectionConfig(string server = null, string environment = null, string user = null, string password = null)
		{
			Server = server ?? "amqp://rabbit";
			Environment = environment ?? "dev";
			User = user ?? "fs-admin-app";
			Password = password ?? "Staging@123";
		}

		public DumpWriter DumpContent(DumpWriter writer)
		{
			return writer
				.Item(nameof(Server), Server)
				.Then(nameof(Environment), Environment)
				.Then(nameof(User), User)
				.Then(nameof(Password), Password);
		}
	}


	public static class Extensions
	{
		public static bool IsEmpty(this RabbitMqConsumerConfig config) => config is null || config == RabbitMqConsumerConfig.Empty;
		public static bool IsEmpty(this RabbitMqProducerConfig config) => config is null || config == RabbitMqProducerConfig.Empty;
		public static bool IsEmpty(this RabbitMqConfig config) => config is null || config == RabbitMqConfig.Empty;
		public static bool IsEmpty(this RabbitMqActionsConfig.ConsumerAction action) => action is null || action == RabbitMqActionsConfig.ConsumerAction.Empty;
		public static bool IsEmpty(this RabbitMqActionsConfig.ProducerAction action) => action is null || action == RabbitMqActionsConfig.ProducerAction.Empty;
	}
}
