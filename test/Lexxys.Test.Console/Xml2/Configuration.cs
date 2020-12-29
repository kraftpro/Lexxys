using System;
using System.Collections.Generic;
using Assertion = System.Diagnostics.Contracts.Contract;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Lexxys;
using Lexxys.Xml;
using System.ComponentModel;
using System.Text.RegularExpressions;
using FoundationSource.Admin.Common;
using System.Threading;
using System.Web;

using XmlTools = Lexxys.Test.Con.Core.Xml2.XmlTools;

namespace FoundationSource.Admin
{

	public class Configuration //: IConfiguration
	{
		public const string ConfigurationRoot = "FsAdmin";

		public ApplicationConfig Application { get; }
		public DataConfig Data { get; }
		public CharityConfig Charity { get; }
		public HvbConfig Hvb { get; }
		public AchWireTransferConfig AchWireTransfer { get; }
		public ContractConfig Contract { get; }
		public TaxReturnConfig TaxReturn { get; }
		public EstimatedTaxConfig EstimatedTax { get; }
		public GrantingConfig Granting { get; }
		public EMailConfig EMail { get; }
		public LinkRepositoryConfig LinkRepository { get; }
		public RepaConfig Repa { get; }
		public UxConfig Ux { get; }
		public SquotConfig Squot { get; }
		public GmxConfig Gmx { get; }
		public RabbitMqConfig RabbitMq { get; }
		public XmlLiteNode Development { get; }
		public IReadOnlyList<string> SafeUrl { get; }
		public WellKnownUrlConfig WellKnownUrl { get; }
		public IReadOnlyList<Ip4AddressMap> SafeHosts { get; }
		public AccountPolicyConfig AccountPolicy { get; }
		public SsoConfig Sso { get; }
		public CheckConfig Check { get; }
		public MdrNotificationConfig MdrNotification { get; }
		public EmailTemplatesConfig EmailTemplates { get; }
		public HouseholdConfig Household { get; }
		public ServicesConfig Services { get; }

		public Configuration()
		{
			Application = ApplicationConfig.Default;
			Data = new DataConfig();
			Charity = CharityConfig.Default;
			Hvb = new HvbConfig();
			AchWireTransfer = new AchWireTransferConfig();
			Contract = new ContractConfig();
			TaxReturn = new TaxReturnConfig();
			EstimatedTax = new EstimatedTaxConfig();
			Granting = new GrantingConfig();
			EMail = EMailConfig.Default;
			LinkRepository = new LinkRepositoryConfig();
			Repa = new RepaConfig();
			Ux = new UxConfig();
			Squot = new SquotConfig();
			Gmx = GmxConfig.Default;
			RabbitMq = RabbitMqConfig.Default;
			Development = XmlLiteNode.Empty;
			SafeUrl = ReadOnly.Empty<string>();
			WellKnownUrl = new WellKnownUrlConfig();
			SafeHosts = ReadOnly.Empty<Ip4AddressMap>();
			AccountPolicy = new AccountPolicyConfig();
			Sso = new SsoConfig();
			Check = new CheckConfig();
			MdrNotification = new MdrNotificationConfig();
			EmailTemplates = new EmailTemplatesConfig();
			Household = new HouseholdConfig();
			Services = ServicesConfig.Empty;
		}

		public Configuration(ApplicationConfig application = null, DataConfig data = null, CharityConfig charity = null, HvbConfig hvb = null, AchWireTransferConfig achWireTransfer = null, ContractConfig contract = null, TaxReturnConfig taxReturn = null, EstimatedTaxConfig estimatedTax = null, GrantingConfig granting = null, EMailConfig eMail = null, LinkRepositoryConfig linkRepository = null, RepaConfig repa = null, UxConfig ux = null, SquotConfig squot = null, GmxConfig gmx = null, RabbitMqConfig rabbitMq = null, XmlLiteNode development = null, IReadOnlyList<string> safeUrl = null, WellKnownUrlConfig wellKnownUrl = null, IReadOnlyList<Ip4AddressMap> safeHosts = null, AccountPolicyConfig accountPolicy = null, SsoConfig sso = null, CheckConfig check = null, MdrNotificationConfig mdrNotification = null, EmailTemplatesConfig emailTemplates = null, HouseholdConfig household = null, ServicesConfig services = null)
		{
			Application = application ?? ApplicationConfig.Default;
			Data = data ?? new DataConfig();
			Charity = charity ?? CharityConfig.Default;
			Hvb = hvb ?? new HvbConfig();
			AchWireTransfer = achWireTransfer ?? new AchWireTransferConfig();
			Contract = contract ?? new ContractConfig();
			TaxReturn = taxReturn ?? new TaxReturnConfig();
			EstimatedTax = estimatedTax ?? new EstimatedTaxConfig();
			Granting = granting ?? new GrantingConfig();
			EMail = eMail ?? EMailConfig.Default;
			LinkRepository = linkRepository ?? new LinkRepositoryConfig();
			Repa = repa ?? new RepaConfig();
			Ux = ux ?? new UxConfig();
			Squot = squot ?? new SquotConfig();
			Gmx = gmx ?? GmxConfig.Default;
			RabbitMq = rabbitMq ?? RabbitMqConfig.Default;
			Development = development ?? XmlLiteNode.Empty;
			SafeUrl = safeUrl ?? ReadOnly.Empty<string>();
			WellKnownUrl = wellKnownUrl ?? new WellKnownUrlConfig();
			SafeHosts = safeHosts ?? ReadOnly.Empty<Ip4AddressMap>();
			AccountPolicy = accountPolicy ?? new AccountPolicyConfig();
			Sso = sso ?? new SsoConfig();
			Check = check ?? new CheckConfig();
			MdrNotification = mdrNotification ?? new MdrNotificationConfig();
			EmailTemplates = emailTemplates ?? new EmailTemplatesConfig();
			Household = household ?? new HouseholdConfig();
			Services = services ?? ServicesConfig.Empty;
		}

		public XmlLiteNode Env => _env.Value;
		private readonly IValue<XmlLiteNode> _env = Config.GetSection<XmlLiteNode>("env");

		public IValue<T> GetSection<T>(string section, Func<T> @default)
		{
			return Config.GetSection(section.StartsWith("::", StringComparison.Ordinal) ? section.Substring(2) : ConfigurationRoot + "." + section, @default);
		}

		public IOptions<T> GetOptions<T>(string section, Func<T> @default) where T : class, new()
		{
			return Config.GetOptions(section.StartsWith("::", StringComparison.Ordinal) ? section.Substring(2) : ConfigurationRoot + "." + section, @default);
		}

		private static T Value<T>(T? value, T min, T max, T @default)
			where T : struct, IComparable<T>
		{
			if (value == null)
				return @default;
			T v = value.Value;
			return v.CompareTo(min) <= 0 ? min :
				v.CompareTo(max) >= 0 ? max : v;
		}

		private static T Value<T>(T value, T min, T max)
			where T : struct, IComparable<T>
		{
			return value.CompareTo(min) <= 0 ? min :
				value.CompareTo(max) >= 0 ? max : value;
		}

		public class ApplicationConfig
		{
			public static readonly ApplicationConfig Default = new ApplicationConfig();

			public int ConcurrencyLevel { get; }
			public AspConfig Asp { get; }
			public SessionConfig Session { get; }
			public IReadOnlyDictionary<string, CachingConfig> Caching { get; }
			public int DefaultJobConcurrency { get; }
			public IReadOnlyDictionary<string, int> JobConcurrency { get; }

			private ApplicationConfig(int? concurrencyLevel = null, AspConfig asp = null, SessionConfig session = null, IReadOnlyDictionary<string, CachingConfig> referenceCache = null, int? defaultJobConcurrency = null, IReadOnlyDictionary<string, int> jobConcurrency = null)
			{
				ConcurrencyLevel = Value(concurrencyLevel, 0, 4096, Environment.ProcessorCount * 4);
				Asp = asp ?? AspConfig.Empty;
				Session = session ?? SessionConfig.Empty;
				Caching = referenceCache ?? new Dictionary<string, CachingConfig>();
				DefaultJobConcurrency = Value(defaultJobConcurrency, 0, 128, 8);
				JobConcurrency = jobConcurrency ?? ReadOnly.Empty<string, int>();
			}

			public int GetJobConcurrencyLevel(string jobName)
			{
				return JobConcurrency.TryGetValue(jobName, out var value) ? value: DefaultJobConcurrency;
			}

			public static ApplicationConfig FromXml(XmlLiteNode node)
			{
				return new ApplicationConfig
				(
					concurrencyLevel: node["concurrencyLevel"].AsInt32(null),
					asp: XmlTools.GetValue<AspConfig>(node.Element("asp"), null),
					session: XmlTools.GetValue<SessionConfig>(node.Element("session"), null),
					referenceCache: ReadOnly.Wrap(node.Element("referenceCache").Elements
						.Select(o => new { Name = o["name"], Capacity = o["capacity"].AsInt32(null), TimeOut = o["timeout"].AsTimeSpan(null) })
						.Where(o => !String.IsNullOrEmpty(o.Name) && (o.Capacity.GetValueOrDefault() > 0 || o.TimeOut.GetValueOrDefault() > default(TimeSpan)))
						.ToDictionary(o => o.Name, o => new CachingConfig(o.Capacity, o.TimeOut), StringComparer.OrdinalIgnoreCase)),
					defaultJobConcurrency: node.Element("concurrency")["default"]?.AsInt32(null),
					jobConcurrency: ReadOnly.Wrap(node.Element("concurrency").Elements
						.ToDictionary(o => o["job"], o => o["count"].AsInt32(8), StringComparer.OrdinalIgnoreCase))
				);
			}

			public class AspConfig
			{
				public static readonly AspConfig Empty = new AspConfig();

				public bool PageTimeConnection { get; }
				public bool PageStatistics { get; }

				public AspConfig(bool? pageTimeConnection = null, bool? pageStatistics = null)
				{
					PageTimeConnection = pageTimeConnection ?? true;
					PageStatistics = pageStatistics ?? true;
				}
			}

			public class SessionConfig
			{
				public static readonly SessionConfig Empty = new SessionConfig();

				public TimeSpan Timeout { get; }
				public int CacheSize { get; }
				public TimeSpan CacheTimeout { get; }
				public TimeSpan TouchInterval { get; }
				public TimeSpan TokenTimeout { get; }
				public TimeSpan TrustedActionTimeout { get; }
				public TimeSpan SecondLoginSleep { get; }
				public IReadOnlyCollection<SessionOverridesConfig> Overrides { get; }

				public SessionConfig(TimeSpan? timeout = null, int? cacheSize = null, TimeSpan? cacheTimeout = null, TimeSpan? touchInterval = null, TimeSpan? tokenTimeout = null, TimeSpan? trustedActionTimeout = null, TimeSpan? secondLoginSleep = null, IReadOnlyCollection<SessionOverridesConfig> overrides = null)
				{
					Timeout = Value(timeout, TimeSpan.Zero, TimeSpan.MaxValue, new TimeSpan(0, 20, 0));
					CacheSize = Value(cacheSize, 0, 4096, 256);
					CacheTimeout = Value(cacheTimeout, new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0), new TimeSpan(0, 5, 0));
					TouchInterval = Value(touchInterval, new TimeSpan(0, 0, 1), new TimeSpan(0, 20, 0), new TimeSpan(0, 5, 0));
					TokenTimeout = Value(tokenTimeout, new TimeSpan(0, 0, 1), new TimeSpan(0, 30, 0), new TimeSpan(0, 0, 20));
					TrustedActionTimeout = Value(trustedActionTimeout, new TimeSpan(0, 0, 1), new TimeSpan(0, 30, 0), new TimeSpan(0, 2, 0));
					SecondLoginSleep = Value(secondLoginSleep, TimeSpan.Zero, new TimeSpan(0, 0, 20), new TimeSpan(0, 0, 3));
					Overrides = overrides ?? Array.Empty<SessionOverridesConfig>();
				}

				public static SessionConfig FromXml(XmlLiteNode node)
				{
					return new SessionConfig
					(
						timeout: node["timeout"].AsTimeSpan(null),
						cacheSize: node["cacheSize"].AsInt32(null),
						cacheTimeout: node["cacheTimeout"].AsTimeSpan(null),
						touchInterval: node["touchInterval"].AsTimeSpan(null),
						tokenTimeout: node["tokenTimeout"].AsTimeSpan(null),
						trustedActionTimeout: node["trustedActionTimeout"].AsTimeSpan(null),
						secondLoginSleep: node["secondLoginSleep"].AsTimeSpan(null),
						overrides: ReadOnly.WrapCopy(node.Where("override").Select(o => o.AsValue<SessionOverridesConfig>(null)).Where(o => o != null))
					);
				}

				public class SessionOverridesConfig
				{
					public int Type { get; }
					public string Rule { get; }
					public TimeSpan Timeout { get; }

					public SessionOverridesConfig(int type, string rule, TimeSpan timeout)
					{
						Type = type;
						Rule = rule;
						Timeout = timeout;
					}
				}
			}

			public class CachingConfig
			{
				public int? Capacity { get; }
				public TimeSpan? Timeout { get; }

				public CachingConfig(int? capacity = 1024 * 8, TimeSpan? timeout = null)
				{
					Capacity = capacity.GetValueOrDefault() <= 0 ? null: capacity;
					Timeout = timeout.GetValueOrDefault() <= default(TimeSpan) ? null:
						timeout < new TimeSpan(0, 0, 5) ? new TimeSpan(0, 0, 5):
						timeout > new TimeSpan(24, 0, 0) ? new TimeSpan(24, 0, 0): timeout;
				}
			}
		}

		public class DataConfig
		{
			public string DataDirectory { get; }
			public string TempDirectory { get; }
			public bool CacheReportTemplate { get; }
			public bool CacheDocumentTemplate { get; }
			public DocumentConverterConfig DocumentConverter { get; }
			public IReadOnlyList<LocalStoreItem> LocalStore { get; }

			public DataConfig(string dataDirectory = null, string tempDirectory = null, string checksClearDirectory = null, bool cacheReportTemplate = false, bool cacheDocumentTemplate = false, DocumentConverterConfig documentConverter = null, IReadOnlyList<LocalStoreItem> localStore = null)
			{
				DataDirectory = dataDirectory?.TrimEnd('\\') ?? @"C:\Application\Data";
				TempDirectory = tempDirectory?.TrimEnd('\\') ?? @"C:\Temp\FSA";
				CacheReportTemplate = cacheReportTemplate;
				CacheDocumentTemplate = cacheDocumentTemplate;
				DocumentConverter = documentConverter ?? new DocumentConverterConfig();
				LocalStore = localStore ?? ReadOnly.Empty<LocalStoreItem>();
			}

			public class DocumentConverterConfig
			{
				public TimeSpan Timeout { get; }

				public DocumentConverterConfig(TimeSpan? timeout = null)
				{
					Timeout = Value(timeout, new TimeSpan(0, 0, 10), new TimeSpan(0, 30, 0), new TimeSpan(0, 1, 0));
				}
			}

			public struct LocalStoreItem
			{
				public readonly string Domain;
				public readonly string Path;

				public LocalStoreItem(string domain, string path)
				{
					Domain = domain;
					Path = path;
				}
			}
		}

		public class WellKnownUrlConfig
		{
			public string ViewEmail { get; }

			public WellKnownUrlConfig(string welcome = null, string viewEmail = null)
			{
				ViewEmail = viewEmail ?? "https://admin.foundationsource.com/email/view?id={0}";
			}
		}

		public class CharityConfig
		{
			public static readonly CharityConfig Default = new CharityConfig();

			public bool AutoFullTextPopulation { get; }
			public string NcesUrlTemplate { get; }
		
			public ChimpConfig Chimp { get; }

			private CharityConfig(bool? autoFullTextPopulation = null, string ncesUrlTemplate = null, ChimpConfig chimp = null)
			{
				AutoFullTextPopulation = autoFullTextPopulation ?? true;
				NcesUrlTemplate = ncesUrlTemplate ?? "https://nces.ed.gov/globallocator/sch_info_popup.asp?Type=Public&ID={{id}}";
				Chimp = chimp ?? new ChimpConfig();
			}

			public class ChimpConfig
			{
				public string Application { get; }

				public ChimpConfig(string application = null)
				{
					Application = application ?? @"C:\Application\Bin\Chimp.exe";
				}
			}
		}

		public class CheckConfig
		{
			public string IncomingDirectory { get; }
			public string ProcessedDirectory { get; }
			public string FilesToParse { get; }

			public CheckConfig(string incomingDirectory = null, string processedDirectory = null, string mask = null)
			{
				IncomingDirectory = incomingDirectory?.TrimEnd('\\') ?? @"C:\Data\ChecksClear";
				ProcessedDirectory = processedDirectory?.TrimEnd('\\') ?? IncomingDirectory + @"\Processed";
				var rex = String.Join("|",
					(mask ?? "*.txt").Split(new [] {';'})
					.Select(o => o.Trim())
					.Where(o => o.Length > 0)
					.Select(o => Regex.Escape((o.IndexOf('.') > 0 ? o: o[0] == '.' ? "*" + o: "*." + o).Replace('?', '\uFFFE').Replace('*', '\uFFFF'))))
					.Replace('\uFFFE', '.').Replace("\uFFFF", ".*");
				FilesToParse = rex.Length == 0 ? @"\A.*\.txt\z": @"\A(" + rex + @")\z";
			}
		}

		public class HvbConfig
		{
			public string From { get; }
			public string To { get; }
			public string Cc { get; }
			public string Bcc { get; }
			public string Subject { get; }
			public string Text { get; }

			public HvbConfig(string from = null, string to = null, string cc = null, string bcc = null, string subject = null, string text = null)
			{
				From = from ?? "support@foundationsource.com";
				To = to ?? "test-primary@foundationsource.com";
				Cc = cc;
				Bcc = bcc ?? "test-transfer@foundationsource.com";
				Subject = subject ?? "Daily file transfer from Foundation Source - {0}";
				Text = text ?? "Please process the attached transfer file";
			}
		}

		public class AchWireTransferConfig
		{
			public bool EnforceWireRules { get; }

			public AchWireTransferConfig(bool enforceWireRules = default)
			{
				EnforceWireRules = enforceWireRules;
			}
		}

		public class ContractConfig
		{
			public int DefaultSigner { get; }

			public ContractConfig(int? defaultSigner = default)
			{
				DefaultSigner = defaultSigner ?? 3380;
			}
		}

		public class TaxReturnConfig
		{
			public bool ProcessQuestionnaire { get; }
			public bool ProcessTaxReturn { get; }
			public bool ProcessEstimatedTaxes { get; }
			public bool SendIssuesNotification { get; }
			public string ReportsDirectory { get; }
			public bool IncompletePackageNotification { get; }
			public IReadOnlyList<Report> NineNinetyPackage { get; }
			public IReadOnlyList<Report> ExtensionPackage { get; }

			public TaxReturnConfig
				(
					bool processQuestionnaire = default,
					bool processTaxreturn = default,
					bool processEstimatedTaxes = default,
					bool sendIssuesNotification = default,
					bool incompletePackageNotification = default,
					string reportsDirectory = null,
					IReadOnlyList<Report> nineNinetyPackage = null,
					IReadOnlyList<Report> extensionPackage = null
				)
			{
				ProcessQuestionnaire = processQuestionnaire;
				ProcessTaxReturn = processTaxreturn;
				ProcessEstimatedTaxes = processEstimatedTaxes;
				SendIssuesNotification = sendIssuesNotification;
				ReportsDirectory = reportsDirectory ?? @"C:\Application\Data\Reports";
				IncompletePackageNotification = incompletePackageNotification;
				NineNinetyPackage = nineNinetyPackage;
				ExtensionPackage = extensionPackage;
			}

			public class Report
			{
				public int CustomReportId { get; }
				public string Name { get; }
				public DocumentOutputFormat Format { get; }
				public bool ConvertToPdf { get; }
				public int? TemplateId { get; }
				public string NeedToAdd { get; }
				public string Extension { get; }
				public bool SkipForStatusNotReady { get; }

				public Report(int customReportId, string name, DocumentOutputFormat format, int? templateId, bool convertToPdf, string extension, string needToAdd, bool skipForStatusNotReady)
				{
					CustomReportId = customReportId;
					Name = name;
					Format = format;
					ConvertToPdf = convertToPdf;
					TemplateId = templateId;
					NeedToAdd = needToAdd;
					Extension = extension;
					SkipForStatusNotReady = skipForStatusNotReady;
				}
			}
		}

		public class EstimatedTaxConfig
		{
			public  int CustomReporttId { get; }
			public EstimatedTaxConfig(int customreportid = 0)
			{
				CustomReporttId = customreportid;
			}
		}

		public class GrantingConfig
		{
			public readonly int[] DefaultCharities = { 136068327, 131837418 };
		}

		public class EMailConfig
		{
			public static readonly EMailConfig Default = new EMailConfig();

			public string From { get; }
			public string LostAndFound { get; }
			public bool UseAgent { get; }
			public bool UseRabbitMq { get; }
			public bool SendToPca { get; }

			public EMailConfig(string from = null, string lostAndFound = null, bool? useAgent = null, bool useRabbitMq = false, bool sendToPca = false)
			{
				From = from ?? "support@foundationsource.com";
				LostAndFound = lostAndFound ?? "support@foundationsource.com";
				UseAgent = useAgent ?? false;
				UseRabbitMq = useRabbitMq;
				SendToPca = sendToPca;
			}
		}

		public class LinkRepositoryConfig
		{
			public string Go { get; }
			public string Error { get; }

			public readonly IReadOnlyList<KeyValuePair<string, string>> Substitute;

			public LinkRepositoryConfig(string go = null, string error = null, IReadOnlyList<KeyValuePair<string, string>> substitute = null)
			{
				Go = go ?? "https://admin.foundationsource.com/go?{0}";
				Error = error ?? "404";
				Substitute = substitute ?? Array.Empty<KeyValuePair<string, string>>();
			}
		}

		public class RepaConfig
		{
			public string Template { get; }

			public RepaConfig(string template = "template-all.xml")
			{
				Template = template;
			}
		}

		public class UxConfig
		{
			public bool LoginAutocomplete { get; }

			public UxConfig(bool loginAutocomplete = true)
			{
				LoginAutocomplete = loginAutocomplete;
			}
		}

		public class MdrNotificationConfig
		{
			public string First { get; }
			public string Second { get; }
			public string Third { get; }
			public string Fourth { get; }

			public MdrNotificationConfig(string first = null, string second = null, string third = null, string fourth = null)
			{
				First = first;
				Second = second;
				Third = third;
				Fourth = fourth;
			}
		}

		public class GmxConfig
		{
			public static readonly GmxConfig Default = new GmxConfig();

			public TimeSpan SessionTimeout { get; }
			public FsolConfig Fsol { get; }

			public RestApi Api { get; }

			public MqConfig Mq { get; }

			public GmxConfig(TimeSpan sessionTimeout = default, FsolConfig fsol = null, MqConfig mq = null, RestApi api = null)
			{
				SessionTimeout = sessionTimeout;
				Fsol = fsol ?? FsolConfig.Empty;
				Mq = mq ?? MqConfig.Empty;
				Api = api ?? RestApi.Empty;
			}

			public class FsolConfig
			{
				public static readonly FsolConfig Empty = new FsolConfig();

				private Dictionary<string, string> _map;

				public bool IsActive => Root != null;

				public string Root { get; }

				public bool KeepAlive { get; }

				private FsolConfig()
				{
				}

				public FsolConfig(string root, bool keepAlive, Dictionary<string, string> map)
				{
					Root = root ?? throw new ArgumentNullException(nameof(root));
					KeepAlive = keepAlive;
					_map = map ?? throw new ArgumentNullException(nameof(map));
				}

				public string GetReference(string name, object values)
				{
					if (Root == null)
						return Missing(name);
					string format = GetItem(name);
					if (format == null)
						return Missing(name);
					if (!format.StartsWith("http", StringComparison.Ordinal))
						format = Root + "/" + format.TrimStart('/');
					if (values == null || format.IndexOf('{') < 0)
						return format;

					var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
					AddValues(map, values);
					return format.Format(map);
				}

				public bool HasReference(string name)
				{
					return Root != null && _map != null && _map.ContainsKey(name);
				}

				private static void AddValues(IDictionary<string, object> dictionary, object values)
				{
					if (dictionary == null)
						throw EX.ArgumentNull("dictionary");

					if (values != null)
					{
						PropertyDescriptorCollection props = TypeDescriptor.GetProperties(values);
						foreach (PropertyDescriptor prop in props)
						{
							object val = prop.GetValue(values);
							dictionary.Add(prop.Name, val);
						}
					}
				}

				private string Missing(string name) => (Root ?? "http://404.org") + "/_" + name + "_is_missing";

				private string GetItem(string reference) => _map?.GetValueOrDefault(reference) ?? reference;

				public static FsolConfig FromXml(XmlLiteNode node)
				{

					if (!node["active"].AsBoolean(true))
						return Empty;

					string root = node["root"].AsString();
					if (root == null)
						return Empty;
					root = root.TrimEnd('/');
					bool keepAlive = node["keepAlive"].AsBoolean(false);

					var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (var item in node.Element("reference").Attributes)
					{
						map[item.Key] = item.Value;
					}
					map["reference"] = root;

					return new FsolConfig(root, keepAlive, map);
				}
			}

			public class RestApi
			{
				public static readonly RestApi Empty = new RestApi();

				public string Root { get; }

				public string SessionKey { get; }

				private RestApi()
				{
				}

				public RestApi(string root, string sessionKey)
				{
					Root = root?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(root));
					SessionKey = sessionKey ?? throw new ArgumentNullException(nameof(sessionKey));
				}

				public string Url(FormattableString value)
				{
					var args = value.GetArguments();
					if (args == null || args.Length == 0)
						return SimpleUrl(value.Format);

					for (int i = 0; i < args.Length; ++i)
					{
						args[i] = args[i]?.ToString();
					}
					return SimpleUrl(String.Format(value.Format, args));
				}

				public string SimpleUrl(string value)
				{
					return Root + "/" + value?.TrimStart('/');
				}
			}

			public class MqConfig
			{
				public static readonly MqConfig Empty = new MqConfig();

				public int ObservedService { get; }
				public TimeSpan ObserverTimeToLive { get; }

				public MqConfig(int? observedService = null, TimeSpan? observerTtl = null)
				{
					ObservedService = observedService ?? 31;
					ObserverTimeToLive = Value(observerTtl, new TimeSpan(0, 1, 0), new TimeSpan(0, 10, 0), new TimeSpan(0, 3, 0));
				}
			}

		}

		public class RabbitMqConfig
		{
			public static readonly RabbitMqConfig Default = new RabbitMqConfig();

			public RabbitMq.RabbitMqConnectionConfig Connection { get; }
			public bool UseTransactNotification { get; }

			public RabbitMqConfig(RabbitMq.RabbitMqConnectionConfig connection = null, bool useTransactNotification = false)
			{
				UseTransactNotification = useTransactNotification;
				Connection = connection ?? new RabbitMq.RabbitMqConnectionConfig();
			}
		}

		public class SquotConfig
		{
			public bool Simulate { get; }
			public int MaxErrorsCount { get; }
			public TimeSpan Delay { get; }
			public TimeSpan MaxDelay { get; }
			public double DelayMult { get; }
			public TimeSpan Timeout { get; }
			public TimeSpan PadBackward { get; }
			public bool Override { get; }
			public int Round { get; }
			public int Batch { get; }
			public string Test { get; }
			public IList<SquotSource> Sources { get; }

			public SquotConfig(bool simulate = false, int? maxErrorsCount = null, TimeSpan? delay = null, TimeSpan? maxDelay = null, double? delayMult = null, TimeSpan? timeout = null, TimeSpan? padBackward = null, int? round = null, int? batch = null, string test = null, bool? @override = null, IList<SquotSource> sources = null)
			{
				Simulate = simulate;
				MaxErrorsCount = Value(maxErrorsCount, 0, int.MaxValue, 8);
				Delay = Value(delay, new TimeSpan(0, 5, 0), new TimeSpan(25, 0, 0), new TimeSpan(0, 10, 0));
				MaxDelay = Value(maxDelay, new TimeSpan(6, 0, 0), new TimeSpan(100, 0, 0, 0), new TimeSpan(15, 0, 0));
				DelayMult = Value(delayMult, 1, 3, 1.75);
				Timeout = Value(timeout, new TimeSpan(0, 0, 5), new TimeSpan(0, 5, 0), new TimeSpan(0, 0, 30));
				PadBackward = Value(padBackward, TimeSpan.Zero, new TimeSpan(90, 0, 0, 0), new TimeSpan(14, 0, 0, 0));
				Round = Value(round, 0, 16, 8);
				Batch = Value(batch, 1, 100, 5);
				Test = test ?? "KO";
				Override = @override ?? false;
				Sources = sources;
			}
		}

		public enum SquotSourceFormat
		{
			Csv = 0,
			Json = 1,
		}

		public class SquotSource: IDumpJson
		{
			public string Name { get; }
			public bool Actual { get; }
			public string TypeFlags { get; }
			public string Query { get; }
			public Regex Header { get; }
			public Regex Footer { get; }
			public Regex Seed { get; }
			public string SeedUrl { get; }
			public SquotSourceFormat Format { get; }
			public int IndexDate { get; }
			public int IndexHigh { get; }
			public int IndexLow { get; }
			public int IndexClose { get; }
			public int IndexVolume { get; }
			public int MaxIndex { get; }
			public string FieldDate { get; } = "date";
			public string FieldHigh { get; } = "high";
			public string FieldLow { get; } = "low";
			public string FieldClose { get; } = "close";
			public string FieldVolume { get; } = "volume";
			public int Round { get; }
			public bool TestVolume { get; }

			public SquotSource(string name, bool? actual = null, string typeFlags = null, string query = null, string header = null, string footer = null, string seed = null, string seedUrl = null, SquotSourceFormat format = SquotSourceFormat.Csv, string fields = null, int? round = null, bool? testVolume = null)
			{
				Name = name;
				Actual = actual ?? true;
				TypeFlags = typeFlags ?? "price:yahoo";
				Query = query;
				Header = String.IsNullOrEmpty(header) ? null: new Regex(header);
				Footer = String.IsNullOrEmpty(footer) ? null: new Regex(footer);
				Seed = String.IsNullOrEmpty(seed) ? null: new Regex(seed);
				SeedUrl = seedUrl;
				Format = format;
				Round = Value(round, 0, 128, 4);
				TestVolume = testVolume ?? false;

				MaxIndex = IndexDate = IndexHigh = IndexLow = IndexClose = -1;

				if (fields == null)
					return;

				string[] ff = fields.Replace(" ", "").ToLowerInvariant().Split(',');

				if (format == SquotSourceFormat.Json)
				{
					FieldDate = GetField(ff, FieldDate);
					FieldHigh = GetField(ff, FieldHigh);
					FieldLow = GetField(ff, FieldLow);
					FieldClose = GetField(ff, FieldClose);
					FieldVolume = GetField(ff, FieldVolume);
					return;
				}

				IndexDate = ff.FindIndex(FieldDate);
				IndexClose = ff.FindIndex(FieldClose);
				if (IndexDate < 0 || IndexClose < 0)
				{
					MaxIndex = IndexDate = IndexHigh = IndexLow = IndexClose = -1;
					return;
				}

				IndexHigh = ff.FindIndex(FieldHigh);
				IndexLow = ff.FindIndex(FieldLow);
				IndexVolume = ff.FindIndex(FieldVolume);
				if (IndexHigh < 0)
					IndexHigh = IndexClose;
				if (IndexLow < 0)
					IndexLow = IndexClose;
				MaxIndex = Math.Max(IndexVolume, Math.Max(Math.Max(IndexDate, IndexClose), Math.Max(IndexHigh, IndexLow)));
			}

			private static string GetField(string[] items, string field)
			{
				int i = Array.FindIndex(items, o => o.StartsWith(field + "=", StringComparison.Ordinal));
				return i < 0 ? field: items[i].Substring(field.Length + 1);
			}

			public JsonBuilder ToJsonContent(JsonBuilder json)
			{
				json.Item(nameof(Name)).Val(Name ?? "")
					.Item(nameof(Actual)).Val(Actual)
					.Item(nameof(Query)).Val(Query ?? "")
					.Item(nameof(Header)).Val(Header?.ToString() ?? "")
					.Item(nameof(Footer)).Val(Footer?.ToString() ?? "")
					.Item(nameof(Seed)).Val(Seed?.ToString() ?? "")
					.Item(nameof(SeedUrl)).Val(SeedUrl ?? "")
					.Item(nameof(Format)).Val(Format)
					.Item(nameof(Round)).Val(Round)
					.Item(nameof(TestVolume)).Val(TestVolume);
				if (Format == SquotSourceFormat.Csv)
					json.Item(nameof(IndexDate)).Val(IndexDate)
						.Item(nameof(IndexHigh)).Val(IndexHigh)
						.Item(nameof(IndexLow)).Val(IndexLow)
						.Item(nameof(IndexClose)).Val(IndexClose)
						.Item(nameof(IndexVolume)).Val(IndexVolume);
				else if (Format == SquotSourceFormat.Json)
					json.Item(nameof(FieldDate)).Val(FieldDate ?? "")
						.Item(nameof(FieldHigh)).Val(FieldHigh ?? "")
						.Item(nameof(FieldLow)).Val(FieldLow ?? "")
						.Item(nameof(FieldClose)).Val(FieldClose ?? "")
						.Item(nameof(FieldVolume)).Val(FieldVolume ?? "");
				return json;
			}
		}

		public class SsoConfig
		{
			public IReadOnlyList<SsoAccountConfig> Account { get; }

			public SsoConfig(IReadOnlyList<SsoAccountConfig> account = null)
			{
				Account = account ?? Array.Empty<SsoAccountConfig>();
			}
		}

		public class SsoAccountConfig
		{
			public string Name { get; }
			public string Type { get; }
			public string Url { get; }
			public string Certificate { get; }

			public SsoAccountConfig(string name, string type, string url, string certificate)
			{
				Name = name;
				Type = type;
				Url = url;
				Certificate = certificate;
			}
		}

		public class AccountPolicyConfig
		{
			public AccountPolicyRuleConfig General { get; }
			public IReadOnlyList<AccountPolicyRuleConfig> Override { get; set; }

			public AccountPolicyConfig(TimeSpan? expiration = null, int? minLength = null, int? maxLength = null, int? minUpper = null, int? minLower = null, int? minLetter = null, int? minDigit = null, int? minOther = null, bool? asciiOnly = null, bool? allowSpace = null, IReadOnlyList<AccountPolicyRuleConfig> @override = null)
			{
				General = new AccountPolicyRuleConfig(null, expiration, minLength, maxLength, minUpper, minLower, minLetter, minDigit, minOther, asciiOnly, allowSpace);
				Override = @override ?? Array.Empty<AccountPolicyRuleConfig>();
			}

			public AccountPolicyRuleConfig GetPolicy(IAuthor user)
			{
				Assertion.Ensures(Assertion.Result<AccountPolicyConfig>() != null);
				if (user == null)
					return General;

				foreach (var item in Override)
				{
					if (user.Request(item.Rule))
						return item;
				}
				return General;
			}

			public AccountPolicyRuleConfig GetPolicy(string rule)
			{
				Assertion.Ensures(Assertion.Result<AccountPolicyConfig>() != null);
				if (rule == null)
					return General;

				foreach (var item in Override)
				{
					if (String.Equals(item.Rule, rule, StringComparison.OrdinalIgnoreCase))
						return item;
				}
				return General;
			}
		}

		public class AccountPolicyRuleConfig
		{
			public string Rule { get; }
			public TimeSpan Expiration { get; }
			public int MinLength { get; }
			public int MaxLength { get; }
			public int MinUpper { get; }
			public int MinLower { get; }
			public int MinLetter { get; }
			public int MinDigit { get; }
			public int MinOther { get; }
			public bool AsciiOnly { get; }
			public bool AllowSpace { get; }

			public AccountPolicyRuleConfig(string rule = null, TimeSpan? expiration = null, int? minLength = null, int? maxLength = null, int? minUpper = null, int? minLower = null, int? minLetter = null, int? minDigit = null, int? minOther = null, bool? asciiOnly = null, bool? allowSpace = null)
			{
				Rule = rule.TrimToNull();
				Expiration = Value(expiration, TimeSpan.FromDays(5), TimeSpan.FromDays(365.25 * 100), TimeSpan.FromDays(365.25 * 100));
				MinLength = Value(minLength, 5, 15, 5);
				MaxLength = Value(maxLength, 5, 999, 999);
				MinUpper = Value(minUpper, 0, MaxLength, 0);
				MinLower = Value(minLower, 0, MaxLength, 0);
				MinLetter = Value(minLetter, 0, MaxLength, 0);
				MinDigit = Value(minDigit, 0, MaxLength, 0);
				MinOther = Value(minOther, 0, MaxLength, 0);
				AsciiOnly = asciiOnly ?? false;
				AllowSpace = allowSpace ?? true;
			}
		}

		public class Ip4AddressMap
		{
			private readonly Regex _rex;

			public Ip4AddressMap ()
			{
			}

			public Ip4AddressMap(string value)
			{
				var v = value.TrimToNull();
				if (v != null && v != "*")
				{
					var s = Regex.Escape(v);
					_rex = new Regex("\\A" + s.Replace("\\*", ".*").Replace("\\?", ".").Replace("\\]", "]").Replace("\\[", "[") + "\\z");
				}
			}

			public string Expression => _rex?.ToString() ?? ".*";

			public bool IsMatch(string address)
			{
				return address != null && (_rex == null || _rex.IsMatch(address));
			}
		}

		public class EmailTemplatesConfig
		{
			private const string DefaultPath = @"C:\Application\Data";
			private const bool DefaultIncludeBrowserLink = false;

			public string Path => System.IO.Path.Combine(_path, "EmailTemplates");

			public bool IncludeBrowserLink { get; }

			public IReadOnlyList<TemplateConfig> Templates { get; private set; }

			public EmailTemplatesConfig(string path = null, bool? includeBrowserLink = null, IReadOnlyList<TemplateConfig> templates = null)
			{
				_path = path ?? DefaultPath;
				IncludeBrowserLink = includeBrowserLink ?? DefaultIncludeBrowserLink;
				Templates = templates ?? Array.Empty<TemplateConfig>();
			}
			private readonly string _path;

			public class TemplateConfig
			{
				public string Name { get; }
				public string File { get; }

				public TemplateConfig(string name, string file)
				{
					if (String.IsNullOrEmpty(name))
						throw EX.ArgumentNull(nameof(name));
					if (String.IsNullOrEmpty(file))
						throw EX.ArgumentNull(nameof(file));
					Name = name;
					File = file;
				}
			}
		}

		public class HouseholdConfig
		{
			public decimal ApplicationLimit { get; }
			public decimal AppicantLimit { get; }

			public HouseholdConfig(decimal? applicationLimit = default, decimal? applicantLimit = default)
			{
				ApplicationLimit = Value(applicationLimit, 0, 9999999, 5000);
				AppicantLimit = Value(applicantLimit, 0, 9999999, 5000);
			}
		}

		public class ServicesConfig
		{
			public static readonly ServicesConfig Empty = new ServicesConfig();

			public SalesforceConfig Salesforce { get; }
			public OneLoginConfig OneLogin { get; }

			public ServicesConfig(SalesforceConfig salesforce = null, OneLoginConfig oneLogin = null)
			{
				Salesforce = salesforce ?? SalesforceConfig.Empty;
				OneLogin = oneLogin ?? OneLoginConfig.Empty;
			}

			private static string Slash(string value) => String.IsNullOrEmpty(value) ? null: value.EndsWith("/") ? value : value + "/";

			public class AuthInfo
			{
				public static readonly AuthInfo Empty = new AuthInfo();

				public string ClientId { get; }
				public string ClientSecret { get; }

				public AuthInfo(string clientId = null, string clientSecret = null)
				{
					ClientId = clientId;
					ClientSecret = clientSecret;
				}
			}

			public class SalesforceConfig
			{
				public static readonly SalesforceConfig Empty = new SalesforceConfig();

				public AuthInfo AdminApp { get; }
				public string AuthUrl { get; }
				public string Api { get; }
				public string User { get; }
				public string Password { get; }

				public SalesforceConfig(AuthInfo adminApp = null, string authUrl = null, string api = null, string user = null, string password = null)
				{
					AdminApp = adminApp ?? AuthInfo.Empty;
					AuthUrl = authUrl ?? "https://test.salesforce.com/services/oauth2/token";
					Api = Slash(api?.TrimStart('/')) ?? "services/data/v48.0/";
					User = user;
					Password = password;
				}
			}

			public class OneLoginConfig
			{
				private const string DefaultConnectUrl = "https://openid-connect-us.onelogin.com/oidc/";
				private const string DefaultApiUrl = "https://api.us.onelogin.com/api/1/";
				private const string DefaultAuthUrl = "https://api.us.onelogin.com/auth/";

				public static OneLoginConfig Empty = new OneLoginConfig();

				public string ConnectUrl { get; }
				public string ApiUrl { get; }
				public string AuthUrl { get; }
				public string RedirectUrl { get; }

				public AuthInfo Auth { get; }
				public AuthInfo Api { get; }

				public OneLoginConfig(
					AuthInfo auth = null,
					AuthInfo api = null,
					string connectUrl = null,
					string apiUrl = null,
					string authUrl = null,
					string redirectUrl = null)
				{
					Auth = auth ?? AuthInfo.Empty;
					Api = api ?? AuthInfo.Empty;
					ConnectUrl = Slash(connectUrl) ?? DefaultConnectUrl;
					ApiUrl = Slash(apiUrl) ?? DefaultApiUrl;
					AuthUrl = Slash(authUrl) ?? DefaultAuthUrl;
					RedirectUrl = redirectUrl;
				}
			}
		}
	}
}
