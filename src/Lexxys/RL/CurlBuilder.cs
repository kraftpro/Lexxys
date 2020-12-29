// Lexxys Infrastructural library.
// file: CurlBuilder.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;

namespace Lexxys.RL
{
	public class CurlBuilder
	{
		public string Scheme { get; set; }
		public string Host { get; set; }
		public int Port { get; set; }
		public string Path { get; set; }
		public string FullPath { get; set; }
		public string Query { get; set; }
		public string Fragment { get; set; }
		public bool IsAbsolute { get; set; }
		public string UserId { get; set; }
		public string Password { get; set; }
		public Dictionary<string, string> QueryParameters { get; private set; }

		public CurlBuilder()
		{
			Port = Curl.DefaultPort;
		}

		public CurlBuilder(Curl url)
		{
			Scheme = url.Scheme;
			Host = url.Host;
			Port = url.Port;
			Path = url.Path;
			FullPath = url.FullPath;
			QueryParameters = new Dictionary<string, string>(url.QueryParameters);
			Fragment = url.Fragment;
			IsAbsolute = url.IsAbsolute;
			UserId = url.UserId;
			Password = url.Password;
		}

		public void SetHost(string value, bool resetUser, bool resetPort)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (resetUser)
			{
				UserId = null;
				Password = null;
			}
			if (resetPort)
			{
				Port = Curl.DefaultPort;
			}
			value = value.Trim();
			int i = value.IndexOf('@');
			if (i >= 0)
			{
				string s = value.Substring(0, i).TrimEnd();
				value = value.Substring(i + 1).TrimStart();
				i = s.IndexOf(':');
				if (i >= 0)
				{
					UserId = s.Substring(0, i).TrimEnd();
					Password = s.Substring(i + 1).TrimStart();
					if (Password.Length == 0)
						Password = null;
				}
				else
				{
					UserId = s;
				}
				if (UserId.Length == 0)
					UserId = null;
			}
			i = value.IndexOf(':');
			if (i >= 0)
			{
				if (Int32.TryParse(value.Substring(i + 1), out int p) && p >= 0 && p <= Curl.MaxPort)
				{
					value = value.Substring(0, i).TrimEnd();
					Port = p;
				}
			}
			Host = value.Length > 0 ? value: null;
		}

		public void SetPath(string value, bool query, bool fragment)
		{
			if (value == null)
				return;
			value = value.Trim();
			if (value.Length == 0)
				return;
			if (fragment)
			{
				int k = value.LastIndexOf('#');
				if (k >= 0)
				{
					Fragment = value.Substring(k + 1).TrimStart();
					value = value.Substring(0, k).TrimEnd();
				}
			}
			if (query)
			{
				int k = value.IndexOf('?');
				if (k >= 0)
				{
					Query = value.Substring(k + 1).TrimStart();
					value = value.Substring(0, k).TrimEnd();
				}
			}
			List<string> segments = SplitPath(value);
			Path = String.Join("", segments);
		}

		private static List<string> SplitPath(string value)
		{
			if (value == null)
				return null;

			//if (!value.EndsWith("/"))
			//    value += "/";

			List<string> segments = new List<string>();
			int i0 = 0;
			int ik;
			while (i0 < value.Length && (ik = value.IndexOf('/', i0)) >= 0)
			{
				string s = value.Substring(i0, ik - i0 + 1);
				if (s.Length == 1)
				{
					if (segments.Count == 0)
						segments.Add(s);
				}
				else if (s == "../")
				{
					int n = segments.Count - 1;
					if (n > 0 && segments[n] != "../")
						segments.RemoveAt(segments.Count - 1);
					else
						segments.Add(s);
				}
				else if (s != "./")
				{
					segments.Add(s);
				}
				i0 = ik + 1;
			}
			if (i0 < value.Length)
			{
				string s = value.Substring(i0);
				if (s.Length >= 0 && s != ".")
				{
					if (s == "..")
					{
						int n = segments.Count - 1;
						if (n > 0 && segments[n] != "../")
							segments.RemoveAt(segments.Count - 1);
						else
							segments.Add("../");
					}
					else
					{
						segments.Add(s);
					}
				}
			}
			return segments;
		}
	}
}


