using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lexxys;
using Lexxys.Xml;

namespace FoundationSource.Admin.RabbitMq
{
	public enum RabbitMqActionType
	{
		None,
		Queue,
		Exchange,
		QueueBind,
		ExchangeBind,
		Qos,
	}

	public class RabbitMqActionsConfig: IReadOnlyCollection<RabbitMqActionsConfig.ActionItem>
	{
		public static readonly RabbitMqActionsConfig Empty = new RabbitMqActionsConfig(null);

		private IReadOnlyCollection<ActionItem> _actions;

		public RabbitMqActionsConfig(IReadOnlyCollection<ActionItem> actions, bool? traceActions = default)
		{
			_actions = actions ?? ReadOnly.Empty<ActionItem>();
			TraceActions = traceActions ?? true;
		}

		public bool TraceActions { get; }

		public int Count => _actions.Count;

		public IEnumerator<ActionItem> GetEnumerator() => _actions.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _actions.GetEnumerator();

		public static RabbitMqActionsConfig FromXml(XmlLiteNode node)
		{
			var aa = new List<ActionItem>();
			foreach (var item in node.Elements)
			{
				var a = ActionItem.FromXml(item);
				if (a != null)
					aa.Add(a);
			}
			return aa.Count == 0 ? Empty: new RabbitMqActionsConfig(aa, node["traceActions"].AsBoolean(null));
		}

		public class ActionItem
		{
			public RabbitMqActionType Action { get; }
			public QueueAction Queue { get; }
			public ExchangeAction Exchange { get; }
			public QueueBindAction QueueBind { get; }
			public ExchangeBindAction ExchangeBind { get; }
			public QosAction Qos { get; }

			public ActionItem(QueueAction queue = null, ExchangeAction exchange = null, QueueBindAction queueBind = null, ExchangeBindAction exchangeBind = null, QosAction qos = null)
			{
				int i = (queue is null ? 0 : 1) + (exchange is null ? 0 : 1) + (queueBind is null ? 0 : 1) + (exchangeBind is null ? 0 : 1) + (qos is null ? 0: 1);
				if (i == 0)
					throw new ArgumentException(i == 0 ? "All arguments are null" : "More than one argument is null");
				Action = queue != null ? RabbitMqActionType.Queue:
					exchange != null ? RabbitMqActionType.Exchange:
					queueBind != null ? RabbitMqActionType.QueueBind:
					exchangeBind != null ? RabbitMqActionType.ExchangeBind:
					qos != null ? RabbitMqActionType.Qos:
					RabbitMqActionType.None;
				Debug.Assert(Action != RabbitMqActionType.None);
				Queue = queue;
				Exchange = exchange;
				QueueBind = queueBind;
				ExchangeBind = exchangeBind;
				Qos = qos;
			}

			public static ActionItem FromXml(XmlLiteNode node)
			{
				switch (node.Name.AsEnum(RabbitMqActionType.None))
				{
					case RabbitMqActionType.Queue:
						return new ActionItem(queue: QueueAction.FromXml(node));
					case RabbitMqActionType.Exchange:
						return new ActionItem(exchange: ExchangeAction.FromXml(node));
					case RabbitMqActionType.QueueBind:
						return new ActionItem(queueBind: QueueBindAction.FromXml(node));
					case RabbitMqActionType.ExchangeBind:
						return new ActionItem(exchangeBind: ExchangeBindAction.FromXml(node));
					default:
						return null;
				}
			}
		}

		public class QueueAction
		{
			public string Name { get; }
			public bool Durable { get; }
			public bool Exclusive { get; }
			public bool AutoDelete { get; }
			public IDictionary<string, object> Parameters { get; }

			public QueueAction(string name, bool durable, bool exclusive, bool autoDelete, IDictionary<string, string> parameters)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Durable = durable;
				Exclusive = exclusive;
				AutoDelete = autoDelete;
				Parameters = Wrap(parameters);
			}

			public static QueueAction FromXml(XmlLiteNode node)
			{
				return new QueueAction(
					name: node["name"],
					durable: node["durable"].AsBoolean(false),
					exclusive: node["exclusive"].AsBoolean(false),
					autoDelete: node["autoDelete"].AsBoolean(false),
					parameters: node.Element("parameters").AsValue<Dictionary<string, string>>(null));
			}
		}

		public class ExchangeAction
		{
			public string Name { get; }
			public string Type { get; }
			public bool Durable { get; }
			public bool AutoDelete { get; }
			public IDictionary<string, object> Parameters { get; }

			public ExchangeAction(string name, string type, bool durable, bool autoDelete, IDictionary<string, string> parameters)
			{
				Name = name ?? throw new ArgumentNullException(nameof(name));
				Type = type ?? "";
				Durable = durable;
				AutoDelete = autoDelete;
				Parameters = Wrap(parameters);
			}

			public static ExchangeAction FromXml(XmlLiteNode item)
			{
				return new ExchangeAction(
					name: item["name"],
					type: item["type"],
					durable: item["durable"].AsBoolean(false),
					autoDelete: item["autoDelete"].AsBoolean(false),
					parameters: item.Element("parameters").AsValue<IDictionary<string, string>>(null));
			}
		}

		public class QueueBindAction
		{
			public string Queue { get; }
			public string Exchange { get; }
			public string RoutingKey { get; }
			public IDictionary<string, object> Parameters { get; }

			public QueueBindAction(string queue, string exchange, string routingKey, IDictionary<string, string> parameters)
			{
				Queue = queue ?? throw new ArgumentNullException(nameof(queue));
				Exchange = exchange;
				RoutingKey = routingKey ?? "";
				Parameters = Wrap(parameters);
			}

			public static QueueBindAction FromXml(XmlLiteNode item)
			{
				return new QueueBindAction(
					queue: item["queue"],
					exchange: item["exchange"],
					routingKey: item["routingKey"],
					parameters: item.Element("parameters").AsValue<IDictionary<string, string>>(null));
			}
		}

		public class ExchangeBindAction
		{
			public string Destination { get; }
			public string Source { get; }
			public string RoutingKey { get; }
			public IDictionary<string, object> Parameters { get; }

			public ExchangeBindAction(string destination, string source, string routingKey, IDictionary<string, string> parameters)
			{
				Source = source ?? throw new ArgumentNullException(nameof(source));
				Destination = destination ?? throw new ArgumentNullException(nameof(destination));
				RoutingKey = routingKey ?? "";
				Parameters = Wrap(parameters);
			}

			public static ExchangeBindAction FromXml(XmlLiteNode item)
			{
				return new ExchangeBindAction(
					destination: item["destination"],
					source: item["source"],
					routingKey: item["routingKey"],
					parameters: item.Element("parameters").AsValue<IDictionary<string, string>>(null));
			}
		}

		public class QosAction
		{
			public static QosAction Empty = new QosAction();

			public int PrefetchSize { get; }
			public int PrefetchCount { get; }
			public bool Global { get; }

			public QosAction()
			{
				PrefetchCount = 1;
			}

			public QosAction(int prefetchSize = 0, int prefetchCount = 0, bool global = false)
			{
				PrefetchSize = Value(prefetchSize, 0, int.MaxValue);
				PrefetchCount = Value(prefetchCount, 1, ushort.MaxValue);
				Global = global;
			}
		}

		public class ConsumerAction
		{
			public static ConsumerAction Empty = new ConsumerAction(".");

			public string Queue { get; }
			public string ConsumerTag { get; }
			public bool AutoAck { get; }
			public bool NoLocal { get; }
			public bool Exclusive { get; }
			public bool AckMultiple { get; }
			public bool NackMultiple { get; }
			public bool NackRequeue { get; }
			public IDictionary<string, object> Parameters { get; }

			public ConsumerAction(string queue = null, string consumerTag = null, bool autoAck = false, bool noLocal = false, bool exclusive = false, bool ackMultiple = false, bool nackMultiple = false, bool nackRequeue = false, IDictionary<string, string> parameters = null)
			{
				Queue = queue ?? throw new ArgumentNullException(nameof(queue));
				ConsumerTag = consumerTag ?? "";
				AutoAck = autoAck;
				NoLocal = noLocal;
				Exclusive = exclusive;
				AckMultiple = ackMultiple;
				NackMultiple = nackMultiple;
				NackRequeue = nackRequeue;
				Parameters = Wrap(parameters);
			}

			public static ConsumerAction FromXml(XmlLiteNode x)
			{
				return new ConsumerAction(
					queue: x["queue"],
					consumerTag: x["consumerTag"],
					autoAck: x["autoAck"].AsBoolean(false),
					noLocal: x["noLocal"].AsBoolean(false),
					exclusive: x["exclusive"].AsBoolean(false),
					ackMultiple: x["ackMultiple"].AsBoolean(false),
					nackMultiple: x["nackMultiple"].AsBoolean(false),
					nackRequeue: x["nackRequeue"].AsBoolean(false),
					parameters: x.Elements.Count == 0 ? null: x.AsValue<IDictionary<string, string>>(null));
			}
		}

		public class ProducerAction
		{
			public static ProducerAction Empty = new ProducerAction(".");

			public string Exchange { get; }
			public string RoutingKey { get; }
			public bool Mandatory { get; }
			public BasicProperties Parameters { get; }

			public ProducerAction(string exchange = default, string routingKey = default, bool mandatory = default, BasicProperties parameters = default)
			{
				Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
				RoutingKey = routingKey ?? "";
				Mandatory = mandatory;
				Parameters = parameters;
			}

			public static ProducerAction FromXml(XmlLiteNode x) => new ProducerAction(x["exchange"], x["routingKey"], x["mandatory"].AsBoolean(false), x.Element("parameters").AsValue<BasicProperties>(default));

			/// <summary>
			/// See <see cref="IBasicProperties"/> for properties details
			/// </summary>
			public class BasicProperties
			{
				string UserId { get; }
				string ReplyTo { get; }
				byte? Priority { get; }
				bool? Persistent { get; }
				string MessageId { get; }
				IDictionary<string, object> Headers { get; }
				string Expiration { get; }
				byte? DeliveryMode { get; }
				string CorrelationId { get; }
				string ContentType { get; }
				string ContentEncoding { get; }
				string ClusterId { get; }
				string AppId { get; }
				string Type { get; }

				public BasicProperties(string userId = default, string replyTo = default, byte? priority = default, bool? persistent = default, string messageId = default, IDictionary<string, string> headers = default, string expiration = default, byte? deliveryMode = default, string correlationId = default, string contentType = default, string contentEncoding = default, string clusterId = default, string appId = default, string type = default)
				{
					UserId = userId;
					ReplyTo = replyTo;
					Priority = priority;
					Persistent = persistent;
					MessageId = messageId;
					Headers = headers == null || headers.Count == 0 ? null: ReadOnly.Wrap(headers.ToDictionary(o => o.Key, o => (object)o.Value));
					Expiration = expiration;
					DeliveryMode = deliveryMode;
					CorrelationId = correlationId;
					ContentType = contentType;
					ContentEncoding = contentEncoding;
					ClusterId = clusterId;
					AppId = appId;
					Type = type;
				}

				public bool IsEmpty =>
					UserId == default &&
					ReplyTo == default &&
					Priority == default &&
					Persistent == default &&
					MessageId == default &&
					Headers == default &&
					Expiration == default &&
					DeliveryMode == default &&
					CorrelationId == default &&
					ContentType == default &&
					ContentEncoding == default &&
					ClusterId == default &&
					AppId == default &&
					Type == default;


				public static BasicProperties FromXml(XmlLiteNode x)
				{
					return new BasicProperties(
						userId: x["userId"],
						replyTo: x["replyTo"],
						priority: x["priority"].AsByte(null),
						persistent: x["persistent"].AsBoolean(null),
						messageId: x["messageId"],
						headers: x["headers"].AsValue<IDictionary<string, string>>(null),
						expiration: x["expiration"],
						deliveryMode: x["deliveryMode"].AsByte(null),
						correlationId: x["correlationId"],
						contentType: x["contentType"],
						contentEncoding: x["contentEncoding"],
						clusterId: x["clusterId"],
						appId: x["appId"],
						type: x["type"]);
				}
			}
		}


		private static T Value<T>(T value, T min, T max)
			where T : struct, IComparable<T>
		{
			return value.CompareTo(min) <= 0 ? min :
				value.CompareTo(max) >= 0 ? max : value;
		}

		private static IDictionary<string, object> Wrap(IDictionary<string, string> parameters)
		{
			if (parameters == null || parameters.Count == 0)
				return ReadOnly.Empty<string, object>();

			var pp = new Dictionary<string, object>();
			foreach (var item in parameters)
			{
				if (Int32.TryParse(item.Value, out int v))
					pp.Add(item.Key, v);
				else
					pp.Add(item.Key, item.Value);
			}
			return ReadOnly.Wrap(pp);
		}
	}
}
 