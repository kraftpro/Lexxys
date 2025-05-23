using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using Lexxys;
using Lexxys.Xml;

using System.Net.Mail;

namespace Lexxys.Data;

/// <summary>
/// Provides information about connection to the data source.
/// </summary>
public class ConnectionStringInfo: IEquatable<ConnectionStringInfo>, IEnumerable<KeyValuePair<string, string>>
{
	public static readonly TimeSpan DefaultConnectionTimeout = new TimeSpan(0, 0, 5);
	public static readonly TimeSpan DefaultConnectionAuditThreshold = new TimeSpan(0, 0, 1);
	public static readonly TimeSpan DefaultCommandTimeout = new TimeSpan(0, 0, 30);
	public static readonly TimeSpan DefaultCommandAuditThreshold = new TimeSpan(0, 0, 5);
	public static readonly TimeSpan DefaultBatchAuditThreshold = new TimeSpan(0, 0, 20);

	private readonly Dictionary<string, string>? _properties;

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" />.
	/// </summary>
	public ConnectionStringInfo()
	{
		Workstation = Tools.MachineName;
		Application = Lxx.ProductName;
		ConnectionAuditThreshold = DefaultConnectionAuditThreshold;
		CommandAuditThreshold = DefaultCommandAuditThreshold;
		BatchAuditThreshold = DefaultBatchAuditThreshold;
		ConnectionTimeout = DefaultConnectionTimeout;
		CommandTimeout = DefaultCommandTimeout;
	}

	/// <summary>
	/// Copy constructor for the <see cref="ConnectionStringInfo" />.
	/// </summary>
	/// <param name="connectionInfo">The source object to copy from.</param>
	public ConnectionStringInfo(ConnectionStringInfo? connectionInfo)
		: this()
	{
		if (connectionInfo == null)
			return;

		Server = connectionInfo.Server;
		Database = connectionInfo.Database;
		Application = connectionInfo.Application;
		Workstation = connectionInfo.Workstation;
		UserId = connectionInfo.UserId;
		Password = connectionInfo.Password;
		ConnectionTimeout = connectionInfo.ConnectionTimeout;
		ConnectionAuditThreshold = connectionInfo.ConnectionAuditThreshold;
		CommandTimeout = connectionInfo.CommandTimeout;
		CommandAuditThreshold = connectionInfo.CommandAuditThreshold;
		BatchAuditThreshold = connectionInfo.BatchAuditThreshold;
		TrustServerCertificate = connectionInfo.TrustServerCertificate;
		if (connectionInfo._properties != null)
			_properties = new Dictionary<string, string>(connectionInfo._properties);
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> and initializes it with the specified <paramref name="options"/>.
	/// </summary>
	/// <param name="options">Collection of key-value pairs of connection parameters.</param>
	public ConnectionStringInfo(IEnumerable<KeyValuePair<string, string>>? options)
		: this(null, options)
	{
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> by parsing the specified <paramref name="connectionString"/>
	/// </summary>
	/// <param name="connectionString">Regular connection string.</param>
	public ConnectionStringInfo(string connectionString)
		: this(null, ParseParameters(connectionString))
	{
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> based on the specified <paramref name="location"/>
	/// </summary>
	/// <param name="location">Uri location of the database in the form of <c>data[base]://[user-info]server[/database][?parameters]</c>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="location"/> is not a valid database location.</exception>
	public ConnectionStringInfo(Uri location)
		: this(null, (location ?? throw new ArgumentNullException(nameof(location))).SplitQuery())
	{
		if (!location.IsAbsoluteUri)
			throw new ArgumentOutOfRangeException(nameof(location), location, null);
		if (location.Scheme is "http" or "https")
		{
			(_properties ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))["URL"] = $"{location.Scheme}://{location.Authority}{location.AbsolutePath}";
		}
		else if (location.Scheme is "database" or "data")
		{
			(Server, Database) = GetServerDatabase(location);
		}
		else
		{
			throw new ArgumentOutOfRangeException(nameof(location), location, null);
		}

		(UserId, Password) = GetUserInfo(location);

		(string? Server, string? Database) GetServerDatabase(Uri uri)
		{
			string? s = uri.Authority.TrimToNull();
			string? d = uri.AbsolutePath.Trim('/').TrimToNull();
			return (s ?? Server, d ?? Database);
		}

		(string? UserId, string? Password) GetUserInfo(Uri uri)
		{
			string? u = uri.UserInfo.TrimToNull();
			if (u == null)
				return (UserId, Password);
			int i = u.IndexOf(':');
			return i < 0 ? (u, Password): (u.Substring(0, i), u.Substring(i + 1));
		}
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> based on the specified <paramref name="connectionString"/> and <paramref name="options"/>.
	/// </summary>
	/// <param name="connectionString">Regular connection string.</param>
	/// <param name="options">Collection of key-value pairs of connection parameters.</param>
	public ConnectionStringInfo(ConnectionStringInfo? connectionString, IEnumerable<KeyValuePair<string, string>>? options)
		: this(connectionString)
	{
		if (options == null)
			return;

		bool integratedSecurity = false;
		foreach (var item in options)
		{
			if (String.IsNullOrWhiteSpace(item.Key))
				continue;
			string name = item.Key.Trim();
			string? value = item.Value ?? String.Empty;
			string lookup = Regex.Replace(name, "[ _-]", "");
			if (Synonyms.TryGetValue(lookup, out string? key))
				lookup = key;
			else
				key = name;
			switch (lookup.ToUpperInvariant())
			{
				case "USERINSTANCE":
				case "REPLICATION":
				case "POOLING":
				case "PERSISTSECURITYINFO":
				case "MULTIPLEACTIVERESULTSETS":
				case "ENCRYPT":
				case "CONTEXTCONNECTION":
				case "ASYNC":
				case "ENLIST":
				case "MULTISUBNETFAILOVER":
					value = Strings.GetBoolean(value, true) ? "true": "false";
					break;
				case "TRUSTSERVERCERTIFICATE":
					TrustServerCertificate = Strings.GetBoolean(value, true);
					value = null;
					break;
				case "MINPOOLSIZE":
				case "CONNECTIONLIFETIME":
					value = Strings.GetInt32(value, 0).ToString();
					break;
				case "MAXPOOLSIZE":
					value = Strings.GetInt32(value, 16).ToString();
					break;
				case "PACKETSIZE":
					value = Strings.GetInt32(value, 8192).ToString();
					break;
				case "TRANSACTIONBINDING":
					if (value.StartsWith("IMP", StringComparison.OrdinalIgnoreCase))
						value = "implicit unbind";
					else if (value.StartsWith("EXP", StringComparison.OrdinalIgnoreCase))
						value = "explicit unbind";
					break;
				case "TRUSTEDCONNECTION":
					integratedSecurity = Strings.GetBoolean(value, true);
					value = null;
					break;
				case "INTEGRATEDSECURITY":
					integratedSecurity = String.Equals(value, "SSPI", StringComparison.OrdinalIgnoreCase) || Strings.GetBoolean(value, true);
					value = null;
					break;
				case "SERVER":
					Server = value;
					value = null;
					break;
				case "DATABASE":
					Database = value;
					value = null;
					break;
				case "APP":
					Application = value;
					value = null;
					break;
				case "WSID":
					Workstation = value;
					value = null;
					break;
				case "CONNECTIONTIMEOUT":
					ConnectionTimeout = Strings.GetTimeSpan(value, DefaultConnectionTimeout, TimeSpan.Zero, new TimeSpan(0, 5, 0));
					value = null;
					break;
				case "COMMANDTIMEOUT":
					CommandTimeout = Strings.GetTimeSpan(value, DefaultCommandTimeout, TimeSpan.Zero, new TimeSpan(0, 30, 0));
					value = null;
					break;
				case "CONNECTIONAUDIT":
					ConnectionAuditThreshold = Strings.GetTimeSpan(value, DefaultConnectionAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
					value = null;
					break;
				case "COMMANDAUDIT":
					CommandAuditThreshold = Strings.GetTimeSpan(value, DefaultCommandAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
					value = null;
					break;
				case "BATCHAUDIT":
					BatchAuditThreshold = Strings.GetTimeSpan(value, DefaultBatchAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 3, 0));
					value = null;
					break;
				case "PWD":
					Password = value;
					value = null;
					break;
				case "UID":
					UserId = value;
					value = null;
					break;
				case "LOGIN":
					int i = value.IndexOf(':');
					if (i < 0)
					{
						Password = value;
					}
					else
					{
						UserId = value.Substring(0, i).TrimEnd();
						Password = value.Substring(i + 1).TrimStart();
					}
					value = null;
					break;
			}
			if (value != null)
			{
				_properties ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_properties[key] = value;
			}
		}
		if (integratedSecurity)
			Password = null;
	}

	/// <summary>
	/// Database server name.
	/// </summary>
	public string? Server { get; init; }

	/// <summary>
	/// Database name.
	/// </summary>
	public string? Database { get; init; }

	/// <summary>
	/// Client application name.
	/// </summary>
	public string? Application { get; init; }

	/// <summary>
	/// Client workstation name.
	/// </summary>
	public string? Workstation { get; init; }

	/// <summary>
	/// User ID.
	/// </summary>
	public string? UserId { get; init; }

	/// <summary>
	/// Password.
	/// </summary>
	public string? Password { get; init; }

	/// <summary>
	/// Connection timeout.
	/// </summary>
	public TimeSpan ConnectionTimeout { get; init; }

	/// <summary>
	/// Connection time audit threshold.
	/// </summary>
	public TimeSpan ConnectionAuditThreshold { get; init; }

	/// <summary>
	/// Command execution timeout.
	/// </summary>
	public TimeSpan CommandTimeout { get; init; }

	/// <summary>
	/// Command execution time audit threshold.
	/// </summary>
	public TimeSpan CommandAuditThreshold { get; init; }

	/// <summary>
	/// Execution time audit threshold for batches.
	/// </summary>
	public TimeSpan BatchAuditThreshold { get; init; }

	/// <summary>
	/// Trust server certificate.
	/// </summary>
	public bool? TrustServerCertificate { get; init; }

	/// <summary>
	/// Returns true if the connection is empty.
	/// </summary>
	public bool IsEmpty => Server == null && Database == null && _properties == null;

	public ConnectionStringInfo WithParameters(params (string Name, string Value)[] options) => WithParameters((IEnumerable<(string Name, string Value)>)options);

	public ConnectionStringInfo WithParameters(IEnumerable<(string Name, string Value)>? options)
	{
		if (options == null)
			return this;
		return new ConnectionStringInfo(this, options.Select(o => new KeyValuePair<string, string>(o.Name, o.Value)));
	}

	public ConnectionStringInfo WithParameters(IEnumerable<KeyValuePair<string, string>>? options)
	{
		if (options == null)
			return this;
		return new ConnectionStringInfo(this, options);
	}

	/// <summary>
	/// Constructs a connection string.
	/// </summary>
	/// <param name="odbc">Use ODBC escaping rules for the connection string.</param>
	/// <returns></returns>
	public string GetConnectionString(bool odbc = false) => ToString(true, odbc);

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		if (Server != null)
			yield return new KeyValuePair<string, string>("server", Server);
		if (Database != null)
			yield return new KeyValuePair<string, string>("database", Database);
		if (Application != null)
			yield return new KeyValuePair<string, string>("app", Application);
		if (Workstation != null)
			yield return new KeyValuePair<string, string>("wsid", Workstation);
		if (ConnectionTimeout != default && ConnectionTimeout != DefaultConnectionTimeout)
			yield return new KeyValuePair<string, string>("timeout", (ConnectionTimeout.Ticks / TimeSpan.TicksPerSecond).ToString());
		if (TrustServerCertificate != null)
			yield return new KeyValuePair<string, string>("trustServerCertificate", TrustServerCertificate.Value ? "true" : "false");
		if (String.IsNullOrEmpty(Password))
		{
			yield return new KeyValuePair<string, string>("trusted_connection", "true");
		}
		else
		{
			yield return new KeyValuePair<string, string>("uid", String.IsNullOrEmpty(UserId) ? "sa": UserId!);
			yield return new KeyValuePair<string, string>("pwd", Password!);
		}
		if (_properties != null)
		{
			foreach (var item in _properties)
			{
				yield return item;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	private string ToString(bool includeCredentials, bool odbc = false)
	{
		StringBuilder connection = new StringBuilder(128);
		foreach (var item in this)
		{
			if (!includeCredentials && item.Key == "pwd")
				continue;
			connection.Append(odbc ? item.Key : item.Key.Replace("=", "==")).Append('=');
			string value = item.Value;
			if (value.Length != 0)
			{
				if (odbc)
					if (IsOdbcCorrect(value))
						connection.Append(value);
					else
						connection.Append('{').Append(value.Replace("}", "}}")).Append('}');
				else // oledb
					if (IsOleDbCorrect(value))
						connection.Append(value);
					else if (value.IndexOf('"') < 0)
						connection.Append('"').Append(value).Append('"');
					else if (value.IndexOf('\'') < 0)
						connection.Append('\'').Append(value).Append('\'');
					else
						connection.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
			}
			connection.Append(';');
		}
		return connection.ToString();

		static bool IsOdbcCorrect(string value) => (value[0] != '{' && !Char.IsWhiteSpace(value[0]) && !Char.IsWhiteSpace(value[^1]) && String.Equals(value, "driver", StringComparison.OrdinalIgnoreCase) && value.IndexOf(';') < 0);

		static bool IsOleDbCorrect(string value) => __oledbCorrect.IsMatch(value);
	}
	private static readonly Regex __oledbCorrect = new Regex(@"^[^""'=;\s\p{Cc}]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Returns a string representation of the connection.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => ToString(false);

	/// <summary>
	/// Returns true if the specified <paramref name="obj"/> is equal to this connection.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns></returns>
	public override bool Equals(object? obj) => obj is ConnectionStringInfo other && Equals(other);

	/// <summary>
	/// Returns a hash code for this connection.
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode()
	{
		return HashCode.Join(
			Server?.GetHashCode() ?? 0,
			Database?.GetHashCode() ?? 0,
			Workstation?.GetHashCode() ?? 0,
			Application?.GetHashCode() ?? 0,
			UserId?.GetHashCode() ?? 0,
			Password?.GetHashCode() ?? 0,
			ConnectionTimeout.GetHashCode(),
			ConnectionAuditThreshold.GetHashCode(),
			CommandTimeout.GetHashCode(),
			CommandAuditThreshold.GetHashCode(),
			BatchAuditThreshold.GetHashCode(),
			TrustServerCertificate?.GetHashCode() ?? 0,
			GetItemsHashCode(_properties)
			);

		static int GetItemsHashCode(Dictionary<string, string>? dictionary)
		{
			if (dictionary == null) return 0;

			int hash = 0;
			foreach (var item in dictionary)
			{
				hash = HashCode.Join(hash, item.Key.GetHashCode(), item.Value.GetHashCode());
			}
			return hash;
		}
	}

	/// <summary>
	/// Returns true if the specified <paramref name="other"/> connection is equal to this connection.
	/// </summary>
	/// <param name="other">The connection to compare.</param>
	/// <returns></returns>
	public bool Equals(ConnectionStringInfo? other)
	{
		if (other == null)
			return false;

		bool fieldsEqual = Server == other.Server &&
			Database == other.Database &&
			Workstation == other.Workstation &&
			Application == other.Application &&
			UserId == other.UserId &&
			Password == other.Password &&
			ConnectionTimeout == other.ConnectionTimeout &&
			ConnectionAuditThreshold == other.ConnectionAuditThreshold &&
			CommandTimeout == other.CommandTimeout &&
			CommandAuditThreshold == other.CommandAuditThreshold &&
			BatchAuditThreshold == other.BatchAuditThreshold &&
			TrustServerCertificate == other.TrustServerCertificate;
		if (!fieldsEqual)
			return false;
		if (_properties == null)
			return other._properties == null;
		if (other._properties == null || _properties.Count != other._properties.Count)
			return false;

		return other._properties.All(o => _properties.TryGetValue(o.Key, out string? value) && o.Value == value);
	}

	/// <summary>
	/// Creates a new connection string from the specified <see cref="IXmlReadOnlyNode"/>.
	/// </summary>
	/// <param name="config"><see cref="IXmlReadOnlyNode"/> configuration node.</param>
	/// <returns></returns>
	public static ConnectionStringInfo? Create(IXmlReadOnlyNode? config)
	{
		if (config == null || config.IsEmpty)
			return null;

		string? reference = Strings.GetString(config.Value, null);
		if (reference == null)
			return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes);

		ConnectionStringInfo? that = Config.Current.GetValue<ConnectionStringInfo?>(reference).Value;
		return that == null ?
			config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes):
			config.Attributes.Count <= 0 ? that : new ConnectionStringInfo(that, config.Attributes);
	}

	#region Parse connection string

	private static IList<KeyValuePair<string, string>> ParseParameters(string value)
	{
		if (String.IsNullOrWhiteSpace(value))
			return [];
		var parameters = new List<KeyValuePair<string, string>>();
		var p = value.AsSpan();
		while (p.Length > 0)
		{
			(int i, string name) = ParseName(p);
			p = p.Slice(i);

			(i, string val) = ParseValue(p);
			p = p.Slice(i);

			if (name.Length > 0)
				parameters.Add(new KeyValuePair<string, string>(name, val));
		}
		return parameters;
	}

	private static int SkipSpace(ReadOnlySpan<char> p)
	{
		for (int i = 0; i < p.Length; ++i)
		{
			if (!Char.IsWhiteSpace(p[i]))
				return i;
		}
		return p.Length;
	}

	private const int LocalBufferSize = 512;
	
	private static (int Length, string Name) ParseName(ReadOnlySpan<char> p)
	{
		int len = SkipSpace(p);
		var q = p[len..];

		if (p.Length == 0)
			return (0, String.Empty);

		Span<char> name = stackalloc char[LocalBufferSize];
		int l = 0;

		for (; ; )
		{
			int i = q.IndexOfAny(['=', ';']);
			if (i < 0)
			{
				l = Append(name, l, q.TrimEnd());
				return (len + q.Length, name[0..l].ToString());
			}

			if (q[i] == ';')
			{
				l = Append(name, l, q[..i].TrimEnd());
				return (len + i + 1, name[0..l].ToString());
			}

			if (q.Length <= i + 1 || q[i + 1] != '=')
			{
				l = Append(name, l, q[..i].TrimEnd());
				return (len + i + 1, name[0..l].ToString());
			}

			l = Append(name, l, q[..(i + 1)]);
			len += i + 1;
			q = q[(i + 2)..];
		}
	}

	private static int Append(Span<char> buffer, int position, ReadOnlySpan<char> part)
	{
		Span<char> span = buffer[position..];
		if (span.Length < part.Length)
			part = part[..span.Length];
		part.CopyTo(span);
		return position + part.Length;
	}

	private static (int Length, string Value) ParseValue(ReadOnlySpan<char> p)
	{
		int len = SkipSpace(p);
		p = p[len..];

		if (p.Length == 0)
			return (len, String.Empty);

		if (p[0] is '"' or '\'')
		{
			var x = ParseOleDbValue(p);
			return (len + x.Length, x.Value);
		}
		if (p[0] is '{')
		{
			var x = ParseOdbcValue(p);
			return (len + x.Length, x.Value);
		}

		int i = p.IndexOf(';');
		return i < 0 ?
			(len + p.Length, p.TrimEnd().ToString()) :
			(len + i + 1, p.Slice(0, i).TrimEnd().ToString());
	}

	private static int SkipSemicolon(ReadOnlySpan<char> p)
	{
		int l = SkipSpace(p);
		return l < p.Length && p[l] == ';' ? l + 1 : l;
	}

	private static (int Length, string Value) ParseOleDbValue(ReadOnlySpan<char> p)
	{
		char d = p[0];
		if (p.Length == 1)
			return (1, String.Empty);
		Span<char> value = stackalloc char[LocalBufferSize];
		int l = 0;

		for (int i = 1; i < p.Length; ++i)
		{
			char c = p[i];
			if (c == d && (++i >= p.Length || p[i] != d))
				return (i + SkipSemicolon(p.Slice(i)), value[0..l].ToString());
			if (l < value.Length)
				value[l++] = c;
		}
		return (p.Length, value[0..l].ToString());
	}

	private static (int Length, string Value) ParseOdbcValue(ReadOnlySpan<char> p)
	{
		if (p.Length == 1)
			return (1, String.Empty);
		Span<char> value = stackalloc char[LocalBufferSize];
		int l = 0;

		for (int i = 1; i < p.Length; ++i)
		{
			char c = p[i];
			if (c == '}' && (++i >= p.Length || p[i] != '}'))
				return (i + SkipSemicolon(p.Slice(i)), value[0..l].ToString());
			if (l < value.Length)
				value[l++] = c;
		}
		return (p.Length, value[0..l].ToString());
	}

	#endregion

	#region Tables
	private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "userId",						"uid" },
			{ "user",						"uid" },
			{ "u",							"uid" },
			{ "password",					"pwd" },
			{ "p",							"pwd" },

			{ "workstationId",				"wsid" },
			{ "workstation",				"wsid" },

			{ "applicationName",			"app" },
			{ "application",				"app" },

			{ "addr",						"server" },
			{ "address",					"server" },
			{ "networkAddress",				"server" },
			{ "dataSource",					"server" },

			{ "async",						"async" },
			{ "asynchronous",				"async" },
			{ "asyncProc",					"async" },
			{ "asynchronousProcessing",		"async" },

			{ "connectTimeout",				"connectionTimeout" },
			{ "connectionAuditThreshold",	"connectionAudit" },
			{ "connectAudit",				"connectionAudit" },
			{ "connectAuditThreshold",		"connectionAudit" },

			{ "queryTimeout",				"commandTimeout" },
			{ "queryAudit",					"commandAudit" },
			{ "queryAuditThreshold",		"commandAudit" },
			{ "commandAuditThreshold",		"commandAudit" },
			{ "batchAuditThreshold",		"batchAudit" },

			{ "currentLanguage",			"language"},
			  
			{ "failover",					"failoverPartner" },

			{ "initialCatalog",				"database" },
			{ "catalog",					"database" },

			{ "integrated",					"integratedSecurity" },
			{ "trusted",					"integratedSecurity" },
			{ "trustedConnection",			"integratedSecurity" },

			{ "poolSize",					"maxPoolSize" },

			{ "networkLibrary",				"net" },
			{ "network",					"net" },

			{ "mars",						"MultipleActiveResultSets" },
		};
	#endregion
}
