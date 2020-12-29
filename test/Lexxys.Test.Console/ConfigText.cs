using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Lexxys;
using Lexxys.Xml;

namespace Lexxys.Test.Con
{
	class ConfigText
	{
		public static void Go()
		{
			Config.AddConfiguration(@"C:\Application\Config\fsadmin.config.txt");

		var configInt = CustomSection("email.subscription.categories", new List<SubscriptionCategory>());

		Console.WriteLine(configInt.Value.Count);

		Config.AddConfiguration(@"string:[json]
{
hello: 'hheelloo'
}");
			var hh = Config.GetValue<XmlLiteNode>("hello");
			var l = new Logger("aaa");
			l.Debug("here");
			Console.WriteLine(Configuration.Instance.EstimatedTax.CustomReporttId);
			var sources = Configuration.Instance.Squot.Sources;
			Console.WriteLine(sources.Count);

			var xx = Config.GetValue<SsoConfig>("sso");
			Console.WriteLine(xx?.Account?.Count);
			Console.ReadLine();
			Console.ReadLine();
			Console.ReadLine();
			Console.ReadLine();
			Console.ReadLine();
		}


		public static IValue<T> CustomSection<T>(string section, T @default = default) where T : class
		{
			var custom = new Custom<T>(section, @default);
			return new Out<T>(() => custom.Value);
		}

		public const string ConfigurationRoot = "FsAdmin";
		private static volatile int _configVersion;

		private class Custom<T>: IValue<T>
		{
			private int _version;
			private T _value;
			private T _default;
			private string _section;

			public Custom(string section, T @default)
			{
				if (section == null || section.Length <= 0)
					throw new ArgumentNullException(nameof(section));

				_section = section.StartsWith("::", StringComparison.Ordinal) ? section.Substring(2) : ConfigurationRoot + "." + section;
				_default = @default;
				_version = -1;
			}

			public T Value
			{
				get
				{
					while (_version != _configVersion)
					{
						_version = _configVersion;
						try
						{
							_value = Config.GetValue(_section, _default);
						}
						catch (Exception flaw)
						{
							flaw.Add("Section", _section);
							throw;
						}
					}
					return _value;
				}
				set { }
			}

			object IValue.Value => Value;
		}

		public enum SubscriptionLevel
		{
			None = 0,
			Person = 1,
			Foundation = 2,
		}

		public class SubscriptionCategory
		{
			public SubscriptionCategory(int id, string name, string displayName = default, SubscriptionLevel? level = default, bool? optOut = default, string sender = default)
			{
				Id = id;
				Name = name;
				DisplayName = displayName ?? name;
				Level = level ?? SubscriptionLevel.Person;
				OptOut = optOut ?? true;
				Sender = sender;
			}

			public int Id { get; }
			public string Name { get; }
			public string DisplayName { get; }
			public SubscriptionLevel Level { get; }
			public bool OptOut { get; }
			public string Sender { get; }
		}

		public static void Fsa()
		{
			Config.AddConfiguration(@"C:\Application\Config\fsadmin.config.txt");
		}

		public class SsoConfig
		{
			public IReadOnlyList<SsoAccountCoufig> Account { get; set; } = Array.Empty<SsoAccountCoufig>();
		}

		public class SsoAccountCoufig
		{
			public string Name { get; }
			public string Type { get; }
			public string Url { get; }
			public string Certificate { get; }

			public SsoAccountCoufig(string name, string type, string url, string certificate)
			{
				Name = name;
				Type = type;
				Url = url;
				Certificate = certificate?.Replace(" ", "").Replace("\t", "");
			}
		}
	}

	public class Configuration
	{
		public const string ConfigurationRoot = "FsAdmin";

		public readonly ApplicationConfig Application = ApplicationConfig.Default;
		public readonly DataConfig Data = DataConfig.Default;
		public readonly CharityConfig Charity = CharityConfig.Default;
		public readonly HvbConfig Hvb = new HvbConfig();
		public readonly ContractConfig Contract = new ContractConfig();
		public readonly TaxReturnConfig TaxReturn = new TaxReturnConfig();
		public readonly EstimatedTaxConfig EstimatedTax = new EstimatedTaxConfig();
		public readonly GrantingConfig Granting = new GrantingConfig();
		public readonly EMailConfig EMail = EMailConfig.Default;
		public readonly LinkRepositoryConfig LinkRepository = new LinkRepositoryConfig();
		public readonly RepaConfig Repa = new RepaConfig();
		public readonly UxConfig Ux = new UxConfig();
		public readonly SquotConfig Squot = new SquotConfig();
		public readonly GmxConfig Gmx = GmxConfig.Default;
		public readonly XmlLiteNode Development = XmlLiteNode.Empty;
		public readonly IReadOnlyList<string> SafeUrl = ReadOnly.Empty<string>();
		public readonly SsoConfig Sso = new SsoConfig();

		private static T Value<T>(T? value, T min, T max, T zero)
			where T : struct, IComparable<T>
		{
			if (value == null)
				return zero;
			T v = value.Value;
			return v.CompareTo(min) <= 0 ? min :
				v.CompareTo(max) >= 0 ? max : v;
		}

		public class ApplicationConfig
		{
			public static readonly ApplicationConfig Default = new ApplicationConfig();

			public int ConcurrencyLevel { get; }
			public AspConfig Asp { get; }
			public SessionConfig Session { get; }
			public ReferenceCacheConfig ReferenceCache { get; }

			private ApplicationConfig(int? concurrencyLevel = null, AspConfig asp = null, SessionConfig session = null, ReferenceCacheConfig referenceCache = null)
			{
				ConcurrencyLevel = Value(concurrencyLevel, 1, 4096, Environment.ProcessorCount*4);
				Asp = asp ?? new AspConfig();
				Session = session ?? new SessionConfig();
				ReferenceCache = referenceCache ?? new ReferenceCacheConfig();
			}

			public class AspConfig
			{
				public bool PageTimeConnection { get; }

				public AspConfig(bool? pageTimeConnection = null)
				{
					PageTimeConnection = pageTimeConnection ?? true;
				}
			}

			public class SessionConfig
			{
				public TimeSpan Timeout { get; }
				public int CacheSize { get; }
				public TimeSpan CacheTimeout { get; }
				public TimeSpan TouchInterval { get; }
				public TimeSpan TokenTimeout { get; }
				public TimeSpan TrustedActionTimeout { get; }
				public TimeSpan SecondLoginSleep { get; }
				public IReadOnlyCollection<SessionOverridesConfig> Overrides { get; set; } = ReadOnly.Empty<SessionOverridesConfig>();

				public SessionConfig(TimeSpan? timeout = null, int? cacheSize = null, TimeSpan? cacheTimeout = null, TimeSpan? touchInterval = null, TimeSpan? tokenTimeout = null, TimeSpan? trustedActionTimeout = null, TimeSpan? secondLoginSleep = null)
				{
					Timeout = Value(timeout, TimeSpan.Zero, TimeSpan.MaxValue, new TimeSpan(0, 20, 0));
					CacheSize = Value(cacheSize, 0, 4096, 256);
					CacheTimeout = Value(cacheTimeout, new TimeSpan(0, 5, 0), new TimeSpan(2, 0, 0), new TimeSpan(0, 30, 0));
					TouchInterval = Value(touchInterval, new TimeSpan(0, 0, 1), new TimeSpan(0, 20, 0), new TimeSpan(0, 5, 0));
					TokenTimeout = Value(tokenTimeout, new TimeSpan(0, 0, 1), new TimeSpan(0, 30, 0), new TimeSpan(0, 2, 0));
					TrustedActionTimeout = Value(trustedActionTimeout, new TimeSpan(0, 0, 1), new TimeSpan(0, 30, 0), new TimeSpan(0, 2, 0));
					SecondLoginSleep = Value(secondLoginSleep, TimeSpan.Zero, new TimeSpan(0, 0, 20), new TimeSpan(0, 0, 3));
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

			public class ReferenceCacheConfig
			{
				public int Capacity { get; }
				public TimeSpan Timeout { get; }

				public ReferenceCacheConfig(int capacity = 1024 * 8, TimeSpan? timeout = null)
				{
					Capacity = capacity;
					Timeout = Value(timeout, new TimeSpan(0, 1, 0), new TimeSpan(0, 30, 0), new TimeSpan(0, 10, 0));
				}
			}
		}

		public class DataConfig
		{
			public static readonly DataConfig Default = FromXml(XmlLiteNode.Empty);

			public string DataDirectory { get; }
			public string TempDirectory { get; }
			public bool CacheReportTemplate { get; }
			public bool CacheDocumentTemplate { get; }
			public DocumentConverterConfig DocumentConverter { get; }

			private DataConfig(string dataDirectory, string tempDirectory, bool cacheReportTemplate, bool cacheDocumentTemplate, DocumentConverterConfig documentConverter)
			{
				DataDirectory = dataDirectory;
				TempDirectory = tempDirectory;
				CacheReportTemplate = cacheReportTemplate;
				CacheDocumentTemplate = cacheDocumentTemplate;
				DocumentConverter = documentConverter;
			}

			public static DataConfig FromXml(XmlLiteNode node)
			{
				return new DataConfig
				(
					dataDirectory: node["dataDirectory"].AsString(@"C:\Application\Data"),
					tempDirectory: node["tempDirectory"].AsString(@"C:\Temp\FSA"),
					cacheReportTemplate: node["cacheReportTemplate"].AsBoolean(false),
					cacheDocumentTemplate: node["cacheDocumentTemplate"].AsBoolean(false),
					documentConverter: XmlTools.GetValue(node.Element("documentConverter"), new DocumentConverterConfig())
				);
			}

			public class DocumentConverterConfig
			{
				public TimeSpan Timeout { get; }

				public DocumentConverterConfig(TimeSpan? timeout = null)
				{
					Timeout = Value(timeout, new TimeSpan(0, 0, 10), new TimeSpan(0, 30, 0), new TimeSpan(0, 1, 0));
				}
			}
		}

		public class CharityConfig
		{
			public static readonly CharityConfig Default = FromXml(XmlLiteNode.Empty);

			public bool AutoFullTextPopulation { get; }
			public ChimpConfig Chimp { get; }

			private CharityConfig(bool autoFullTextPopulation, ChimpConfig chimp)
			{
				AutoFullTextPopulation = autoFullTextPopulation;
				Chimp = chimp;
			}

			public static CharityConfig FromXml(XmlLiteNode node)
			{
				if (node == null)
					throw EX.ArgumentNull("node");
				return new CharityConfig
				(
					autoFullTextPopulation: node["autoFullTextPopulation"].AsBoolean(true),
					chimp: XmlTools.GetValue(node, new ChimpConfig())
				);
			}

			public class ChimpConfig
			{
				public string Application { get; }

				public ChimpConfig(string application = @"C:\Application\Bin\Chimp.exe")
				{
					Application = application;
				}
			}
		}

		public class HvbConfig
		{
			public string From { get; }
			public string To { get; }
			public string Cc { get; }
			public string Bcc { get; }
			public string Subject { get; }
			public string Text { get; set; }

			public HvbConfig(
				string from = "support@foundationsource.com",
				string to = "test-primary@foundationsource.com",
				string cc = null,
				string bcc = "test-transfer@foundationsource.com",
				string subject = "Daily file transfer from Foundation Source - {0}",
				string text = "Please process the attached transfer file")
			{
				From = from;
				To = to;
				Cc = cc;
				Bcc = bcc;
				Subject = subject;
				Text = text;
			}
		}

		public class ContractConfig
		{
			public int DefaultSigner { get; }

			public ContractConfig(int defaultSigner = 3380)
			{
				DefaultSigner = defaultSigner;
			}
		}

		public class TaxReturnConfig
		{
			public bool ProcessQuestionnaire { get; }
			public bool ProcessTaxReturn { get; }
			public bool ProcessEstimatedTaxes { get; }
			public bool SendIssuesNotification { get; }
			public string ReportsDirectory { get; }

			public TaxReturnConfig
				(
				bool processQuestionnaire = false,
				bool processTaxreturn = false,
				bool processEstimatedTaxes = false,
				bool sendIssuesNotification = false,
				string reportsDirectory = @"C:\Application\Data\Reports"
				)
			{
				ProcessQuestionnaire = processQuestionnaire;
				ProcessTaxReturn = processTaxreturn;
				ProcessEstimatedTaxes = processEstimatedTaxes;
				SendIssuesNotification = sendIssuesNotification;
				ReportsDirectory = reportsDirectory;
			}
		}

		public class EstimatedTaxConfig
		{
			public int CustomReporttId { get; }
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
			public static readonly EMailConfig Default = FromXml(XmlLiteNode.Empty);

			public string Method { get; }
			public string From { get; }
			public bool UseAgent { get; }
			public SmtpConfig Smtp { get; }
			public PickupConfig Pickup { get; }

			private EMailConfig(string method, string from, bool useAgent, SmtpConfig smtp, PickupConfig pickup)
			{
				Method = method;
				From = from;
				UseAgent = useAgent;
				Smtp = smtp;
				Pickup = pickup;
			}

			public static EMailConfig FromXml(XmlLiteNode node)
			{
				return new EMailConfig
				(
					method: node["method"].AsString("pickup"),
					from: node["from"].AsString("support@foundationsource.com"),
					useAgent: node["useAgent"].AsBoolean(false),
					smtp: XmlTools.GetValue(node.Element("smtp"), new SmtpConfig()),
					pickup: XmlTools.GetValue(node.Element("pickup"), new PickupConfig())
				);
			}

			public class SmtpConfig
			{
				public string Server { get; }
				public int Port { get; }
				public TimeSpan Timeout { get; }
				public string User { get; }
				public string Password { get; }

				public SmtpConfig(string server = null, int port = 25, TimeSpan? timeout = null, string user = null, string password = null)
				{
					Server = server;
					Port = port;
					Timeout = Value(timeout, new TimeSpan(0, 0, 3), new TimeSpan(0, 5, 0), new TimeSpan(0, 0, 30));
					User = user;
					Password = password;
				}

			}

			public class PickupConfig
			{
				public string Directory { get; }

				public PickupConfig(string directory = null)
				{
					Directory = directory.TrimToNull() ??  @"C:\InetPub\mailroot\pickup";
				}
			}
		}

		public class LinkRepositoryConfig
		{
			public string Go { get; }
			public string Error { get; }
			public readonly IReadOnlyList<KeyValuePair<string, string>> Substitute;

			public LinkRepositoryConfig(string go = null, string error = null)
			{
				Go = go ?? "https://admin.foundationsource.com/go?{0}";
				Error = error ?? "404";
				Substitute = new List<KeyValuePair<string, string>>();
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

		public class GmxConfig
		{
			public static readonly GmxConfig Default = FromXml(XmlLiteNode.Empty);

			public TimeSpan SessionTimeout { get; }
			public FsolConfig Fsol { get; }
			public MqConfig Mq { get; }

			private GmxConfig(TimeSpan sessionTimeout, FsolConfig fsol, MqConfig mq)
			{
				SessionTimeout = sessionTimeout;
				Fsol = fsol;
				Mq = mq;
			}

			public static GmxConfig FromXml(XmlLiteNode node)
			{
				return new GmxConfig
				(
					sessionTimeout: node["sessionTimeout"].AsTimeSpan(default(TimeSpan)),
					fsol: XmlTools.GetValue(node.Element("fsol"), new FsolConfig()),
					mq: XmlTools.GetValue(node.Element("mq"), new MqConfig())
				);
			}

			public class FsolConfig
			{
				private Dictionary<string, string> _map;

				public bool IsActive => Root != null;

				public string Root { get; private set; }

				public bool KeepAlive { get; private set; }

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

				private string Missing(string name)
				{
					return (Root ?? "404.org") + "/_" + name + "_is_missing";
				}

				private string GetItem(string reference)
				{
					string result;
					return _map == null || !_map.TryGetValue(reference, out result) ? null : result;
				}

				private void Def(string name, string value)
				{
					if (!_map.ContainsKey(name))
						_map.Add(name, value);
				}

				public static FsolConfig FromXml(XmlLiteNode node)
				{
					var me = new FsolConfig();

					if (!node["active"].AsBoolean(true))
						return me;

					me.Root = node["root"].AsString();
					if (me.Root == null)
						return me;

					me.Root = me.Root.TrimEnd('/');
					me.KeepAlive = node["keepAlive"].AsBoolean(false);
					me._map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (var item in node.Element("reference").Attributes)
					{
						me._map[item.Key] = item.Value;
					}
					me._map["reference"] = me.Root;
					me.Def("keepAlive", "fsbo/keepalive.do?seed={seed}");
					me.Def("doLogin", "fsol/loginprocess.do?ss={session}");
					me.Def("home", "fsol/home.do");
					me.Def("alerts", "fsol/home.do");
					me.Def("clientSatisfaction", "fsbo/satisfaction.do");
					me.Def("committeeAdministration", "fsbo/admin/mf/committees.do?fID={foundation}");
					me.Def("documentRepository", "fsbo/admin/mf/documents.do?fID={foundation}");
					me.Def("editGrantLetter", "fsbo/admin/mf/grants/processgrantletter.do?id={grant}");
					me.Def("grantRequestSearch", "fsbo/admin/mf/requests/grantreqsearch.do?fID={foundation}");
					me.Def("modifyGrant", "fsbo/admin/mf/grants/editgrant.do?id={grant}");
					me.Def("organizationRightsLimits", "fsbo/admin/mf/org.do?fID={foundation}");
					me.Def("previewGrantLetter", "fsbo/admin/mf/grants/previewgrantletter.do?id={grant}");
					me.Def("processCorrespondence", "fsbo/corr/pending.do?fID={foundation}");
					me.Def("programAreaAdministration", "fsbo/admin/mf/programs.do?fID={foundation}");
					me.Def("requestsSiteAdministration", "fsbo/admin/mf/sites.do?fID={foundation}");
					me.Def("session", "fsbo/keepalive.do?session={session}&seed={seed}");
					me.Def("transactOnBehalfOf", "fsbo/admin/mu/processobo.do?x={user}&f={foundation}&ss={session}");
					me.Def("usersLimits", "fsbo/admin/mf/userslimits.do?fID={foundation}");
					me.Def("commitmentLetter", "fsbo/corr/edit.do?id={CorrInstanceId}&fID={foundation}&backUrl={backUrl}");
					return me;
				}
			}

			public class MqConfig
			{
				public int ObservedService { get; }
				public TimeSpan ObserverTimeToLive { get; }

				public MqConfig(int observedService = 31, TimeSpan? observerTtl = null)
				{
					ObservedService = observedService;
					ObserverTimeToLive = Value(observerTtl, new TimeSpan(0, 1, 0), new TimeSpan(0, 10, 0), new TimeSpan(0, 3, 0));
				}
			}
		}


		public class SquotConfig
		{
			public bool Simulate { get; }
			public TimeSpan Delay { get; }
			public TimeSpan Timeout { get; }
			public int Round { get; }
			public IList<SquotSource> Sources { get; set; }

			public SquotConfig(bool simulate = false, TimeSpan? delay = null, TimeSpan? timeout = null, int? round = null)
			{
				Simulate = simulate;
				Delay = Value(delay, new TimeSpan(0, 1, 0), new TimeSpan(24, 0, 0), new TimeSpan(0, 20, 0));
				Timeout = Value(timeout, new TimeSpan(0, 0, 5), new TimeSpan(0, 5, 0), new TimeSpan(0, 0, 30));
				Round = Value(round, 0, 16, 8);
			}
		}

		public enum SquotSourceFormat
		{
			Csv = 0,
			Json = 1,
		}

		public class SquotSource
		{
			public string Name { get; }
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

			public SquotSource(string name, string query = null, string header = null, string footer = null, string seed = null, string seedUrl = null, SquotSourceFormat format = SquotSourceFormat.Csv, string fields = null)
			{
				Name = name;
				Query = query;
				Header = String.IsNullOrEmpty(header) ? null : new Regex(header);
				Footer = String.IsNullOrEmpty(footer) ? null : new Regex(footer);
				Seed = String.IsNullOrEmpty(seed) ? null : new Regex(seed);
				SeedUrl = seedUrl;
				Format = format;
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
				return i < 0 ? field : items[i].Substring(field.Length + 1);
			}
		}


		public class SsoConfig
		{
			public IReadOnlyList<SsoAccountConfig> Account { get; set; } = Array.Empty<SsoAccountConfig>();
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
				AllowSpace = allowSpace ?? false;
			}
		}

		#region Singleton

		private Configuration()
		{
		}

		private static Configuration _instatnce;

		static Configuration()
		{
			Lxx.ConfigurationChanged += OnConfigurationChanged;
		}

		public static Configuration Instance
		{
			get { return _instatnce ?? (_instatnce = Config.GetValue(ConfigurationRoot, () => new Configuration())); }
		}

		private static void OnConfigurationChanged(object sender, ConfigurationEventArgs e)
		{
			_instatnce = null;
		}

		#endregion
	}
}
