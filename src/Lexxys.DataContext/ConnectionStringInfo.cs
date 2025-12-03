using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using Lexxys.Xml;

namespace Lexxys.Data;


public enum ConnectionType
{
	Default,
	SqlServer,
	Oracle,
	MySql,
	PostgreSql,
	SqLite,
	Redis,
	IbmDb2,
	MongoDb,
}

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
	public ConnectionStringInfo(ConnectionStringInfo? connectionInfo): this()
	{
		if (connectionInfo == null)
			return;

		Server = connectionInfo.Server;
		Database = connectionInfo.Database;
		Application = connectionInfo.Application;
		Workstation = connectionInfo.Workstation;
		UserId = connectionInfo.UserId;
		Password = connectionInfo.Password;
		TrustedConnection = connectionInfo.TrustedConnection;
		ConnectionTimeout = connectionInfo.ConnectionTimeout;
		ConnectionType = connectionInfo.ConnectionType;
		ConnectionAuditThreshold = connectionInfo.ConnectionAuditThreshold;
		CommandTimeout = connectionInfo.CommandTimeout;
		CommandAuditThreshold = connectionInfo.CommandAuditThreshold;
		BatchAuditThreshold = connectionInfo.BatchAuditThreshold;
		TrustServerCertificate = connectionInfo.TrustServerCertificate;
		if (connectionInfo._properties != null)
			_properties = new Dictionary<string, string>(connectionInfo._properties, connectionInfo._properties.Comparer);
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> and initializes it with the specified <paramref name="options"/>.
	/// </summary>
	/// <param name="options">Collection of key-value pairs of connection parameters.</param>
	public ConnectionStringInfo(IEnumerable<KeyValuePair<string, string?>>? options): this(null, options)
	{
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> by parsing the specified <paramref name="connectionString"/>
	/// </summary>
	/// <param name="connectionString">Regular connection string.</param>
	public ConnectionStringInfo(string connectionString): this(null, ParseParameters(connectionString))
	{
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> based on the specified <paramref name="location"/>
	/// </summary>
	/// <param name="location">Uri location of the database in the form of <c>data[base]://[user-info]server[/database][?parameters]</c>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="location"/> is not a valid database location.</exception>
	public ConnectionStringInfo(Uri location): this(null, (location ?? throw new ArgumentNullException(nameof(location))).SplitQuery()!)
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
		else if (location.Scheme is "redis" or "rediss" or "cache")
		{
			ConnectionType = ConnectionType.Redis;
			(Server, Database) = GetServerDatabase(location);
			if (location.Scheme is "redis" or "rediss")
				Server = location.Scheme + "://" + Server;
		}
		else if (location.Scheme is "mongodb" or "mongo")
		{
			ConnectionType = ConnectionType.MongoDb;
			(Server, Database) = GetServerDatabase(location);
			if (location.Scheme == "mongodb")
				Server = location.Scheme + "://" + Server;
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
			string u = uri.UserInfo;
			if (String.IsNullOrEmpty(u))
				return (UserId, Password);
			int i = u.IndexOf(':');
			var usr = i < 0 ? u: u[..i].Trim();
			var pwd = i < 0 ? null: u[(i + 1)..].Trim();
			return (usr is { Length: >0 } ? usr: UserId,  pwd is { Length: >0 } ? pwd: Password);
		}
	}

	/// <summary>
	/// Creates new instance of <see cref="ConnectionStringInfo" /> based on the specified <paramref name="template"/> and <paramref name="options"/>.
	/// </summary>
	/// <param name="template">Existing instance to use as a starting point.</param>
	/// <param name="options">Collection of key-value pairs of connection parameters.</param>
	private ConnectionStringInfo(ConnectionStringInfo? template, IEnumerable<KeyValuePair<string, string?>>? options): this(template)
	{
		if (options == null)
			return;

		foreach (var item in options)
		{
			if (String.IsNullOrWhiteSpace(item.Key))
				continue;
			string name = item.Key.Trim();
			string? value = String.IsNullOrWhiteSpace(item.Value) ? null: item.Value!.Trim();
			string lookup = Regex.Replace(name, "[ _-]", "");
			if (Synonyms.TryGetValue(lookup, out string? key))
				lookup = key;
			else
				key = name;
			bool collect = false;
			switch (lookup.ToUpperInvariant())
			{
				case "USERINSTANCE":
					key = "user instance";
					value = Strings.GetBoolean(value, true) ? "true" : "false";
					collect = true;
					break;
				case "PERSISTSECURITYINFO":
					key = "persist security info";
					value = Strings.GetBoolean(value, true) ? "true" : "false";
					collect = true;
					break;
				case "REPLICATION":
				case "POOLING":
				case "MULTIPLEACTIVERESULTSETS":
				case "ENCRYPT":
				case "CONTEXTCONNECTION":
				case "ASYNC":
				case "ENLIST":
				case "MULTISUBNETFAILOVER":
					value = Strings.GetBoolean(value, true) ? "true": "false";
					collect = true;
					break;
				case "TRUSTSERVERCERTIFICATE":
					TrustServerCertificate = Strings.GetBoolean(value, true);
					break;
				case "MINPOOLSIZE":
					key = "min pool size";
					value = Strings.GetInt32(value, 0).ToString();
					collect = true;
					break;
				case "MAXPOOLSIZE":
					key = "max pool size";
					value = Strings.GetInt32(value, 100).ToString();
					collect = true;
					break;
				case "CONNECTIONLIFETIME":
					key = "connection lifetime";
					value = Strings.GetInt32(value, 16).ToString();
					collect = true;
					break;
				case "PACKETSIZE":
					key = "packet size";
					value = Strings.GetInt32(value, 8192).ToString();
					collect = true;
					break;
				case "TRANSACTIONBINDING":
					if (value == null)
						break;
					if (value.StartsWith("IMP", StringComparison.OrdinalIgnoreCase))
						value = "implicit unbind";
					else if (value.StartsWith("EXP", StringComparison.OrdinalIgnoreCase))
						value = "explicit unbind";
					collect = true;
					break;
				case "TRUSTED_CONNECTION":
					TrustedConnection = String.Equals(value, "SSPI", StringComparison.OrdinalIgnoreCase) || Strings.GetBoolean(value, true);
					break;
				case "SERVER":
					var (server, connectionType) = SplitServerName(value ?? String.Empty);
					Server = server;
					if (connectionType != ConnectionType.Default)
						ConnectionType = connectionType;
					break;
				case "DATABASE":
					Database = value;
					break;
				case "APP":
					Application = value;
					break;
				case "WSID":
					Workstation = value;
					break;
				case "CONNECTIONTIMEOUT":
					ConnectionTimeout = Strings.GetTimeSpan(value, DefaultConnectionTimeout, TimeSpan.Zero, new TimeSpan(0, 5, 0));
					break;
				case "COMMANDTIMEOUT":
					CommandTimeout = Strings.GetTimeSpan(value, DefaultCommandTimeout, TimeSpan.Zero, new TimeSpan(0, 30, 0));
					break;
				case "CONNECTIONAUDIT":
					ConnectionAuditThreshold = Strings.GetTimeSpan(value, DefaultConnectionAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
					break;
				case "COMMANDAUDIT":
					CommandAuditThreshold = Strings.GetTimeSpan(value, DefaultCommandAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
					break;
				case "BATCHAUDIT":
					BatchAuditThreshold = Strings.GetTimeSpan(value, DefaultBatchAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 3, 0));
					break;
				case "PWD":
					Password = String.IsNullOrWhiteSpace(value) ? null: value;
					break;
				case "UID":
					UserId = String.IsNullOrWhiteSpace(value) ? null: value;
					break;
				case "LOGIN":
					if (value == null)
					{
						UserId = null;
						Password = null;
					}
					else
					{
						int i = value.IndexOf(':');
						if (i < 0)
						{
							UserId = null;
							Password = null;
						}
						else
						{
							UserId = value[..i].TrimEnd();
							Password = value[(i + 1)..].TrimStart();
							if (UserId.Length == 0 || Password.Length == 0)
							{
								UserId = null;
								Password = null;
							}
						}
					}
					break;

				default:
					collect = true;
					break;
			}
			if (collect)
			{
				_properties ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_properties[key] = value ?? String.Empty;
			}
		}
	}

	/// <summary>
	/// Database server name.
	/// </summary>
	public string? Server { get; 
		init
		{
			var (server, connectionType) = SplitServerName(value ?? String.Empty);
			field = server;
			if (connectionType != ConnectionType.Default)
				ConnectionType = connectionType;
		}
	}

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

	public ConnectionType ConnectionType { get; private init; }

	/// <summary>
	/// Password.
	/// </summary>
	public string? Password { get; init; }

	public bool? TrustedConnection { get; init; }

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

	public ConnectionStringInfo WithParameters(params (string Name, string? Value)[] options) => WithParameters((IEnumerable<(string Name, string? Value)>)options);

	public ConnectionStringInfo WithParameters(IEnumerable<(string Name, string? Value)>? options) => options == null ? this:
		new ConnectionStringInfo(this, options.Select(o => new KeyValuePair<string, string?>(o.Name, o.Value)));

	public ConnectionStringInfo WithParameters(IEnumerable<KeyValuePair<string, string?>>? options) => options == null ? this:
		new ConnectionStringInfo(this, options);

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
		if (ConnectionTimeout != TimeSpan.Zero && ConnectionTimeout != DefaultConnectionTimeout)
			yield return new KeyValuePair<string, string>("timeout", (ConnectionTimeout.Ticks / TimeSpan.TicksPerSecond).ToString());
		if (TrustServerCertificate != null)
			yield return new KeyValuePair<string, string>("trustServerCertificate", TrustServerCertificate.Value ? "true": "false");
		if (TrustedConnection != null)
			yield return new KeyValuePair<string, string>("trusted_connection", TrustedConnection.Value ? "true": "false");
		if (UserId != null)
			yield return new KeyValuePair<string, string>("uid", UserId);
		if (Password != null)
			yield return new KeyValuePair<string, string>("pwd", Password);

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
		if (ConnectionType == ConnectionType.Redis)
			return ToRedisString(Server ?? String.Empty, includeCredentials);
		if (ConnectionType == ConnectionType.MongoDb)
			return ToMongoString(Server ?? String.Empty, includeCredentials);

		var connectionInfo = ConnectionTypeMap.GetValueOrDefault(ConnectionType, DefaultConnectionType);
		bool trustedConnection = TrustedConnection ?? (connectionInfo.TrustedConnectionWhenPasswordNull && Password == null);
		bool trustedConnectionEmitted = false;
		foreach (var item in this)
		{
			var (key, value) = (item.Key, item.Value);
			switch (key)
			{
				case "pwd": if (!includeCredentials || trustedConnection) continue; break;
				case "uid": if (trustedConnection) continue; break;
				case "trusted_connection": trustedConnectionEmitted = true; break;
			}
			key = connectionInfo.MapKeyword(key);
			connection.Append(odbc ? key: key.Replace("=", "==")).Append('=');
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
		if (trustedConnection && !trustedConnectionEmitted)
		{
			var k = connectionInfo.MapKeyword("trusted_connection");
			connection.Append(odbc ? k: k.Replace("=", "==")).Append("=true;");
		}
		return connection.ToString();

		static bool IsOdbcCorrect(string value) => OdbcCorrectRex.IsMatch(value);

		static bool IsOleDbCorrect(string value) => OleDbCorrectRex.IsMatch(value);
	}
	private static readonly Regex OdbcCorrectRex = new Regex(@"^[^\s;{}]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex OleDbCorrectRex = new Regex(@"^[^\s=;""']+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private string ToRedisString(string server, bool includeCredentials)
	{
		StringBuilder connection = new StringBuilder(128);

		int i = server.IndexOf("://", StringComparison.Ordinal);
		string scheme = "rediss://";
		if (i >= 0)
			(scheme, server) = (server[..(i + 3)], server[(i + 3)..]);
		connection.Append(scheme);
		if (includeCredentials && (UserId != null || Password != null))
		{
			connection.Append(UserId);
			if (Password != null)
				connection.Append(':').Append(Password);
			connection.Append('@');
		}
		connection.Append(server);
		if (_properties != null && _properties.TryGetValue("port", out string? port))
			connection.Append(':').Append(port);
		if (Database != null)
			connection.Append('/').Append(Database);
		return connection.ToString();
	}

	private string ToMongoString(string server, bool includeCredentials)
	{
		StringBuilder connection = new StringBuilder(128);

		int i = server.IndexOf("://", StringComparison.Ordinal);
		string scheme = "mongodb://";
		if (i >= 0)
			(scheme, server) = (server[..(i + 3)], server[(i + 3)..]);
		connection.Append(scheme);
		if (includeCredentials && (UserId != null || Password != null))
		{
			connection.Append(UserId);
			if (Password != null)
				connection.Append(':').Append(Password);
			connection.Append('@');
		}
		connection.Append(server);
		if (Database != null)
			connection.Append('/').Append(Database);
		if (_properties is { Count: > 0 })
		{
			char sep = '?';
			foreach (var kvp in _properties)
			{
				connection.Append(sep).Append(kvp.Key).Append('=').Append(kvp.Value);
				sep = '&';
			}
		}
		return connection.ToString();
	}

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
			TrustedConnection?.GetHashCode() ?? 0,
			ConnectionTimeout.GetHashCode(),
			ConnectionType.GetHashCode(),
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
			foreach (var item in dictionary.OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase))
			{
				hash = HashCode.Join(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(item.Key), item.Value.GetHashCode());
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
			ConnectionType == other.ConnectionType &&
			ConnectionAuditThreshold == other.ConnectionAuditThreshold &&
			CommandTimeout == other.CommandTimeout &&
			CommandAuditThreshold == other.CommandAuditThreshold &&
			BatchAuditThreshold == other.BatchAuditThreshold &&
			TrustServerCertificate == other.TrustServerCertificate &&
			TrustedConnection == other.TrustedConnection;
		if (!fieldsEqual)
			return false;
		if (_properties == null)
			return other._properties == null;
		if (other._properties == null || _properties.Count != other._properties.Count)
			return false;

		return other._properties
			.All(o => _properties.TryGetValue(o.Key, out string? value) && o.Value == value);
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
			return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes!);

		ConnectionStringInfo? that = Config.Current.GetValue<ConnectionStringInfo?>(reference).Value;
		return that == null ?
			config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes!):
			config.Attributes.Count <= 0 ? that: new ConnectionStringInfo(that, config.Attributes!);
	}

	#region Parse connection string

	private static List<KeyValuePair<string, string?>> ParseParameters(string value)
	{
		if (String.IsNullOrWhiteSpace(value))
			return [];
		var parameters = new List<KeyValuePair<string, string?>>();
		var p = value.AsSpan();
		while (p.Length > 0)
		{
			(int i, string name) = ParseName(p);
			p = p[i..];

			(i, string val) = ParseValue(p);
			p = p[i..];

			if (name.Length > 0)
				parameters.Add(new KeyValuePair<string, string?>(name, val));
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

		int i = q.IndexOfAny(['=', ';']);
		if (i < 0)
			return (len + q.Length, q.TrimEnd().ToString());
		if (q[i] == ';')
			return (len + i + 1, q[..i].TrimEnd().ToString());
		if (q.Length <= i + 1 || q[i + 1] != '=')
			return (len + i + 1, q[..i].TrimEnd().ToString());

		Span<char> buffer = stackalloc char[LocalBufferSize];
		ValueBuilder name = new ValueBuilder(buffer);
		name.Append(q[..(i + 1)]);

		for (; ; )
		{
			len += i + 2;
			q = q[(i + 2)..];
			i = q.IndexOfAny(['=', ';']);
			if (i < 0)
			{
				name.Append(q.TrimEnd());
				return (len + q.Length, name.ToString());
			}

			if (q[i] == ';')
			{
				name.Append(q[..i].TrimEnd());
				return (len + i + 1, name.ToString());
			}

			if (q.Length <= i + 1 || q[i + 1] != '=')
			{
				name.Append(q[..i].TrimEnd());
				return (len + i + 1, name.ToString());
			}

			name.Append(q[..(i + 1)]);
		}
	}

	private ref struct ValueBuilder(Span<char> buffer)
	{
		private readonly Span<char> _buffer = buffer;
		private int _position;
		private StringBuilder? _overflow;

		public void Append(ReadOnlySpan<char> value)
		{
			if (_overflow != null)
			{
				_overflow.Append(value);
			}
			else if (_position + value.Length < _buffer.Length)
			{
				Span<char> span = _buffer[_position..];
				value.CopyTo(span);
				_position += value.Length;
			}
			else
			{
				_overflow = new StringBuilder(_buffer.Length * 2).Append(_buffer[.._position]);
				_overflow.Append(value);
			}
		}

		public override readonly string ToString() => _overflow?.ToString() ?? _buffer[.._position].ToString();
	}

	private static (int Length, string Value) ParseValue(ReadOnlySpan<char> p)
	{
		int len = SkipSpace(p);
		p = p[len..];

		if (p.Length == 0)
			return (len, String.Empty);

		if (p[0] is '"' or '\'')
		{
			var x = ParseValue(p, p[0]);
			return (len + x.Length, x.Value);
		}
		if (p[0] is '{')
		{
			var x = ParseValue(p, '}');
			return (len + x.Length, x.Value);
		}

		int i = p.IndexOf(';');
		return i < 0 ?
			(len + p.Length, p.TrimEnd().ToString()):
			(len + i + 1, p[..i].TrimEnd().ToString());
	}

	private static int SkipSemicolon(ReadOnlySpan<char> p)
	{
		int l = SkipSpace(p);
		return l < p.Length && p[l] == ';' ? l + 1: l;
	}

	private static (int Length, string Value) ParseValue(ReadOnlySpan<char> p, char d)
	{
		if (p.Length == 1)
			return (1, String.Empty);

		int i = p[1..].IndexOf(d);
		if (i < 0)
			return (p.Length, p[1..].ToString());
		++i;
		if (i + 1 >= p.Length || p[i + 1] != d)
			return (i + 1 + SkipSemicolon(p[(i + 1)..]), p[1..i].ToString());

		Span<char> buffer = stackalloc char[LocalBufferSize];
		ValueBuilder text = new ValueBuilder(buffer);
		text.Append(p[1..(i + 1)]);

		do
		{
			int j = i + 2;
			i = p[j..].IndexOf(d);
			if (i < 0)
			{
				text.Append(p[j..]);
				return (p.Length, text.ToString());
			}
			i += j;
			if (i + 1 >= p.Length || p[i + 1] != d)
			{
				text.Append(p[j..i]);
				return (i + 1 + SkipSemicolon(p[(i + 1)..]), text.ToString());
			}
			text.Append(p[j..(i + 1)]);
		} while (true);
	}

	#endregion

	private static readonly Dictionary<string, ConnectionType> ConnectionTypes = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "sqlserver", ConnectionType.SqlServer },
			{ "mssql", ConnectionType.SqlServer },
			{ "sql", ConnectionType.SqlServer },
			{ "ora", ConnectionType.Oracle },
			{ "oracle", ConnectionType.Oracle },
			{ "mysql", ConnectionType.MySql },
			{ "postgresql", ConnectionType.PostgreSql },
			{ "postgres", ConnectionType.PostgreSql },
			{ "pgsql", ConnectionType.PostgreSql },
			{ "pgs", ConnectionType.PostgreSql },
			{ "sqlite", ConnectionType.SqLite },
			{ "redis", ConnectionType.Redis },
			{ "db2", ConnectionType.IbmDb2 },
			{ "mongo", ConnectionType.MongoDb },
			{ "mongodb", ConnectionType.MongoDb },
		};

	private	static (string Server, ConnectionType ConnectionType) SplitServerName(string server)
	{
		int i = server.IndexOf(':');
		if (i < 0)
			return (server, ConnectionType.SqlServer);
		var (pfx, srv) = (server[..i], server[(i + 1)..]);
		return ConnectionTypes.TryGetValue(pfx, out ConnectionType type) ? (srv, type): (server, ConnectionType.SqlServer);
	}

	#region Tables

	private record ConnectionTypeInfo(bool TrustedConnectionWhenPasswordNull, Dictionary<string, string> KeyMap)
	{
		public string MapKeyword(string name) => KeyMap.GetValueOrDefault(name, name);
	}

	private static readonly ConnectionTypeInfo DefaultConnectionType = new ConnectionTypeInfo(true, []);

	private static readonly Dictionary<ConnectionType, ConnectionTypeInfo> ConnectionTypeMap = new()
		{
			{ ConnectionType.SqlServer, DefaultConnectionType },
			{ ConnectionType.Oracle,
				new (false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ "server", "data source" },
					{ "database", "service name" },
					{ "app", "application name" },
					{ "timeout", "connection timeout" },
					{ "uid", "user id" },
					{ "pwd", "password" },
					{ "trusted_connection", "integrated security" },
				}) },
			{ ConnectionType.MySql,
				new (true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ "app", "application name" },
					{ "timeout", "connection timeout" },
					{ "uid", "user id" },
					{ "pwd", "password" },
					{ "trustServerCertificate", "trust server certificate" },
				}) },
			{ ConnectionType.PostgreSql,
				new (true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ "server", "host" },
					{ "app", "application name" },
					{ "uid", "username" },
					{ "pwd", "password" },
					{ "trustServerCertificate", "trust server certificate" },
				}) },
			{ ConnectionType.SqLite,
				new (true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ "server", "data source" },
					{ "pwd", "password" },
				}) },
		};

	private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "applicationName",			"app" },
			{ "application",				"app" },
			{ "appName",					"app" },

			{ "extendedProperties",			"attachdbfilename" },
			{ "initialFileName",			"attachdbfilename" },

			{ "autoCommit",					"autocommit" },	// MySQL, Oracle

			{ "characterSet",				"charset" },	// MySQL
			{ "characterSetServer",			"charset" },	// MySQL

			{ "allowMultiQueries",			"allowmultiqueries" },	// MySQL

			{ "keepAlive",					"keepalive" },	// MySQL
			{ "tcpKeepAlive",				"keepalive" },	// MySQL

			{ "poolRecycle",				"pool_recycle" },	// MySQL
			{ "maxLifeTime",				"pool_recycle" },	// MySQL

			{ "asynchronous",				"async" },
			{ "asyncProc",					"async" },
			{ "asynchronousProcessing",		"async" },

			{ "queryTimeout",				"commandtimeout" },
			{ "oracle.jdbc.readTimeout",	"commandtimeout" },

			{ "connectionLifeTime",			"connectionlifetime" },
			{ "loadBalanceTimeout",			"connectionlifetime" },

			{ "connectionTimeout",			"connectiontimeout" },
			{ "connectTimeout",				"connectiontimeout" },
			{ "oracle.net.connectTimeout",	"connectiontimeout" },
			{ "loginTimeout",				"connectiontimeout" },

			{ "currentSchema",				"schema" },
			{ "alterSessionSetCurrent",		"schema" },
			{ "defaultSchema",				"schema" },

			{ "ssl",						"encrypt" },
			{ "tls",						"encrypt" },
			{ "sslMode",					"encrypt" },
			{ "ajax.net.ssl",				"encrypt" },

			{ "instanceName",				"instance" },
			{ "namedInstance",				"instance" },
			
			{ "sspi",						"trusted_connection" },
			{ "integratedSecurity",			"trusted_connection" },
			{ "trustedConnection",			"trusted_connection" },
			{ "trusted",					"trusted_connection" },
			{ "windows",                    "trusted_connection" },

			{ "maxPoolSize",				"maxpoolsize" },
			{ "minPoolSize",				"minpoolsize" },
			{ "poolSize",					"maxpoolsize" },
			
			{ "packetSize",					"packetsize" },

			{ "auth",						"pwd" },
			{ "password",					"pwd" },
			{ "pass",						"pwd" },
			{ "p",							"pwd" },

			{ "addr",						"server" },
			{ "address",					"server" },
			{ "dataSource",					"server" },
			{ "host",						"server" },
			{ "hostname",					"server" },
			{ "networkAddress",				"server" },

			{ "transactionIsolation",		"isolationlevel" },
			{ "isolationLevel",				"isolationlevel" },

			{ "trustServerCertificate",		"trustservercertificate" },
			{ "trustServerCert",			"trustservercertificate" },

			{ "userId",						"uid" },
			{ "username",					"uid" },
			{ "user",						"uid" },
			{ "u",							"uid" },

			{ "initialCatalog",				"database" },
			{ "catalog",					"database" },
			{ "serviceName",				"database" },
			{ "service_name",				"database" },
			{ "sid",						"database" },
			{ "tns",						"database" },
			{ "db",							"database" },
			{ "dbnumber",					"database" },
			{ "select",						"database" },
			{ "databaseName",				"database" },
			{ "defaultHdb",					"database" },
			{ "authSource",					"database" },

			{ "workstationId",				"wsid" },
			{ "workstation",				"wsid" },


			{ "batchAuditThreshold",        "batchAudit" },
			{ "commandAuditThreshold",      "commandAudit" },
			{ "connectionAuditThreshold",   "connectionAudit" },
			{ "connectAudit",               "connectionAudit" },
			{ "connectAuditThreshold",      "connectionAudit" },

			{ "currentLanguage",            "language"},

			{ "queryAudit",					"commandAudit" },
			{ "queryAuditThreshold",		"commandAudit" },

			{ "failover",					"failoverPartner" },

			{ "networkLibrary",				"net" },
			{ "network",					"net" },

			{ "mars",						"MultipleActiveResultSets" },
		};

	#endregion
}
