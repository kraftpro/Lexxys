// Lexxys Infrastructural library.
// file: PartialScheme.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.RL
{
	class PartialScheme: ISchemeParser
	{
		public const string ConfigSection = "resource.location";

		private static IDictionary<string, RootLocation> _schemasDefinition;
		private static IList<string> _schemas;
		private static string _defaultScheme;

		static PartialScheme()
		{
			LoadConfiguration();
			Config.Changed += OnConfigurationChanged;
		}

		public IEnumerable<string> SupportedSchemas => _schemas;

		public CurlBuilder Parse(string value, Vote<string> scheme)
		{
			if (value == null || (value = value.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(value));
			if (!_schemasDefinition.TryGetValue(scheme.Value, out var root))
				throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);

			var x = new CurlBuilder(root.Value);
			var y = new CurlBuilder();
			y.SetPath(value, true, true);
			if (String.IsNullOrEmpty(x.Path))
				x.Path = y.Path;
			else if (!String.IsNullOrEmpty(y.Path))
				x.Path += y.Path.StartsWith("/") ? y.Path.Substring(1): y.Path;
			if (String.IsNullOrEmpty(x.FullPath))
				x.FullPath = y.FullPath;
			else if (!String.IsNullOrEmpty(y.FullPath))
				x.FullPath += y.FullPath.StartsWith("/") ? y.FullPath.Substring(1): y.FullPath;
			x.Query = y.Query;
			if (!String.IsNullOrEmpty(y.Fragment))
				x.Fragment = y.Fragment;

			return x;
		}

		public Vote<string> Analyze(string value)
		{
			if (value == null || _defaultScheme.Length == 0)
				return Vote<string>.Empty;
			value = value.Trim();
			if (value.Length == 0)
				return Vote.ProbablyYes(_defaultScheme);

			value = value.Replace('\\', '/');
			if (value.StartsWith("//"))
				return Vote.AlmostNo(_defaultScheme);

			var r = new CurlBuilder();
			r.SetPath(value, true, true);
			if (r.Path.IndexOfAny(__badChars) >= 0)
				return Vote.AlmostNo(_defaultScheme);

			if (r.Path.StartsWith("/"))
				return Vote.Maybe(_defaultScheme);
			if (r.Path.StartsWith("."))
				return Vote.AlmostYes(_defaultScheme);

			return Vote.ProbablyYes(_defaultScheme);
		}
		private static readonly char[] __badChars = { '@', ':' };

		private abstract class RootLocation
		{
			private readonly string _value;
			private Curl _parsedValue;

			protected RootLocation(string value) => _value = value;

			public Curl Value => _parsedValue ??= Curl.Create(_value) ?? Curl.Empty;
		}

		private static void OnConfigurationChanged(object sender, ConfigurationEventArgs args)
		{
			LoadConfiguration();
		}

		private static void LoadConfiguration()
		{
			List<KeyValuePair<string, RootLocation>> ss = Config.GetValue<List<KeyValuePair<string, RootLocation>>>(ConfigSection);
			if (ss == null || ss.Count == 0)
			{
				_schemasDefinition = ReadOnly.Empty<string, RootLocation>();
				_schemas = ReadOnly.Empty<string>();
				_defaultScheme = "";
			}
			else
			{
				var d = new Dictionary<string, RootLocation>(ss.Count);
				foreach (var item in ss)
				{
					d[item.Key] = item.Value;
				}
				_schemasDefinition = ReadOnly.Wrap(d);
				_schemas = ReadOnly.WrapCopy(ss.Select(o => o.Key));
				_defaultScheme = d.ContainsKey("default") ? "default": ss[0].Key;
			}
		}
	}
}
