// Lexxys Infrastructural library.
// file: SystemConfigurationProvider.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
#if !NETCOREAPP
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lexxys.Xml;


#nullable enable

namespace Lexxys.Configuration
{
	[DebuggerDisplay("[System.Configuration]")]
	public sealed class SystemConfigurationProvider: IConfigurationProvider
	{
		public string Name => "System.Configuration";

		public object? GetValue(string reference, Type returnType)
		{
			if (returnType == null)
				throw EX.ArgumentNull(nameof(returnType));
			if (reference == null)
				throw EX.ArgumentNull(nameof(reference));

			string value = ConfigurationManager.AppSettings[reference];
			if (value != null && XmlTools.TryGetValue(value, returnType, out object result))
				return result;
			if (reference.StartsWith("connection.", StringComparison.OrdinalIgnoreCase) && returnType == typeof(string))
				return ConfigurationManager.ConnectionStrings[reference.Substring(11)];
			result = ConfigurationManager.GetSection(reference);
			if (returnType.IsInstanceOfType(result))
				return result;
			return null;
		}

		public List<T> GetList<T>(string reference)
		{
			if (reference == null)
				throw EX.ArgumentNull(nameof(reference));

			string[] values = ConfigurationManager.AppSettings.GetValues(reference);
			var result = new List<T>();
			if (values != null)
			{
				foreach (string value in values)
				{
					if (XmlTools.TryGetValue(value, typeof(T), out object x))
					{
						result.Add((T)x);
					}
				}
				if (result.Count > 0)
					return result;
			}
			if (reference.StartsWith("connection.", StringComparison.OrdinalIgnoreCase))
			{
				var cc = ConfigurationManager.ConnectionStrings;
				if (cc.Count > 0)
				{
					if (typeof(T) == typeof(string))
					{
						for (int i = 0; i < cc.Count; ++i)
						{
							result.Add(Tools.Cast<T>(cc[i].ConnectionString));
						}
						return result;
					}
					if (typeof(T) == typeof(ConnectionStringSettings))
					{
						for (int i = 0; i < cc.Count; ++i)
						{
							result.Add(Tools.Cast<T>(cc[i]));
						}
						return result;
					}
					return new List<T>();
				}
			}
			return ConfigurationManager.GetSection(reference) as List<T> ?? new List<T>();
		}

		public event EventHandler<ConfigurationEventArgs>? Changed
		{
			add { }
			remove { }
		}
	}
}
#endif
