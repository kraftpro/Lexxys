// Lexxys Infrastructural library.
// file: HttpScheme.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.RL
{
	class HttpScheme: ISchemeParser
	{
		public const string SchemeName = "http";
		public const string SecuredSchemeName = "https";

		public IEnumerable<string> SupportedSchemas
		{
			get { return __schemas; }
		}
		private static readonly IList<string> __schemas = ReadOnly.Wrap(new[] { SchemeName, SecuredSchemeName });

		public CurlBuilder Parse(string value, Vote<string> scheme)
		{
			if (scheme.Value != SchemeName && scheme.Value != SecuredSchemeName)
				throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
			if (value == null || (value = value.Trim()).Length == 0)
				throw new ArgumentNullException(nameof(value));

			var r = new CurlBuilder { Scheme = scheme.Value };

			if (value.StartsWith("//", StringComparison.Ordinal))
			{
				if (scheme.Score != VoteScore.Yes)
					return null;
				value = value.Substring(2);
			}
			int i = value.IndexOf('/');
			if (i >= 0)
			{
				r.SetHost(value.Substring(0, i), true, true);
				r.SetPath(value.Substring(i), true, true);
			}
			else
			{
				r.SetHost(value, true, true);
				r.Path = "/";
			}
			return r;
		}

		public Vote<string> Analyze(string value)
		{
			if (value == null)
				return Vote<string>.Empty;
			value = value.Trim().ToLowerInvariant();
			if (value.Length < 2)
				return Vote<string>.Empty;

			var r = new CurlBuilder();
			int i = value.IndexOf('/');
			if (i >= 0)
			{
				r.SetHost(value.Substring(0, i), true, true);
				r.SetPath(value.Substring(i), true, true);
			}
			else
			{
				r.SetHost(value, true, true);
			}
			if (r.Host == null || r.UserId != null || r.Password != null)
				return Vote<string>.Empty;

			int ext = EndsWithAny(r.Path, __wwwExt) == null ? 0: 1;
			for (int j = 0; j < r.Host.Length; ++j)
			{
				char c = r.Host[j];
				if (!((c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == '-'))
					return Vote.ProbablyNot(HttpScheme.SchemeName);
			}

			string[] ss = r.Host.Split('.');
			string ip4 = Ip4(ss);
			if (ip4 != null)
				return ip4 == "127.0.0.1" ? Vote.AlmostYes(HttpScheme.SchemeName):
					Vote.ProbablyYes(HttpScheme.SchemeName, ext);

			bool www = ss[0].Trim().StartsWith("www");

			if (ss.Length > 1)
			{
				Vote<string> probablyNot = www ? Vote.Maybe(HttpScheme.SchemeName, ext): Vote.ProbablyNot(HttpScheme.SchemeName, ext);

				string s = ss[ss.Length - 1].Trim();
				if (s.Length < 2)
					return probablyNot;

				if (s.Length > 2)
				{
					if (Array.BinarySearch(__longName, s) < 0)
						return probablyNot;
				}
				else
				{
					if (s[0] < 'a' || s[0] > 'z')
						return probablyNot;
					if (__shortName[s[0] - 'a'].IndexOf(s[1], 1) < 0)
						return probablyNot;
				}
				return www ? Vote.AlmostYes(HttpScheme.SchemeName, ext): Vote.ProbablyYes(HttpScheme.SchemeName, ext);
			}

			return www  || ss[0] == "localhost" ? Vote.AlmostYes(HttpScheme.SchemeName, ext): Vote.ProbablyYes(HttpScheme.SchemeName, ext);
		}

		private static string EndsWithAny(string value, string[] ends)
		{
			if (value == null || value.Length == 0)
				return null;

			for (int i = 0; i < ends.Length; ++i)
			{
				if (value.EndsWith(ends[i]))
					return ends[i];
			}
			return null;
		}

		private static string Ip4(string[] value)
		{
			if (value.Length != 4)
				return null;

			var ip = new StringBuilder(20);
			for (int i = 0; i < value.Length; ++i)
			{
				if (value.Length > 3)
					return null;
				int k = GetInt(value[i]);
				if (k < 0 || k > 254)
					return null;
				if (i > 0)
					ip.Append('.');
				ip.Append(k);
			}
			return ip.ToString();
		}

		private static int GetInt(string value)
		{
			if (value.Length == 0)
				return -1;

			int x = 0;
			for (int i = 0; i < value.Length; ++i)
			{
				if (value[i] < '0' || value[i] > '9')
					return -1;
				x = x * 10 + (value[i] - '0');
			}
			return x;
		}

		#region Tables
		private static readonly string[] __wwwExt = { ".asp", ".aspx", ".htm", ".html", ".shtml", ".jsp", ".cgi", ".php", ".do", ".cfm" };

		private static readonly string[] __longName =
			{
				"aero", "asia", "biz", "cat", "com",
				"coop", "edu", "gov", "info", "int", "jobs", 
				"local", "localhost",
				"mil", "mobi", "museum", "name",
				"net", "org", "pro", "tel", "travel"
			};
		private static readonly string[] __shortName =
			{
				"acdefgilmnoqrstuwxz",
				"bbdefghijmnorstvwyz",
				"cacdfghiklmnoruvxyz",
				"dejkmoz",
				"ecegrstu",
				"fijkmor",
				"gabdefghilmnpqrstuwy",
				"hkmnrtu",
				"idelmnoqrst",
				"jemop",
				"keghimnrwyz",
				"labcikrstuvy",
				"macdghklmnopqrstuvwxyz",
				"nacefgilopruz",
				"om",
				"paefghklmnrstwy",
				"qa",
				"reouw",
				"sabcdeghijklmnortuvyz",
				"tcdfghjklmnoprtvwz",
				"uagkmsyz",
				"vaceginu",
				"wfs",
				"x",
				"yetu",
				"zamw",
			};

		#endregion
	}
}
