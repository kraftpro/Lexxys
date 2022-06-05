// Lexxys Infrastructural library.
// file: ConnectionStringInfo.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace Lexxys.Data
{
	using Xml;

	/// <summary>
	/// Provides information about connection to database and commands behaviours.
	/// </summary>
	public class ConnectionStringInfo: IEquatable<ConnectionStringInfo>
	{
		public static readonly TimeSpan DefaultConnectionTimeout = new TimeSpan(0, 0, 5);
		public static readonly TimeSpan DefaultConnectionAuditThreshold = new TimeSpan(0, 0, 1);
		public static readonly TimeSpan DefaultCommandTimeout = new TimeSpan(0, 0, 30);
		public static readonly TimeSpan DefaultCommandAuditThreshold = new TimeSpan(0, 0, 5);
		public static readonly TimeSpan DefaultBatchAuditThreshold = new TimeSpan(0, 0, 20);

		private readonly Dictionary<string, string>? _properties;

		private ConnectionStringInfo()
		{
			Workstation = Tools.MachineName;
			Application = Lxx.ProductName;
			ConnectionAuditThreshold = DefaultConnectionAuditThreshold;
			CommandAuditThreshold = DefaultCommandAuditThreshold;
			BatchAuditThreshold = DefaultBatchAuditThreshold;
		}

		public ConnectionStringInfo(IEnumerable<KeyValuePair<string, string?>>? attribs)
			: this(null, attribs)
		{
		}

		public ConnectionStringInfo(string connectionString)
			: this(null, ParseParameters(connectionString))
		{
		}

		public ConnectionStringInfo(string? server, string? database, IEnumerable<KeyValuePair<string, string?>>? pairs = null)
			: this(null, pairs)
		{
			if (!String.IsNullOrWhiteSpace(server))
				Server = server!.Trim();
			if (!String.IsNullOrWhiteSpace(database))
				Database = database!.Trim();
		}

		public ConnectionStringInfo(Uri location)
			: this(null, location.SplitQuery())
		{
			if (!location.IsAbsoluteUri || location.Scheme != "database")
				throw new ArgumentOutOfRangeException(nameof(location), location, null);
			string s;
			if ((s = location.Authority.Trim()).Length > 0)
				Server = s;
			if ((s = location.AbsolutePath.Trim('/').Trim()).Length > 0)
				Database = s;
		}

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
			if (connectionInfo._properties != null)
				_properties = new Dictionary<string, string>(connectionInfo._properties);
		}

		public ConnectionStringInfo(ConnectionStringInfo? connectionString, IEnumerable<KeyValuePair<string, string?>>? attribs)
			: this(connectionString)
		{
			if (attribs == null)
				return;

			bool integratedSecurity = false;
			foreach (var item in attribs)
			{
				if (String.IsNullOrWhiteSpace(item.Key))
					continue;
				string name = item.Key.Trim();
				string? value = item.Value ?? "";
				string lookup = name.Replace(" ", "");
				if (_synonyms.TryGetValue(lookup, out string? key))
					lookup = key;
				else
					key = name;
				switch (lookup.ToUpperInvariant())
				{
					case "USERINSTANCE":
					case "TRUSTSERVERCERTIFICATE":
					case "REPLICATION":
					case "POOLING":
					case "PERSISTSECURITYINFO":
					case "MULTIPLEACTIVERESULTSETS":
					case "ENCRYPT":
					case "CONTEXTCONNECTION":
					case "ASYNC":
					case "ENLIST":
						value = XmlTools.GetBoolean(value, true) ? "true": "false";
						break;
					case "MINPOOLSIZE":
					case "CONNECTIONLIFETIME":
						value = XmlTools.GetInt32(value, 0).ToString();
						break;
					case "MAXPOOLSIZE":
						value = XmlTools.GetInt32(value, 16).ToString();
						break;
					case "PACKETSIZE":
						value = XmlTools.GetInt32(value, 8192).ToString();
						break;
					case "TRANSACTIONBINDING":
						if (value.StartsWith("IMP", StringComparison.OrdinalIgnoreCase))
							value = "implicit unbind";
						else if (value.StartsWith("EXP", StringComparison.OrdinalIgnoreCase))
							value = "explicit unbind";
						break;
					case "TRUSTED_CONNECTION":
					case "TRUSTEDCONNECTION":
						integratedSecurity = XmlTools.GetBoolean(value, true);
						value = null;
						break;
					case "INTEGRATEDSECURITY":
						integratedSecurity = String.Equals(value, "SSPI", StringComparison.OrdinalIgnoreCase) || XmlTools.GetBoolean(value, true);
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
						ConnectionTimeout = XmlTools.GetTimeSpan(value, DefaultConnectionTimeout, TimeSpan.Zero, new TimeSpan(0, 5, 0));
						value = null;
						break;
					case "COMMANDTIMEOUT":
						CommandTimeout = XmlTools.GetTimeSpan(value, DefaultCommandTimeout, TimeSpan.Zero, new TimeSpan(0, 30, 0));
						value = null;
						break;
					case "CONNECTIONAUDIT":
						ConnectionAuditThreshold = XmlTools.GetTimeSpan(value, DefaultConnectionAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
						value = null;
						break;
					case "COMMANDAUDIT":
						CommandAuditThreshold = XmlTools.GetTimeSpan(value, DefaultCommandAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 1, 0));
						value = null;
						break;
					case "BATCHAUDIT":
						BatchAuditThreshold = XmlTools.GetTimeSpan(value, DefaultBatchAuditThreshold, TimeSpan.Zero, new TimeSpan(0, 3, 0));
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
						int i = value.IndexOf(';');
						if (i < 0)
						{
							Password = value;
						}
						else
						{
							UserId = value.Substring(0, i).TrimEnd();
							Password = value.Substring(i+1).TrimStart();
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

		public string? Server { get; }
		public string? Database { get; }
		public string? Application { get; }
		public string? Workstation { get; }
		public string? UserId { get; }
		public string? Password { get; }
		public TimeSpan ConnectionTimeout { get; }
		public TimeSpan ConnectionAuditThreshold { get; }
		public TimeSpan CommandTimeout { get; }
		public TimeSpan CommandAuditThreshold { get; }
		public TimeSpan BatchAuditThreshold { get; }

		public bool IsEmpty => Server == null && Database == null && _properties == null;

		public string? this[string property]
		{
			get
			{
				if (_properties == null)
					return null;
				_properties.TryGetValue(property, out string? value);
				return value;
			}
		}

		public string GetConnectionString(bool odbc = false)
		{
			return ToString(true, odbc);
		}

		private string ToString(bool includeCredentials, bool odbc = false)
		{
			StringBuilder connection = new StringBuilder(128);
			if (Server != null)
				Append("server", Server);
			if (Database != null)
				Append("database", Database);
			if (Application != null)
				Append("app", Application);
			if (Workstation != null)
				Append("wsid", Workstation);
			if (ConnectionTimeout.Ticks > 0)
				Append("timeout", (ConnectionTimeout.Ticks / TimeSpan.TicksPerSecond).ToString());

			if (Password == null)
			{
				connection.Append("trusted_connection=true;");
			}
			else
			{
				Append("uid", String.IsNullOrEmpty(UserId) ? "sa": UserId!);
				if (includeCredentials)
					Append("pwd", Password);
			}
			if (_properties != null)
			{
				foreach (var item in _properties)
				{
					Append(item.Key, item.Value);
				}
			}
			if (connection.Length > 0)
				--connection.Length;
			return connection.ToString();

			void Append(string name, string value)
			{
				connection.Append(name).Append('=').Append(Value(value)).Append(';');
			}

			string Value(string value)
			{
				if (value.Length == 0)
					return value;
				if (odbc)
					return value[0] == '{' || value.IndexOf(';') >= 0 ? "{" + value.Replace("}", "}}") + "}": value;

				return
					value.IndexOfAny(__adoAny) >= 0 || value.Length != value.Trim().Length || value.Any(Char.IsControl) ?
						value.IndexOf('"') < 0 ?
							"\"" + value + "\"":
						value.IndexOf('\'') < 0 ?
							"'" + value + "'":
							"\"" + value.Replace("\"", "\"\"") + "\"":
						value;
			}
		}
		private static readonly char[] __adoAny = new [] {';', '\'', '"'};

		public override string ToString()
		{
			return ToString(false);
		}

		public override bool Equals(object? obj)
		{
			return obj is ConnectionStringInfo other && Equals(other);
		}

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
				_properties?.GetHashCode() ?? 0
				);
		}

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
				BatchAuditThreshold == other.BatchAuditThreshold;
			if (!fieldsEqual)
				return false;
			if (_properties == null || _properties.Count == 0)
				return other._properties == null || other._properties.Count == 0;
			if (other._properties == null || _properties.Count != other._properties.Count)
				return false;

			return other._properties.All(o => _properties.TryGetValue(o.Key, out var value) && o.Value == value);
		}

		public static ConnectionStringInfo? Create(XmlLiteNode? config)
		{
			if (config == null || config.IsEmpty)
				return null;

			string? reference = XmlTools.GetString(config.Value, null);
			if (reference == null)
				return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes);

			ConnectionStringInfo that = Config.Current.GetValue<ConnectionStringInfo>(reference).Value;
			if (that == null)
				return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes);

			if (config.Attributes.Count > 0)
				return new ConnectionStringInfo(that, config.Attributes);

			return that;
		}

		private static IList<KeyValuePair<string, string?>> ParseParameters(string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				return Array.Empty<KeyValuePair<string, string?>>();
			var parameters = new List<KeyValuePair<string, string?>>();
			var p = value.AsSpan();
			while (p.Length > 0)
			{
				var i = p.IndexOf('=');
				var j = p.IndexOf(';');
				string name;
				string val;

				if (i < 0 || j >= 0 && j < i)
				{
					if (j < 0)
					{
						j = p.Length - 1;
						name = p.Trim().ToString();
					}
					else
					{
						name = p.Slice(0, j).Trim().ToString();
					}
					if (name.Length > 0)
						parameters.Add(new KeyValuePair<string, string?>(name, null));
					p = p.Slice(j + 1);
					continue;
				}

				name = p.Slice(0, i).Trim().ToString();

				p = SkipSpace(p.Slice(i + 1));
				if (p.Length == 0 || p[0] == ';')
				{
					if (name.Length > 0)
						parameters.Add(new KeyValuePair<string, string?>(name, null));
					if (p.Length > 0)
						p = p.Slice(1);
					continue;
				}

				(i, val) = ParseValue(p);
				p = p.Slice(i);
				j = p.IndexOf(';');
				if (j < 0)
				{
					if (name.Length > 0)
						parameters.Add(new KeyValuePair<string, string?>(name, val));
					return parameters;
				}
				if (name.Length > 0)
					parameters.Add(new KeyValuePair<string, string?>(name, val));
				p = p.Slice(j + 1);
			}
			return parameters;

			ReadOnlySpan<char> SkipSpace(ReadOnlySpan<char> p)
			{
				for (int i = 0; i < p.Length; ++i)
				{
					if (!Char.IsWhiteSpace(p[i]))
						return p.Slice(i);
				}
				return ReadOnlySpan<char>.Empty;
			}

			(int Length, string Value) ParseValue(ReadOnlySpan<char> p)
			{
				if (p.Length == 0)
					return (0, String.Empty);
				if (p[0] is '"' or '\'')
					return ParseAdoValue(p);
				if (p[0] is '{')
					return ParseOdbcValue(p);
				int i = p.IndexOf(';');
				return i < 0 ?
					(p.Length, p.TrimEnd().ToString()) :
					(i, p.Slice(0, i).TrimEnd().ToString());
			}

			(int Length, string Value) ParseAdoValue(ReadOnlySpan<char> p)
			{
				char d = p[0];
				if (p.Length == 1)
					return (1, String.Empty);
				var text = new StringBuilder();
				for (int i = 1; i < p.Length; ++i)
				{
					char c = p[i];
					if (c == '\\')
						text.Append(++i < p.Length ? p[i]: c);
					else if (c == d)
						if (++i < p.Length && p[i] == d)
							text.Append(d);
						else
							return (i, text.ToString());
					else
						text.Append(c);
				}
				return (p.Length, text.ToString());
			}

			(int Length, string Value) ParseOdbcValue(ReadOnlySpan<char> p)
			{
				if (p.Length == 1)
					return (1, String.Empty);
				var text = new StringBuilder();
				for (int i = 1; i < p.Length; ++i)
				{
					char c = p[i];
					if (c == '}')
						if (++i < p.Length && p[i] == '}')
							text.Append('}');
						else
							return (i, text.ToString());
					else
						text.Append(c);
				}
				return (p.Length, text.ToString());
			}
		}


		#region Tables
		private static readonly Dictionary<string, string> _synonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
}
