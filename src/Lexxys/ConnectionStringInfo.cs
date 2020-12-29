// Lexxys Infrastructural library.
// file: ConnectionStringInfo.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lexxys.Configuration;
using Lexxys.Xml;

namespace Lexxys
{
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

		private Dictionary<string, string> _properties;

		public ConnectionStringInfo()
		{
			Workstation = Tools.MachineName;
			Application = Lxx.ProductName;
			ConnectionAuditThreshold = DefaultConnectionAuditThreshold;
			CommandAuditThreshold = DefaultCommandAuditThreshold;
			BatchAuditThreshold = DefaultBatchAuditThreshold;
			//ConnectionTimeout = DefaultConnectionTimeout;
			//CommandTimeout = DefaultCommandTimeout;
		}

		public ConnectionStringInfo(IEnumerable<KeyValuePair<string, string>> pairs)
			: this()
		{
			Define(pairs);
		}

		public ConnectionStringInfo(string connectionString)
			: this()
		{
			if (String.IsNullOrEmpty(connectionString))
				return;
			var pairs = connectionString.Split(';')
				.Where(o => !String.IsNullOrWhiteSpace(o))
				.Select(o =>
				{
					int i = o.IndexOf('=');
					return i < 0 ? new KeyValuePair<string, string>(o, null):
						new KeyValuePair<string, string>(o.Substring(0, i), o.Substring(i+1));
				});
			Define(pairs);
		}

		public ConnectionStringInfo(string server, string database, IEnumerable<KeyValuePair<string, string>> pairs = null)
			: this()
		{
			Server = String.IsNullOrWhiteSpace(server) ? null: server;
			Database = String.IsNullOrWhiteSpace(database) ? null: database;
			Define(pairs);
		}

		public ConnectionStringInfo(ConfigurationLocator location)
			: this()
		{
			if (location == null)
				throw new ArgumentNullException(nameof(location));
			Define(location.QueryParameters);
			string s = location.Host + location.Path;
			if (s.StartsWith("^"))
				Append(Config.GetValue<ConnectionStringInfo>(s.Substring(1)));
			else if (!String.IsNullOrWhiteSpace(s))
				Server = s;
		}

		public ConnectionStringInfo(ConnectionStringInfo value)
			: this()
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Server = value.Server;
			Database = value.Database;
			Application = value.Application;
			Workstation = value.Workstation;
			UserId = value.UserId;
			Password = value.Password;
			ConnectionTimeout = value.ConnectionTimeout;
			ConnectionAuditThreshold = value.ConnectionAuditThreshold;
			CommandTimeout = value.CommandTimeout;
			CommandAuditThreshold = value.CommandAuditThreshold;
			BatchAuditThreshold = value.BatchAuditThreshold;
			if (value._properties != null)
				_properties = new Dictionary<string,string>(value._properties);
		}

		public string Server { get; private set; }
		public string Database { get; private set; }
		public string Application { get; set; }
		public string Workstation { get; private set; }
		public string UserId { get; private set; }
		public string Password { get; private set; }
		public TimeSpan ConnectionTimeout { get; private set; }
		public TimeSpan ConnectionAuditThreshold { get; private set; }
		public TimeSpan CommandTimeout { get; private set; }
		public TimeSpan CommandAuditThreshold { get; private set; }
		public TimeSpan BatchAuditThreshold { get; private set; }

		public bool IsEmpty => Server == null && Database == null && _properties == null;

		public string this[string property]
		{
			get
			{
				if (_properties == null)
					return null;
				_properties.TryGetValue(property, out string value);
				return value;
			}
		}

		public string GetConnectionString()
		{
			return ToString(true);
		}

		public void Append(ConnectionStringInfo value)
		{
			if (value == null)
				return;

			if (!String.IsNullOrEmpty(value.Server))
				Server = value.Server;
			if (!String.IsNullOrEmpty(value.Database))
				Database = value.Database;
			if (!String.IsNullOrEmpty(value.Application))
				Application = value.Application;
			if (!String.IsNullOrEmpty(value.Workstation))
				Workstation = value.Workstation;
			if (!String.IsNullOrEmpty(value.UserId))
				UserId = value.UserId;
			if (!String.IsNullOrEmpty(value.Password))
				Password = value.Password;
			if (value.ConnectionTimeout != TimeSpan.Zero)
				value.ConnectionTimeout = value.ConnectionTimeout;
			if (value.ConnectionAuditThreshold != TimeSpan.Zero)
				ConnectionAuditThreshold = value.ConnectionAuditThreshold;

			if (value._properties != null && _properties.Count > 0)
			{
				if (_properties == null || _properties.Count == 0)
				{
					_properties = new Dictionary<string,string>(value._properties);
				}
				else
				{
					foreach (var item in value._properties)
					{
						_properties[item.Key] = item.Value;
					}
				}
			}
		}

		private string ToString(bool includeCredentials)
		{
			var connection = new StringBuilder(128);
			if (Server != null)
				connection.Append("server=").Append(Server).Append(';');
			if (Database != null)
				connection.Append("database=").Append(Database).Append(';');
			if (Application != null)
				connection.Append("app=").Append(Application).Append(';');
			if (Workstation != null)
				connection.Append("wsid=").Append(Workstation).Append(';');
			if (ConnectionTimeout.Ticks > 0)
				connection.Append("timeout=").Append(ConnectionTimeout.Ticks / TimeSpan.TicksPerSecond).Append(';');

			if (Password == null)
			{
				connection.Append("integrated security=SSPI;");
			}
			else
			{
				connection.Append("uid=").Append(String.IsNullOrEmpty(UserId) ? "sa": UserId).Append(';');
				if (includeCredentials)
					connection.Append("pwd=").Append(Password).Append(';');
			}
			if (_properties != null)
			{
				foreach (var item in _properties)
				{
					connection.Append(item.Key).Append('=').Append(item.Value).Append(';');
				}
			}
			if (connection.Length > 0)
				--connection.Length;
			return connection.ToString();
		}

		public override string ToString()
		{
			return ToString(false);
		}

		public bool Equals(ConnectionStringInfo other)
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
			if (_properties.Count != other._properties.Count)
				return false;
			using (var ai = _properties.GetEnumerator())
			using (var bi = _properties.GetEnumerator())
			{
				while (ai.MoveNext() && bi.MoveNext())
				{
					if (ai.Current.Key != bi.Current.Key || ai.Current.Value != bi.Current.Value)
						return false;
				}
			}
			return true;
		}

		public static ConnectionStringInfo Create(XmlLiteNode config)
		{
			if (config == null || config.IsEmpty)
				return null;

			string reference = XmlTools.GetString(config.Value, null);
			if (reference == null)
				return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes);

			ConnectionStringInfo that = Config.GetValue<ConnectionStringInfo>(reference);
			if (that == null)
				return config.Attributes.Count == 0 ? null: new ConnectionStringInfo(config.Attributes);

			if (config.Attributes.Count > 0)
			{
				that = new ConnectionStringInfo(that);
				that.Define(config.Attributes);
			}

			return that;
		}

		private void Define(IEnumerable<KeyValuePair<string, string>> pairs)
		{
			if (pairs == null)
				return;

			bool integratedSecurity = false;
			foreach (var item in pairs)
			{
				if (String.IsNullOrWhiteSpace(item.Key))
					continue;
				string name = item.Key.Trim();
				string value = item.Value?.Trim() ?? "";
				string lookup = name.Replace(" ", "");
				if (_synonyms.TryGetValue(lookup, out string key))
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
					if (_properties == null)
						_properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					_properties[key.ToLowerInvariant()] = value;
				}
			}
			if (integratedSecurity)
				Password = null;
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

				{ "integrated",					"integratedsecurity" },
				{ "trusted",					"integratedsecurity" },
				{ "trustedConnection",			"integratedsecurity" },

				{ "poolSize",					"maxPoolSize" },

				{ "networkLibrary",				"net" },
				{ "network",					"net" },

				{ "mars",						"MultipleActiveResultSets" },
			};
		#endregion
	}
}


